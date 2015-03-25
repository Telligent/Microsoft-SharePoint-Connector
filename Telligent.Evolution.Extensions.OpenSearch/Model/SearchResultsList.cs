using System;
using System.Collections.Generic;
using System.Xml;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class SearchResultsList
    {
        public List<SearchResult> Items { get; set; }

        private readonly XmlDocument xmlResults = new XmlDocument();

        public SearchResultsList() { }

        public SearchResultsList(string xml)
        {
            if (!String.IsNullOrEmpty(xml))
                xmlResults.LoadXml(xml);
        }

        public String GetTitle()
        {
            XmlNode title = xmlResults.SelectSingleNode("rss/channel/title");
            return title != null ? title.InnerText : String.Empty;
        }

        public int Count()
        {
            const string os = @"http://a9.com/-/spec/opensearch/1.1/";
            var nsmgr = new XmlNamespaceManager(xmlResults.NameTable);
            nsmgr.AddNamespace("os", os);
            XmlNode totalNumber = xmlResults.SelectSingleNode("rss/channel/os:totalResults", nsmgr);
            return totalNumber != null ? int.Parse(totalNumber.InnerText) : 0;
        }

        public List<SearchResult> GetItems()
        {
            XmlNodeList nodeList = xmlResults.SelectNodes("rss/channel/item");
            if (nodeList != null)
            {
                return LoadItemsFromNodeList(nodeList, nodeList.Count);
            }
            return new List<SearchResult>();
        }

        public List<SearchResult> GetItems(int count)
        {
            XmlNodeList nodeList = xmlResults.SelectNodes("rss/channel/item");
            return LoadItemsFromNodeList(nodeList, count);
        }

        private List<SearchResult> LoadItemsFromNodeList(XmlNodeList nodeList, int maxcount)
        {
            var results = new List<SearchResult>();
            for (int i = 0; i < maxcount && i < nodeList.Count; i++)
            {
                results.Add(new SearchResult(nodeList[i]));
            }
            return results;
        }
    }
}
