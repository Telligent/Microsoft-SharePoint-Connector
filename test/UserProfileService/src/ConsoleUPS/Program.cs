using System;
using ConsoleUPS.Util;

namespace ConsoleUPS
{
    class Program
    {
        static void Main(string[] args)
        {
            var waitForUser = true;
            var userLimit = 0;
            var userAccount = string.Empty;
            var ignoreChangeToken = false;
            
            if (args != null && args.Length > 0)
            {
                bool.TryParse(args[0], out waitForUser);

                if (args.Length > 1) int.TryParse(args[1], out userLimit);

                if (args.Length > 2) userAccount = args[2];

                if (args.Length > 3) bool.TryParse(args[3], out  ignoreChangeToken);
            }

            SyncUtil.JsonOpen();

            try
            {
                var userProfileSync = new ProfileSync();
                userProfileSync.Sync(new SyncOptions
                {
                    UserLimit = userLimit,
                    UserAccountFilter = userAccount,
                    IgnoreChangeToken = ignoreChangeToken
                });
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("/*");
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error occurred while running ProfileSync.\n");

                Console.ResetColor();
                Console.WriteLine(ex.ToString());

                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("*/");
                Console.ResetColor();
            }

            SyncUtil.JsonClose();

            if (!waitForUser) return;

            Console.WriteLine("\nPress Enter to exit");
            Console.ReadLine();
        }
    }
}
