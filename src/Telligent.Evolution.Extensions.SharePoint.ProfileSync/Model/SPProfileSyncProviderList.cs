using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model
{
    public class SPProfileSyncProviderList
    {
        private const string SPProfileSyncSettingsElement = "SPProfileSyncSettings";
        private readonly List<SPProfileSyncProvider> settingsCollection;

        public SPProfileSyncProviderList()
        {
            settingsCollection = new List<SPProfileSyncProvider>();
        }

        public SPProfileSyncProviderList(String xml)
            : this()
        {
            if (String.IsNullOrEmpty(xml)) return;

            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(xml);

                if (!doc.HasChildNodes) return;

                XmlNode spProfileSyncSettingsListXml = doc[SPProfileSyncSettingsElement];
                if (spProfileSyncSettingsListXml == null) return;

                foreach (XmlNode spProfileSyncSettingsXml in spProfileSyncSettingsListXml.ChildNodes)
                {
                    SPProfileSyncProvider settings;
                    if (SPProfileSyncProvider.TryParse(spProfileSyncSettingsXml, out settings))
                    {
                        settingsCollection.Add(settings);
                    }
                }
            }
            catch (Exception ex)
            {
                SPLog.SiteSettingsInvalidXML(ex, String.Format("An exception of type {0} occurred while parsing XML node for a profile sync settings list. The exception message is: {1}", ex.GetType().Name, ex.Message));
            }
        }

        public string ToXml()
        {
            var doc = new XmlDocument();
            XmlElement providersElement = doc.CreateElement(SPProfileSyncSettingsElement);
            doc.AppendChild(providersElement);
            settingsCollection.ForEach(item => item.ToXml(providersElement));
            return doc.OuterXml;
        }

        public List<SPProfileSyncProvider> All()
        {
            return settingsCollection;
        }

        public SPProfileSyncProvider Get(int id)
        {
            return settingsCollection.FirstOrDefault(item => item.Id == id);
        }

        public SPProfileSyncProvider Get(string id)
        {
            int intId;
            if (int.TryParse(id, out intId))
            {
                return Get(intId);
            }
            return null;
        }

        public void Remove(int id)
        {
            settingsCollection.RemoveAll(item => item.Id == id);
        }

        public void Remove(string id)
        {
            int intId;
            if (int.TryParse(id, out intId))
            {
                Remove(intId);
            }
        }

        public void Add(SPProfileSyncProvider manager)
        {
            Remove(manager.Id);
            settingsCollection.Add(manager);
        }
    }
}
