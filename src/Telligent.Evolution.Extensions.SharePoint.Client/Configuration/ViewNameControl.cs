using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;

[assembly: WebResource("Telligent.Evolution.Extensions.SharePoint.Client.Configuration.ViewNameControl.js", "text/javascript")]
namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    public class ViewNameControl : Control, IPropertyControl
    {
        #region Controls
        private TextBox tbViewName = new TextBox();
        private HtmlGenericControl divMsg = new HtmlGenericControl("div");
        private CustomValidator viewNameValidator = new CustomValidator();
        #endregion

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            EnsureChildControls();
            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(ViewNameControl),
                "Telligent.Evolution.Extensions.SharePoint.Client.Configuration.ViewNameControl.js");
            
            string script = String.Format(
                @"jQuery.telligent.evolution.extensions.lookupSharePointViewName.register({{
                    WebItemControl: jQuery('input#web-item-url'),
                    ListTextBox: jQuery('input#list-item-id'),
                    ViewTextBox: jQuery('input#{0}'),
                    Spinner: '<div style=""text-align: center;""><img src=""' + $.telligent.evolution.site.getBaseUrl() + 'Utility/spinner.gif"" /></div>'
                }});", tbViewName.ClientID);
            CSControlUtility.Instance().RegisterClientScriptBlock(this, typeof(ViewNameControl), "Telligent.Evolution.Extensions.SharePoint.Client.Configuration.ViewNameControl",
                @"<script type='text/javascript' language='javascript'>
                    jQuery(document).ready(function(){
                        " + script + @"
                    });
                </script>", false);
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            tbViewName.Style.Add("display", "none");
            this.Controls.Add(tbViewName);
            //CreateViewNameValidator(this);
            InitDivMsg();
        }

        private void InitDivMsg()
        {
            divMsg.Attributes["style"] = "display: none;";
            HtmlGenericControl msg = new HtmlGenericControl("p");
            msg.InnerText = version2.SharePointLibraryExtension.Plugin.ViewNotFoundMsg();
            HtmlGenericControl views = new HtmlGenericControl("ul");
            divMsg.Controls.Add(msg);
            divMsg.Controls.Add(views);
            this.Controls.Add(divMsg);
        }

        private void CreateViewNameValidator(Control ownerControl)
        {
            tbViewName.CausesValidation = true;
            viewNameValidator.ControlToValidate = viewNameValidator.ID;
            viewNameValidator.ServerValidate += new ServerValidateEventHandler(widgetTitleValidator_ServerValidate);
            ownerControl.Controls.Add(viewNameValidator);
        }

        private void widgetTitleValidator_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = !String.IsNullOrEmpty(tbViewName.Text);
            if (!args.IsValid)
            {
                divMsg.Attributes["style"] = "";
            }
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
            return tbViewName.Text;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            tbViewName.Text = value != null ? value.ToString() : String.Empty;
        }
        #endregion
    }
}
