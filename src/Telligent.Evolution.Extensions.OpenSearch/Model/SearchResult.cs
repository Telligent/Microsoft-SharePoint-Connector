using System;
using System.Xml;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class SearchResult
    {
        public SearchResult() { }
        public SearchResult(XmlNode node)
        {
            Title = node["title"] != null ? node["title"].InnerText : String.Empty;
            Link = node["link"] != null ? node["link"].InnerText : String.Empty;
            Description = node["description"] != null ? node["description"].InnerText : String.Empty;
            Author = node["author"] != null ? node["author"].InnerText : String.Empty;
            DateTime date;
            if (node["pubDate"] != null && DateTime.TryParse(node["pubDate"].InnerText, out date))
            {
                PubDate = date;
            }
            else
            {
                PubDate = null;
            }
            FileSize = ParseFileSize(node["search:size"] != null ? node["search:size"].InnerText : "0");
            FileExtension = node["search:dotfileextension"] != null ? node["search:dotfileextension"].InnerText : String.Empty;
            HighlightedSummary = node["search:hithighlightedsummary"] != null ? node["search:hithighlightedsummary"].InnerText : String.Empty;
        }

        public String Title { get; private set; }
        public String Link { get; private set; }
        public String Description { get; private set; }
        public String Author { get; private set; }
        public DateTime? PubDate { get; private set; }
        public String FileExtension { get; private set; }
        public String HighlightedSummary { get; private set; }
        public int FileSize { get; private set; }

        private int ParseFileSize(string data)
        {
            const int DefaultSize = 0;
            const int KB_Value = 1024;
            int fileSize = DefaultSize;
            if (int.TryParse(data, out fileSize))
            {
                return fileSize / KB_Value;
            }
            return DefaultSize;
        }
    }
}
