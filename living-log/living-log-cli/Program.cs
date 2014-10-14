﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace living_log_cli
{
    class Program
    {
        #region Console exit handler
        
        [System.Runtime.InteropServices.DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        private static HandlerRoutine exitHandler = null;
        public static bool SetExitHandler(HandlerRoutine handler)
        {
            exitHandler = handler;
            return SetConsoleCtrlHandler(exitHandler, true);
        }
       
        #endregion

        #region Command line arguments handling
       
        private static void Help()
        {
            Console.WriteLine(
@"
living-log-cli

Starts a keyboard and mouse activity logger

Command: living-log-cli [-log LOG -delay DELAY -sync DELAY]

Options: -log LOG     Uses the file LOG as log for the activity
                      Default is ""My Documents""\living-log.log
         -delay DELAY Delay in seconds between each dump to the log file
                      Default is each minute (60s)
         -sync DELAY  Delay in seconds between each sync message in the dump
                      Default is each hour (3600s)
"
            );
        }

        private static bool TryProcessingArguments(string[] args)
        {
            int index = 0;
            while (index < args.Length)
            {
                if (index + 2 <= args.Length)
                {
                    switch (args[index])
                    {
                        case "-log":
                            Constants.LogFilename = args[index + 1];
                            break;
                        case "-delay":
                            Constants.DumpDelayInMs = 1000 * Convert.ToInt32(args[index + 1]);
                            break;
                        case "-sync":
                            Constants.SyncDelayInMs = 1000 * Convert.ToInt32(args[index + 1]);
                            break;
                        default:
                            return false;
                    }
                    index += 2;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
       
        #endregion

        static void Main(string[] args)
        {
            if (!TryProcessingArguments(args))
            {
                Help();
                return;
            }

            Console.WriteLine("Using " + Constants.LogFilename + " as log file"); 

            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            Program p = new Program(Constants.LogFilename);
            SetExitHandler((t) =>
            {
                p.ForceExit();
                return true;
            });
            System.Windows.Forms.Application.Run();
        }

        Program(string filename)
        {
            m_activityList = new List<Activity>();

            m_previous = new Timestamp(DateTime.UtcNow);

            m_living = new LivingLogger(Constants.SyncDelayInMs);
            m_living.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };
            m_living.Enabled = true;

            m_mouse = new MouseLogger();
            m_mouse.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };
            m_mouse.Enabled = true;

            m_keyboard = new KeyboardLogger();
            m_keyboard.ActivityLogged += (s, a) => { lock (locker) { m_activityList.Add(a); } };
            m_keyboard.Enabled = true;

            m_dumpTimer = new Timer();
            m_dumpTimer.Interval = Constants.DumpDelayInMs;
            m_dumpTimer.Tick += (s, e) => { Dump(); };
            m_dumpTimer.Enabled = true;

            m_filename = filename;
        }

        private void ForceExit()
        {
            m_dumpTimer.Enabled = false;
            m_keyboard.Enabled = false;
            m_mouse.Enabled = false;
            m_living.Enabled = false;
            Dump();
        }

        private void Dump()
        {
            Console.WriteLine("Writing " + m_activityList.Count + " activities");
            lock (locker)
            {
                try
                {
                    if (WriteBinary(File.Open(m_filename, FileMode.Append)))
                    {
                        m_activityList.Clear();
                    }
                }
                catch (IOException e)
                {
                    // Most likely to be the output file already in use
                    // Just keep storing Activities until we can access the file
                }
            }
        }

        private Timestamp m_previous;
        private bool WriteText(Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                using (var text = new StringWriter())
                {
                    Timestamp previous = m_previous;

                    try
                    {
                        m_activityList.ForEach((a) =>
                        {
                            text.Write(a.Timestamp - previous);
                            text.Write(" ");
                            text.Write(a.Type.Id);
                            text.Write(" ");
                            text.Write(a.Info.ToString());
                            text.WriteLine();

                            previous = a.Timestamp;
                        });
                    }
                    catch (IOException e)
                    {
                        return false;
                    }

                    writer.Write(text.ToString());
                    m_previous = previous;
                    return true;
                }
            }
        }

        public static class Binary
        {
            public static long FloodLeft(long value)
            {
                var r = value;
                r |= (r << 1);
                r |= (r << 2);
                r |= (r << 4);
                r |= (r << 8);
                r |= (r << 16);
                r |= (r << 32);
                return r;
            }

            public static long FloodRight(long value)
            {
                var r = value;
                r |= (r >> 1);
                r |= (r >> 2);
                r |= (r >> 4);
                r |= (r >> 8);
                r |= (r >> 16);
                r |= (r >> 32);
                return r;
            }
        }
        
        public static class BinaryEncoding
        {
            public static byte[] Encode(long value)
            {
                long limit_1b = (1L << 6);
                long limit_2b = (1L << 13);
                long limit_3b = (1L << 28);
                long limit_4b = (1L << 59);

                if (-limit_1b <= value && value < limit_1b)
                {
                    return new byte[] { 
                        (byte)(value & 0x7F),
                    };
                }
                else if (-limit_2b <= value && value < limit_2b)
                {
                    return new byte[] {
                        (byte)(((value >> 8) & 0x3F) | 0x80),
                        (byte) ((value >> 0) & 0xFF),
                    };
                }
                else if (-limit_3b <= value && value < limit_3b)
                {
                    return new byte[] {
                        (byte)(((value >> 24) & 0x1F) | 0xC0),
                        (byte) ((value >> 16) & 0xFF),
                        (byte) ((value >>  8) & 0xFF),
                        (byte) ((value >>  0) & 0xFF),
                    };
                }
                else if (-limit_4b <= value && value < limit_4b)
                {
                    return new byte[] {
                        (byte)(((value >> 56) & 0x0F) | 0xE0),
                        (byte) ((value >> 48) & 0xFF),
                        (byte) ((value >> 40) & 0xFF),
                        (byte) ((value >> 32) & 0xFF),
                        (byte) ((value >> 24) & 0xFF),
                        (byte) ((value >> 16) & 0xFF),
                        (byte) ((value >>  8) & 0xFF),
                        (byte) ((value >>  0) & 0xFF),
                    };
                }
                else
                {
                    return new byte[] {
                        (byte) (0xFF),
                        (byte) ((value >> 56) & 0xFF),
                        (byte) ((value >> 48) & 0xFF),
                        (byte) ((value >> 40) & 0xFF),
                        (byte) ((value >> 32) & 0xFF),
                        (byte) ((value >> 24) & 0xFF),
                        (byte) ((value >> 16) & 0xFF),
                        (byte) ((value >>  8) & 0xFF),
                        (byte) ((value >>  0) & 0xFF),
                    };
                }
            }

            public static long Decode(BinaryReader reader)
            {

                byte head = reader.ReadByte();
                if ((head & 0x80) == 0)
                {
                    long sign = Binary.FloodLeft(head & 0x40);
                    return sign | (head & 0x7FL);
                }
                else if ((head & 0x40) == 0)
                {
                    long sign = Binary.FloodLeft(head & 0x20);
                    return ((sign | (head & 0x3FL)) << 8)
                        | ((long)reader.ReadByte());
                }
                else if ((head & 0x20) == 0)
                {
                    long sign = Binary.FloodLeft(head & 0x10);
                    return ((sign | (head & 0x1FL)) << 24)
                        | ((long)reader.ReadByte() << 16)
                        | ((long)reader.ReadByte() << 8)
                        | ((long)reader.ReadByte());
                }
                else if ((head & 0x10) == 0)
                {
                    long sign = Binary.FloodLeft(head & 0x08);
                    return ((sign | (head & 0x0FL)) << 56)
                        | ((long)reader.ReadByte() << 48)
                        | ((long)reader.ReadByte() << 40)
                        | ((long)reader.ReadByte() << 32)
                        | ((long)reader.ReadByte() << 24)
                        | ((long)reader.ReadByte() << 16)
                        | ((long)reader.ReadByte() << 8)
                        | ((long)reader.ReadByte());
                }
                else
                {
                    return ((long)reader.ReadByte() << 56)
                        | ((long)reader.ReadByte() << 48)
                        | ((long)reader.ReadByte() << 40)
                        | ((long)reader.ReadByte() << 32)
                        | ((long)reader.ReadByte() << 24)
                        | ((long)reader.ReadByte() << 16)
                        | ((long)reader.ReadByte() << 8)
                        | ((long)reader.ReadByte());
                }
            }
        }
        
        private bool WriteBinary(Stream output)
        {
            using (var writer = new BinaryWriter(output))
            {
                var data = new MemoryStream();
                using (var dataWriter = new BinaryWriter(data))
                {
                    Timestamp previous = m_previous;

                    try
                    {
                        m_activityList.ForEach((a) =>
                        {
                            dataWriter.Write(BinaryEncoding.Encode((a.Timestamp - previous).Milliseconds));
                            dataWriter.Write(BinaryEncoding.Encode(a.Type.Id));
                            dataWriter.Write(a.Info.ToString());

                            previous = a.Timestamp;
                        });
                    }
                    catch (IOException e)
                    {
                        return false;
                    }

                    writer.Write(data.ToArray());
                    m_previous = previous;
                    return true;
                }
            }
        }

        Timer m_dumpTimer;
        string m_filename;

        MouseLogger m_mouse;
        KeyboardLogger m_keyboard;
        LivingLogger m_living;

        List<Activity> m_activityList;

        private object locker = new object();
    }
}
