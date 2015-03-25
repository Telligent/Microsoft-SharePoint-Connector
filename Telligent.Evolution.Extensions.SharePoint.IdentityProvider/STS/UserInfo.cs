using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Claims;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.IdentityProvider.STS
{
    public class TelligentClaimTypes
    {
        public static string EmailAddress = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        public static string Title = "http://schemas.telligent.com/sharepoint/claims/title";
        public static string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    }

    public class UserInfo
    {
        public static List<string> GetAllUsers()
        {
            var allUsers = new List<string>();

            const int pageSize = 100;
            var pageIndex = 0;
            PagedList<User> userListHolder;

            do
            {
                userListHolder = PublicApi.Users.List(new UsersListOptions
                {
                    PageSize = pageSize,
                    PageIndex = pageIndex
                });

                allUsers.AddRange(userListHolder.Select(item => item.PrivateEmail));
                pageIndex++;

            } while (userListHolder.Count > 0);

            return allUsers;
        }

        public static List<Claim> GetClaimsForUser(string username)
        {
            var userClaims = new List<Claim>();
            var user = PublicApi.Users.Get(new UsersGetOptions { Username = username });
            userClaims.Add(new Claim(TelligentClaimTypes.EmailAddress, user.PrivateEmail, ClaimValueTypes.String));
            userClaims.Add(new Claim(TelligentClaimTypes.Title, user.DisplayName, ClaimValueTypes.String));
            userClaims.AddRange(PublicApi.Roles.List(new RolesListOptions { UserId = user.Id }).Select(role => new Claim(TelligentClaimTypes.Role, role.Name, ClaimValueTypes.String)));
            return userClaims;
        }
    }
}
