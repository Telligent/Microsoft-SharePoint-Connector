using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IDocumentUrls
    {
        string BrowseDocuments(ListUrlQuery library);
        string AddDocument(ListUrlQuery library);
        string ViewDocument(ListUrlQuery library, ItemUrlQuery document);
        string EditDocument(ListUrlQuery library, ItemUrlQuery document);
    }

    internal class SharePointDocumentUrls : IDocumentUrls
    {
        private readonly DocumentsRouteTable documentsRouteTable;

        public SharePointDocumentUrls() : this(DocumentsRouteTable.Get()) { }
        public SharePointDocumentUrls(DocumentsRouteTable documentsRouteTable)
        {
            this.documentsRouteTable = documentsRouteTable;
        }

        public string BrowseDocuments(ListUrlQuery library)
        {
            return documentsRouteTable.List.BuildUrl(library.GroupId, documentsRouteTable.BuildUrlTokens(library));
        }

        public string AddDocument(ListUrlQuery library)
        {
            return documentsRouteTable.Add.BuildUrl(library.GroupId, documentsRouteTable.BuildUrlTokens(library));
        }

        public string ViewDocument(ListUrlQuery library, ItemUrlQuery document)
        {
            return documentsRouteTable.Show.BuildUrl(library.GroupId, documentsRouteTable.BuildUrlTokens(library, document));
        }

        public string EditDocument(ListUrlQuery library, ItemUrlQuery document)
        {
            return documentsRouteTable.Edit.BuildUrl(library.GroupId, documentsRouteTable.BuildUrlTokens(library, document));
        }
    }
}
