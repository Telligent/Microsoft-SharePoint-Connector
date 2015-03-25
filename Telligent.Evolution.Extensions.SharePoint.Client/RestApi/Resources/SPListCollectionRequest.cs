using Telligent.Evolution.Rest.Infrastructure.Version2;
using Version2 = Telligent.Evolution.Extensibility.Rest.Version2;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources
{
    public class SPListCollectionRequest : DefaultRestRequest
    {
        private const int PageSizeDefaultValue = 20;

        public SPListCollectionRequest(Version2.IRestRequest request)
            : base(request.Request, request.Form, request.PathParameters, request.UserId)
        {
            Url = request.Request.QueryString["url"];
            ListType = request.Request.QueryString["listType"];
            ExcludeListType = request.Request.QueryString["excludeListType"];
            ListNameFilter = request.Request.QueryString["ListNameFilter"];

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

        //Specify the Url of the SharePoint Site.
        public string Url { get; set; }

        //Specify the List Type. Announcements, Contacts, Calendar, Tasks or Custom.
        public string ListType { get; set; }

        //Specify the List Type the should be excluded. Announcements, Contacts, Calendar, Tasks or Custom.
        public string ExcludeListType { get; set; }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }

        public string ListNameFilter { get; set; }
    }
}
