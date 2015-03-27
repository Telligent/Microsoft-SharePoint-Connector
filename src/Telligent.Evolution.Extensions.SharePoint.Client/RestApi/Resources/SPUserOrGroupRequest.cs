using System.Globalization;
using Telligent.Evolution.Rest.Infrastructure.Version2;
using Version2 = Telligent.Evolution.Extensibility.Rest.Version2;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources
{
    public class SPUserOrGroupRequest : DefaultRestRequest
    {
        public SPUserOrGroupRequest(Version2.IRestRequest request)
            : base(request.Request, request.Form, request.PathParameters, request.UserId)
        {
            Url = request.Request.QueryString["url"].ToString(CultureInfo.InvariantCulture);
            Search = request.Request.QueryString["search"].ToString(CultureInfo.InvariantCulture);
        }

        // Specify the Url of the SharePoint Site.
        public string Url { get; set; }
        // Specify a part of a user or group name, that will be searched.
        public string Search { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
    }
}
