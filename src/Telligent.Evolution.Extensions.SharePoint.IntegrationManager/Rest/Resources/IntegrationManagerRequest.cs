using Telligent.Evolution.Rest.Infrastructure.Version2;
using Version2 = Telligent.Evolution.Extensibility.Rest.Version2;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Resources
{
    public class IntegrationManagerRequest : DefaultRestRequest
    {
        public IntegrationManagerRequest(Version2.IRestRequest request)
            : base(request.Request, request.Form, request.PathParameters, request.UserId)
        {
            if (request.PathParameters["managerId"] != null)
            {
                ManagerId = request.PathParameters["managerId"].ToString();
            }

            int groupId;
            if (request.PathParameters["groupId"] != null && int.TryParse(request.PathParameters["groupId"].ToString(), out groupId))
            {
                GroupId = groupId;
            }
        }

        // Specify the Integration Manager Id.
        public string ManagerId { get; set; }

        // Specify the mapped Group Id.
        public int? GroupId { get; set; }
    }
}
