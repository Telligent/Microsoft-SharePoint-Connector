using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model;
using SP = Microsoft.SharePoint.Client;
using User = Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities.User;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi
{
    public class SiteUserProfileService : IFullProfileSyncService, IIncrementalProfileSyncService, IDisposable
    {
        private const string SiteSettingsPropertyKey = "TEUserSyncSettings";
        private const string FarmSyncEnabledPropertyKey = "TEUserPropEnable";

        private readonly int userProfileBatchCapacity = 100;
        private readonly SPProfileSyncProvider syncSettings;
        private readonly SPContext spcontext;

        private bool? isSyncEnabled = null;

        public SiteUserProfileService(SPProfileSyncProvider settings)
        {
            syncSettings = settings;
            spcontext = new SPContext(syncSettings.SPSiteURL, syncSettings.Authentication);
        }

        public SiteUserProfileService(SPProfileSyncProvider settings, int userProfileBatchCapacity)
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
                            isSyncEnabled = !syncSettings.SyncConfig.FarmSyncEnabled;
                        }
                        else
                        {
                            SP.Web web = spcontext.Site.RootWeb;
                            spcontext.Load(web.AllProperties);

                            spcontext.ExecuteQuery();

                            // if FarmSync disabled, then use SiteSync
                            isSyncEnabled = web.AllProperties.FieldValues.ContainsKey(FarmSyncEnabledPropertyKey) && !Convert.ToBoolean(web.AllProperties.FieldValues[FarmSyncEnabledPropertyKey]);
                        }
                    }
                    catch (Exception ex)
                    {
                        string msg = string.Format("SiteUserProfileService.Enabled Failed: {0} {1}", ex.Message, ex.StackTrace);
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
                    if (syncSettings.SyncConfig != null && syncSettings.SyncConfig.FarmProfileMappedFields != null && syncSettings.SyncConfig.FarmProfileMappedFields.Count > 0)
                    {
                        return syncSettings.SyncConfig.FarmProfileMappedFields;
                    }

                    if (syncSettings.SyncConfig != null && syncSettings.SyncConfig.SiteProfileMappedFields != null && syncSettings.SyncConfig.SiteProfileMappedFields.Count > 0)
                    {
                        return syncSettings.SyncConfig.SiteProfileMappedFields;
                    }

                    SP.Web web = spcontext.Site.RootWeb;
                    spcontext.Load(web.AllProperties);

                    spcontext.ExecuteQuery();

                    if (!web.AllProperties.FieldValues.ContainsKey(SiteSettingsPropertyKey))
                    {
                        return Enumerable.Empty<UserFieldMapping>();
                    }

                    var jsonMapping = (string)web.AllProperties.FieldValues[SiteSettingsPropertyKey];
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
            var userList = new List<User>();
            SP.Web web = spcontext.Site.RootWeb;
            foreach (string camlQuery in CamlQueryBuilder(emails.ToArray(), userProfileBatchCapacity, syncSettings.SPUserEmailFieldName))
            {
                SP.ListItemCollection spuserCollection = web.SiteUserInfoList.GetItems(new CamlQuery { ViewXml = camlQuery });
                spcontext.Load(spuserCollection);
                spcontext.ExecuteQuery();
                InitUserList(spuserCollection, userList);
            }
            return userList;
        }

        public void Update(User mergeUser, IEnumerable<string> fields)
        {
            var userProfile = mergeUser as SPSiteUser;
            if (userProfile == null)
            {
                return;
            }

            SP.ListItem spUser = userProfile.Profile;
            foreach (string fieldName in fields)
            {
                SetSanitizeUserFieldValue(spUser, fieldName, mergeUser[fieldName]);
            }
            spUser.Update();
            spcontext.ExecuteQuery();
        }

        #endregion

        #region IFullProfileSyncService Members

        public List<User> List(ref int nextIndex)
        {
            try
            {
                SP.Web web = spcontext.Site.RootWeb;

                // Set up pageSize (pageIndex == 0 by default)
                CamlQuery paginationQuery = CamlQuery.CreateAllItemsQuery(userProfileBatchCapacity);

                // Load position to update pagination query
                int currentItemCounterIndex = userProfileBatchCapacity * nextIndex;
                if (currentItemCounterIndex > 0)
                {
                    CamlQuery query = CamlQuery.CreateAllItemsQuery(currentItemCounterIndex);
                    ListItemCollection emptySPUserCollection = web.SiteUserInfoList.GetItems(query);
                    spcontext.Load(emptySPUserCollection, items => items.ListItemCollectionPosition);

                    spcontext.ExecuteQuery();

                    // Set up pageIndex
                    paginationQuery.ListItemCollectionPosition = emptySPUserCollection.ListItemCollectionPosition;
                    if (emptySPUserCollection.ListItemCollectionPosition == null)
                    {
                        return new List<User>();
                    }
                }

                // Load user profiles
                ListItemCollection spuserCollection = web.SiteUserInfoList.GetItems(paginationQuery);
                spcontext.Load(spuserCollection);

                spcontext.ExecuteQuery();

                var dataUserList = new List<User>();
                InitUserList(spuserCollection, dataUserList);
                return dataUserList;
            }
            catch (Exception ex)
            {
                SPLog.RoleOperationUnavailable(ex, ex.Message);
            }
            finally
            {
                nextIndex++;
            }
            return new List<User>();
        }

        #endregion

        #region IIncrementalProfileSyncService Members

        public List<User> List(DateTime date)
        {
            SP.Web web = spcontext.Site.RootWeb;
            SP.ListItemCollection spuserCollection = web.SiteUserInfoList.GetItems(new CamlQuery { ViewXml = ViewQueryWhere(LastModifiedDate(date)) });
            spcontext.Load(spuserCollection);

            spcontext.ExecuteQuery();

            var userList = new List<User>();
            InitUserList(spuserCollection, userList);
            return userList;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            spcontext.Dispose();
        }

        #endregion

        private object GetSanitizeUserFieldValue(object field)
        {
            if (field is FieldUrlValue)
                return ((FieldUrlValue)field).Url;
            if (field is FieldUserValue)
                return ((FieldUserValue)field).LookupValue;
            if (field is FieldLookupValue)
                return ((FieldLookupValue)field).LookupValue;
            return field;
        }

        private void SetSanitizeUserFieldValue(ListItem spItem, string fieldName, object value)
        {
            object field = spItem[fieldName];
            if (field is FieldUrlValue)
                ((FieldUrlValue)field).Url = (string)value;
            else
                spItem[fieldName] = value;
        }

        private void InitUserList(SP.ListItemCollection spuserCollection, ICollection<User> users)
        {
            foreach (ListItem spuser in spuserCollection)
            {
                var user = new SPSiteUser(syncSettings.SPUserIdFieldName, syncSettings.SPUserEmailFieldName, spuser);
                foreach (var kvp in spuser.FieldValues)
                {
                    user.Fields.Add(kvp.Key, GetSanitizeUserFieldValue(kvp.Value));
                }
                if (!string.IsNullOrEmpty(user.Email))
                {
                    users.Add(user);
                }
            }
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

        private string ViewQueryWhere(string query)
        {
            return String.Format(@"<View><Query><Where>{0}</Where></Query></View>", query);
        }

        private string LastModifiedDate(DateTime date)
        {
            string dateString = date.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return String.Format("<Gt><FieldRef Name='Modified' /><Value IncludeTimeValue='TRUE' Type='DateTime'>{0}</Value></Gt>", dateString);
        }
    }
}