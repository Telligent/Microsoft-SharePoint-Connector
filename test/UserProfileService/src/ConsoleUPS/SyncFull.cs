using ConsoleUPS.MyProfileUPSService;
using ConsoleUPS.Util;
using System;
using System.Collections.Generic;

namespace ConsoleUPS
{
    public class SyncFull : SyncBase
    {
        private readonly UserProfileService _ups;

        private SyncFull(UserProfileService ups)
        {
            _ups = ups;
        }

        public static SyncFull Instance(UserProfileService ups)
        {
            return new SyncFull(ups);
        }

        public void Sync(SyncOptions options)
        {
            var totalIndexedUsers = GetAllUsers(options.UserLimit, options.UserAccountFilter);
            
            Console.WriteLine(@"""fullSync"":{""getUserProfileByIndex"":" + totalIndexedUsers.Count + ",");
            Console.WriteLine(string.Concat(@"""users"" : [", string.Join(",\n", totalIndexedUsers), "]"));
            
            SyncUtil.JsonClose(",");
        }

        private List<string> GetAllUsers(int userLimit, string userAccountFilter = null)
        {
            var users = new List<string>();
            var nextIndex = -1;
            
            try
            {
                GetUserProfileByIndexResult userInstance;

                do
                {
                    userInstance = _ups.GetUserProfileByIndex(nextIndex);
                    if (userInstance == null || userInstance.UserProfile == null) continue;

                    try
                    {
                        if (userLimit > 0 && users.Count >= userLimit) return users;
                        if (IsValidProfile(userInstance.UserProfile, userAccountFilter))
                        {
                            users.Add(FieldsToJson(userInstance.UserProfile));
                            if (users.Count % 100 == 0) SyncUtil.WriteLine("Saving {0] users.", users.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        var fieldCount = userInstance.UserProfile.Length;
                        SyncUtil.WriteLine("Error : {0} FieldCount: {1}", ex.Message, fieldCount);
                    }

                    var nextValue = userInstance.NextValue ?? string.Empty;

                    if (!int.TryParse(nextValue.Replace(",", ""), out nextIndex))
                    {
                        SyncUtil.WriteLine("Error with next index : {0}", nextValue);
                    }
                }
                while (userInstance != null && userInstance.UserProfile != null);
            }
            catch (Exception ex)
            {
                SyncUtil.WriteLine("FullSyncGetUserProfileByIndexFailed : {0} {1}", ex.Message, ex.StackTrace);
            }

            return users;
        }
    }
}
