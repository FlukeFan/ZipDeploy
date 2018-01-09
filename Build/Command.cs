using System;
using System.Collections.Generic;
using System.Linq;

namespace Build
{
    public abstract class Command
    {
        public static Action<Action> TryConsole = (Action action) =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                UsingConsoleColor(ConsoleColor.Red, () => Console.Error.WriteLine(e.Message));
                Console.Error.WriteLine(e);
                Environment.Exit(1);
            }
        };

        public static void UsingConsoleColor(ConsoleColor color, Action action)
        {
            var previousColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = color;
                action();
            }
            finally
            {
                Console.ForegroundColor = previousColor;
            }
        }

        public static void Execute(string[] args)
        {
            var argStack = new Stack<string>(new Stack<string>(args));

            if (argStack.Count == 0)
                throw new Exception("please supply the command as the first argument");

            var commandName = argStack.Pop();

            var commandType = typeof(Command).Assembly.GetTypes()
                .Where(t => t.Name == commandName)
                .SingleOrDefault();

            if (commandType == null)
                throw new Exception("Could not load type: " + commandName);

            var command = (Command)Activator.CreateInstance(commandType);
            command.Execute(argStack);
        }

        public abstract void Execute(Stack<string> args);

        protected void Retry(int numberOfTimes, Action method)
        {
            int timesLeft = numberOfTimes;
            while (timesLeft > 0)
            {
                try
                {
                    method();
                    timesLeft = 0;
                }
                catch (Exception e)
                {
                    timesLeft--;
                    Console.WriteLine(string.Format("Failed ({0}) - {1} times left to retry", e.Message, timesLeft));

                    if (timesLeft <= 0)
                        throw;
                }
            }
        }
    }
}
