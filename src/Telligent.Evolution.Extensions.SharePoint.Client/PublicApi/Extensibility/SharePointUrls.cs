using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public interface ISharePointUrls
    {
        // Libraries
        string CreateLibrary(int groupId);
        string ImportLibrary(int groupId);
        string EditLibrary(Guid libraryId);
        string BrowseLibraries(int groupId);

        // Documents
        string BrowseDocuments(Guid libraryId);
        string Document(Guid documentId);
        string EditDocument(Guid documentId);
        string AddDocument(Guid libraryId);

        // Lists
        string ImportList(int groupId);
        string EditList(Guid listId);
        string BrowseLists(int groupId);

        // ListItems
        string BrowseListItems(Guid listId);
        string ListItem(Guid listItemId);
        string EditListItem(Guid listItemId);
        string AddListItem(Guid listId);
    }

    public class SharePointUrls : ISharePointUrls
    {
        private readonly IListUrls listUrls;
        private readonly IListItemUrls listItemUrls;
        private readonly ILibraryUrls libraryUrls;
        private readonly IDocumentUrls documentUrls;
        private readonly IListItemDataService listItemDataService;
        private readonly IListDataService listDataService;
        private readonly DocumentsRouteTable documentsRouteTable = DocumentsRouteTable.Get();
        private readonly LibrariesRouteTable librariesRouteTable = LibrariesRouteTable.Get();
        private readonly ListItemsRouteTable listItemsRouteTable = ListItemsRouteTable.Get();
        private readonly ListsRouteTable listsRouteTable = ListsRouteTable.Get();

        public SharePointUrls()
            : this(ServiceLocator.Get<IListUrls>(), ServiceLocator.Get<IListItemUrls>(), ServiceLocator.Get<ILibraryUrls>(), ServiceLocator.Get<IDocumentUrls>(), ServiceLocator.Get<IListDataService>(), ServiceLocator.Get<IListItemDataService>())
        {
        }

        internal SharePointUrls(IListUrls listUrls, IListItemUrls listItemUrls, ILibraryUrls libraryUrls, IDocumentUrls documentUrls, IListDataService listDataService, IListItemDataService listItemDataService)
        {
            this.listUrls = listUrls;
            this.listItemUrls = listItemUrls;
            this.libraryUrls = libraryUrls;
            this.documentUrls = documentUrls;
            this.listDataService = listDataService;
            this.listItemDataService = listItemDataService;
        }

        #region Libraries

        public string BrowseLibraries(int groupId)
        {
            return libraryUrls.BrowseLibraries(groupId);
        }

        public string CreateLibrary(int groupId)
        {
            return libraryUrls.CreateLibrary(groupId);
        }

        public string ImportLibrary(int groupId)
        {
            return libraryUrls.ImportLibrary(groupId);
        }

        public string EditLibrary(Guid libraryId)
        {
            var library = listDataService.Get(libraryId);
            if (library != null && library.GroupId > 0)
            {
                return libraryUrls.EditLibrary(library);
            }
            return null;
        }

        #endregion

        #region Documents

        public string BrowseDocuments(Guid libraryId)
        {
            var library = listDataService.Get(libraryId);
            if (library != null && library.GroupId > 0)
            {
                return documentUrls.BrowseDocuments(library);
            }
            return null;
        }

        public string Document(Guid documentId)
        {
            ItemBase document;
            ListBase library;
            if ((document = listItemDataService.Get(documentId)) != null
                && (library = listDataService.Get(document.ApplicationId)) != null
                && library.GroupId > 0)
            {
                return documentUrls.ViewDocument(library, document);
            }
            return null;
        }

        public string EditDocument(Guid documentId)
        {
            ItemBase document;
            ListBase library;
            if ((document = listItemDataService.Get(documentId)) != null
                && (library = listDataService.Get(document.ApplicationId)) != null
                && library.GroupId > 0)
            {
                return documentUrls.EditDocument(library, document);
            }
            return null;
        }

        public string AddDocument(Guid libraryId)
        {
            var library = listDataService.Get(libraryId);
            if (library != null && library.GroupId > 0)
            {
                return documentUrls.AddDocument(library);
            }
            return null;
        }

        #endregion

        #region Lists

        public string BrowseLists(int groupId)
        {
            return listUrls.BrowseLists(groupId);
        }

        public string ImportList(int groupId)
        {
            return listUrls.ImportList(groupId);
        }

        public string EditList(Guid listId)
        {
            var list = listDataService.Get(listId);
            if (list != null && list.GroupId > 0)
            {
                return listUrls.EditList(list);
            }
            return null;
        }

        #endregion

        #region ListItems

        public string BrowseListItems(Guid listId)
        {
            var list = listDataService.Get(listId);
            if (list != null && list.GroupId > 0)
            {
                return listItemUrls.BrowseListItems(list);
            }
            return null;
        }

        public string ListItem(Guid listItemId)
        {
            ItemBase item;
            ListBase list;
            if ((item = listItemDataService.Get(listItemId)) != null
                && (list = listDataService.Get(item.ApplicationId)) != null
                && list.GroupId > 0)
            {
                return listItemUrls.ViewListItem(list, item);
            }
            return null;
        }

        public string EditListItem(Guid listItemId)
        {
            ItemBase item;
            ListBase list;
            if ((item = listItemDataService.Get(listItemId)) != null
                && (list = listDataService.Get(item.ApplicationId)) != null
                && list.GroupId > 0)
            {
                return listItemUrls.EditListItem(list, item);
            }
            return null;
        }

        public string AddListItem(Guid listId)
        {
            var list = listDataService.Get(listId);
            if (list != null && list.GroupId > 0)
            {
                return listItemUrls.AddListItem(list);
            }
            return null;
        }

        #endregion
    }
}
