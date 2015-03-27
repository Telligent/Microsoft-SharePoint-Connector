using Telligent.Evolution.Rest.Infrastructure.Version2;
using Version2 = Telligent.Evolution.Extensibility.Rest.Version2;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources
{
    public class SPViewItemRequest : DefaultRestRequest
    {
        public SPViewItemRequest(Version2.IRestRequest request)
            : base(request.Request, request.Form, request.PathParameters, request.UserId)
        {
            ListId = request.PathParameters["listId"].ToString();
            Url = request.Request.QueryString["url"].ToString();
            ViewId = request.PathParameters["viewId"].ToString();
        }

        // Specify the Url of the SharePoint Site.
        public string Url { get; set; }

        // Specify the List Id.
        public string ListId { get; set; }

        // Specify the View Id.
        public string ViewId { get; set; }
    }
}
