using Microsoft.SharePoint.Client.Taxonomy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Term = Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Term;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class TaxonomiesExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v2_taxonomies"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ITaxonomies>(); }
        }

        public string Name
        {
            get { return "Taxonomies (sharepoint_v2_taxonomies)"; }
        }

        public string Description
        {
            get { return "Allows to work with SharePoint Taxonomies."; }
        }

        public void Initialize() { }
    }

    public interface ITaxonomies
    {
        ApiList<Term> Terms(string url, Guid sspId, Guid termSetId);
        ApiList<Term> Terms(string url, Guid sspId, Guid termSetId, Guid termId);
        TaxonomyField ParseFieldSchemaXml(string schemaXml);
        ApiList<Term> GetCreateKeywords(string url, string labels);
        ApiList<Term> ParseFieldValue(string url, object fieldValue);
    }

    public class TaxonomyField
    {
        public Guid SSPId { get; set; }
        public Guid TermSetId { get; set; }
        public Guid TextFieldId { get; set; }
        public bool AllowMultiple { get; set; }
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class Taxonomies : ITaxonomies
    {
        public ApiList<Term> Terms(string url, Guid sspId, Guid termSetId)
        {
            return new ApiList<Term>(PublicApi.Taxonomies.Terms(url, sspId, GetUserCultureLCID(), termSetId));
        }

        public ApiList<Term> Terms(string url, Guid sspId, Guid termSetId, Guid termId)
        {
            return new ApiList<Term>(PublicApi.Taxonomies.Terms(url, sspId, GetUserCultureLCID(), termSetId, termId));
        }

        public TaxonomyField ParseFieldSchemaXml(string schemaXml)
        {
            var taxonomy = new TaxonomyField();

            var readerSettings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
            using (var xmlReader = XmlReader.Create(new StringReader(schemaXml), readerSettings))
            {
                var xpathDocument = new XPathDocument(xmlReader);
                var nav = xpathDocument.CreateNavigator();

                var allowMultipleValues = false;
                var allowMultipleValuesNode = nav.SelectSingleNode(@"/Field");
                if (allowMultipleValuesNode != null && bool.TryParse(allowMultipleValuesNode.GetAttribute("Mult", ""), out allowMultipleValues))
                {
                    taxonomy.AllowMultiple = allowMultipleValues;
                }

                Guid sspId;
                var sspIdPropertyNode = nav.SelectSingleNode(@"/Field/Customization/ArrayOfProperty/Property[Name='SspId']/Value");
                if (sspIdPropertyNode != null && Guid.TryParse(sspIdPropertyNode.Value, out sspId))
                {
                    taxonomy.SSPId = sspId;
                }

                Guid termSetId;
                var termSetIdPropertyNode = nav.SelectSingleNode(@"/Field/Customization/ArrayOfProperty/Property[Name='TermSetId']/Value");
                if (termSetIdPropertyNode != null && Guid.TryParse(termSetIdPropertyNode.Value, out termSetId))
                {
                    taxonomy.TermSetId = termSetId;
                }

                Guid textFieldId;
                var textFieldPropertyNode = nav.SelectSingleNode(@"/Field/Customization/ArrayOfProperty/Property[Name='TextField']/Value");
                if (textFieldPropertyNode != null && Guid.TryParse(textFieldPropertyNode.Value, out textFieldId))
                {
                    taxonomy.TextFieldId = textFieldId;
                }
            }

            return taxonomy;
        }

        public ApiList<Term> GetCreateKeywords(string url, string labels)
        {
            var termLabels = HandleLabels(labels);
            return new ApiList<Term>(PublicApi.Taxonomies.GetCreateKeywords(url, GetUserCultureLCID(), termLabels));
        }
        
        public ApiList<Term> ParseFieldValue(string url, object fieldValue)
        {
            var terms = new ApiList<Term>();
            if (fieldValue != null)
            {
                Guid id;
                if (fieldValue is IEnumerable<object>)
                {
                    foreach (var term in ((IEnumerable<object>)fieldValue).Where(_ => _ != null).Select(t => ParseTerm(url,t)))
                    {
                        if (term != null)
                            terms.Add(term);
                    }
                }
                else
                {
                    var term = ParseTerm(url, fieldValue);
                    if (term != null)
                        terms.Add(term);
                }
            }
            return terms;
        }

        private IEnumerable<string> HandleLabels(string labels)
        {
            return labels.Replace(';', ',').Split(',').Where(label => !string.IsNullOrWhiteSpace(label)).Select(label => label.Trim());
        }

        private static Term ParseTerm(string url, object term)
        {
            if (term is TaxonomyFieldValue)
            {
                var taxonomyTerm = (TaxonomyFieldValue)term;
                return new Term(Guid.Parse(taxonomyTerm.TermGuid), taxonomyTerm.Label)
                {
                    WSSId = taxonomyTerm.WssId
                };
            }
            else
            {
                Guid id;
                var termNameIdPair = term.ToString().Split('|');
                if (termNameIdPair.Length == 2
                    && !string.IsNullOrEmpty(termNameIdPair[0])
                    && Guid.TryParse(termNameIdPair[1], out id))
                {
                    var label = termNameIdPair[0];
                    var wssId = PublicApi.Taxonomies.GetWSSId(url, label);
                    return new Term(id, label) { WSSId = wssId};
                }
                    
                return null;
            }
        }

        private int GetUserCultureLCID()
        {
            var user = Extensibility.Api.Version1.PublicApi.Users.AccessingUser;
            var userCulture = user != null ? new CultureInfo(user.Language) : CultureInfo.CurrentUICulture;
            return userCulture.LCID;
        }
    }
}
