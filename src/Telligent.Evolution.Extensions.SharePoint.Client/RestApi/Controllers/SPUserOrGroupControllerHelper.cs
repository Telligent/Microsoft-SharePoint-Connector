using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public interface ISPUserOrGroupController
    {
        IRestResponse List(SPUserOrGroupRequest request, bool onlyGroups = false, bool onlyUsers = false);
    }

    public class SPUserOrGroupController : ISPUserOrGroupController
    {
        private static readonly TimeSpan CacheTimeOut = TimeSpan.FromSeconds(60);
        private static readonly SharePointEndpoints plugin = PluginManager.Get<SharePointEndpoints>().FirstOrDefault();

        private const int DefaultPageSize = 10;

        private readonly ICacheService cacheService;

        internal SPUserOrGroupController() : this(ServiceLocator.Get<ICacheService>()) { }

        internal SPUserOrGroupController(ICacheService cacheService)
        {
            this.cacheService = cacheService;
        }

        public IRestResponse List(SPUserOrGroupRequest request, bool onlyGroups = false, bool onlyUsers = false)
        {
            var response = new DefaultRestResponse
            {
                Name = "List"
            };

            var errors = new List<string>();
            ValidateUrl(request.Url, errors);
            if (errors.Any())
            {
                response.Errors = errors.ToArray();
                return response;
            }

            string url = request.Url;
            string search = request.Search;
            int pageSize = request.PageSize > 0 ? request.PageSize : DefaultPageSize;
            int pageIndex = request.PageIndex > 0 ? request.PageIndex : 0;

            response.Data = cacheService.Get(CacheKey(url, onlyGroups, onlyUsers, search, pageSize, pageIndex), CacheScope.Context | CacheScope.Process);
            if (response.Data == null)
            {
                try
                {
                    using (var clientContext = new SPContext(url, IntegrationManagerPlugin.CurrentAuth(url), runAsServiceAccount: true))
                    {
                        var web = clientContext.Web;
                        clientContext.Load(web, w => w.Id);

                        var site = clientContext.Site;
                        clientContext.Load(site, s => s.Id);

                        var query = CamlQuery.CreateAllItemsQuery(pageSize);
                        query.ViewXml = String.Concat("<View><Query>", ViewFieldsSection(RestSPUserOrGroup.ViewFields), WhereSection(search, onlyGroups, onlyUsers), "</Query></View>");

                        if (pageIndex > 0)
                        {
                            var tempListItems = web.SiteUserInfoList.GetItems(CamlQuery.CreateAllItemsQuery(pageSize * pageIndex));
                            clientContext.Load(tempListItems, itemsCollection => itemsCollection.ListItemCollectionPosition);
                            clientContext.ExecuteQuery();

                            query.ListItemCollectionPosition = tempListItems.ListItemCollectionPosition;
                        }

                        var siteUsersAndGroups = clientContext.LoadQuery(web.SiteUserInfoList.GetItems(query));
                        clientContext.ExecuteQuery();

                        response.Data = siteUsersAndGroups.Select(RestSPUserOrGroup.Get).ToArray();
                    }
                    cacheService.Put(CacheKey(url, onlyGroups, onlyUsers, search, pageSize, pageIndex), response.Data, CacheScope.Context | CacheScope.Process, new string[] { }, CacheTimeOut);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the RESTApi.GroupsAndUsers.List() method for SPWebUrl: '{1}' Search: '{2}' OnlyGroups: {3} OnlyUsers: {4}. The exception message is: {5}", ex.GetType(), url, search, onlyGroups, onlyUsers, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    response.Errors = new[] { plugin.Translate(SharePointEndpoints.Translations.UnknownError) };
                }
            }
            return response;
        }

        private static string ContainsQuery(string fieldName, string fieldValue, string valueType)
        {
            return String.Format("<Contains><FieldRef Name='{0}' /><Value Type='{2}'>{1}</Value></Contains>", fieldName, fieldValue, valueType);
        }

        private static string ViewFieldsSection(IEnumerable<string> viewFields)
        {
            if (viewFields == null)
                return string.Empty;

            var viewFieldsSection = new StringBuilder();
            viewFieldsSection.Append("<ViewFields>");
            foreach (var field in viewFields)
            {
                viewFieldsSection.AppendFormat("<FieldRef Name='{0}' />", field);
            }
            viewFieldsSection.Append("</ViewFields>");
            return viewFieldsSection.ToString();
        }

        private static string WhereSection(string search, bool onlyGroups = false, bool onlyUsers = false)
        {
            if (string.IsNullOrEmpty(search))
            {
                return string.Empty;
            }

            var whereSection = new StringBuilder();
            whereSection.Append("<Where>");
            if (onlyGroups || onlyUsers)
            {
                whereSection.Append("<And>");
                whereSection.Append(onlyUsers
                                        ? "<IsNotNull><FieldRef Name='EMail' /></IsNotNull>"
                                        : "<IsNull><FieldRef Name='EMail' /></IsNotNull>");
            }
            whereSection.Append("<Or>");
            whereSection.Append(ContainsQuery("Name", search, "Text"));
            whereSection.Append(ContainsQuery("Title", search, "Text"));
            whereSection.Append("</Or>");
            if (onlyGroups || onlyUsers)
            {
                whereSection.Append("</And>");
            }
            whereSection.Append("</Where>");
            return whereSection.ToString();
        }

        private static void ValidateUrl(string url, ICollection<string> errors)
        {
            if (string.IsNullOrEmpty(url))
            {
                errors.Add(plugin.Translate(SharePointEndpoints.Translations.UrlCannotBeEmpty));
            }
        }

        private static string CacheKey(string url, bool onlyGroups, bool onlyusers, string search, int pageSize, int pageIndex)
        {
            return string.Format("REST_SharePoint_GroupsAndUsers:{0}:{1}{2}:{3}:{4}:{5}", url, Convert.ToInt32(onlyGroups), Convert.ToInt32(onlyusers), search, pageSize, pageIndex);
        }
    }
}
