using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.WebServices;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi
{
    internal static class SPConfigurationService
    {
        internal static SPConfiguration Get(string url, Authentication auth)
        {
            var config = new SPConfiguration(url, auth);
            using (var spcontext = new SPContext(url, auth))
            {
                try
                {
                    SP.Web web = spcontext.Site.RootWeb;
                    var siteProfileFieldsQuery = spcontext.LoadQuery(web.SiteUserInfoList.Fields).Where(f => !f.Hidden);

                    spcontext.ExecuteQuery();
                    config.SiteProfileFields = siteProfileFieldsQuery.Select(f => new ProfileField(f.StaticName, f.Title, !f.ReadOnlyField)).OrderBy(f => f.Title).ToList();

                    spcontext.Load(web.AllProperties,
                        prop => prop[SPWebPropertyKey.SyncEnabled],
                        prop => prop[SPWebPropertyKey.SiteSettings],
                        prop => prop[SPWebPropertyKey.FarmSettings],
                        prop => prop[SPWebPropertyKey.FarmSyncEnabled]);

                    spcontext.ExecuteQuery();
                    ParseWebProperties(web, config);
                }
                catch (Exception)
                {
                    SPLog.Info("Profile Sync properites do not exist in SharePoint. These are optional fields, no action is required.");
                }
            }

            config.FarmProfileFields = new List<ProfileField>();
            using (var userProfileService = new ProfileService(url, auth))
            {
                try
                {
                    var properties = userProfileService.GetUserProfileSchema();
                    foreach (var property in properties.OrderBy(p => p.DisplayName))
                    {
                        config.FarmProfileFields.Add(new ProfileField(property.Name, property.DisplayName, property.IsUserEditable));
                    }
                }
                catch (Exception ex)
                {
                    SPLog.RoleOperationUnavailable(ex, ex.Message);
                }
            }
            return config;
        }

        internal static void Set(SPConfiguration config)
        {
            using (var spcontext = new SPContext(config.Url, config.Auth))
            {
                SP.Web web = spcontext.Site.RootWeb;
                web.AllProperties[SPWebPropertyKey.SyncEnabled] = config.SyncEnabled;
                web.AllProperties[SPWebPropertyKey.FarmSyncEnabled] = config.FarmSyncEnabled;
                web.AllProperties[SPWebPropertyKey.SiteSettings] = new JavaScriptSerializer().Serialize(config.SiteProfileMappedFields);
                web.AllProperties[SPWebPropertyKey.FarmSettings] = new JavaScriptSerializer().Serialize(config.FarmProfileMappedFields);
                web.Update();

                spcontext.ExecuteQuery();
            }
        }

        private static void ParseWebProperties(SP.Web web, SPConfiguration config)
        {
            bool syncEnabled;
            if (web.TryParse(SPWebPropertyKey.SyncEnabled, out syncEnabled))
            {
                config.SyncEnabled = syncEnabled;
            }

            bool farmSyncEnabled;
            if (web.TryParse(SPWebPropertyKey.FarmSyncEnabled, out farmSyncEnabled))
            {
                config.FarmSyncEnabled = farmSyncEnabled;
            }

            config.SiteProfileMappedFields = new List<UserFieldMapping>();
            web.TryParse(SPWebPropertyKey.SiteSettings, config.SiteProfileMappedFields);

            config.FarmProfileMappedFields = new List<UserFieldMapping>();
            web.TryParse(SPWebPropertyKey.FarmSettings, config.FarmProfileMappedFields);
        }

        private static bool TryParse(this SP.Web web, string propertyKey, out bool value)
        {
            value = false;
            return web.AllProperties.FieldValues.ContainsKey(SPWebPropertyKey.SiteSettings) && bool.TryParse(web.AllProperties[propertyKey].ToString(), out value);
        }

        private static bool TryParse(this SP.Web web, string propertyKey, ICollection<UserFieldMapping> value)
        {
            if (web.AllProperties.FieldValues.ContainsKey(propertyKey))
            {
                var jsonMapping = (string)web.AllProperties[propertyKey];
                if (!String.IsNullOrEmpty(jsonMapping))
                {
                    foreach (var item in new JavaScriptSerializer().Deserialize<List<UserFieldMapping>>(jsonMapping))
                        value.Add(item);
                    return true;
                }
            }
            return false;
        }
    }
}
