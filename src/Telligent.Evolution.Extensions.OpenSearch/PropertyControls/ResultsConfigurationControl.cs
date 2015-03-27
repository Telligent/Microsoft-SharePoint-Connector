using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class ResultsConfigurationControl : ConfigureProviderControl, IPropertyControl
    {
        private SearchProvidersList openSearchProviders;

        #region Overriden
        protected override void OnInit(EventArgs e)
        {
            this.openSearchProviders = OpenSearchPlugin.GetSearchProvidersList;
            EnsureChildControls();
            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (openSearchProviders != null)
            {
                foreach (var provider in openSearchProviders.Get())
                {
                    var providerName = new ListItem(HttpUtility.HtmlDecode(provider.Name), provider.Id);
                    providerName.Attributes["moreresults"] = provider.CanShowMoreResults.ToString(CultureInfo.InvariantCulture);
                    ProvidersList.Items.Add(providerName);
                }
            }
            else
            {
                ProvidersListDiv.Visible = false;
                ShowError("The OpenSearch Providers plugin is not activated!");
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            bool canShowMoreResults = false;
            if (ProvidersList.Items.Count > 0)
                canShowMoreResults = bool.Parse(ProvidersList.SelectedItem.Attributes["moreresults"]);
            if (!canShowMoreResults)
                ShowMoreResultsDiv.Attributes["style"] = "display: none;";
        }
        #endregion

        #region IPropertyControl

        public Property ConfigurationProperty { get; set; }

        public ResultsConfigurationControl()
        {
            ConfigurationData = null;
            ConfigurationProperty = null;
        }

        public ConfigurationDataBase ConfigurationData { get; set; }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public Control Control
        {
            get { return this; }
        }

        public object GetConfigurationPropertyValue()
        {
            var widgetConfig = new SearchWidgetConfiguration
                {
                    Name = HttpUtility.HtmlAttributeEncode(WidgetTitle.Text),
                    ResultsPerPage = int.Parse(ResultsPerPage.Text),
                    TextOnlyResults = TextonlyResults.Checked
                };

            if (ProvidersList.Items.Count > 0)
            {
                bool showMore = openSearchProviders.Get(ProvidersList.SelectedValue).CanShowMoreResults && ShowMoreResultsLink.Checked;
                widgetConfig.ProviderId = ProvidersList.SelectedValue;
                widgetConfig.ResultsPerPage = int.Parse(ResultsPerPage.Text);
                widgetConfig.ShowMoreResultsLink = showMore;
            }
            return widgetConfig.ToXml();
        }

        public void SetConfigurationPropertyValue(object value)
        {
            try
            {
                var widgetConfig = new SearchWidgetConfiguration(value.ToString());
                WidgetTitle.Text = HttpUtility.HtmlDecode(widgetConfig.Name);
                if (ProvidersList.Items.Count > 0)
                    ProvidersList.SelectedItem.Selected = false;
                var item = ProvidersList.Items.FindByValue(widgetConfig.ProviderId);
                if (item != null)
                    item.Selected = true;
                ResultsPerPage.Text = widgetConfig.ResultsPerPage.ToString(CultureInfo.InvariantCulture);
                ShowMoreResultsLink.Checked = widgetConfig.ShowMoreResultsLink;
                TextonlyResults.Checked = widgetConfig.TextOnlyResults;
            }
            catch (FormatException)
            {
                // widget configuration is invalid
                ShowError("The widget configuration is invalid!");
            }
        }
        #endregion
    }
}
