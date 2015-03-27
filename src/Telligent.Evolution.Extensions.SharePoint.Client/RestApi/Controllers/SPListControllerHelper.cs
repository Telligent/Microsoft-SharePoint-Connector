using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensibility.Rest.Version2;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Rest.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;
using Telligent.Evolution.Rest.Infrastructure.Version2;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Api.Version1
{
    public interface ISPListController
    {
        IRestResponse Get(SPListItemRequest request);

        IRestResponse List(SPListCollectionRequest request);
    }

    public class SPListController : ISPListController
    {
        private static readonly TimeSpan CacheTimeOut = TimeSpan.FromSeconds(60);
        private static readonly SharePointEndpoints plugin = PluginManager.Get<SharePointEndpoints>().FirstOrDefault();

        private readonly ICacheService cacheService;

        internal SPListController() : this(ServiceLocator.Get<ICacheService>()) { }

        internal SPListController(ICacheService cacheService)
        {
            this.cacheService = cacheService;
        }

        public IRestResponse Get(SPListItemRequest request)
        {
            var response = new DefaultRestResponse
                {
                    Name = "List"
                };

            var errors = new List<string>();
            ValidateUrl(request.Url, errors);
            var listId = ValidateListId(request.ListId, errors);
            if (errors.Any())
            {
                response.Errors = errors.ToArray();
                return response;
            }

            response.Data = cacheService.Get(CacheKey(listId, request.Url), CacheScope.Context | CacheScope.Process);
            if (response.Data == null)
            {
                try
                {
                    using (var clientContext = new SPContext(request.Url, IntegrationManagerPlugin.CurrentAuth(request.Url), runAsServiceAccount: true))
                    {
                        var site = clientContext.Site;
                        clientContext.Load(site, s => s.Id);

                        var web = clientContext.Web;
                        clientContext.Load(web, w => w.Id);

                        var list = clientContext.Web.Lists.GetById(listId);
                        clientContext.Load(list, RestSPList.InstanceQuery);

                        clientContext.ExecuteQuery();

                        response.Data = new SPListItemData
                            {
                                Item = new RestSPList(list),
                                SiteId = site.Id,
                                WebId = web.Id
                            };
                    }
                    cacheService.Put(CacheKey(listId, request.Url), response.Data, CacheScope.Context | CacheScope.Process, new string[] { }, CacheTimeOut);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the RESTApi.Lists.Get() method for ListId: {1} SPWebUrl: '{2}'. The exception message is: {3}", ex.GetType(), listId, request.Url, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    response.Errors = new[] { plugin.Translate(SharePointEndpoints.Translations.UnknownError) };
                }
            }
            return response;
        }

        public IRestResponse List(SPListCollectionRequest request)
        {
            var response = new DefaultRestResponse
                {
                    Name = "Lists"
                };

            var errors = new List<string>();
            ValidateUrl(request.Url, errors);
            if (errors.Any())
            {
                response.Errors = errors.ToArray();
                return response;
            }

            string url = request.Url;
            string includeType = request.ListType;
            string excludeType = request.ExcludeListType;
            string listNameFilter = request.ListNameFilter;
            int pageSize = request.PageSize;
            int pageIndex = request.PageIndex;

            var listCollection = (IEnumerable<List>)cacheService.Get(CacheKey(url, includeType, excludeType), CacheScope.Context | CacheScope.Process);
            if (listCollection == null)
            {
                try
                {
                    using (var clientContext = new SPContext(url, IntegrationManagerPlugin.CurrentAuth(url), runAsServiceAccount: true))
                    {
                        ListTemplateType includeLookUpTemplate;
                        ListTemplateType excludeLookUpTemplate;
                        var includeItemsByTemplate = TryGetTemplateType(includeType, out includeLookUpTemplate);
                        var excludeItemsByTemplate = TryGetTemplateType(excludeType, out excludeLookUpTemplate);
                        if (includeItemsByTemplate)
                        {
                            var lookUpTemplateValue = (int)includeLookUpTemplate;
                            listCollection = clientContext.LoadQuery(clientContext.Web.Lists.Where(list => list.BaseTemplate == lookUpTemplateValue && list.Hidden == false).Include(RestSPList.InstanceQuery));
                        }
                        else if (excludeItemsByTemplate)
                        {
                            var lookUpTemplateValue = (int)excludeLookUpTemplate;
                            listCollection = clientContext.LoadQuery(clientContext.Web.Lists.Where(list => list.BaseTemplate != lookUpTemplateValue && list.Hidden == false).Include(RestSPList.InstanceQuery));
                        }
                        else
                        {
                            listCollection = clientContext.LoadQuery(clientContext.Web.Lists.Include(RestSPList.InstanceQuery));
                        }

                        clientContext.ExecuteQuery();
                    }
                    cacheService.Put(CacheKey(url, includeType, excludeType), listCollection, CacheScope.Context | CacheScope.Process, new string[] { }, CacheTimeOut);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the RESTApi.Lists.List() method for SPWebUrl: '{1}'. The exception message is: {2}", ex.GetType(), url, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    response.Errors = new[] { plugin.Translate(SharePointEndpoints.Translations.UnknownError) };
                }
            }

            if (listCollection != null)
            {
                try
                {
                    Func<List, bool> filter = (m => true);
                    bool hasListNameFilter = !String.IsNullOrEmpty(listNameFilter);
                    if (hasListNameFilter)
                    {
                        filter = l => l.Title.StartsWith(listNameFilter, StringComparison.OrdinalIgnoreCase) || listNameFilter.Split(' ').Where(f => !string.IsNullOrWhiteSpace(f)).All(f => l.Title.Split(' ').Any(t => t.StartsWith(f, StringComparison.OrdinalIgnoreCase)));
                    }

                    List<List> itemsList = listCollection.Where(filter).ToList();
                    response.Data = new SPListCollectionData(itemsList.Skip(pageIndex).Take(pageSize).Select(item => new RestSPList(item)), itemsList.Count);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the RESTApi.Lists.List() method while processing not empty collection of Lists from SPWebUrl: '{1}'. The exception message is: {2}", ex.GetType(), url, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    response.Errors = new[] { plugin.Translate(SharePointEndpoints.Translations.UnknownError) };
                }
            }
            return response;
        }

        private static bool TryGetTemplateType(string templateType, out ListTemplateType listTemplateType)
        {
            return Enum.TryParse(templateType, true, out listTemplateType);
        }

        private static void ValidateUrl(string url, ICollection<string> errors)
        {
            if (string.IsNullOrEmpty(url))
            {
                errors.Add(plugin.Translate(SharePointEndpoints.Translations.UrlCannotBeEmpty));
            }
        }

        private static Guid ValidateListId(string listId, ICollection<string> errors)
        {
            Guid id = Guid.Empty;
            if (string.IsNullOrEmpty(listId) || !Guid.TryParse(listId, out id) || id == Guid.Empty)
            {
                errors.Add(plugin.Translate(SharePointEndpoints.Translations.InvalidListId));
            }
            return id;
        }

        private static string CacheKey(Guid listId, string url)
        {
            return string.Format("REST_SharePoint_List:{0}:{1}", listId.ToString("N"), url);
        }

        private static string CacheKey(string url, string includeType, string excludeType)
        {
            return string.Format("REST_SharePoint_Lists:{0}:{1}:{2}", url, includeType, excludeType);
        }

    }
}
