using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvonaTest
{
    class Program
    {
        static int Main(string[] args)
        {
            var commands = ManyConsole.ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));

            return ManyConsole.ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }
    }
}
