using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_gps_cli
{
    
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
        
        Program(TextReader input, TextWriter output)
        {
            IsRunning = true;
            Input = input;
            Output = output;
        }

        Func<string> Read()
        {
            string command = Input.ReadLine();
            if (command.Trim().ToLower().Equals("exit"))
            {
                return () =>
                {
                    IsRunning = false;
                    return "Exiting..." + System.Environment.NewLine;
                };
            }
            return () => { return string.Empty; };
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
