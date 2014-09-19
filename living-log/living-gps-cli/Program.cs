using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace living_gps_cli
{
    public delegate string Command(string arguments, Workspace workspace);
    public static class Commands
    {
        public static string Empty = string.Empty;
        public static string Unknown = "Unknown command";
        public static string Error = "Error";
    }

    class CommandGarmin : REPL
    {
        Garmin.Device Device;

        public CommandGarmin(TextReader input, TextWriter output)
            : base(input, output)
        {
            Name = "garmin";

            AddCommand("help", (a, w) => "Garmin commands: help, version");
            AddCommand("version", (a, w) => "Garmin utility v0.1");
            AddCommand("detect", (a, w) =>
            {
                List<Garmin.Device> devices = Garmin.DetectDevices();

                StringBuilder result = new StringBuilder();
                result.AppendLine("Garmin devices detected:");
                for (int i = 0; i < devices.Count; ++i)
                {
                    result.AppendLine("    [" + i + "] " + devices[i].Name + " - " + devices[i].Id + " (" + devices[i].Drive.Name + ")");
                }
                return result.ToString();
            });
            AddCommand("open", (a, w) =>
            {
                int deviceIndex;
                if (int.TryParse(a.Split(' ').First(), out deviceIndex))
                {
                    Device = Garmin.Open(deviceIndex);
                    if (Device != null)
                    {
                        StringBuilder result = new StringBuilder();
                        result.AppendLine("Using " + Device.Name + " - " + Device.Id + " (" + Device.Drive.Name + ")");
                        result.AppendLine("    " + Device.ActivityDir + "/" + Device.ActivityFilter);
                        return result.ToString();
                    }
                }
                return Commands.Error;
            });
            AddCommand("list", (a, w) =>
            {
                StringBuilder result = new StringBuilder();
                result.AppendLine(Device.Name + " - " + Device.Id);
                result.AppendLine(Device.ActivityDir.FullName);
                var files = Garmin.List(Device).ToList();
                for (int i = 0; i < files.Count; ++i)
                {
                    result.AppendLine("    [" + i + "] " + files[i].Name);
                }
                return result.ToString();
            });
            AddCommand("load", (a, w) =>
            {
                int index;
                if (int.TryParse(a.Split(' ').First(), out index))
                {
                    var file = Garmin.List(Device).ElementAtOrDefault(index);
                    if (file != null && file.Exists)
                    {
                        var fitFile = file.OpenRead();
                        var reader = new Dynastream.Fit.Decode();
                        if (reader.IsFIT(fitFile) && reader.CheckIntegrity(fitFile))
                        {
                            var trigger = new Dynastream.Fit.MesgBroadcaster();
                            reader.MesgEvent += trigger.OnMesg;
                            reader.MesgDefinitionEvent += trigger.OnMesgDefinition;

                            trigger.MesgEvent += (s, e) =>
                            {
                                Console.WriteLine("OnMesg: Received Mesg with global ID#{0}, its name is {1}", e.mesg.Num, e.mesg.Name);
                                //for (byte i = 0; i < e.mesg.GetNumFields(); i++)
                                //    for (int j = 0; j < e.mesg.fields[i].GetNumValues(); j++)
                                //        Console.WriteLine("\tField{0} Index{1} (\"{2}\" Field#{4}) Value: {3} (raw value {5})", i, j, e.mesg.fields[i].GetName(), e.mesg.fields[i].GetValue(j), e.mesg.fields[i].Num, e.mesg.fields[i].GetRawValue(j));
                            };
                            trigger.MesgDefinitionEvent += (s, e) =>
                            {
                                Console.WriteLine("OnMesgDef: Received Defn for local message #{0}, global num {1}", e.mesgDef.LocalMesgNum, e.mesgDef.GlobalMesgNum);
                                Console.WriteLine("\tIt has {0} fields and is {1} bytes long", e.mesgDef.NumFields, e.mesgDef.GetMesgSize());
                            };

                            reader.Read(fitFile);
                            fitFile.Close();
                        }
                        StringBuilder result = new StringBuilder();
                        return result.ToString();
                    }
                }
                return Commands.Error;
            });
        }

    }

    public class Workspace
    {
        public static Property Path = new Property() { Section = "workspace", Name = "path", DefaultValue = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };

        public Configuration Config { get; set; }
    }

    public class REPL
    {
        TextReader Input;
        TextWriter Output;
        bool IsRunning;

        Dictionary<string, Command> CommandList;

        public string Name;
        public REPL(TextReader input, TextWriter output)
        {
            Input = input;
            Output = output;

            CommandList = new Dictionary<string, Command>();
            AddCommand("exit", (a, w) =>
            {
                IsRunning = false;
                return "exit: " + Name;
            });

            IsRunning = false;
        }

        private string Parse(string commandLine, Workspace workspace)
        {
            if (!string.IsNullOrEmpty(commandLine))
            {
                string commandName = commandLine.Split(' ').First();
                string arguments = commandLine.Substring(commandName.Length).Trim();

                Command match;
                if (CommandList.TryGetValue(commandName, out match))
                {
                    return match(arguments, workspace);
                }
                return Commands.Unknown;
            }
            return Commands.Empty;
        }

        public void AddCommand(string name, Command command)
        {
            CommandList.Add(name, command);
        }

        public void AddREPL(REPL repl)
        {
            AddCommand(repl.Name, repl.GetCommand);
        }

        private string GetCommand(string arguments, Workspace workspace)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                Run(workspace);
                return Commands.Empty;
            }
            else
            {
                return Parse(arguments, workspace);
            }
        }

        public void Run(Workspace workspace)
        {
            IsRunning = true;
            while (IsRunning)
            {
                Output.Write(Name + ">");
                string commandLine = Input.ReadLine().Trim();
                string result = Parse(commandLine, workspace);
                Output.Write(result + Environment.NewLine);
            }
        }
    }

    class Program
    {
        #region Command line arguments

        static void Help()
        {
            Console.WriteLine(
@"
living-log-gps

Utilities for GPS handling

Command: living-log-gps [-log LOG]

Options: -log LOG     Uses the file LOG as log for the activity
                      Default is ""My Documents""\living-log.config
"
            );
        }

        static bool TryProcessingArguments(string[] args)
        {
            int index = 0;
            while (index < args.Length)
            {
                if (index + 2 <= args.Length)
                {
                    switch (args[index])
                    {
                        case "-log":
                            configFilename = args[index + 1];
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

        static string configFilename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\living-log.config";
        static void Main(string[] args)
        {
            if (!TryProcessingArguments(args))
            {
                Help();
                return;
            }

            Configuration config = new Configuration();
            config.Load(configFilename);

            Console.WriteLine("Living-gps @ " + config.Get(Workspace.Path));
            Program p = new Program(Console.In, Console.Out, config);
            p.Run();
        }

        private REPL m_repl;
        private Workspace m_workspace;
        Program(TextReader input, TextWriter output, Configuration cfg)
        {
            m_workspace = new Workspace() { Config = cfg };

            m_repl = new REPL(input, output) { Name = "gps" };
            m_repl.AddREPL(new CommandGarmin(input, output));
        }
        void Run()
        {
            m_repl.Run(m_workspace);
        }
    }
}
