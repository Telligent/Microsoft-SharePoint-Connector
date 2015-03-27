using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IListUrls
    {
        string BrowseLists(int groupId);
        string ImportList(int groupId);
        string EditList(ListUrlQuery list);
    }

    internal class SharePointListUrls : IListUrls
    {
        private readonly ListsRouteTable listsRouteTable;

        public SharePointListUrls() : this(ListsRouteTable.Get()) { }
        public SharePointListUrls(ListsRouteTable listsRouteTable)
        {
            this.listsRouteTable = listsRouteTable;
        }

        public string BrowseLists(int groupId)
        {
            return listsRouteTable.List.BuildUrl(groupId);
        }

        public string ImportList(int groupId)
        {
            return listsRouteTable.Add.BuildUrl(groupId);
        }

        public string EditList(ListUrlQuery list)
        {
            return listsRouteTable.Edit.BuildUrl(list.GroupId, listsRouteTable.BuildUrlTokens(list));
        }
    }
}
