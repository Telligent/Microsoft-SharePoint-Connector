using System;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Xml;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;

namespace Telligent.Evolution.Extensions.SharePoint.Components.Controls
{
    public class ModalPage : Page
    {
        protected const string PageHeadersWrapper = "headers";

        protected virtual XmlNode PageHeaders
        {
            get
            {
                var headersXml = (new XmlDocument()).CreateElement(PageHeadersWrapper);
                var pageHeaders = new StringBuilder();
                pageHeaders.Append(CSControlUtility.Instance().GetJQueryScriptTag(JQueryScript.JQuery));
                pageHeaders.Append(CSControlUtility.Instance().GetJQueryScriptTag(JQueryScript.JQueryEvolution));
                pageHeaders.Append(CSControlUtility.Instance().GetJQueryScriptTag(JQueryScript.JQueryGlow));
                pageHeaders.Append(CSControlUtility.Instance().GetJQueryScriptTag(JQueryScript.JQueryValidate));
                headersXml.InnerXml = pageHeaders.ToString();
                return headersXml;
            }
        }

        protected void AddHeader(XmlNode headerXml)
        {
            var headerControl = new HtmlGenericControl(headerXml.Name);

            if (headerXml.Attributes == null) return;

            foreach (XmlAttribute attr in headerXml.Attributes)
            {
                headerControl.Attributes[attr.Name] = attr.Value;
            }

            headerControl.InnerHtml = headerXml.InnerXml;
            Header.Controls.Add(headerControl);
        }

        protected override void OnInit(EventArgs e)
        {
            CSContext.Current.IsModal = true;
            foreach (XmlNode headerXml in PageHeaders.ChildNodes)
            {
                AddHeader(headerXml);
            }
            base.OnInit(e);
        }
    }
}
