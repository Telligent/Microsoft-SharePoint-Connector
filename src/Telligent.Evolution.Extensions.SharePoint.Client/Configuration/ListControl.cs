using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;

[assembly: WebResource("Telligent.Evolution.Extensions.SharePoint.Client.Configuration.LibraryControl.js", "text/javascript")]
namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    public class ListControl : Control, IPropertyControl
    {
        private TextBox tbListId;
        protected virtual string Title
        {
            get { return version2.SharePointLibraryExtension.Plugin.SPListMsg(); }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            EnsureChildControls();
            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(ListControl),
                "Telligent.Evolution.Extensions.SharePoint.Client.Configuration.LibraryControl.js");
            CSControlUtility.Instance().RegisterClientScriptBlock(this, typeof(ListControl), "Telligent.Evolution.Extensions.SharePoint.Client.Configuration.LibraryControl", GetInitialClientScript(), true);
        }

        protected virtual string GetInitialClientScript()
        {
            return @"jQuery(document).ready(function(){
                        jQuery.telligent.evolution.extensions.lookupSharePointList.register({
                            WebItemControl: jQuery('input#web-item-url'),
                            LookUpTextBox: jQuery('input#list-item-id'),
                            Spinner: '<div style=""text-align: center;""><img src=""' + $.telligent.evolution.site.getBaseUrl() + 'Utility/spinner.gif"" /></div>',
                            Loader: '<span style=""margin: 4px;"" class=""loading""><img src=""' + $.telligent.evolution.site.getBaseUrl() + 'Utility/spinner.gif"" /></span>',
                            listType: '',
                            excludeType: 'DocumentLibrary'
                        });
                    });";
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            Label lblListId = new Label()
            {
                Text = Title,
                CssClass = "field-item-header"
            };
            tbListId = new TextBox
            {
                ClientIDMode = ClientIDMode.Static,
                ID = "list-item-id"
            };
            this.Controls.Add(lblListId);
            this.Controls.Add(tbListId);
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
            return tbListId.Text;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            tbListId.Text = value != null ? value.ToString() : String.Empty;
        }
        #endregion
    }
}
