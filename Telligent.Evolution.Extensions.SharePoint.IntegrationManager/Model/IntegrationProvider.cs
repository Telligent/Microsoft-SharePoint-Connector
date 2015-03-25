using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using System.Xml;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model
{
    public class IntegrationProvider
    {
        internal class EqualityComparer : IEqualityComparer<IntegrationProvider>
        {
            public bool Equals(IntegrationProvider provider1, IntegrationProvider provider2)
            {
                return provider1 != null && provider2 != null && provider1.SPSiteID == provider2.SPSiteID && provider1.SPWebID == provider2.SPWebID;
            }

            public int GetHashCode(IntegrationProvider provider)
            {
                return provider.SPSiteID != Guid.Empty && provider.SPWebID != Guid.Empty ? provider.SPSiteID.GetHashCode() ^ provider.SPWebID.GetHashCode() : provider.GetHashCode();
            }
        }

        private const string AuthenticationElement = "auth";
        private const string IdAttr = "id";
        private const string IntegrationManagerLoadError = "An exception in the process of loading SharePoint SiteCollection of type {0} has been occurred. The exception message is: {1}";
        private const string IntegrationManagerParseError = "An exception of type {0} occurred while parsing XML node for an Integration Manager. The exception message is: {1}";
        private const string IsDefaultAttr = "isdefault";
        private const string NameAttr = "name";
        private const string PartnershipElement = "Partnership";
        private const string SPSiteIDAttribute = "siteid";
        private const string SPSiteUrlElement = "spurl";
        private const string SPWebIDAttribute = "webid";
        private const string TEGroupNameElement = "tegroup";
        private static int nextId;

        private string siteUrl;

        public IntegrationProvider()
        {
            Authentication = new Anonymous();
            Id = (nextId++).ToString();
        }

        public IntegrationProvider(string spSiteUrl, int teGroupId, Authentication auth)
            : this()
        {
            Authentication = auth ?? new Anonymous();
            SPSiteURL = spSiteUrl;
            TEGroupId = teGroupId;
            Initialize();
        }

        public IntegrationProvider(XmlNode xmlNode)
        {
            LoadFromXml(xmlNode);
        }

        public IntegrationProvider(String xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var partnership = doc[PartnershipElement];
            if (partnership != null) LoadFromXml(partnership.FirstChild);
        }

        public string Id { get; private set; }
        public string SPSiteName { get; set; }
        public string SPSiteURL
        {
            get
            {
                return siteUrl;
            }
            set
            {
                siteUrl = value.Trim('/');
            }
        }
        public Guid SPSiteID { get; set; }
        public Guid SPWebID { get; set; }
        public string TEGroupName { get; set; }
        public int TEGroupId { get; set; }
        public bool IsDefault { get; set; }
        public string AuthName
        {
            get
            {
                return Authentication != null ? Authentication.Text : String.Empty;
            }
        }
        public Authentication Authentication { get; set; }

        public void Initialize()
        {
            try
            {
                var web = SPSite.OpenWeb(SPSiteURL, Authentication);

                SPSiteName = web.Title;
                SPWebID = web.WebId;
                SPSiteID = web.SiteId;
            }
            catch (Exception ex)
            {
                EventLogs.Warn(String.Format(IntegrationManagerLoadError, ex.GetType().Name, ex.Message), "Integration Manager", 468626, CSContext.Current.SettingsID);
            }

            TEGroupName = TEHelper.GetGroupName(TEGroupId);
        }

        public static bool TryParse(XmlNode xmlNode, out IntegrationProvider manager)
        {
            manager = null;

            try
            {
                manager = new IntegrationProvider(xmlNode);
                return true;
            }
            catch (Exception ex)
            {
                SPLog.DataProvider(ex, String.Format(IntegrationManagerParseError, ex.GetType().Name, ex.Message));
            }

            return false;
        }

        public string ToXml()
        {
            var doc = new XmlDocument();

            XmlElement xmlRoot = doc.CreateElement(PartnershipElement);
            doc.AppendChild(xmlRoot);
            ToXml(xmlRoot);

            return doc.OuterXml;
        }

        public void ToXml(XmlNode node)
        {
            XmlDocument doc = node.OwnerDocument;

            if (doc == null)
                return;

            XmlElement xmlProvider = doc.CreateElement(PartnershipElement);
            xmlProvider.SetAttribute(IdAttr, Id);
            xmlProvider.SetAttribute(NameAttr, SPSiteName);
            xmlProvider.SetAttribute(IsDefaultAttr, IsDefault.ToString());

            XmlNode spSiteURLNode = doc.CreateElement(SPSiteUrlElement);

            XmlAttribute spSiteIdAttr = doc.CreateAttribute(SPSiteIDAttribute);
            spSiteIdAttr.Value = SPSiteID.ToString();

            if (spSiteURLNode.Attributes == null)
                return;

            spSiteURLNode.Attributes.Append(spSiteIdAttr);

            XmlAttribute spWebIdAttr = doc.CreateAttribute(SPWebIDAttribute);
            spWebIdAttr.Value = SPWebID.ToString();
            spSiteURLNode.Attributes.Append(spWebIdAttr);

            spSiteURLNode.InnerText = SPSiteURL;
            xmlProvider.AppendChild(spSiteURLNode);

            XmlNode teGroupNameNode = doc.CreateElement(TEGroupNameElement);
            XmlAttribute teGroupIdAttr = doc.CreateAttribute(IdAttr);
            teGroupIdAttr.Value = TEGroupId.ToString();

            if (teGroupNameNode.Attributes == null)
                return;

            teGroupNameNode.Attributes.Append(teGroupIdAttr);
            teGroupNameNode.InnerText = TEGroupName;
            xmlProvider.AppendChild(teGroupNameNode);

            XmlNode auth = doc.CreateElement(AuthenticationElement);
            auth.InnerText = Authentication.ToQueryString();

            xmlProvider.AppendChild(auth);
            node.AppendChild(xmlProvider);
        }

        public static void Merge(IntegrationProvider oldvalue, IntegrationProvider newvalue)
        {
            PropertyInfo[] properties = typeof(IntegrationProvider).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (property.CanRead && property.CanWrite)
                    property.SetValue(oldvalue, property.GetValue(newvalue, null), null);
            }
        }

        private void LoadFromXml(XmlNode xmlNode)
        {
            try
            {
                Id = (xmlNode.Attributes != null) ? xmlNode.Attributes[IdAttr].Value : string.Empty;
                SPSiteName = (xmlNode.Attributes != null) ? xmlNode.Attributes[NameAttr].Value : string.Empty;

                var spSiteUrlElement = xmlNode[SPSiteUrlElement];

                SPSiteURL = spSiteUrlElement != null ? HttpUtility.UrlDecode(spSiteUrlElement.InnerText) : String.Empty;
                SPSiteID = spSiteUrlElement != null && spSiteUrlElement.Attributes[SPSiteIDAttribute] != null ? Guid.Parse(spSiteUrlElement.Attributes[SPSiteIDAttribute].Value) : Guid.Empty;
                SPWebID = spSiteUrlElement != null && spSiteUrlElement.Attributes[SPWebIDAttribute] != null ? Guid.Parse(spSiteUrlElement.Attributes[SPWebIDAttribute].Value) : Guid.Empty;

                var teGroupNameElement = xmlNode[TEGroupNameElement];

                TEGroupName = teGroupNameElement != null ? teGroupNameElement.InnerText : String.Empty;
                TEGroupId = teGroupNameElement != null && teGroupNameElement.Attributes[IdAttr] != null ? int.Parse(teGroupNameElement.Attributes[IdAttr].Value) : -1;

                Authentication = new Anonymous();
                var authenticationElement = xmlNode[AuthenticationElement];

                if (authenticationElement != null)
                {
                    Authentication = AuthenticationHelper.FromQueryString(authenticationElement.InnerText);
                }

                IsDefault = xmlNode.Attributes != null && xmlNode.Attributes[IsDefaultAttr] != null && bool.Parse(xmlNode.Attributes[IsDefaultAttr].Value);

                nextId = Math.Max(nextId, int.Parse(Id) + 1);
            }
            catch (Exception ex)
            {
                SPLog.DataProvider(ex, ex.Message);
            }
        }
    }
}