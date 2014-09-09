using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_gps_cli
{
    public delegate string Command(Workspace workspace);
    public static class Commands
    {
        public static Command Empty = (_) => string.Empty;
        public static Command Unknown = (_) => "Unknown command";
    }

    public interface ICommandMatcher
    {
        bool IsMatch(string commandName);
        Command GetCommand(string arguments);
    }
    class CommandMatcherSimple : ICommandMatcher
    {
        public string Name { get; private set; }
        private Command m_Command;

        public CommandMatcherSimple(string name, Command command) { Name = name; m_Command = command; }
        public bool IsMatch(string commandName) { return commandName.Equals(Name); }
        public Command GetCommand(string arguments) { return m_Command; }
    }

    class CommandGarmin : REPL, ICommandMatcher
    {
        public CommandGarmin(TextReader input, TextWriter output)
            : base(input, output)
        {
            Name = "garmin";

            AddCommand(new CommandMatcherSimple("help", (_) => "Garmin commands: help, version"));
            AddCommand(new CommandMatcherSimple("version", (_) => "Garmin utility v0.1"));
        }

        public bool IsMatch(string commandName) { return commandName.Equals(Name); }

        public Command GetCommand(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                return (workspace) =>
                {
                    Run(workspace);
                    return string.Empty;
                };
            }
            else
            {
                return Parse(arguments);
            }
        }
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
        #endregion

        public void Load(string filename) { m_configFile = new ConfigFile(filename); }
        public void Save(string filename) { m_configFile.Write(filename); }

        public string WorkspacePath
        {
            get { return GetProperty("workspace", "path", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); }
            set { SetProperty("workspace", "path", value); }
        }
    }

    public class Workspace
    {
        public Configuration Config { get; set; }
    }

    public class REPL
    {
        TextReader Input;
        TextWriter Output;
        bool IsRunning;
        List<ICommandMatcher> CommandList;

        public string Name;
        public REPL(TextReader input, TextWriter output)
        {
            Input = input;
            Output = output;

            CommandList = new List<ICommandMatcher>();
            CommandList.Add(new CommandMatcherSimple("exit", (_) =>
            {
                IsRunning = false;
                return "exit: " + Name;
            }));

            IsRunning = true;
        }
        
        public Command Parse(string commandLine)
        {
            if (!string.IsNullOrEmpty(commandLine))
            {
                string commandName = commandLine.Split(' ').First();
                string arguments = commandLine.Substring(commandName.Length).Trim();

                ICommandMatcher match = CommandList.Find(m => m.IsMatch(commandName));
                if (match != null)
                {
                    return match.GetCommand(arguments);
                }
                return Commands.Unknown;
            }
            return Commands.Empty;
        }

        public void AddCommand(ICommandMatcher command)
        {
            CommandList.Add(command);
        }

        public Command Prompt;
        public void Run(Workspace workspace)
        {
            while (IsRunning)
            {
                if (Prompt != null) Output.Write(Prompt(workspace));
                string commandLine = Input.ReadLine().Trim();
                Command eval = Parse(commandLine);
                Output.Write(eval(workspace) + Environment.NewLine);
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

            Configuration cfg = new Configuration();
            cfg.Load(configFilename);

            Console.WriteLine("Living-gps @ " + cfg.WorkspacePath);
            Program p = new Program(Console.In, Console.Out, cfg);
            p.Run();
        }

        private REPL m_repl;
        private Workspace m_workspace;
        Program(TextReader input, TextWriter output, Configuration cfg)
        {
            m_workspace = new Workspace() { Config = cfg };

            m_repl = new REPL(input, output) { Name = "gps", Prompt = (_) => "gps>" };
            m_repl.AddCommand(new CommandGarmin(input, output) { Prompt = (_) => "garmin>" });
        }
        void Run()
        {
            m_repl.Run(m_workspace);
        }
    }
}
