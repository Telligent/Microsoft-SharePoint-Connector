using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Xml;
using Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil;
using Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil.Methods;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class SearchProvider
    {
        // Xml Constants
        private const string ProviderElement = "Provider";
        private const string IdAttr = "id";
        private const string NameAttr = "name";
        private const string AuthenticationElement = "Authentication";
        private const string OSDXURLElement = "OSDX_URL";
        private const string OpenSearchUrlElement = "OpenSearchUrl";
        private const string MoreLinkTemplateElement = "MoreLinkTemplate";
        private const string ShowMoreResultsElement = "ShowMoreResults";

        private static int nextId;

        public SearchProvider()
        {
            Authentication = new Anonymous();
            MoreResultsUrl = String.Empty;
            Id = (nextId++).ToString(CultureInfo.InvariantCulture);
        }

        public SearchProvider(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var xmlElement = doc[ProviderElement];
            if (xmlElement != null)
                LoadFromXml(xmlElement.FirstChild);
        }

        public SearchProvider(XmlNode xmlProvider)
        {
            LoadFromXml(xmlProvider);
        }

        public SearchProvider(NameValueCollection queryString)
        {
            Id = queryString["id"];
            Name = queryString["providername"];
            Authentication = AuthenticationHelper.FromQueryString(queryString.ToString());
        }

        public string Id { get; private set; }
        public string MoreLinkTemplate { get; private set; }
        public bool CanShowMoreResults { get; private set; }
        public string Name { get; set; }
        public string MoreResultsUrl { get; private set; }
        public string OpenSearchUrl { get; private set; }
        public string AuthName
        {
            get
            {
                return Authentication != null ? Authentication.Text : String.Empty;
            }
        }
        public Authentication Authentication { get; set; }

        public void InitXml(XmlNode node)
        {
            XmlDocument doc = node.OwnerDocument ?? node as XmlDocument;
            if (doc == null)
                return;

            XmlElement xmlProvider = doc.CreateElement(ProviderElement);
            xmlProvider.SetAttribute(IdAttr, Id);
            xmlProvider.SetAttribute(NameAttr, Name);

            XmlNode osdxURLNode = doc.CreateElement(OSDXURLElement);
            osdxURLNode.InnerText = MoreResultsUrl;
            xmlProvider.AppendChild(osdxURLNode);

            XmlNode openSearchUrlNode = doc.CreateElement(OpenSearchUrlElement);
            openSearchUrlNode.InnerText = OpenSearchUrl;
            xmlProvider.AppendChild(openSearchUrlNode);

            XmlNode moreLinkTemplateNode = doc.CreateElement(MoreLinkTemplateElement);
            moreLinkTemplateNode.InnerText = MoreLinkTemplate;
            xmlProvider.AppendChild(moreLinkTemplateNode);

            XmlNode showMoreResultsNode = doc.CreateElement(ShowMoreResultsElement);
            showMoreResultsNode.InnerText = CanShowMoreResults.ToString(CultureInfo.InvariantCulture);
            xmlProvider.AppendChild(showMoreResultsNode);

            XmlNode auth = doc.CreateElement(AuthenticationElement);
            auth.InnerText = Authentication.ToQueryString();

            xmlProvider.AppendChild(auth);
            node.AppendChild(xmlProvider);
        }

        public String ToXml()
        {
            var doc = new XmlDocument();
            XmlElement xmlRoot = doc.CreateElement(ProviderElement);
            doc.AppendChild(xmlRoot);
            InitXml(xmlRoot);
            return doc.OuterXml;
        }

        #region Utility methods
        public void ProcessOSDXFile(string value)
        {
            var osdxFile = new XmlDocument();
            if (!String.IsNullOrEmpty(value))
            {
                try
                {
                    osdxFile.LoadXml(value);
                    string osdxURL;
                    string openSearchUrl;
                    string moreLinkTemplate;
                    Parse_OSDX_File(osdxFile, out osdxURL, out openSearchUrl, out moreLinkTemplate);
                    MoreResultsUrl = osdxURL;
                    OpenSearchUrl = openSearchUrl;
                    MoreLinkTemplate = moreLinkTemplate;
                    CanShowMoreResults = !String.IsNullOrEmpty(osdxURL);
                }
                catch (Exception e)
                {
                    throw new FormatException("Xml file is invalid!", e);
                }
            }
        }

        private void Parse_OSDX_File(XmlDocument osdxFile, out string osdxURL, out string openSearchUrl, out string moreLinkTemplate)
        {
            osdxURL = null;
            openSearchUrl = null;
            moreLinkTemplate = null;

            var nsmgr = new XmlNamespaceManager(osdxFile.NameTable);
            nsmgr.AddNamespace("ns", "http://a9.com/-/spec/opensearch/1.1/");
            nsmgr.AddNamespace("sc", "http://schemas.microsoft.com/Search/2007/location");

            XmlNode osdxURLNode = osdxFile.SelectSingleNode("ns:OpenSearchDescription/ns:Url[@type='text/html']", nsmgr);
            if (osdxURLNode != null && osdxURLNode.Attributes != null)
            {
                osdxURL = HttpUtility.HtmlDecode(osdxURLNode.Attributes["template"].Value);
            }

            XmlNode openSearchUrlNode = osdxFile.SelectSingleNode("ns:OpenSearchDescription/ns:Url[@type='application/rss+xml']", nsmgr);
            if (openSearchUrlNode != null && openSearchUrlNode.Attributes != null)
            {
                openSearchUrl = HttpUtility.HtmlDecode(openSearchUrlNode.Attributes["template"].Value);
            }

            XmlNode moreLinkTemplateNode = osdxFile.SelectSingleNode("ns:OpenSearchDescription/sc:MoreLinkTemplate", nsmgr);
            if (moreLinkTemplateNode != null)
            {
                moreLinkTemplate = moreLinkTemplateNode.InnerText;
            }
        }

        private void LoadFromXml(XmlNode xmlProvider)
        {
            try
            {
                if (xmlProvider.Attributes != null)
                {
                    Id = xmlProvider.Attributes[IdAttr].Value;
                    Name = xmlProvider.Attributes[NameAttr].Value;
                }
                var osdxURLXml = xmlProvider[OSDXURLElement];
                if (osdxURLXml != null)
                {
                    MoreResultsUrl = HttpUtility.UrlDecode(osdxURLXml.InnerText);
                }

                var openSearchUrlXml = xmlProvider[OpenSearchUrlElement];
                if (openSearchUrlXml != null)
                {
                    OpenSearchUrl = HttpUtility.UrlDecode(openSearchUrlXml.InnerText);
                }

                var moreLinkTemplateXml = xmlProvider[MoreLinkTemplateElement];
                if (moreLinkTemplateXml != null)
                {
                    MoreLinkTemplate = HttpUtility.UrlDecode(moreLinkTemplateXml.InnerText);
                }

                var showMoreResultsXml = xmlProvider[ShowMoreResultsElement];
                if (showMoreResultsXml != null)
                {
                    CanShowMoreResults = bool.Parse(showMoreResultsXml.InnerText);
                }

                Authentication = new Anonymous();
                var authenticationXml = xmlProvider[AuthenticationElement];
                if (authenticationXml != null)
                {
                    Authentication = AuthenticationHelper.FromQueryString(authenticationXml.InnerText);
                }
                nextId = Math.Max(nextId, int.Parse(Id) + 1);
            }
            catch (Exception e)
            {
                throw new FormatException(e.Message, e);
            }
        }
        #endregion
    }
}
