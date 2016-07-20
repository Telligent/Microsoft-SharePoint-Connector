using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls.Layout;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model;
using SP = Microsoft.SharePoint.Client;
using WindowsAuth = Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods.Windows;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync
{
    public class SPProfileSyncPresenter
    {
        private const string AddEditModalWidth = "350";
        private const string AddEditModalHeight = "400";
        private const string ConfigurationModalRelativeUrl = "/SharePoint/ProfileSync/Configuration.aspx";

        private const string AdministrationModalWidth = "750";
        private const string AdministrationModalHeight = "400";
        private const string AdministrationModalRelativeUrl = "/SharePoint/ProfileSync/Administration.aspx";

        private readonly string syncSettingsListKey;

        private readonly SPProfileSyncProvider syncSettings;

        private readonly string baseUrl;

        private SPProfileSyncPresenter(SPProfileSyncProvider syncSettings)
        {
            this.syncSettings = syncSettings;
        }

        public SPProfileSyncPresenter(SPProfileSyncProvider syncSettings, SPProfileSyncProviderList settingsList, string baseUrl, string syncSettingsListKey)
            : this(syncSettings)
        {
            this.baseUrl = baseUrl;
            this.syncSettingsListKey = syncSettingsListKey;
            Init();
        }

        [ItemCollection(IsId = true)]
        public string Id { get { return syncSettings.Id.ToString(CultureInfo.InvariantCulture); } }

        [ItemCollection(Style = "font-weight: bold;", Order = 0, Region = Region.Title, Filtered = true)]
        public String SPSiteTitle { get; private set; }

        [ItemCollection(CssClass = "description", Order = 0, Region = Region.SubTitle)]
        public String SPSiteURL { get { return syncSettings.SPSiteURL; } }

        [ItemCollection(CssClass = "flags", Order = 2, Region = Region.SubTitle)]
        public String AuthName
        {
            get
            {
                if (syncSettings.Authentication is WindowsAuth)
                {
                    return "Windows";
                }
                if (syncSettings.Authentication is ServiceAccount)
                {
                    return "Service";
                }
                if (syncSettings.Authentication is SAML)
                {
                    return "SAML";
                }
                if (syncSettings.Authentication is Components.AuthenticationUtil.Methods.OAuth)
                {
                    return "OAuth";
                }
                return "Anonymous";
            }
        }

        [ItemCollection(CssClass = "flags", Order = 2, Region = Region.SubTitle)]
        public String FarmEnabled
        {
            get
            {
                bool? farmSyncEnabled = null;
                if (syncSettings.SyncConfig != null)
                {
                    farmSyncEnabled = syncSettings.SyncConfig.FarmSyncEnabled;
                }

                if (!farmSyncEnabled.HasValue)
                {
                    using (var farmUserProfileService = new FarmUserProfileService(syncSettings))
                    {
                        farmSyncEnabled = farmUserProfileService.Enabled;
                    }
                }
                return farmSyncEnabled.Value ? "Farm" : "Site";
            }
        }

        [ItemCollection(CssClass = "flags", Order = 2, Region = Region.SubTitle)]
        public String SyncEnabled
        {
            get
            {
                bool? syncEnabled = null;
                if (syncSettings.SyncConfig != null)
                {
                    syncEnabled = syncSettings.SyncConfig.SyncEnabled;
                }

                if (!syncEnabled.HasValue)
                {
                    using (var spUserProfileService = new SPProfileSyncService(syncSettings))
                    {
                        syncEnabled = spUserProfileService.Enabled;
                    }
                }
                return syncEnabled.Value ? "In-Sync" : "Out-Of-Sync";
            }
        }

        [ItemCollection(Text = "Configure", Style = "float:right;", Order = 1, Region = Region.HoverButtons, CssClass = "CommonTextButton")]
        public String Configure
        {
            get
            {
                return ConfigureBtnLink();
            }
        }

        [ItemCollection(Text = "Edit", Style = "float:right;", Order = 1, Region = Region.HoverButtons, CssClass = "CommonTextButton")]
        public String Edit
        {
            get
            {
                return EditBtnLink();
            }
        }

        [ItemCollection(Text = "Delete", Style = "float:right;", Order = 1, Region = Region.HoverButtons, CssClass = "CommonTextButton")]
        public String Delete
        {
            get
            {
                return DeleteLinkQueryString();
            }
        }

        public static List<Control> HeaderButtons(SPProfileSyncProviderList settingsList, string contentDivId, string baseUrl)
        {
            var addBtn = new HtmlGenericControl("a")
            {
                InnerText = "Add"
            };
            addBtn.Attributes["href"] = AddBtnLink(settingsList, baseUrl);
            addBtn.Attributes["class"] = "PanelSaveButton CommonTextButton";
            addBtn.Attributes["style"] = "float: right;";

            var deleteBtn = new HtmlGenericControl("a")
            {
                InnerText = "Delete Selected"
            };
            deleteBtn.Attributes["href"] = String.Format("javascript: DeleteSelectedSyncSettings(jQuery('#{0}'))", contentDivId);
            deleteBtn.Attributes["class"] = "PanelSaveButton CommonTextButton";
            deleteBtn.Attributes["style"] = "float: right;";

            return new List<Control> { addBtn, deleteBtn };
        }

        #region Utility methods
        private static string AddBtnLink(SPProfileSyncProviderList settingsList, string baseUrl)
        {
            const string functionCallback = "AddSyncSettings";

            const string queryString = "?mode=add";

            return String.Format("javascript: Telligent_Modal.Open('{0}', {1}, {2}, {3})",
                String.Concat(baseUrl, ConfigurationModalRelativeUrl, queryString),
                AddEditModalWidth,
                AddEditModalHeight,
                functionCallback);
        }

        private string ConfigureBtnLink()
        {
            const string functionCallback = "AddSyncSettings";

            string queryString = "?" + String.Join("&",
                String.Format("id={0}", syncSettings.Id),
                String.Format("{0}={1}", SPProfileSyncControl.SettingsListKeyName, syncSettingsListKey));

            return String.Format("javascript: Telligent_Modal.Open('{0}&rnd='+new Date().getMilliseconds(), {1}, {2}, {3})",
                String.Concat(baseUrl, AdministrationModalRelativeUrl, queryString),
                AdministrationModalWidth,
                AdministrationModalHeight,
                functionCallback);
        }

        private string EditBtnLink()
        {
            const string functionCallback = "AddSyncSettings";

            string queryString = "?" + String.Join("&",
                "mode=edit",
                String.Format("id={0}", syncSettings.Id),
                String.Format("{0}={1}", SPProfileSyncControl.SettingsListKeyName, syncSettingsListKey));

            return String.Format("javascript: Telligent_Modal.Open('{0}&rnd='+new Date().getMilliseconds(), {1}, {2}, {3})",
                String.Concat(baseUrl, ConfigurationModalRelativeUrl, queryString),
                AddEditModalWidth,
                AddEditModalHeight,
                functionCallback);
        }

        private string DeleteLinkQueryString()
        {
            return String.Format("javascript: DeleteSyncSettings({0})", syncSettings.Id);
        }
        #endregion

        private void Init()
        {
            try
            {
                using (var clientContext = new SPContext(syncSettings.SPSiteURL, syncSettings.Authentication))
                {
                    SP.Web web = clientContext.Site.RootWeb;
                    clientContext.Load(web, spweb => spweb.Title);
                    clientContext.ExecuteQuery();
                    SPSiteTitle = web.Title;
                }
            }
            catch (Exception ex)
            {
                SPLog.UserInvalidCredentials(ex, String.Format("An exception of type {0} occurred while loading SharePoint web site {1}. The exception message is: {2}", ex.GetType().Name, syncSettings.SPSiteURL, ex.Message));
            }
        }
    }
}
