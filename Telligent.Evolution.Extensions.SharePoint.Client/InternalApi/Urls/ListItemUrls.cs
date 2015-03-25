using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IListItemUrls
    {
        string BrowseListItems(ListUrlQuery list);
        string AddListItem(ListUrlQuery list);
        string ViewListItem(ListUrlQuery list, ItemUrlQuery item);
        string EditListItem(ListUrlQuery list, ItemUrlQuery item);
    }

    internal class SharePointListItemUrls : IListItemUrls
    {
        private readonly ListItemsRouteTable listItemsRouteTable;

        public SharePointListItemUrls() : this(ListItemsRouteTable.Get()) { }
        public SharePointListItemUrls(ListItemsRouteTable listItemsRouteTable)
        {
            this.listItemsRouteTable = listItemsRouteTable;
        }

        public string BrowseListItems(ListUrlQuery list)
        {
            return listItemsRouteTable.List.BuildUrl(list.GroupId, listItemsRouteTable.BuildUrlTokens(list));
        }

        public string AddListItem(ListUrlQuery list)
        {
            return listItemsRouteTable.Add.BuildUrl(list.GroupId, listItemsRouteTable.BuildUrlTokens(list));
        }

        public string ViewListItem(ListUrlQuery list, ItemUrlQuery item)
        {
            return listItemsRouteTable.Show.BuildUrl(list.GroupId, listItemsRouteTable.BuildUrlTokens(list, item));
        }

        public string EditListItem(ListUrlQuery list, ItemUrlQuery item)
        {
            return listItemsRouteTable.Edit.BuildUrl(list.GroupId, listItemsRouteTable.BuildUrlTokens(list, item));
        }
    }
}
