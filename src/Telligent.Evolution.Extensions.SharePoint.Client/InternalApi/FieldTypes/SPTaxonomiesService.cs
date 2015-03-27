using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Telligent.Evolution.Extensions.SharePoint.WebServices;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    public class Term
    {
        public Term(Guid id, string name)
        {
            WSSId = -1;
            Id = id;
            Name = name;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public bool HasChilds { get; set; }
        public int WSSId { get; set; }
    }

    internal interface ITaxonomiesService
    {
        List<Term> Terms(string url, Guid sspId, int lcid, Guid termSetId);
        List<Term> Terms(string url, Guid sspId, int lcid, Guid termSetId, Guid termId);
        List<Term> GetCreateKeywords(string url, int lcid, IEnumerable<string> labels);
    }

    internal class SPTaxonomiesService : ITaxonomiesService
    {
        private readonly ICredentialsManager credentials;

        public SPTaxonomiesService() : this(ServiceLocator.Get<ICredentialsManager>()) { }

        public SPTaxonomiesService(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public List<Term> Terms(string url, Guid sspId, int lcid, Guid termSetId)
        {
            using (var tax = new TaxonomyService(url, credentials.Get(url)))
            {
                return ParseXml(tax.GetChildTermsInTermSet(sspId, lcid, termSetId)).ToList();
            }
        }

        public List<Term> Terms(string url, Guid sspId, int lcid, Guid termSetId, Guid termId)
        {
            using (var tax = new TaxonomyService(url, credentials.Get(url)))
            {
                return ParseXml(tax.GetChildTermsInTerm(sspId, lcid, termId, termSetId)).ToList();
            }
        }

        public List<Term> GetCreateKeywords(string url, int lcid, IEnumerable<string> labels)
        {
            var terms = new List<Term>();
            using (var tax = new TaxonomyService(url, credentials.Get(url)))
            {
                foreach (var label in labels)
                {
                    var termDefinitionXml = tax.GetTermsByLabel(label, lcid, WebServices.TaxonomyClientService.StringMatchOption.ExactMatch, resultCollectionSize: 1, termIds: string.Empty, addIfNotFound: true);
                    terms.Add(ParseXml(termDefinitionXml).FirstOrDefault());
                }
            }
            return terms;
        }

        private static IEnumerable<Term> ParseXml(string xml)
        {
            var xdoc = new XmlDocument();
            xdoc.LoadXml(xml);
            var termSet = xdoc.GetElementsByTagName("T");
            return from XmlNode termDefinition in termSet
                   let termId = termDefinition.Attributes["a9"]
                   where termId != null && !string.IsNullOrEmpty(termId.Value)
                   let wssId = termDefinition.Attributes["a1000"]
                   let termDefinitionTL = termDefinition.SelectSingleNode("LS/TL[@a32]")
                   where termDefinitionTL != null
                   let termName = termDefinitionTL.Attributes["a32"]
                   where termName != null && !string.IsNullOrEmpty(termName.Value)
                   select new Term(new Guid(termId.Value), termName.Value)
                   {
                       WSSId = wssId != null && !string.IsNullOrEmpty(wssId.Value) ? int.Parse(wssId.Value) : -1,
                       HasChilds = termDefinition.SelectSingleNode("TMS/TM[@a69='true']") != null
                   };
        }
    }
}
