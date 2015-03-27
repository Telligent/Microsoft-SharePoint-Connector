using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model
{
    public class IntegrationProviders
    {
        private const string ProviderElement = "SPObjectManager";
        private const string AllProviders = "SPObjectManagerList_GetAllManagers";

        private static readonly TimeSpan CacheTimeOut = TimeSpan.FromMinutes(5);

        public IntegrationProviders()
        {
            Collection = new List<IntegrationProvider>();
        }

        public IntegrationProviders(String xml)
            : this()
        {
            if (String.IsNullOrEmpty(xml)) return;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var spObjectProviderListXml = doc[ProviderElement];

                if (spObjectProviderListXml == null) return;

                foreach (XmlNode spObjectProviderXml in spObjectProviderListXml.ChildNodes)
                {
                    IntegrationProvider spObjectProvider;
                    if (IntegrationProvider.TryParse(spObjectProviderXml, out spObjectProvider))
                    {
                        Collection.Add(spObjectProvider);
                    }
                }

            }
            catch (Exception ex)
            {
                SPLog.DataProvider(ex, ex.Message);
            }
        }

        public List<IntegrationProvider> Collection { get; private set; }

        public string ToXml()
        {
            var doc = new XmlDocument();
            var providersElement = doc.CreateElement(ProviderElement);

            doc.AppendChild(providersElement);
            Collection.ForEach(partnership => partnership.ToXml(providersElement));

            return doc.OuterXml;
        }

        public IntegrationProvider FindByUrl(string url)
        {
            var provider = (IntegrationProvider)CacheService.Get(CacheKey(url), CacheScope.All);

            if (provider == null)
            {
                provider = GetByUrl(url) ?? Default();
                CacheService.Put(CacheKey(url), provider, CacheScope.All, new[] { Tag() }, CacheTimeOut);
            }

            return provider;
        }

        public List<IntegrationProvider> GetAllProviders()
        {
            var fullProviderList = (List<IntegrationProvider>)CacheService.Get(CacheKey(AllProviders), CacheScope.All);

            if (fullProviderList == null)
            {
                fullProviderList = new List<IntegrationProvider>();

                foreach (var provider in Collection)
                {
                    fullProviderList.Add(provider);
                    LoadSubProviders(fullProviderList, provider);
                }

                fullProviderList = fullProviderList.Where(item => item != null).Distinct(new IntegrationProvider.EqualityComparer()).ToList();

                CacheService.Put(CacheKey(AllProviders), fullProviderList, CacheScope.All, new[] { Tag() }, CacheTimeOut);
            }

            return fullProviderList;
        }

        public IntegrationProvider GetByUrl(string url)
        {
            url = url.Trim('/');

            var match = Collection.FirstOrDefault(m => String.Equals(m.SPSiteURL, url, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                return match;
            }

            var matches = Collection.Where(m => url.StartsWith(m.SPSiteURL, StringComparison.OrdinalIgnoreCase));

            foreach (var provider in matches)
            {
                match = (match == null || match.SPSiteURL.Length < provider.SPSiteURL.Length) ? provider : match;
            }

            return (match != null) ? new IntegrationProvider(url, match.TEGroupId, match.Authentication) : null;
        }

        public IntegrationProvider GetById(string id)
        {
            return Collection.FirstOrDefault(provider => provider.Id == id);
        }

        public IntegrationProvider GetByGroupId(int groupId)
        {
            var provider = Collection.FirstOrDefault(c => c.TEGroupId == groupId);

            if (provider == null)
            {
                var mappedGroups = Collection.Select(c => c.TEGroupId).ToList();
                var group = TEHelper.GetGroupById(groupId);
                var relativeUrl = new StringBuilder();

                while (group.Id.HasValue && group.ParentGroupId >= 0 && !mappedGroups.Contains(group.Id.Value))
                {
                    relativeUrl.Insert(0, string.Format("{0}/", group.Key));
                    group = TEHelper.GetGroupById(group.ParentGroupId);
                }

                provider = Collection.FirstOrDefault(c => c.TEGroupId == group.Id);

                if (provider != null)
                {
                    provider.TEGroupId = groupId;
                    provider.SPSiteURL = string.Format("{0}/{1}", provider.SPSiteURL.TrimEnd('/'), relativeUrl);
                    provider.Initialize();
                    return provider;
                }
            }

            return provider;
        }

        public IntegrationProvider Default()
        {
            return Collection.FirstOrDefault(provider => provider.IsDefault);
        }

        public void Insert(IntegrationProvider provider)
        {
            var oldProvider = GetById(provider.Id);

            if (oldProvider != null)
            {
                IntegrationProvider.Merge(oldProvider, provider);
            }
            else
            {
                Collection.Add(provider);
            }
        }

        public void Delete(string id)
        {
            Collection.RemoveAll(partnership => partnership.Id == id);
        }

        private void LoadSubProviders(List<IntegrationProvider> collection, IntegrationProvider current)
        {
            using (var clientContext = new SPContext(current.SPSiteURL, current.Authentication))
            {
                var site = clientContext.Site;
                clientContext.Load(site, s => s.Id, s => s.Url);

                var webs = clientContext.Web.Webs;
                clientContext.Load(webs, ws => ws.Include(w => w.Id, w => w.ServerRelativeUrl, w => w.Title, w => w.Webs));

                try
                {
                    clientContext.ExecuteQuery();
                }
                catch (Exception)
                {
                    return;
                }

                Parallel.ForEach(webs, new ParallelOptions { MaxDegreeOfParallelism = 5 }, website =>
                {
                    var provider = new IntegrationProvider
                    {
                        SPSiteURL = SPSite.MergeUrl(current.SPSiteURL, website.ServerRelativeUrl),
                        TEGroupId = current.TEGroupId,
                        TEGroupName = current.TEGroupName,
                        Authentication = current.Authentication,
                        SPSiteName = website.Title,
                        SPWebID = website.Id,
                        SPSiteID = site.Id
                    };

                    collection.Add(provider);

                    if (website.Webs.Count > 0)
                        LoadSubProviders(collection, provider);
                });
            }
        }

        #region Cache Methods

        public static void ExpireTags()
        {
            CacheService.RemoveByTags(new[] { Tag() }, CacheScope.All);
        }

        private static string CacheKey(string key)
        {
            return string.Concat("SharePoint_Integration_Manager:", key.TrimEnd('/'));
        }

        private static string Tag()
        {
            return string.Concat("SharePoint_Integration_Manager_TAG:", ProviderElement);
        }

        #endregion
    }
}
