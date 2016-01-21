using ConsoleUPS.MyProfileUPSService;
using ConsoleUPS.Properties;
using ConsoleUPS.UserProfileChangeService;
using ConsoleUPS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using ChangeService = ConsoleUPS.UserProfileChangeService.UserProfileChangeService;

namespace ConsoleUPS
{
    public class SyncIncremental : SyncBase
    {
        private readonly ChangeService _changeService;
        private readonly UserProfileService _ups;
        private string _changeToken;

        private SyncIncremental(UserProfileService ups)
        {
            _ups = ups;
            _changeToken = string.Empty;
            
            _changeService = new ChangeService
            {
                UseDefaultCredentials = false,
                Credentials = ups.Credentials
            };
        }

        public static SyncIncremental Instance(UserProfileService ups)
        {
            return new SyncIncremental(ups);
        }

        public void Sync(SyncOptions options)
        {
            var totalIndexedUsers = GetChanges(options);

            Console.WriteLine(@"""incrementalSync"":{""userProfileChangeServiceGetChanges"":" + totalIndexedUsers.Count + ",");
            Console.WriteLine(@"""changeToken"":""" + _changeToken + @""",");
            Console.WriteLine(string.Concat(@"""users"" : [", string.Join(",\n", totalIndexedUsers), "]"));
            
            SyncUtil.JsonClose();
        }

        private List<string> GetChanges(SyncOptions options)
        {
            var users = new List<string>();
            
            try
            {
                _changeToken = options.IgnoreChangeToken ? string.Empty : Settings.Default.ChangeToken;

                var changeTokenStart = new UserProfileChangeToken();
                var profileChanges = _changeService.GetChanges(_changeToken, new UserProfileChangeQuery
                {
                    ChangeTokenStart = changeTokenStart,
                    Add = true,
                    Update = true,
                    UserProfile = true,
                    SingleValueProperty = true,
                    MultiValueProperty = true,
                });

                Settings.Default.ChangeToken = profileChanges.ChangeToken;
                Settings.Default.Save();

                var accountNameChanges = profileChanges.Changes.GroupBy(d => d.UserAccountName).Select(gr => gr.Key).ToList();

                foreach (var accountChanged in accountNameChanges)
                {
                    try
                    {
                        var userProfile = _ups.GetUserProfileByName(accountChanged);
                        if (options.UserLimit > 0 && users.Count >= options.UserLimit) return users;
                        if (IsValidProfile(userProfile, options.UserAccountFilter))
                        {
                            users.Add(FieldsToJson(userProfile));
                        }
                    }
                    catch (Exception ex)
                    {
                        SyncUtil.WriteLine("Error : {0} AccountChanged: {1}", ex.Message, accountChanged);
                    }
                }

            }
            catch (Exception ex)
            {
                SyncUtil.WriteLine("IncrementalSyncGetChangesFailed : {0} {1}", ex.Message, ex.StackTrace);
                Settings.Default.ChangeToken = string.Empty;
                Settings.Default.Save();
            }

            return users;
        }
    }
}
