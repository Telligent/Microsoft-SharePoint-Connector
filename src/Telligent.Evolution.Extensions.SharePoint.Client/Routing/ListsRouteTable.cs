using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Routing
{
    internal class ListsRouteTable : SPRouteTable
    {
        private static readonly ListsRouteTable instance = new ListsRouteTable();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();

        private ListsRouteTable()
        {
            Add = new RoutedPage
                    {
                        ShortName = "AddSPList",
                        PageName = "sharepoint-list-add",
                        UrlName = "sharepoint.list.add",
                        UrlPattern = "import",
                        ParseContext = (pageContext) => { pageContext.ContextItems.Put(BuildContextItem()); }
                    };

            Edit = new RoutedPage
                    {
                        ShortName = "EditSPList",
                        PageName = "sharepoint-list-edit",
                        UrlName = "sharepoint.list.edit",
                        UrlPattern = "{listId}/edit",
                        ParseContext = ParseListContextItem
                    };

            Show = new RoutedPage
                    {
                        ShortName = "SPList",
                        PageName = "sharepoint-list-show",
                        UrlName = "sharepoint.list.show",
                        UrlPattern = "{listId}/show",
                        ParseContext = ParseListContextItem
                    };

            List = new RoutedPage
                    {
                        ShortName = "SPLists",
                        PageName = "sharepoint-list-list",
                        UrlName = "sharepoint.list.list",
                        UrlPattern = "",
                        ParseContext = (pageContext) => { pageContext.ContextItems.Put(BuildContextItem()); }
                    };
        }

        public Dictionary<string, string> BuildUrlTokens(ListUrlQuery list)
        {
            return new Dictionary<string, string>
                {
                    {"listId", GetListTokenValue(list)}
                };
        }

        public static Guid ParseListId(PageContext pageContext)
        {
            Guid applicationId = Guid.Empty;
            var tokenValue = pageContext.GetTokenValue("listId");
            if (tokenValue != null)
            {
                Guid listId = Guid.Empty;
                var applicationKey = tokenValue.ToString();
                if (!string.IsNullOrEmpty(applicationKey))
                {
                    var groupId = int.Parse(pageContext.ContextItems.GetItemByContentType(Telligent.Evolution.Extensibility.Api.Version1.PublicApi.Groups.ContentTypeId).Id);
                    var list = listDataService.Get(applicationKey, groupId);
                    if (list != null && list.Id != Guid.Empty)
                    {
                        applicationId = list.Id;
                    }
                    else if (Guid.TryParse(applicationKey, out listId) && listDataService.Get(listId) != null)
                    {
                        applicationId = listId;
                    }
                }
            }
            return applicationId;
        }


        public static void ParseListContextItem(PageContext pageContext)
        {
            var applicationId = ParseListId(pageContext);
            if (applicationId != Guid.Empty)
            {
                pageContext.ContextItems.Put(BuildContextItem(applicationId));
            }
        }

        public static Telligent.Evolution.Extensibility.Urls.Version1.ContextItem BuildContextItem()
        {
            return new Telligent.Evolution.Extensibility.Urls.Version1.ContextItem
            {
                TypeName = "SPList",
                ApplicationTypeId = ListApplicationType.Id,
            };
        }

        public static Telligent.Evolution.Extensibility.Urls.Version1.ContextItem BuildContextItem(Guid applicationId)
        {
            var listContext = BuildContextItem();
            listContext.ApplicationId = applicationId;
            return listContext;
        }

        public static ListsRouteTable Get()
        {
            return instance;
        }
    }
}
