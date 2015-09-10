using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model;
using Telligent.Evolution.Extensions.SharePoint.WebServices.UserProfileChangeService;
using Telligent.Evolution.Extensions.SharePoint.WebServices.UserProfileService;
using SP = Microsoft.SharePoint.Client;
using User = Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities.User;
using UserProfileChangeService = Telligent.Evolution.Extensions.SharePoint.WebServices.UserProfileChangeService.UserProfileChangeService;
using UserProfileService = Telligent.Evolution.Extensions.SharePoint.WebServices.UserProfileService.UserProfileService;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi
{
    public class FarmUserProfileService : IFullProfileSyncService, IIncrementalProfileSyncService
    {
        private const string FarmSettingsPropertyKey = "TEUserPropSettings";
        private const string FarmSyncEnabledPropertyKey = "TEUserPropEnable";
        private const string UserProfileServiceUrl = "/_vti_bin/UserProfileService.asmx";
        private const string UserProfileChangeServiceUrl = "/_vti_bin/UserProfileChangeService.asmx";

        private readonly int userProfileBatchCapacity = 100;
        private readonly SPProfileSyncProvider syncSettings;
        private readonly UserProfileService farmUserProfileService;
        private readonly UserProfileChangeService farmUserProfileChangeService;
        private readonly SPContext spcontext;

        private readonly string[] TimeZones = {"(UTC) Greenwich Mean Time : Dublin, Edinburgh, Lisbon, London", "(UTC) Greenwich Mean Time : Dublin, Edinburgh, Lisbon, London", "(UTC) Greenwich Mean Time : Dublin, Edinburgh, Lisbon, London", "(UTC+01:00) Brussels, Copenhagen, Madrid, Paris", "(UTC+01:00) Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna", "(UTC+02:00) Athens, Bucharest, Istanbul", "(UTC+01:00) Belgrade, Bratislava, Budapest, Ljubljana, Prague", "(UTC+02:00) Minsk", "(UTC-03:00) Brasilia", "(UTC-04:00) Atlantic Time (Canada)", "(UTC-05:00) Eastern Time (US and Canada)", "(UTC-06:00) Central Time (US and Canada)", "(UTC-07:00) Mountain Time (US and Canada)", "(UTC-08:00) Pacific Time (US and Canada)", "(UTC-09:00) Alaska", "(UTC-10:00) Hawaii", "(UTC-11:00) Midway Island, Samoa", "(UTC+12:00) Auckland, Wellington", "(UTC+10:00) Brisbane", "(UTC+09:30) Adelaide", "(UTC+09:00) Osaka, Sapporo, Tokyo", "(UTC+08:00) Kuala Lumpur, Singapore", "(UTC+07:00) Bangkok, Hanoi, Jakarta", "(UTC+05:30) Chennai, Kolkata, Mumbai, New Delhi", "(UTC+04:00) Abu Dhabi, Muscat", "(UTC+03:30) Tehran", "(UTC+03:00) Baghdad", "(UTC+02:00) Jerusalem", "(UTC-03:30) Newfoundland and Labrador", "(UTC-01:00) Azores", "(UTC-02:00) Mid-Atlantic", "(UTC) Monrovia, Reykjavik", "(UTC-03:00) Cayenne", "(UTC-04:00) Georgetown, La Paz, San Juan", "(UTC-05:00) Indiana (East)", "(UTC-05:00) Bogota, Lima, Quito", "(UTC-06:00) Saskatchewan", "(UTC-06:00) Guadalajara, Mexico City, Monterrey", "(UTC-07:00) Arizona", "(UTC-12:00) International Date Line West", "(UTC+12:00) Fiji Is., Marshall Is.", "(UTC+11:00) Magadan, Solomon Is., New Caledonia", "(UTC+10:00) Hobart", "(UTC+10:00) Guam, Port Moresby", "(UTC+09:30) Darwin", "(UTC+08:00) Beijing, Chongqing, Hong Kong S.A.R., Urumqi", "(UTC+06:00) Novosibirsk", "(UTC+05:00) Tashkent", "(UTC+04:30) Kabul", "(UTC+02:00) Cairo", "(UTC+02:00) Harare, Pretoria", "(UTC+03:00) Moscow, St. Petersburg, Volgograd", "(UTC-01:00) Cape Verde Is.", "(UTC+04:00) Baku", "(UTC-06:00) Central America", "(UTC+03:00) Nairobi", "(UTC+01:00) Sarajevo, Skopje, Warsaw, Zagreb", "(UTC+05:00) Ekaterinburg", "(UTC+02:00) Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius", "(UTC-03:00) Greenland", "(UTC+06:30) Yangon (Rangoon)", "(UTC+05:45) Kathmandu", "(UTC+08:00) Irkutsk", "(UTC+07:00) Krasnoyarsk", "(UTC-04:00) Santiago", "(UTC+05:30) Sri Jayawardenepura", "(UTC+13:00) Nuku'alofa", "(UTC+10:00) Vladivostok", "(UTC+01:00) West Central Africa", "(UTC+09:00) Yakutsk", "(UTC+06:00) Astana, Dhaka", "(UTC+09:00) Seoul", "(UTC+08:00) Perth", "(UTC+03:00) Kuwait, Riyadh", "(UTC+08:00) Taipei", "(UTC+10:00) Canberra, Melbourne, Sydney", "(UTC-07:00) Chihuahua, La Paz, Mazatlan", "(UTC-08:00) Tijuana, Baja California", "(UTC+02:00) Amman", "(UTC+02:00) Beirut", "(UTC-04:00) Manaus", "(UTC+04:00) Tbilisi", "(UTC+02:00) Windhoek", "(UTC+04:00) Yerevan", "(UTC-03:00) Buenos Aires", "(UTC) Casablanca", "(UTC+05:00) Islamabad, Karachi", "(UTC-04:30) Caracas", "(UTC+04:00) Port Louis", "(UTC-03:00) Montevideo", "(UTC-04:00) Asuncion", "(UTC+12:00) Petropavlovsk-Kamchatsky", "(UTC) Coordinated Universal Time", "(UTC+08:00) Ulaanbaatar"};

        private Dictionary<string, PropertyInfo> farmUserPropertyInfo;
        private bool? isSyncEnabled;

        public FarmUserProfileService(SPProfileSyncProvider settings)
        {
            syncSettings = settings;

            farmUserProfileService = new UserProfileService
            {
                UseDefaultCredentials = false,
                Credentials = syncSettings.Authentication.Credentials(),
                Url = syncSettings.SPSiteURL.TrimEnd('/') + UserProfileServiceUrl
            };

            farmUserProfileChangeService = new UserProfileChangeService
            {
                UseDefaultCredentials = false,
                Credentials = syncSettings.Authentication.Credentials(),
                Url = syncSettings.SPSiteURL.TrimEnd('/') + UserProfileChangeServiceUrl
            };

            spcontext = new SPContext(syncSettings.SPSiteURL, syncSettings.Authentication);
        }

        public FarmUserProfileService(SPProfileSyncProvider settings, int userProfileBatchCapacity)
            : this(settings)
        {
            this.userProfileBatchCapacity = userProfileBatchCapacity;
        }

        #region IProfileSyncService Members

        public bool Enabled
        {
            get
            {
                if (isSyncEnabled == null)
                {
                    try
                    {
                        if (syncSettings.SyncConfig != null)
                        {
                            isSyncEnabled = syncSettings.SyncConfig.FarmSyncEnabled;
                        }
                        else
                        {
                            SP.Web web = spcontext.Site.RootWeb;
                            spcontext.Load(web.AllProperties);

                            spcontext.ExecuteQuery();

                            // if FarmSync enabled
                            isSyncEnabled = web.AllProperties.FieldValues.ContainsKey(FarmSyncEnabledPropertyKey) && Convert.ToBoolean(web.AllProperties.FieldValues[FarmSyncEnabledPropertyKey]);
                        }
                    }
                    catch (Exception ex)
                    {
                        string msg = string.Format("FarmUserProfileService.Enabled Failed: {0} {1}", ex.Message, ex.StackTrace);
                        SPLog.UserProfileUpdated(ex, msg);
                    }
                }
                return isSyncEnabled ?? false;
            }
        }

        public IEnumerable<UserFieldMapping> Fields
        {
            get
            {
                try
                {
                    if (syncSettings.SyncConfig != null && syncSettings.SyncConfig.FarmProfileMappedFields != null)
                    {
                        return syncSettings.SyncConfig.FarmProfileMappedFields;
                    }

                    SP.Web web = spcontext.Site.RootWeb;
                    spcontext.Load(web.AllProperties);

                    spcontext.ExecuteQuery();

                    if (!web.AllProperties.FieldValues.ContainsKey(FarmSettingsPropertyKey))
                    {
                        return Enumerable.Empty<UserFieldMapping>();
                    }

                    var jsonMapping = (string)web.AllProperties.FieldValues[FarmSettingsPropertyKey];
                    return new JavaScriptSerializer().Deserialize<UserFieldMapping[]>(jsonMapping);
                }
                catch (Exception ex)
                {
                    SPLog.RoleOperationUnavailable(ex, ex.Message);
                }
                return Enumerable.Empty<UserFieldMapping>();
            }
        }

        public List<User> List(IEnumerable<string> emails)
        {
            // Load account names using spcontext
            var accNameList = new List<string>();
            var web = spcontext.Site.RootWeb;

            foreach (var camlQuery in CamlQueryBuilder(emails.ToArray(), userProfileBatchCapacity, syncSettings.SPUserEmailFieldName))
            {
                var spuserCollection = web.SiteUserInfoList.GetItems(new CamlQuery { ViewXml = camlQuery });
                spcontext.Load(spuserCollection, uList => uList.Include(u => u["Name"]));
                spcontext.ExecuteQuery();
                accNameList.AddRange(from spuser in spuserCollection where spuser["Name"] != null select spuser["Name"].ToString());
            }

            var users = new List<User>();
            
            foreach (var accName in accNameList)
            {
                try
                {
                    var userProfileData = farmUserProfileService.GetUserProfileByName(accName);
                    var user = new SPFarmUser(syncSettings.SPFarmUserIdFieldName, syncSettings.SPFarmUserEmailFieldName, userProfileData);
                    
                    InitUserFields(user, userProfileData);
                    
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        users.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("could not be found") || ex.StackTrace.Contains("could not be found"))
                    {
                        SPLog.Event(string.Format("User with account name {0} could not be found in SharePoint.", accName));
                        continue;
                    }

                    SPLog.UserProfileUpdated(ex, string.Format("FarmUserProfileService.GetUserProfileByName() Failed: {0} {1}", ex.Message, ex.StackTrace));
                }
            }
            return users;
        }

        public void Update(User mergeUser, IEnumerable<string> fields)
        {
            var accountName = mergeUser.Fields["AccountName"].ToString();

            if (String.IsNullOrEmpty(accountName))
            {
                return;
            }

            var userProfile = mergeUser as SPFarmUser;
            if (userProfile == null)
            {
                return;
            }

            var fieldList = fields.ToList();
            var userProfileHashData = userProfile.Profile.ToDictionary(p => p.Name, p => p);
            var updatedUserProfileData = new PropertyData[fieldList.Count];

            for (int i = 0; i < updatedUserProfileData.Length; i++)
            {
                PropertyData propertyData;
                if (userProfileHashData.TryGetValue(fieldList[i], out propertyData) && propertyData != null)
                {
                    var value = mergeUser[fieldList[i]];

                    DateTime dateValue;
                    if (value != null && DateTime.TryParse(value.ToString(), out dateValue) && dateValue.Year < 1753)
                    {
                        continue;
                    }

                    SetSanitizeUserFieldValue(propertyData, value);
                    updatedUserProfileData[i] = propertyData;
                }
            }

            farmUserProfileService.ModifyUserPropertyByAccountName(accountName, updatedUserProfileData);
        }

        #endregion

        #region IFullProfileSyncService Members

        public List<User> List(ref int nextIndex)
        {
            var users = new List<User>();
            try
            {
                GetUserProfileByIndexResult userInstance;
                if (nextIndex <= 0)
                {
                    // start index for the Profile Web Service
                    nextIndex = -1;
                }

                do
                {
                    userInstance = farmUserProfileService.GetUserProfileByIndex(nextIndex);
                    if (userInstance == null || userInstance.UserProfile == null) continue;

                    try
                    {
                        var user = new SPFarmUser(syncSettings.SPFarmUserIdFieldName, syncSettings.SPFarmUserEmailFieldName, userInstance.UserProfile);
                        InitUserFields(user, userInstance.UserProfile);
                        if (!string.IsNullOrEmpty(user.Email)) { users.Add(user); }
                    }
                    catch (Exception ex)
                    {
                        SPLog.Event(string.Format("Error : {0} FieldCount: {1} Fields: {2}", 
                            ex.Message,
                            userInstance.UserProfile.Length, 
                            ProfileFieldDump(userInstance.UserProfile)));
                    }
                        
                    nextIndex = Convert.ToInt32(userInstance.NextValue);
                }
                while (userInstance != null && userInstance.UserProfile != null && users.Count < userProfileBatchCapacity);
            }
            catch (Exception ex)
            {
                SPLog.UserProfileUpdated(ex, string.Format("FarmUserProfileService.List() Failed: {0} {1}", ex.Message, ex.StackTrace));
            }
            return users;
        }

        private static string ProfileFieldDump(IEnumerable<PropertyData> fields)
        {
            string values;

            try
            {
                values = string.Join(" | \n",
                    fields.Select(p =>
                    string.Format("{0}:{1}", p.Name,
                    string.Join(", ", p.Values.Select(v => v.Value.ToString())))));
            }
            catch (Exception ex)
            {
                values = ex.Message;
            }

            return values;
        }

        #endregion

        #region IIncrementalProfileSyncService Members

        public List<User> List(DateTime date)
        {
            var users = new List<User>();
            try
            {
                var changeToken = new UserProfileChangeToken();
                var profileChanges = farmUserProfileChangeService.GetChanges(string.Empty, new UserProfileChangeQuery
                {
                    ChangeTokenStart = changeToken,
                    Add = true,
                    Update = true,
                    UserProfile = true,
                    SingleValueProperty = true,
                    MultiValueProperty = true,
                });

                var accNameList = profileChanges.Changes.Where(ch => ch.EventTime >= date).GroupBy(d => d.UserAccountName).Select(gr => gr.Key).ToList();
                
                foreach (var accName in accNameList)
                {
                    try
                    {
                        var userProfileData = farmUserProfileService.GetUserProfileByName(accName);
                        var user = new SPFarmUser(syncSettings.SPFarmUserIdFieldName, syncSettings.SPFarmUserEmailFieldName, userProfileData);
                        
                        InitUserFields(user, userProfileData);
                        
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            users.Add(user);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("could not be found"))
                        {
                            SPLog.Event(string.Format("User with account name {0} could not be found in SharePoint.", accName)); continue;
                        }

                        SPLog.UserProfileUpdated(ex, string.Format("FarmUserProfileService.GetUserProfileByName() Failed: {0} {1}", ex.Message, ex.StackTrace));
                    }
                }
            }
            catch (Exception ex)
            {
                SPLog.UserProfileUpdated(ex, string.Format("FarmUserProfileService.List() Failed: {0} {1}", ex.Message, ex.StackTrace));
            }

            return users;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            spcontext.Dispose();
            farmUserProfileService.Dispose();
        }

        #endregion

        private Dictionary<string, PropertyInfo> FarmUserPropertyInfo
        {
            get
            {
                if (farmUserPropertyInfo == null)
                {
                    try
                    {
                        farmUserPropertyInfo = farmUserProfileService.GetUserProfileSchema().ToDictionary(p => p.Name, p => p);
                    }
                    catch (Exception ex)
                    {
                        var msg = string.Format("FarmUserProfileService.FarmUserPropertyInfo() Failed: {0} {1}", ex.Message, ex.StackTrace);
                        SPLog.UserProfileUpdated(ex, msg);
                    }
                }
                return farmUserPropertyInfo;
            }
        }

        private void InitUserFields(SPFarmUser user, IEnumerable<PropertyData> userProfileData)
        {
            foreach (var propertyData in userProfileData)
            {
                if (propertyData == null)
                {
                    PublicApi.Eventlogs.Write("Null PropertyData object found in User Profile while initializing user fields.", 
                        new EventLogEntryWriteOptions
                        {
                            Category = "SharePoint"
                        });

                    continue;
                }

                var name = propertyData.Name;
                user.Fields.Add(name, GetSanitizeUserFieldValue(propertyData));
            }
        }

        private object GetSanitizeUserFieldValue(PropertyData propertyData)
        {
            object propertyDataValue;
            if (!propertyData.Values.Any()) return null;

            if (!FarmUserPropertyInfo.ContainsKey(propertyData.Name))
            {
                PublicApi.Eventlogs.Write(string.Format("ProperyInfo does not contain key ({0}).", propertyData.Name),
                    new EventLogEntryWriteOptions
                    {
                        Category = "SharePoint"
                    });

                return null;
            }

            var propertyInfo = FarmUserPropertyInfo[propertyData.Name];

            if (propertyInfo == null)
            {
                PublicApi.Eventlogs.Write(string.Format("ProperyInfo cannot be null ({0}).", propertyData.Name),
                    new EventLogEntryWriteOptions
                    {
                        Category = "SharePoint"
                    });

                return null;
            }

            if (!propertyInfo.IsMultiValue)
            {
                propertyDataValue = propertyData.Values[0].Value;
                if (propertyData.Name.ToLowerInvariant() == "pictureurl")
                {
                    propertyDataValue = ((string)propertyDataValue).Replace("_MThumb.jpg", "_LThumb.jpg");
                }

                if (propertyData.Name.ToLowerInvariant() != "accountname" && propertyInfo.Type.ToLowerInvariant() == "person")
                {
                    var personValue = propertyDataValue as string;
                    if (personValue == null) return propertyDataValue;

                    try
                    {
                        var person = farmUserProfileService.GetUserProfileByName(personValue);
                        propertyDataValue = person.First(p => p.Name == "PreferredName").Values[0].Value;
                    }
                    catch (Exception)
                    {
                        propertyDataValue = personValue;
                    }
                }

                var tzValue = propertyDataValue as SPTimeZone;
                if (tzValue == null) return propertyDataValue;

                var tz = ((SPTimeZone)propertyDataValue).ID;
                propertyDataValue = TimeZones[tz];
            }
            else
            {
                var vals = propertyData.Values.Select(item => item.Value.ToString()).ToArray();
                propertyDataValue = string.Join(";", vals);
            }

            return propertyDataValue;
        }

        private void SetSanitizeUserFieldValue(PropertyData propertyData, object value)
        {
            if (propertyData == null) return;

            var propertyInfo = FarmUserPropertyInfo[propertyData.Name];
            
            if (!propertyInfo.IsMultiValue)
            {
                propertyData.Values = new[] { new ValueData { Value = value } };
            }
            else
            {
                propertyData.Values = value == null ? new[] {new ValueData()} : value.ToString().Split(';').Select(item => new ValueData { Value = item }).ToArray();
            }

            propertyData.IsValueChanged = true;
        }

        private IEnumerable<string> CamlQueryBuilder(string[] emails, int batchSize, string fieldName)
        {
            var queries = new List<string>();

            int batchesCount = emails.Length / batchSize + (emails.Length % batchSize != 0 ? 1 : 0);
            for (int batchIndex = 0; batchIndex < batchesCount; batchIndex++)
            {
                int startIndex = batchIndex * batchSize;
                int endIndex = (batchIndex + 1) * batchSize;
                var query = new StringBuilder(EqualQuery(fieldName, emails[startIndex]));
                for (int i = startIndex + 1; i < endIndex && i < emails.Length; i++)
                {
                    query.Insert(0, "<Or>" + EqualQuery(fieldName, emails[i]));
                    query.Append("</Or>");
                }
                queries.Add(String.Format(@"<View><Query><Where>{0}</Where></Query></View>", query));
            }

            return queries;
        }

        private string EqualQuery(string fieldName, string fieldValue)
        {
            return String.Format("<Eq><FieldRef Name='{0}' /><Value Type='Text'>{1}</Value></Eq>", fieldName, fieldValue);
        }
    }
}