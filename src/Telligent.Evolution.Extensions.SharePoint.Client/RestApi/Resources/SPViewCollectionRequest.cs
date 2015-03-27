using Telligent.Evolution.Rest.Infrastructure.Version2;
using Version2 = Telligent.Evolution.Extensibility.Rest.Version2;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources
{
    public class SPViewCollectionRequest : DefaultRestRequest
    {
        private const int PageSizeDefaultValue = 20;

        public SPViewCollectionRequest(Version2.IRestRequest request)
            : base(request.Request, request.Form, request.PathParameters, request.UserId)
        {
            Url = request.Request.QueryString["url"];
            ListId = request.PathParameters["listId"].ToString();
            ViewNameFilter = request.Request.QueryString["ViewNameFilter"];

            int pageSize;
            if (!int.TryParse(request.Request.QueryString["pageSize"], out pageSize))
            {
                pageSize = PageSizeDefaultValue;
            }
            PageSize = pageSize;

            int pageIndex;
            if (!int.TryParse(request.Request.QueryString["pageIndex"], out pageIndex))
            {
                pageIndex = 0;
            }
            PageIndex = pageIndex;
        }

        // Specify the Url of the SharePoint Site.
        public string Url { get; set; }

        // Specify the List Id.
        public string ListId { get; set; }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }

        public string ViewNameFilter { get; set; }
    }
}
