using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_gps_cli
{
    public delegate string Command();
    public static class Commands
    {
        public static Command Empty = () => string.Empty;
        public static Command Unknown = () => "Unknown command";

        public static Command Concatenate(Command first, Command second)
        {
            return () => first() + Environment.NewLine + second();
        }
    }

    interface ICommandMatcher
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

    class CommandGarmin : ICommandMatcher
    {
        public string Name { get; private set; }

        private List<ICommandMatcher> GarminCommands;
        public CommandGarmin()
        {
            Name = "garmin";

            GarminCommands = new List<ICommandMatcher>();
            GarminCommands.Add(new CommandMatcherSimple("help", () => "Garmin commands: help, version"));
            GarminCommands.Add(new CommandMatcherSimple("version", () => "Garmin utility v0.1"));
        }

        public bool IsMatch(string commandName) { return commandName.Equals("garmin"); }

        public Command GetCommand(string arguments)
        {
            if (!string.IsNullOrEmpty(arguments))
            {
                string newName = arguments.Split(' ').First();
                string newArgs = arguments.Substring(newName.Length).Trim();
                ICommandMatcher match = GarminCommands.Find(m => m.IsMatch(newName));
                if (match != null) return match.GetCommand(newArgs);
                return Commands.Concatenate(
                    Commands.Unknown,
                    GarminCommands.Find(m => m.IsMatch("help")).GetCommand(string.Empty)
                    );
            }
            return Commands.Empty;
        }
    }

    class Program
    {
        #region Command line arguments
        
        static void Help()
        {

        }

        static bool TryProcessingArguments(string[] args)
        {
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

            Console.WriteLine("Living-gps");

            Program p = new Program(Console.In, Console.Out);
            p.REPL();
        }

        private TextReader Input;
        private TextWriter Output;
        private bool IsRunning;
        private List<ICommandMatcher> CommandList;

        class CommandExit : ICommandMatcher
        {
            private Program Repl;
            public CommandExit(Program repl) { Repl = repl; }
            public bool IsMatch(string commandName) { return commandName.Equals("exit"); }
            public Command GetCommand(string arguments) { return () => { Repl.IsRunning = false; return "Exiting..."; }; }
        }

        Program(TextReader input, TextWriter output)
        {
            IsRunning = true;
            Input = input;
            Output = output;

            CommandList = new List<ICommandMatcher>();
            CommandList.Add(new CommandExit(this));
            CommandList.Add(new CommandGarmin());
        }

        Command Read()
        {
            string commandLine = Input.ReadLine().Trim();
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
        
        void REPL()
        {
            while (IsRunning)
            {
                Command eval = Read();
                Output.Write(eval() + Environment.NewLine);
            }
        }
    }
}
