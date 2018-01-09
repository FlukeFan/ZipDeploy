namespace Build
{
    class Program
    {
        static void Main(string[] args)
        {
            Command.TryConsole(() =>
            {
                Command.Execute(args);
            });
        }
    }
}
