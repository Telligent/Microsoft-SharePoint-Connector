using TE = Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities
{
    public class TEApiUser : User
    {
        public TEApiUser(string idFieldName, string emailFieldName, TE.User userProfile)
            : base(idFieldName, emailFieldName)
        {
            Profile = userProfile;
        }

        public TE.User Profile { get; private set; }
    }
}
