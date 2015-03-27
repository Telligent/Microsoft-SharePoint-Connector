using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.Plugins
{
    public class SPProfileSyncPlugin : IConfigurablePlugin, IPluginGroup, ICategorizedPlugin
    {
        public static class PropertyId
        {
            public const string SPProfileSyncSettings = "ProfileSyncSettings";
        }

        #region ICategorizedPlugin

        public string[] Categories { get { return new[] { "SharePoint" }; } }

        #endregion

        #region IPlugin Members

        public string Name
        {
            get { return "SharePoint User Profile Sync"; }
        }

        public string Description
        {
            get { return "Enables Telligent to sync with SharePoint profiles"; }
        }

        public void Initialize() { }

        #endregion

        #region IConfigurablePlugin Members

        public IPluginConfiguration Configuration { get; private set; }

        public void Update(IPluginConfiguration configuration)
        {
            Configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                const string profileSyncTab = "Profile Sync Settings";
                var groups = new[] { new PropertyGroup("SyncSettingsGroup", profileSyncTab, 0) };
                var spSites = new Property(PropertyId.SPProfileSyncSettings, String.Empty, PropertyType.Custom, 0, String.Empty)
                {
                    ControlType = typeof(SPProfileSyncControl)
                };
                groups[0].Properties.Add(spSites);
                return groups;
            }
        }

        #endregion

        #region IPluginGroup
        public IEnumerable<Type> Plugins
        {
            get
            {
                return new[]
                {
                    typeof(FullProfileSyncJob),
                    typeof(IncrementalProfileSyncJob),
                };
            }
        }
        #endregion

        public static SPProfileSyncPlugin Plugin
        {
            get
            {
                return PluginManager.Get<SPProfileSyncPlugin>().FirstOrDefault();
            }
        }

        /// <summary>
        /// The method returns the authentication method by SiteCollection Url
        /// </summary>
        /// <param name="url">SharePoint SiteCollection Url</param>
        /// <returns>The Authentication method</returns>
        public static Authentication CurrentAuth(string url)
        {
            SPProfileSyncProvider profileSyncSettings = GetProfileSyncProviders().FirstOrDefault(manager => manager.SPSiteURL.Trim('/').Equals(url.Trim('/')));
            return profileSyncSettings != null ? profileSyncSettings.Authentication : DefaultAuth();
        }

        private static Authentication DefaultAuth()
        {
            return new Anonymous();
        }

        private static IEnumerable<SPProfileSyncProvider> GetProfileSyncProviders()
        {
            var profileSyncPlugin = Plugin;
            if (profileSyncPlugin != null)
            {
                var settingsList = new SPProfileSyncProviderList(profileSyncPlugin.Configuration.GetString(PropertyId.SPProfileSyncSettings));
                return settingsList.All();
            }
            return new List<SPProfileSyncProvider>();
        }
    }
}
