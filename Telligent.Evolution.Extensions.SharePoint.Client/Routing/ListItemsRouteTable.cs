using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Routing
{
    internal class ListItemsRouteTable : SPRouteTable
    {
        private static readonly ListItemsRouteTable instance = new ListItemsRouteTable();
        private static readonly IListItemDataService listItemDataService = ServiceLocator.Get<IListItemDataService>();
        private static readonly IListItemService listItemService = ServiceLocator.Get<IListItemService>();

        private ListItemsRouteTable()
        {
            Add = new RoutedPage
                    {
                        ShortName = "CreateSPListItem",
                        PageName = "sharepoint-listItem-add",
                        UrlName = "sharepoint.listItem.add",
                        UrlPattern = "{listId}/new",
                        ParseContext = ListsRouteTable.ParseListContextItem
                    };

            Edit = new RoutedPage
                    {
                        ShortName = "EditSPListItem",
                        PageName = "sharepoint-listItem-edit",
                        UrlName = "sharepoint.listItem.edit",
                        UrlPattern = "{listId}/{itemId}/edit",
                        ParseContext = ParseListItemContextItem
                    };

            Show = new RoutedPage
                    {
                        ShortName = "SPListItem",
                        PageName = "sharepoint-listItem-show",
                        UrlName = "sharepoint.listItem.show",
                        UrlPattern = "{listId}/{itemId}",
                        ParseContext = ParseListItemContextItem
                    };

            List = new RoutedPage
                    {
                        ShortName = "SPListItems",
                        PageName = "sharepoint-listItem-list",
                        UrlName = "sharepoint.listItem.list",
                        UrlPattern = "{listId}",
                        ParseContext = ListsRouteTable.ParseListContextItem
                    };
        }

        public Dictionary<string, string> BuildUrlTokens(ListUrlQuery list, ItemUrlQuery item = null)
        {
            var tokens = new Dictionary<string, string> { { "listId", GetListTokenValue(list) } };
            if (item != null)
            {
                tokens.Add("itemId", GetItemTokenValue(item));
            }
            return tokens;
        }

        public static Guid ParseListItemId(Guid listId, PageContext pageContext)
        {
            var tokenValue = pageContext.GetTokenValue("itemId");
            if (tokenValue == null) return Guid.Empty;

            var token = tokenValue.ToString();
            var contentId = listItemDataService.GetItemUniqueId(token, listId);
            if (contentId == Guid.Empty)
            {
                contentId = listItemService.GetItemUniqueId(token, listId);
            }
            return contentId;
        }

        private static void ParseListItemContextItem(PageContext pageContext)
        {
            var listId = ListsRouteTable.ParseListId(pageContext);
            if (listId == Guid.Empty) return;

            var itemId = ParseListItemId(listId, pageContext);

            pageContext.ContextItems.Put(BuildContextItem(listId, itemId));
        }

        public static ContextItem BuildContextItem(Guid applicationId, Guid contentId)
        {
            return new ContextItem
            {
                TypeName = "SPListItem",
                ApplicationId = applicationId,
                ApplicationTypeId = ListApplicationType.Id,
                ContentId = contentId,
                ContentTypeId = ItemContentType.Id
            };
        }

        public static ListItemsRouteTable Get()
        {
            return instance;
        }
    }
}
