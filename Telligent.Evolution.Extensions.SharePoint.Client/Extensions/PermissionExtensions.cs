using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    public static class PermissionExtensions
    {
        public static SPPermissionsLevel ToPermissionLevel(this RoleDefinition rd)
        {
            return new SPPermissionsLevel(rd.Id, rd.Name)
            {
                Description = rd.Description
            };
        }

        public static SPPermissions ToPermission(this RoleAssignment ra)
        {
            var permission = new SPPermissions(ra.Member);
            foreach (var rd in ra.RoleDefinitionBindings)
            {
                permission.Level.Add(rd.ToPermissionLevel());
            }
            return permission;
        }
    }
}
