using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;

[assembly: WebResource("Telligent.Evolution.Extensions.SharePoint.Client.Configuration.WebControl.js", "text/javascript")]
namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    public class WebControl : Control, IPropertyControl
    {
        public class WebItemDTO
        {
            public string Name { get; set; }
            public string Url { get; set; }
        }

        private HiddenField hdnWebItemCollection;
        private HiddenField hdnWebItem;
        private TextBox tbWebUrl;
        private IntegrationProviders managerCollection;
        private List<WebItemDTO> webItemList;

        protected override void OnInit(EventArgs e)
        {
            InitManagerCollection();
            base.OnInit(e);
            EnsureChildControls();
            CSControlUtility.Instance().RegisterClientScriptResource(this, typeof(WebControl),
                "Telligent.Evolution.Extensions.SharePoint.Client.Configuration.WebControl.js");

            CSControlUtility.Instance().RegisterClientScriptBlock(this, typeof(WebControl), "Telligent.Evolution.Extensions.SharePoint.Client.Configuration.WebControl",
                @"<script type='text/javascript' language='javascript'>
                    jQuery(document).ready(function(){
                        var startValue = jQuery('#web-item').val();
                        jQuery.telligent.evolution.extensions.lookupSharePointWeb.register({
                            LookUpTextBox: jQuery('input#web-item-url'),
                            Data: eval('(' + jQuery('input#web-item-collection').val() + ')'),
                            SiteUrl: startValue == 'null' || startValue == '' ? [] : [eval('(' + startValue+ ')').Name],
                            Spinner: '<div style=""text-align: center;""><img src=""' + $.telligent.evolution.site.getBaseUrl() + 'Utility/spinner.gif"" /></div>'
                        });
                    });
                </script>", false);
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            hdnWebItemCollection = new HiddenField()
            {
                ClientIDMode = ClientIDMode.Static,
                ID = "web-item-collection"
            };

            if (managerCollection == null)
                webItemList = new List<WebItemDTO>();
            else
                webItemList = managerCollection.GetAllProviders().Select(item => new WebItemDTO { Name = item.SPSiteName, Url = item.SPSiteURL }).ToList();
            hdnWebItemCollection.Value = webItemList.ToJSON();
            this.Controls.Add(hdnWebItemCollection);

            hdnWebItem = new HiddenField()
            {
                ClientIDMode = ClientIDMode.Static,
                ID = "web-item"
            };
            this.Controls.Add(hdnWebItem);

            Label lblSPWeb = new Label()
            {
                Text = version2.SharePointLibraryExtension.Plugin.SPSiteMsg(),
                CssClass = "field-item-header"
            };
            tbWebUrl = new TextBox
            {
                ClientIDMode = ClientIDMode.Static,
                ID = "web-item-url"
            };
            this.Controls.Add(lblSPWeb);
            this.Controls.Add(tbWebUrl);
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
            return tbWebUrl.Text;
        }

        public void SetConfigurationPropertyValue(object value)
        {
            if (value != null)
            {
                tbWebUrl.Text = value.ToString();
                hdnWebItem.Value = webItemList.FirstOrDefault(item => item.Url == value.ToString()).ToJSON();
            }
        }
        #endregion

        private void InitManagerCollection()
        {
            var plugin = IntegrationManagerPlugin.Plugin;
            if (plugin != null)
            {
                managerCollection = new IntegrationProviders(plugin.Configuration.GetCustom(IntegrationManagerPlugin.PropertyId.SPObjectManager));
            }
        }
    }

    public static class JSONUtility
    {
        public static string ToJSON<T>(this T obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            MemoryStream memory = new MemoryStream();
            serializer.WriteObject(memory, obj);
            memory.Seek(0, SeekOrigin.Begin);
            return (new StreamReader(memory)).ReadToEnd();
        }

        public static T FromJSON<T>(this string JSON)
        {
            MemoryStream memory = new MemoryStream(Encoding.Default.GetBytes(JSON));
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            return (T)serializer.ReadObject(memory);
        }
    }
}
