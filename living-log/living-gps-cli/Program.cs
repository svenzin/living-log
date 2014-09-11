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
        List<Device> Devices;
        Device CurrentDevice;

        class Device
        {
            public DriveInfo Drive;
            public string Name;
            public string Id;
            public DirectoryInfo ActivityDir;
            public string ActivityFilter;
            public Device(DriveInfo drive)
            {
                Drive = drive;

                try
                {
                    XDocument doc = XDocument.Load(drive.RootDirectory.FullName + "/Garmin/GarminDevice.xml");
                    var ns = doc.Root.Name.Namespace;
                    Name = doc.Root.Element(ns + "Model").Element(ns + "Description").Value;
                    Id = doc.Root.Element(ns + "Id").Value;
                    var activity = doc.Root
                        .Descendants(ns + "DataType")
                        .Where(e => e.Element(ns + "Name").Value == "FIT_TYPE_4")
                        .Descendants(ns + "File")
                        .Where(e => e.Element(ns + "TransferDirection").Value == "OutputFromUnit")
                        .First().Element(ns + "Location");
                    ActivityDir = new DirectoryInfo(Drive.RootDirectory + activity.Element(ns + "Path").Value);
                    ActivityFilter = "*." + activity.Element(ns + "FileExtension").Value;
                }
                catch (Exception e) {}
            }
        }

        public CommandGarmin(TextReader input, TextWriter output)
            : base(input, output)
        {
            Name = "garmin";

            AddCommand("help", (a, w) => "Garmin commands: help, version");
            AddCommand("version", (a, w) => "Garmin utility v0.1");
            AddCommand("detect", (a, w) =>
            {
                DetectDevices();

                StringBuilder result = new StringBuilder();
                result.AppendLine("Garmin devices detected:");
                for (int i = 0; i < Devices.Count; ++i)
                {
                    result.AppendLine("    [" + i + "] " + Devices[i].Name + " - " + Devices[i].Id + " (" + Devices[i].Drive.Name + ")");
                }
                return result.ToString();
            });
            AddCommand("use", (a, w) =>
            {
                int deviceIndex;
                if (string.IsNullOrEmpty(a)) a = "0";
                if (int.TryParse(a.Split(' ').First(), out deviceIndex))
                {
                    if (Devices == null) DetectDevices();
                    if (0 <= deviceIndex && deviceIndex < Devices.Count)
                    {
                        CurrentDevice = Devices[deviceIndex];
                        StringBuilder result = new StringBuilder();
                        result.AppendLine("Using " + CurrentDevice.Name + " - " + CurrentDevice.Id + " (" + CurrentDevice.Drive.Name + ")");
                        result.AppendLine("    " + CurrentDevice.ActivityDir + "/" + CurrentDevice.ActivityFilter);
                        return result.ToString();
                    }
                }
                return Commands.Error;
            });
            AddCommand("list", (a, w) =>
            {
                StringBuilder result = new StringBuilder();
                result.AppendLine(CurrentDevice.Name + " - " + CurrentDevice.Id);
                result.AppendLine(CurrentDevice.ActivityDir.FullName);
                foreach (var f in CurrentDevice.ActivityDir.EnumerateFiles(CurrentDevice.ActivityFilter)) result.AppendLine("    " + f.Name);
                return result.ToString();
            });
        }

        void DetectDevices()
        {
            Devices = DriveInfo.GetDrives()
                .Where((drive) => File.Exists(drive.RootDirectory.FullName + "/Garmin/GarminDevice.xml"))
                .Select(d => new Device(d))
                .ToList();
        }
    }

    public class Property
    {
        public string Section;
        public string Name;
        public string DefaultValue;
    }
    public class Configuration
    {
        #region Property management
        private ConfigFile m_configFile;
        private string GetProperty(string section, string name, string defaultValue)
        {
            if (m_configFile != null && m_configFile.Exists(section, name))
            {
                return m_configFile.Get(section, name);
            }
            else
            {
                return defaultValue;
            }
        }
        private void SetProperty(string section, string name, string value)
        {
            if (m_configFile == null)
            {
                m_configFile = new living_gps_cli.ConfigFile();
            }
            m_configFile.Set(section, name, value);
        }
        private void ResetProperty(string section, string name)
        {
            if (m_configFile != null)
            {
                m_configFile.Reset(section, name);
            }
        }
        #endregion

        public string Get(Property p) { return GetProperty(p.Section, p.Name, p.DefaultValue); }
        public void Set(Property p, string value) { SetProperty(p.Section, p.Name, value); }
        public void Reset(Property p) { ResetProperty(p.Section, p.Name); }

        public void Load(string filename) { m_configFile = new ConfigFile(filename); }
        public void Save(string filename) { m_configFile.Write(filename); }
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
