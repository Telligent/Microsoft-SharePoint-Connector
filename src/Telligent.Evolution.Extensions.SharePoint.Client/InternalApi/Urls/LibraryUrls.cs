using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface ILibraryUrls
    {
        string BrowseLibraries(int groupId);
        string CreateLibrary(int groupId);
        string ImportLibrary(int groupId);
        string EditLibrary(ListUrlQuery library);
    }

    internal class SharePointLibraryUrls : ILibraryUrls
    {
        private readonly LibrariesRouteTable librariesRouteTable;

        public SharePointLibraryUrls() : this(LibrariesRouteTable.Get()) { }
        public SharePointLibraryUrls(LibrariesRouteTable librariesRouteTable)
        {
            this.librariesRouteTable = librariesRouteTable;
        }

        public string BrowseLibraries(int groupId)
        {
            return librariesRouteTable.List.BuildUrl(groupId);
        }

        public string CreateLibrary(int groupId)
        {
            return librariesRouteTable.Create.BuildUrl(groupId);
        }

        public string ImportLibrary(int groupId)
        {
            return librariesRouteTable.Add.BuildUrl(groupId);
        }

        public string EditLibrary(ListUrlQuery library)
        {
            return librariesRouteTable.Edit.BuildUrl(library.GroupId, librariesRouteTable.BuildUrlTokens(library));
        }
    }
}
