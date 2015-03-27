using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;

[assembly: WebResource("Telligent.Evolution.Extensions.SharePoint.IntegrationManager.PropertyControls.IntegrationManagerControl.js", "text/javascript")]
namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager
{
    public class IntegrationManagerControl : ItemCollectionControl, IPropertyControl
    {
        public const string ProviderListId = "ManagerListXML";
        private IntegrationProviders Providers
        {
            get
            {
                return new IntegrationProviders(hidden.Value);
            }
            set
            {
                hidden.Value = value.ToXml();
            }
        }
        private HiddenField hidden;
        private HiddenField hdnProviderData;
        private HiddenField hdnProviderAction;

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public IntegrationManagerControl()
        {
            ConfigurationData = null;
            ConfigurationProperty = null;
        }

        public Property ConfigurationProperty { get; set; }
        public ConfigurationDataBase ConfigurationData { get; set; }

        #region Overriden

        protected override string CSSpath()
        {
            return "/SharePoint/IntegrationManager/Style/IntegrationManager.css";
        }

        protected override void OnInit(EventArgs e)
        {
            ID = "object-manager-control";
            ClientIDMode = ClientIDMode.Static;
            base.OnInit(e);
            hidden = new HiddenField { ID = ProviderListId, ClientIDMode = ClientIDMode.Static };
            hdnProviderData = new HiddenField { ID = "ManagerData", ClientIDMode = ClientIDMode.Static };
            hdnProviderAction = new HiddenField { ID = "ManagerAction", ClientIDMode = ClientIDMode.Static };
            Controls.Add(hidden);
            Controls.Add(hdnProviderData);
            Controls.Add(hdnProviderAction);

            // Register javascript function callback
            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(IntegrationManagerControl),
                "Telligent.Evolution.Extensions.SharePoint.IntegrationManager.PropertyControls.IntegrationManagerControl.js");

            FilterDefaultText = "Find Site Collection";
        }

        protected override void OnPreRender(EventArgs e)
        {
            IntegrationProviders list = Providers;
            ProcessSubmitedData(list);
            Providers = list;
            string key = TemporaryStore.Add(list.ToXml()).ToString();
            var presenters = list.Collection.Select(provider => new IntegrationManagerPresenter(provider, key, BaseUrl)).ToList();
            Bind(presenters, true);
            base.OnPreRender(e);
            SetControlStyles();
        }

        #endregion

        #region Links for modal pages

        protected override List<Control> HeaderButtons()
        {
            return IntegrationManagerPresenter.HeaderButtons(Providers, contentDiv.ClientID, BaseUrl);
        }

        #endregion

        #region IPropertyControl

        public Control Control
        {
            get { return this; }
        }

        public object GetConfigurationPropertyValue()
        {
            return Providers != null ? Providers.ToXml() : String.Empty;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            Providers = value is String ? new IntegrationProviders((String)value) : new IntegrationProviders();
        }

        #endregion

        private void SetControlStyles()
        {
            contentDiv.Attributes["class"] = "sp-object-manager ";
            itemListHeader.Attributes["class"] = "header";
            scrollableItemList.Attributes["class"] = "scrollable-content";
            itemListContent.Attributes["class"] = "content";
            itemListFooter.Attributes["class"] = "footer";

            var separator = new HtmlGenericControl("div");
            separator.Attributes["class"] = "separator";
            itemListFooter.Controls.Add(separator);
        }

        private void ProcessSubmitedData(IntegrationProviders providers)
        {
            if (String.IsNullOrEmpty(hdnProviderAction.Value) || String.IsNullOrEmpty(hdnProviderData.Value))
                return;

            switch (hdnProviderAction.Value)
            {
                case "add": InsertProvider(providers, hdnProviderData.Value); break;
                case "delete": DeleteProvider(providers, hdnProviderData.Value); break;
            }
        }

        private void InsertProvider(IntegrationProviders providers, string data)
        {
            var provider = new IntegrationProvider(data);
            providers.Insert(provider);
        }

        private void DeleteProvider(IntegrationProviders providers, string data)
        {
            foreach (var idData in data.Split('&'))
            {
                int id;
                if (int.TryParse(idData, out id))
                {
                    providers.Delete(id.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
    }
}
