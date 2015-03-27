using SPProfile = Telligent.Evolution.Extensions.SharePoint.WebServices.UserProfileService;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities
{
    public class SPFarmUser : User
    {
        public SPFarmUser(string idFieldName, string emailFieldName, SPProfile.PropertyData[] userProfile)
            : base(idFieldName, emailFieldName)
        {
            Profile = userProfile;
        }

        public SPProfile.PropertyData[] Profile { get; private set; }
    }
}
