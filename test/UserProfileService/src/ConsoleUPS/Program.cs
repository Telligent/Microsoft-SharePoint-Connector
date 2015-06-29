using System.Linq;
using ConsoleUPS.MyProfileUPSService;
using ConsoleUPS.Properties;
using System;
using System.Collections.Generic;
using System.Net;

namespace ConsoleUPS
{
    class Program
    {
        static void Main(string[] args)
        {
            var nc = new NetworkCredential
            {
                Domain = Settings.Default.Domain,
                UserName = Settings.Default.UserName,
                Password = Settings.Default.Password
            };

            try
            {
                if (Settings.Default.ConsoleUPS_MyProfileUPSService_UserProfileService.StartsWith("http://sharepoint2010.dev"))
                {
                    Console.WriteLine("Verify ConsoleUPS.exe.config settings before running.");
                    End();
                    return;
                }
                
                var ups = new UserProfileService
                {
                    PreAuthenticate = false, 
                    Credentials = nc
                };

                var total = ups.GetUserProfileCount();
                Console.WriteLine("UPS.GetUserProfileCount() Total:" + total);

                var totalFound = GetAllUsers(ups);
                Console.WriteLine("UPS.GetUserProfileByIndex() Found:" + totalFound);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("status 401"))
                {
                    Console.WriteLine("Login failed please check ConsoleUPS.exe.config");
                }
                else
                {
                    Console.WriteLine(ex.Message + " - Stack: " + ex.StackTrace);
                }
            }

            End();
        }

        static long GetAllUsers(UserProfileService ups)
        {
            var users = new List<string>();
            var nextIndex = -1;
            try
            {
                GetUserProfileByIndexResult userInstance;
                
                do
                {
                    userInstance = ups.GetUserProfileByIndex(nextIndex);
                    if (userInstance == null || userInstance.UserProfile == null) continue;

                    try
                    {
                        users.Add(userInstance.UserProfile[1].Values[0].Value.ToString());
                    }
                    catch (Exception ex)
                    {
                        var fieldCount = userInstance.UserProfile.Length;
                        var values = string.Join(" | \n", 
                            userInstance.UserProfile.Select(p => 
                                string.Format("{0}:{1}", p.Name, 
                                string.Join(", ", p.Values.Select(v=>v.Value.ToString())))));
                        Console.WriteLine("Error : {0} FieldCount: {1} Fields: {2}", ex.Message, fieldCount, values);
                    }

                    nextIndex = Convert.ToInt32(userInstance.NextValue);
                }
                while (userInstance != null && userInstance.UserProfile != null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FarmUserProfileService.List() Failed: {0} {1}", ex.Message, ex.StackTrace);
            }

            return users.Count;
        }

        static void End()
        {
            Console.WriteLine("\nPress Enter to exit");
            Console.ReadLine();
        }
    }
}
