using System;
using System.Globalization;
using System.Web;
using System.Xml;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class SearchWidgetConfiguration
    {
        #region xml constants
        const string RootElement = "widget";
        const string NameAttr = "name";
        const string ProviderIdAttr = "id";
        const string ResultsAttr = "results";
        const string ShowMoreAttr = "more";
        const string TextOnlyAttr = "textonly";
        #endregion

        public string Name { get; set; }
        public string ProviderId { get; set; }
        public int ResultsPerPage { get; set; }
        public bool ShowMoreResultsLink { get; set; }
        public bool TextOnlyResults { get; set; }

        public SearchWidgetConfiguration() { }

        public SearchWidgetConfiguration(string xml)
        {
            if (String.IsNullOrEmpty(xml))
                return;

            var data = new XmlDocument();
            try
            {
                data.LoadXml(xml);
                XmlElement rootXml = data[RootElement];
                if(rootXml == null)
                    throw new Exception();

                Name = HttpUtility.HtmlEncode(rootXml.Attributes[NameAttr].Value);
                ProviderId = rootXml.Attributes[ProviderIdAttr].Value;
                ResultsPerPage = int.Parse(rootXml.Attributes[ResultsAttr].Value);
                ShowMoreResultsLink = bool.Parse(rootXml.Attributes[ShowMoreAttr].Value);
                TextOnlyResults = rootXml.Attributes[TextOnlyAttr] != null && bool.Parse(rootXml.Attributes[TextOnlyAttr].Value);
            }
            catch (Exception e)
            {
                // format of xml is inavalid
                throw new FormatException(e.Message, e);
            }
        }

        public string ToXml()
        {
            var data = new XmlDocument();
            XmlElement root = data.CreateElement(RootElement);
            XmlAttribute name = data.CreateAttribute(NameAttr);
            name.Value = Name;

            XmlAttribute providerId = data.CreateAttribute(ProviderIdAttr);
            providerId.Value = ProviderId;

            XmlAttribute resultPerPage = data.CreateAttribute(ResultsAttr);
            resultPerPage.Value = ResultsPerPage.ToString(CultureInfo.InvariantCulture);

            XmlAttribute showMoreResultsLink = data.CreateAttribute(ShowMoreAttr);
            showMoreResultsLink.Value = ShowMoreResultsLink.ToString(CultureInfo.InvariantCulture);

            XmlAttribute textonlyResults = data.CreateAttribute(TextOnlyAttr);
            textonlyResults.Value = TextOnlyResults.ToString(CultureInfo.InvariantCulture);

            root.Attributes.Append(name);
            root.Attributes.Append(providerId);
            root.Attributes.Append(resultPerPage);
            root.Attributes.Append(showMoreResultsLink);
            root.Attributes.Append(textonlyResults);
            data.AppendChild(root);

            return data.InnerXml;
        }
    }
}
