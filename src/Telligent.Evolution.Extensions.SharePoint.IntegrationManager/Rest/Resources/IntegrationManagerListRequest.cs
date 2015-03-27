using Telligent.Evolution.Rest.Infrastructure.Version2;
using Version2 = Telligent.Evolution.Extensibility.Rest.Version2;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Resources
{
    public class IntegrationManagerListRequest : DefaultRestRequest
    {
        private const int PageSizeDefaultValue = 1000;

        public IntegrationManagerListRequest(Version2.IRestRequest request)
            : base(request.Request, request.Form, request.PathParameters, request.UserId)
        {
            SiteNameFilter = request.Request.QueryString["SiteNameFilter"];
            GroupNameFilter = request.Request.QueryString["GroupNameFilter"];

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

        public string SiteNameFilter { get; set; }

        public string GroupNameFilter { get; set; }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }
    }
}
