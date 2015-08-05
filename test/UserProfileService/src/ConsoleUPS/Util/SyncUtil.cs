using System;

namespace ConsoleUPS.Util
{
    public class SyncUtil
    {
        public static void JsonOpen()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("{");
            Console.ResetColor();
        }

        public static void JsonClose(string suffix = null)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("}" + (suffix ?? string.Empty));
            Console.ResetColor();
        }

        public static void WriteLine(string line)
        {
            Console.WriteLine("//" + line);
        }

        public static void WriteLine(string line, params object[] args)
        {
            Console.WriteLine("//" + string.Format(line, args));
        }
    }
}
