using System;
using System.Collections.Generic;
using System.Threading;

namespace Build
{
    public class AcquireLock : Command
    {
        public override void Execute(Stack<string> args)
        {
            if (args.Count != 1)
                throw new Exception($"usage: bin\\Build.exe AcquireLock AcquireLock <lock name>");

            var lockName = args.Pop();

            UsingConsoleColor(ConsoleColor.Gray, () => Console.WriteLine($"acquiring on lock = '{lockName}'"));

            bool createdNew = false;
            Semaphore semaphore = null;

            while (!createdNew)
            {
                semaphore = new Semaphore(1, 1, lockName, out createdNew);

                if (!createdNew)
                    using (semaphore)
                        Thread.Sleep(200);
            }

            UsingConsoleColor(ConsoleColor.Gray, () => Console.WriteLine($"semaphore created createdNew={createdNew}"));
            UsingConsoleColor(ConsoleColor.Green, () => Console.WriteLine($"acquired lock = '{lockName}' - press enter to release"));
            Console.ReadLine();

            UsingConsoleColor(ConsoleColor.Gray, () => Console.WriteLine($"process exiting"));
        }
    }
}
