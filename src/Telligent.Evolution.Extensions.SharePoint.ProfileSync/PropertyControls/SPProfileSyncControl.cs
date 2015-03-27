using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model;

[assembly: WebResource("Telligent.Evolution.Extensions.SharePoint.ProfileSync.PropertyControls.SPProfileSyncControl.js", "text/javascript")]
namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync
{
    public class SPProfileSyncControl : ItemCollectionControl, IPropertyControl
    {
        public const string SettingsListKeyName = "SyncSettingsKey";
        public const string ManagerListId = "SyncSettingsXML";

        private HiddenField hdnSyncSettingsList;
        private HiddenField hdnManagerData;
        private HiddenField hdnManagerAction;
        private SPProfileSyncProviderList syncSettingsList;

        #region Overriden
        protected override string CSSpath()
        {
            return "/SharePoint/ProfileSync/Style/SPProfileSync.css";
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ID = "profile-sync-control";
            ClientIDMode = ClientIDMode.Static;
            hdnSyncSettingsList = new HiddenField { ID = ManagerListId, ClientIDMode = ClientIDMode.Static };
            hdnManagerData = new HiddenField { ID = "ManagerData", ClientIDMode = ClientIDMode.Static };
            hdnManagerAction = new HiddenField { ID = "ManagerAction", ClientIDMode = ClientIDMode.Static };
            Controls.Add(hdnSyncSettingsList);
            Controls.Add(hdnManagerData);
            Controls.Add(hdnManagerAction);
            // Register javascript function callback
            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(SPProfileSyncControl),
                "Telligent.Evolution.Extensions.SharePoint.ProfileSync.PropertyControls.SPProfileSyncControl.js");
            FilterDefaultText = "Find Site Collection";
        }

        protected override void OnPreRender(EventArgs e)
        {
            if (Page.IsPostBack)
            {
                syncSettingsList = new SPProfileSyncProviderList(hdnSyncSettingsList.Value);
            }
            ProcessSubmitedData(syncSettingsList);
            hdnSyncSettingsList.Value = syncSettingsList.ToXml();
            string syncSettingsListKey = TemporaryStore.Add(syncSettingsList.ToXml()).ToString();
            var presenters = (from settings in syncSettingsList.All()
                              orderby settings.Id
                              select new SPProfileSyncPresenter(settings, syncSettingsList, BaseUrl, syncSettingsListKey)).ToList();
            Bind(presenters, true);
            base.OnPreRender(e);
            SetControlStyles();
        }
        #endregion

        #region Links for modal pages
        protected override List<Control> HeaderButtons()
        {
            return SPProfileSyncPresenter.HeaderButtons(syncSettingsList, contentDiv.ClientID, BaseUrl);
        }
        #endregion

        private void SetControlStyles()
        {
            contentDiv.Attributes["class"] = "sp-profile-sync";
            itemListHeader.Attributes["class"] = "header";
            scrollableItemList.Attributes["class"] = "scrollable-content";
            itemListContent.Attributes["class"] = "content";
            itemListFooter.Attributes["class"] = "footer";

            var separator = new HtmlGenericControl("div");
            separator.Attributes["class"] = "separator";
            itemListFooter.Controls.Add(separator);
        }

        #region IPropertyControl

        public Property ConfigurationProperty { get; set; }

        public ConfigurationDataBase ConfigurationData { get; set; }

        public Control Control
        {
            get { return this; }
        }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public object GetConfigurationPropertyValue()
        {
            return hdnSyncSettingsList.Value;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            string xmlData = value != null ? value.ToString() : String.Empty;
            syncSettingsList = new SPProfileSyncProviderList(xmlData);
        }
        #endregion

        #region Utility methods
        private void ProcessSubmitedData(SPProfileSyncProviderList managerList)
        {
            if (!String.IsNullOrEmpty(hdnManagerAction.Value) && !String.IsNullOrEmpty(hdnManagerData.Value))
            {
                switch (hdnManagerAction.Value)
                {
                    case "add": Add(managerList, hdnManagerData.Value); break;
                    case "delete": Delete(managerList, hdnManagerData.Value); break;
                }
            }
        }

        private void Add(SPProfileSyncProviderList managerList, string dataXml)
        {
            SPProfileSyncProvider spProfileSyncSettings;
            if (SPProfileSyncProvider.TryParse(dataXml, out spProfileSyncSettings))
            {
                managerList.Add(spProfileSyncSettings);
            }
        }
        private void Delete(SPProfileSyncProviderList managerList, string data)
        {
            foreach (var idData in data.Split('&'))
            {
                managerList.Remove(idData);
            }
        }
        #endregion
    }
}
