using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPBaseUser : ApiEntity
    {
        public SPBaseUser(int id, string loginName)
        {
            Id = id;
            LoginName = loginName;
            Name = loginName;
        }

        public int Id { get; private set; }
        public string Name { get; set; }
        public string LoginName { get; private set; }
    }
}
