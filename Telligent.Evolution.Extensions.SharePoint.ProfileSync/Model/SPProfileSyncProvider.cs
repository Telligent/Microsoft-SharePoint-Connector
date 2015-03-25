using System;
using System.Globalization;
using System.IO;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model
{
    public class SPProfileSyncProvider
    {
        #region xml constants
        private const string ProfileSyncElement = "ProfileSync";
        private const string IdAttr = "id";
        private const string SPSiteUrlElement = "spurl";
        private const string SPUserElement = "spuser";
        private const string SPUserIdAttr = "id";
        private const string SPUserEmailAttr = "email";
        private const string SPFarmUserEmailAttr = "farmemail";
        private const string AuthenticationElement = "auth";
        #endregion

        private static int nextId;

        private SPProfileSyncProvider()
        {
            Authentication = new Anonymous();
            Id = nextId++;
        }

        public SPProfileSyncProvider(string spSiteUrl, string spUserIdFieldName, string spUserEmailFieldName, string spFarmUserEmailFieldName, Authentication auth)
            : this()
        {
            Authentication = auth ?? new Anonymous();
            SPSiteURL = spSiteUrl;
            SPUserIdFieldName = spUserIdFieldName;
            SPUserEmailFieldName = spUserEmailFieldName;
            SPFarmUserEmailFieldName = spFarmUserEmailFieldName;
        }

        public int Id { get; private set; }
        public string SPUserIdFieldName { get; set; }
        public string SPUserEmailFieldName { get; set; }
        public string SPFarmUserIdFieldName { get { return "UserProfile_GUID"; } }
        public string SPFarmUserEmailFieldName { get; set; }
        public string SPSiteURL { get; set; }
        public Authentication Authentication { get; set; }
        public string AuthName
        {
            get
            {
                return Authentication != null ? Authentication.Text : String.Empty;
            }
        }

        public SPBaseConfig SyncConfig { get; set; }

        public string ToXml()
        {
            var doc = new XmlDocument();
            XmlElement xmlRoot = doc.CreateElement(ProfileSyncElement);
            doc.AppendChild(xmlRoot);
            ToXml(xmlRoot);
            return doc.OuterXml;
        }

        public void ToXml(XmlNode node)
        {
            XmlDocument doc = node.OwnerDocument;

            if (doc == null) return;

            XmlElement xmlProvider = doc.CreateElement(ProfileSyncElement);
            xmlProvider.SetAttribute(IdAttr, Id.ToString(CultureInfo.InvariantCulture));

            XmlNode spSiteURLNode = doc.CreateElement(SPSiteUrlElement);
            spSiteURLNode.InnerText = SPSiteURL;
            xmlProvider.AppendChild(spSiteURLNode);

            XmlNode spUserNode = doc.CreateElement(SPUserElement);
            XmlAttribute spUserIdAttr = doc.CreateAttribute(SPUserIdAttr);
            spUserIdAttr.Value = SPUserIdFieldName;

            if (spUserNode.Attributes != null)
            {
                spUserNode.Attributes.Append(spUserIdAttr);
            }

            XmlAttribute spUserEmailAttr = doc.CreateAttribute(SPUserEmailAttr);
            spUserEmailAttr.Value = SPUserEmailFieldName;
            if (spUserNode.Attributes != null)
            {
                spUserNode.Attributes.Append(spUserEmailAttr);
            }

            XmlAttribute spFarmUserEmailAttr = doc.CreateAttribute(SPFarmUserEmailAttr);
            spFarmUserEmailAttr.Value = SPFarmUserEmailFieldName;
            if (spUserNode.Attributes != null)
            {
                spUserNode.Attributes.Append(spFarmUserEmailAttr);
            }

            xmlProvider.AppendChild(spUserNode);

            XmlNode auth = doc.CreateElement(AuthenticationElement);
            auth.InnerText = Authentication.ToQueryString();
            xmlProvider.AppendChild(auth);

            XmlNode syncConfig = doc.CreateElement("config");
            var innerXml = Serialize(SyncConfig);
            syncConfig.InnerXml = innerXml;
            xmlProvider.AppendChild(syncConfig);

            node.AppendChild(xmlProvider);
        }

        public static bool TryParse(string xml, out SPProfileSyncProvider spProfileSyncSettings)
        {
            var doc = new XmlDocument();
            XmlNode xmlNode = null;
            try
            {
                doc.LoadXml(xml);
                var profileSyncXml = doc[ProfileSyncElement];
                if (profileSyncXml != null)
                {
                    xmlNode = profileSyncXml.FirstChild;
                }
            }
            catch (Exception ex)
            {
                SPLog.SiteSettingsInvalidXML(ex, String.Format("An exception of type {0} occurred while parsing XML for a profile sync settings. The exception message is: {1}", ex.GetType().Name, ex.Message));
            }
            return TryParse(xmlNode, out spProfileSyncSettings);
        }

        public static bool TryParse(XmlNode xmlNode, out SPProfileSyncProvider spProfileSyncSettings)
        {
            spProfileSyncSettings = new SPProfileSyncProvider();

            if (xmlNode == null || xmlNode.Attributes == null || xmlNode.Attributes.Count == 0 || xmlNode[SPUserElement] == null) return false;

            XmlNode userNode = xmlNode[SPUserElement];
            if (userNode == null) return false;

            XmlNode siteUrlNode = xmlNode[SPSiteUrlElement];
            if (siteUrlNode == null) return false;

            // try to get id
            int id;
            if (xmlNode.Attributes[IdAttr] != null && int.TryParse(xmlNode.Attributes[IdAttr].Value, out id))
            {
                nextId = Math.Max(nextId, id + 1);
                try
                {
                    spProfileSyncSettings = new SPProfileSyncProvider
                    {
                        Id = id,
                        SPSiteURL = HttpUtility.UrlDecode(siteUrlNode.InnerText),
                        SPUserIdFieldName = userNode.Attributes != null && userNode.Attributes[SPUserIdAttr] != null ? userNode.Attributes[SPUserIdAttr].Value : String.Empty,
                        SPUserEmailFieldName = userNode.Attributes != null && userNode.Attributes[SPUserEmailAttr] != null ? userNode.Attributes[SPUserEmailAttr].Value : String.Empty,
                        SPFarmUserEmailFieldName = userNode.Attributes != null && userNode.Attributes[SPFarmUserEmailAttr] != null ? userNode.Attributes[SPFarmUserEmailAttr].Value : String.Empty
                    };
                    var authXml = xmlNode[AuthenticationElement];
                    if (authXml != null)
                    {
                        spProfileSyncSettings.Authentication = AuthenticationHelper.FromQueryString(authXml.InnerText);
                    }

                    var xmlConfig = xmlNode["config"];
                    if (xmlConfig != null)
                    {
                        spProfileSyncSettings.SyncConfig = Deserialize<SPBaseConfig>(xmlConfig.InnerXml);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    SPLog.SiteSettingsInvalidXML(ex, String.Format("An exception of type {0} occurred while parsing XML node for a profile sync settings. The exception message is: {1}", ex.GetType().Name, ex.Message));
                    return false;
                }
            }
            return false;
        }

        #region Utility

        private static string Serialize<T>(T obj)
        {
            var serializer = new XmlSerializer(typeof(T), string.Empty);
            using (var mm = new MemoryStream())
            {
                using (var xw = XmlWriter.Create(mm, new XmlWriterSettings { OmitXmlDeclaration = true }))
                {
                    serializer.Serialize(xw, obj, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                }

                mm.Seek(0, new SeekOrigin());

                using (var sr = new StreamReader(mm))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static T Deserialize<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T), string.Empty);
            using (var reader = new StringReader(xml))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        #endregion
    }
}
