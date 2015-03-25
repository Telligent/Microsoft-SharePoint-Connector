using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Routing
{
    internal class DocumentsRouteTable : SPRouteTable
    {
        private static readonly DocumentsRouteTable instance = new DocumentsRouteTable();
        private static readonly IListItemDataService listItemDataService = ServiceLocator.Get<IListItemDataService>();
        private static readonly IListItemService listItemService = ServiceLocator.Get<IListItemService>();

        private DocumentsRouteTable()
        {
            Add = new RoutedPage
                    {
                        ShortName = "AddDocument",
                        PageName = "sharepoint-document-add",
                        UrlName = "sharepoint.document.add",
                        UrlPattern = "{libraryId}/new",
                        ParseContext = LibrariesRouteTable.ParseLibraryContextItem
                    };

            Edit = new RoutedPage
                    {
                        ShortName = "EditDocument",
                        PageName = "sharepoint-document-edit",
                        UrlName = "sharepoint.document.edit",
                        UrlPattern = "{libraryId}/{documentId}/edit",
                        ParseContext = ParseDocumentContextItem
                    };

            Show = new RoutedPage
                    {
                        ShortName = "Document",
                        PageName = "sharepoint-document-show",
                        UrlName = "sharepoint.document.show",
                        UrlPattern = "{libraryId}/{documentId}",
                        ParseContext = ParseDocumentContextItem
                    };

            List = new RoutedPage
                    {
                        ShortName = "Documents",
                        PageName = "sharepoint-document-list",
                        UrlName = "sharepoint.document.list",
                        UrlPattern = "{libraryId}",
                        ParseContext = LibrariesRouteTable.ParseLibraryContextItem
                    };
        }

        public Dictionary<string, string> BuildUrlTokens(ListUrlQuery list, ItemUrlQuery item = null)
        {
            var tokens = new Dictionary<string, string> { { "libraryId", GetListTokenValue(list) } };
            if (item != null)
            {
                tokens.Add("documentId", GetItemTokenValue(item));
            }
            return tokens;
        }

        public static Guid ParseDocumentId(Guid libraryId, PageContext pageContext)
        {
            var tokenValue = pageContext.GetTokenValue("documentId");
            if (tokenValue == null) return Guid.Empty;

            var token = tokenValue.ToString();
            var contentId = listItemDataService.GetItemUniqueId(token, libraryId);
            if (contentId == Guid.Empty)
            {
                contentId = listItemService.GetItemUniqueId(token, libraryId);
            }
            return contentId;
        }

        private static void ParseDocumentContextItem(PageContext pageContext)
        {
            var libraryId = LibrariesRouteTable.ParseLibraryId(pageContext);
            if (libraryId == Guid.Empty) return;

            var documentId = ParseDocumentId(libraryId, pageContext);

            pageContext.ContextItems.Put(BuildContextItem(libraryId, documentId));
        }

        public static ContextItem BuildContextItem(Guid applicationId, Guid contentId)
        {
            return new ContextItem
            {
                TypeName = "Document",
                ApplicationId = applicationId,
                ApplicationTypeId = LibraryApplicationType.Id,
                ContentId = contentId,
                ContentTypeId = DocumentContentType.Id
            };
        }

        public static DocumentsRouteTable Get()
        {
            return instance;
        }
    }
}
