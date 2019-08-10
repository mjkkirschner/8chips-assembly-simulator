using System;

namespace core
{
    public class Logger
    {
        public bool enabled { get; set; }

        public void log(string message)
        {
            if (!enabled)
            {
                return;
            }
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }

    }
}
