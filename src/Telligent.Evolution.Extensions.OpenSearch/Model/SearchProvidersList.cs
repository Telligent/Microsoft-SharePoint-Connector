using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class SearchProvidersList
    {
        const string ProvidersElement = "OpenSearchProviders";
        private readonly List<SearchProvider> providerList = new List<SearchProvider>();

        public SearchProvidersList(String xml)
        {
            if (String.IsNullOrEmpty(xml))
                return;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var providersXml = doc[ProvidersElement];
                if (providersXml != null)
                {
                    foreach (XmlNode xmlProvider in providersXml.ChildNodes)
                    {
                        providerList.Add(new SearchProvider(xmlProvider));
                    }
                }
            }
            catch (Exception e)
            {
                // format of xml is inavalid
                throw new FormatException(e.Message, e);
            }
        }

        public string ToXml()
        {
            var doc = new XmlDocument();
            XmlElement providersElement = doc.CreateElement(ProvidersElement);
            doc.AppendChild(providersElement);
            providerList.ForEach(provider => provider.InitXml(providersElement));
            return doc.OuterXml;
        }

        public List<SearchProvider> Get()
        {
            return providerList;
        }

        public SearchProvider Get(string id)
        {
            return providerList.FirstOrDefault(provider => provider.Id == id);
        }

        public void Remove(string id)
        {
            providerList.RemoveAll(provider => provider.Id == id);
        }

        public void Add(SearchProvider provider)
        {
            Remove(provider.Id);
            providerList.Add(provider);
        }
    }
}
