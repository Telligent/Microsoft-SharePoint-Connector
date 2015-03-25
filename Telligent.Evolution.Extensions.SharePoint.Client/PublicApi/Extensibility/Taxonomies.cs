using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public interface ITaxonomies : ICacheable
    {
        List<Term> Terms(string url, Guid sspId, int lcid, Guid termSetId);
        List<Term> Terms(string url, Guid sspId, int lcid, Guid termSetId, Guid termId);
        List<Term> GetCreateKeywords(string url, int lcid, IEnumerable<string> labels);
        int GetWSSId(string url, string label);
    }

    public class Taxonomies : ITaxonomies
    {
        private readonly ICredentialsManager credentials;
        private readonly ICacheService cacheService;
        private readonly ITaxonomiesService taxonomies;

        public Taxonomies() : this(ServiceLocator.Get<ICredentialsManager>(), ServiceLocator.Get<ITaxonomiesService>(), ServiceLocator.Get<ICacheService>()) { }

        internal Taxonomies(ICredentialsManager credentials, ITaxonomiesService taxonomies, ICacheService cacheService)
        {
            this.credentials = credentials;
            this.taxonomies = taxonomies;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        public List<Term> Terms(string url, Guid sspId, int lcid, Guid termSetId)
        {
            var cacheId = string.Concat("Taxonomies", sspId.ToString("N"), lcid, termSetId.ToString("N"));
            var terms = (List<Term>)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (terms == null)
            {
                terms = taxonomies.Terms(url, sspId, lcid, termSetId);
                cacheService.Put(cacheId, terms, CacheScope.Context | CacheScope.Process, new[] { Tag(termSetId) }, CacheTimeOut);
            }
            return terms;
        }

        public List<Term> Terms(string url, Guid sspId, int lcid, Guid termSetId, Guid termId)
        {
            var cacheId = string.Concat("Taxonomies", sspId.ToString("N"), lcid, termSetId.ToString("N"), termId.ToString("N"));
            var terms = (List<Term>)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (terms == null)
            {
                terms = taxonomies.Terms(url, sspId, lcid, termSetId, termId);
                cacheService.Put(cacheId, terms, CacheScope.Context | CacheScope.Process, new[] { Tag(termSetId) }, CacheTimeOut);
            }
            return terms;
        }

        public List<Term> GetCreateKeywords(string url, int lcid, IEnumerable<string> labels)
        {
            return taxonomies.GetCreateKeywords(url, lcid, labels);
        }

        public int GetWSSId(string url, string label)
        {
            using (var context = new SPContext(url, credentials.Get(url)))
            {
                try
                {
                    var taxonomyList = context.Web.Lists.GetByTitle("TaxonomyHiddenList");
                    var taxItems = taxonomyList.GetItems(CAMLQueryBuilder.GetItemByTitle(label, new[] {"Title", "ID"}));
                    context.Load(taxItems);

                    context.ExecuteQuery();

                    if (taxItems.Any())
                    {
                        return taxItems[0].Id;
                    }
                }
                catch (Exception ex)
                {
                    SPLog.UnKnownError(ex, ex.Message);
                }
            }

            return -1;
        }

        public static string Tag(Guid termSetId)
        {
            return string.Concat("Taxonomies", termSetId.ToString("N"));
        }
    }
}
