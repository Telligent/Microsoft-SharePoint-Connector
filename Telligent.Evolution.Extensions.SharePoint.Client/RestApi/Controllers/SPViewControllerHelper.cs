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
    public interface ISPViewController
    {
        IRestResponse Get(SPViewItemRequest request);

        IRestResponse List(SPViewCollectionRequest request);
    }

    public class SPViewController : ISPViewController
    {
        private static readonly TimeSpan CacheTimeOut = TimeSpan.FromSeconds(60);
        private static readonly SharePointEndpoints plugin = PluginManager.Get<SharePointEndpoints>().FirstOrDefault();

        private readonly ICacheService cacheService;

        internal SPViewController() : this(ServiceLocator.Get<ICacheService>()) { }

        internal SPViewController(ICacheService cacheService)
        {
            this.cacheService = cacheService;
        }

        public IRestResponse Get(SPViewItemRequest request)
        {
            var response = new DefaultRestResponse
                {
                    Name = "View"
                };

            var errors = new List<string>();
            ValidateUrl(request.Url, errors);
            var listId = ValidateListId(request.ListId, errors);
            var viewId = ValidateViewId(request.ViewId, errors);
            if (errors.Any())
            {
                response.Errors = errors.ToArray();
                return response;
            }

            response.Data = cacheService.Get(CacheKey(listId, viewId, request.Url), CacheScope.Context | CacheScope.Process);
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
                        var view = list.GetView(viewId);
                        clientContext.Load(view, RestSPView.InstanceQuery);

                        clientContext.ExecuteQuery();

                        response.Data = new SPViewItemData
                        {
                            Item = new RestSPView(view),
                            SiteId = site.Id,
                            WebId = web.Id
                        };
                    }
                    cacheService.Put(CacheKey(listId, viewId, request.Url), response.Data, CacheScope.Context | CacheScope.Process, new string[] { }, CacheTimeOut);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the RESTApi.Views.Get() method for ListId: {1} ViewId: {2} SPWebUrl: '{3}'. The exception message is: {4}",
                        ex.GetType(), listId, viewId, request.Url, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    response.Errors = new[] { plugin.Translate(SharePointEndpoints.Translations.UnknownError) };
                }
            }
            return response;
        }

        public IRestResponse List(SPViewCollectionRequest request)
        {
            var response = new DefaultRestResponse
                {
                    Name = "Views"
                };

            var errors = new List<string>();
            ValidateUrl(request.Url, errors);
            var listId = ValidateListId(request.ListId, errors);
            if (errors.Any())
            {
                response.Errors = errors.ToArray();
                return response;
            }

            string url = request.Url;
            string viewNameFilter = request.ViewNameFilter;
            int pageSize = request.PageSize;
            int pageIndex = request.PageIndex;

            var viewCollection = (IEnumerable<View>)cacheService.Get(CacheKey(url, listId), CacheScope.Context | CacheScope.Process);
            if (viewCollection == null)
            {
                try
                {
                    using (var clientContext = new SPContext(url, IntegrationManagerPlugin.CurrentAuth(url), runAsServiceAccount: true))
                    {
                        var list = clientContext.Web.Lists.GetById(listId);
                        viewCollection = clientContext.LoadQuery(list.Views.Include(RestSPView.InstanceQuery));

                        clientContext.ExecuteQuery();
                    }
                    cacheService.Put(CacheKey(url, listId), viewCollection, CacheScope.Context | CacheScope.Process, new string[] { }, CacheTimeOut);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the RESTApi.View.List() method ListId:{1} SPWebUrl: '{2}'. The exception message is: {3}", ex.GetType(), listId, url, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    response.Errors = new[] { plugin.Translate(SharePointEndpoints.Translations.UnknownError) };
                }
            }

            if (viewCollection != null)
            {
                try
                {
                    Func<View, bool> filter = (v => true);
                    bool hasViewNameFilter = !String.IsNullOrEmpty(viewNameFilter);
                    if (hasViewNameFilter)
                    {
                        filter = v => v.Title.Contains(viewNameFilter, StringComparison.OrdinalIgnoreCase);
                    }

                    List<View> viewList = viewCollection.Where(filter).ToList();
                    response.Data = new SPViewCollectionData(viewList.Skip(pageIndex).Take(pageSize).Select(view => new RestSPView(view)), viewList.Count);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the RESTApi.View.List() method while processing not empty collection of Views for ListId: {1} SPWebUrl: '{2}'. The exception message is: {3}", ex.GetType(), listId, url, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    response.Errors = new[] { plugin.Translate(SharePointEndpoints.Translations.UnknownError) };
                }
            }

            return response;
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

        private static Guid ValidateViewId(string viewId, ICollection<string> errors)
        {
            Guid id = Guid.Empty;
            if (string.IsNullOrEmpty(viewId) || !Guid.TryParse(viewId, out id) || id == Guid.Empty)
            {
                errors.Add(plugin.Translate(SharePointEndpoints.Translations.InvalidViewId));
            }
            return id;
        }

        private static string CacheKey(Guid listId, Guid viewId, string url)
        {
            return string.Format("REST_SharePoint_View:{0}:{1}:{2}", listId.ToString("N"), viewId.ToString("N"), url);
        }

        private static string CacheKey(string url, Guid listId)
        {
            return string.Format("REST_SharePoint_Views:{0}:{1}", url, listId);
        }
    }
}
