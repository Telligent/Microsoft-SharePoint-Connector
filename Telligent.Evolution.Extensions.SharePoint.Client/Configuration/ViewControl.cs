using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;

[assembly: WebResource("Telligent.Evolution.Extensions.SharePoint.Client.Configuration.ViewControl.js", "text/javascript")]
namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    public class ViewControl : Control, IPropertyControl
    {
        private TextBox tbViewId;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            EnsureChildControls();
            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(ViewControl),
                "Telligent.Evolution.Extensions.SharePoint.Client.Configuration.ViewControl.js");

            var sharePointListService = PublicApi.Lists;
            var list = sharePointListService.Get(SPCoreService.Context.ListId);
            if (list != null)
            {
                string script =
                @"jQuery.telligent.evolution.extensions.lookupSharePointView.register({{
                    WebItemControl: jQuery('input#web-item-url'),
                    ListTextBox: jQuery('input#list-item-id'),
                    ViewTextBox: jQuery('input#sp-view-id')
                    Spinner: '<div style=""text-align: center;""><img src=""' + $.telligent.evolution.site.getBaseUrl() + 'Utility/spinner.gif"" /></div>'
                }});";
                CSControlUtility.Instance().RegisterClientScriptBlock(this, typeof(ViewControl), "Telligent.Evolution.Extensions.SharePoint.Client.Configuration.ViewControl",
                @"<script type='text/javascript' language='javascript'>
                    jQuery(document).ready(function(){
                        " + script + @"
                    });
                </script>", false);
            }
            else
            {
                this.Visible = false;
            }
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            Label lblListId = new Label()
            {
                Text = "SharePoint View Name",
                CssClass = "field-item-header"
            };
            tbViewId = new TextBox
            {
                ClientIDMode = ClientIDMode.Static,
                ID = "sp-view-id"
            };
            this.Controls.Add(lblListId);
            this.Controls.Add(tbViewId);
        }

        #region IPropertyControl
        private Property _configurationProperty = null;
        public Property ConfigurationProperty
        {
            get { return _configurationProperty; }
            set { _configurationProperty = value; }
        }

        private ConfigurationDataBase _configurationData = null;
        public ConfigurationDataBase ConfigurationData
        {
            get { return _configurationData; }
            set { _configurationData = value; }
        }

        public Control Control
        {
            get { return this; }
        }

        public event ConfigurationPropertyChanged ConfigurationValueChanged;

        public object GetConfigurationPropertyValue()
        {
            return tbViewId.Text;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            tbViewId.Text = value != null ? value.ToString() : String.Empty;
        }
        #endregion
    }
}
