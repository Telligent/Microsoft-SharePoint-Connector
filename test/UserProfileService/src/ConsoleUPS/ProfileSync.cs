using ConsoleUPS.MyProfileUPSService;
using ConsoleUPS.Properties;
using ConsoleUPS.Util;
using System;
using System.Net;

namespace ConsoleUPS
{
    public class SyncOptions
    {
        public int UserLimit { get; set; }
        public string UserAccountFilter { get; set; }
        public bool IgnoreChangeToken { get; set; }

        public SyncOptions()
        {
            UserLimit = 0;
            UserAccountFilter = string.Empty;
            IgnoreChangeToken = false;
        }
    }

    public class ProfileSync
    {
        private const string DefaultSite = "http://test.sharepoint2010.dev";

        private readonly string _domain;
        private readonly string _username;
        private readonly string _password;

        public ProfileSync() : this(Settings.Default.Domain, Settings.Default.UserName, Settings.Default.Password) { }

        public ProfileSync(string domain, string username, string password)
        {
            _domain = domain != "domain" ? domain : string.Empty;
            _username = domain != "username" ? username : string.Empty;
            _password = domain != "password" ? password : string.Empty;
        }

        public void Sync(SyncOptions options)
        {
            NetworkCredential nc = null;

            if (!string.IsNullOrEmpty(_domain) && !string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                nc = new NetworkCredential {Domain = _domain, UserName = _username, Password = _password};
                SyncUtil.WriteLine("Using Credentials for {0}\\{1}.", _domain, _username);
            }
            else
            {
                SyncUtil.WriteLine("Using Default Credentials.");
            }

            try
            {
                if (Settings.Default.ConsoleUPS_MyProfileUPSService_UserProfileService.StartsWith(DefaultSite))
                {
                    SyncUtil.WriteLine("Verify ConsoleUPS.exe.config settings before running.");
                    return;
                }

                using(var ups = new UserProfileService { PreAuthenticate = false })
                {
                    if (nc != null) { ups.Credentials = nc; }
                    else { ups.UseDefaultCredentials = true; }

                    var total = ups.GetUserProfileCount();
                    Console.WriteLine(@"""getUserProfileCount"":" + total + ",");

                    SyncFull.Instance(ups).Sync(options);
                    SyncIncremental.Instance(ups).Sync(options);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("status 401"))
                {
                    SyncUtil.WriteLine("Login failed please check ConsoleUPS.exe.config");
                }

                throw;
            }
        }
    }
}
