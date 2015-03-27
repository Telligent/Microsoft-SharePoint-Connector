using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities
{
    public class SPSiteUser : User
    {
        public SPSiteUser(string idFieldName, string emailFieldName, SP.ListItem userProfile)
            : base(idFieldName, emailFieldName)
        {
            Profile = userProfile;
        }

        public SP.ListItem Profile { get; private set; }
    }
}
