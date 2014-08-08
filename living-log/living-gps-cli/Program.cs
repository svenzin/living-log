using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_gps_cli
{
    interface ICommandMatcher
    {
        bool IsMatch(string command);
        Func<string> GetCommand(string command);
    }
    class CommandEmpty : ICommandMatcher
    {
        public static Func<string> EmptyCommand = () => { return string.Empty; };
        public bool IsMatch(string command) { return command.Equals(string.Empty); }
        public Func<string> GetCommand(string command) { return EmptyCommand; }
    }
    class CommandGarmin : ICommandMatcher
    {
        class CommandHelp : ICommandMatcher
        {
            public bool IsMatch(string command) { return command.Equals("help"); }
            public Func<string> GetCommand(string command) { return () => { return "Garmin commands : version" + Environment.NewLine; }; }
        }
        class CommandVersion : ICommandMatcher
        {
            public bool IsMatch(string command) { return command.Equals("version"); }
            public Func<string> GetCommand(string command) { return () => { return "Version 0.1" + Environment.NewLine; }; }
        }

        private List<ICommandMatcher> CommandList;
        public CommandGarmin()
        {
            CommandList = new List<ICommandMatcher>();
            CommandList.Add(new CommandHelp());
            CommandList.Add(new CommandVersion());
        }
        public bool IsMatch(string command)
        {
            return command.StartsWith("garmin ");
        }

        public Func<string> GetCommand(string command)
        {
            string garminCommand = command.Substring("garmin ".Length);
            ICommandMatcher match = CommandList.Find((matcher) => { return matcher.IsMatch(garminCommand); });
            if (match != null)
            {
                return match.GetCommand(garminCommand);
            }
            else
            {
                return CommandEmpty.EmptyCommand;
            }
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

        class CommandExit : ICommandMatcher
        {
            private Program Repl;
            public CommandExit(Program repl) { Repl = repl; }
            public bool IsMatch(string command)
            {
                return command.Equals("exit");
            }

            public Func<string> GetCommand(string command)
            {
                return () => { Repl.IsRunning = false; return "Exiting..." + Environment.NewLine; };
            }
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

        private List<ICommandMatcher> CommandList;
        Func<string> Read()
        {
            string line = Input.ReadLine().Trim();
            ICommandMatcher command = CommandList.Find((item) => { return item.IsMatch(line); });
            if (command != null)
            {
                return command.GetCommand(line);
            }
            else
            {
                return CommandEmpty.EmptyCommand;
            }
        }
        
        void REPL()
        {
            while (IsRunning)
            {
                Func<string> evaluate = Read();
                Output.Write(evaluate());
            }
        }
    }
}
