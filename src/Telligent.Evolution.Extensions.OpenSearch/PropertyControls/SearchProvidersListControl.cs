using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.OpenSearch.Controls;

[assembly: WebResource("Telligent.Evolution.Extensions.OpenSearch.PropertyControls.SearchProvidersListControl.js", "text/javascript")]
namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class SearchProvidersListControl : ItemCollectionControl, IPropertyControl
    {
        // Modal settings
        const string Width = "350";
        const string Height = "400";
        const string EditPageRelativeUrl = "/SharePoint/OpenSearch/OpenSearchProviderPage.aspx";

        private SearchProvidersList providers;
        private HiddenField hdnProviderList;
        private HiddenField hdnProviderData;
        private HiddenField hdnProviderAction;

        #region Overriden
        protected override string CSSpath()
        {
            return "/SharePoint/OpenSearch/Style/OpenSearch.css";
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            hdnProviderList = new HiddenField { ID = "CurrentProviderListXML" };
            hdnProviderData = new HiddenField { ID = "ProviderData" };
            hdnProviderAction = new HiddenField { ID = "ProviderAction" };
            Controls.Add(hdnProviderList);
            Controls.Add(hdnProviderData);
            Controls.Add(hdnProviderAction);
            // Register javascript function callback
            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(SearchProvidersListControl),
                "Telligent.Evolution.Extensions.OpenSearch.PropertyControls.SearchProvidersListControl.js");
            FilterDefaultText = "Find Provider";
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            LoadProviders();
            ProcessSubmitedData(providers);
            var providerList = providers.Get().OrderBy(p => p.Name).Select(provider => new SearchProviderPresenter(provider, providers, BaseUrl)).ToList();
            Bind(providerList, true);
            SetControlStyles();
            // Save to viewstate
            SaveProviders();
        }

        protected override List<Control> HeaderButtons()
        {
            var addBtn = new HtmlGenericControl("a");
            addBtn.Attributes["href"] = AddBtnLink();
            addBtn.Attributes["class"] = "PanelSaveButton CommonTextButton";
            addBtn.Attributes["style"] = "float: right;";
            addBtn.InnerText = "Add";

            var deleteBtn = new HtmlGenericControl("a");
            deleteBtn.Attributes["href"] = String.Format("javascript: DeleteSelected(jQuery('#{0}'))", contentDiv.ClientID);
            deleteBtn.Attributes["class"] = "PanelSaveButton CommonTextButton";
            deleteBtn.Attributes["style"] = "float: right;";
            deleteBtn.InnerText = "Delete Selected";

            return new List<Control> { addBtn, deleteBtn };
        }
        #endregion

        #region Links for modal pages
        // ************ Telligent specification ********************* //
        // Telligent_Modal.Open(url, width, height, functionCallback) //
        // ********************************************************** //
        private string AddBtnLink()
        {
            const string functionCallback = "AddProvider";
            const string queryString = "?mode=add";
            return String.Format("javascript:Telligent_Modal.Open('{0}', {1}, {2}, {3})",
                String.Concat(BaseUrl, EditPageRelativeUrl, queryString),
                Width,
                Height,
                functionCallback);
        }
        #endregion

        #region IPropertyControl

        public Property ConfigurationProperty { get; set; }

        private ConfigurationDataBase configurationData;

        public SearchProvidersListControl()
        {
            ConfigurationProperty = null;
        }

        public ConfigurationDataBase ConfigurationData
        {
            get { return configurationData; }
            set { configurationData = value; }
        }

        public Control Control
        {
            get { return this; }
        }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public object GetConfigurationPropertyValue()
        {
            EnsureChildControls();
            LoadProviders();
            var oldproviders = new SearchProvidersList(configurationData.GetCustomValue(OpenSearchPlugin.PropertyId.OpenSearch, String.Empty));
            MergeEncryptedFields(providers, oldproviders);
            return providers != null ? providers.ToXml() : String.Empty;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            providers = new SearchProvidersList(value as String ?? String.Empty);
            foreach (var provider in providers.Get())
            {
                var authentication = provider.Authentication as IEncrypted;
                if (authentication != null)
                    (authentication).InvokeEncryption();
            }
            SaveProviders();
        }
        #endregion

        #region Utility methods
        private void SetControlStyles()
        {
            contentDiv.Attributes["class"] = "open-search-provider ";
            itemListHeader.Attributes["class"] = "header";
            scrollableItemList.Attributes["class"] = "scrollable-content";
            itemListContent.Attributes["class"] = "content";
            itemListFooter.Attributes["class"] = "footer";
            var separator = new HtmlGenericControl("div");
            separator.Attributes["class"] = "separator";
            itemListFooter.Controls.Add(separator);
        }

        private void ProcessSubmitedData(SearchProvidersList list)
        {
            if (!String.IsNullOrEmpty(hdnProviderAction.Value) && !String.IsNullOrEmpty(hdnProviderData.Value))
            {
                switch (hdnProviderAction.Value)
                {
                    case "add": InsertProvider(list, hdnProviderData.Value); break;
                    case "delete": DeleteProvider(list, hdnProviderData.Value); break;
                }
            }
        }

        private void MergeEncryptedFields(SearchProvidersList newvalue, SearchProvidersList oldvalue)
        {
            foreach (var newprovider in newvalue.Get())
            {
                var oldprovider = oldvalue.Get(newprovider.Id);
                if (oldprovider == null)
                    continue;
                var authentication = newprovider.Authentication as IEncrypted;
                if (authentication != null)
                {
                    (authentication).UpdateEncryptedFields(oldprovider.Authentication);
                }
            }
        }

        private void InsertProvider(SearchProvidersList list, string data)
        {
            list.Add(new SearchProvider(data));
        }

        private void DeleteProvider(SearchProvidersList list, string data)
        {
            foreach (var idData in data.Split('&'))
            {
                int id;
                if (int.TryParse(idData, out id))
                {
                    list.Remove(id.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private void LoadProviders()
        {
            providers = new SearchProvidersList(hdnProviderList.Value);
        }

        private void SaveProviders()
        {
            hdnProviderList.Value = providers.ToXml();
        }
        #endregion
    }
}
