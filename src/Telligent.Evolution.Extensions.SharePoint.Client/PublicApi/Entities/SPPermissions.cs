using System.Collections.Generic;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPPermissions : ApiEntity
    {
        public SPPermissions() { }
        public SPPermissions(Principal member)
        {
            Member = member;
            Level = new List<SPPermissionsLevel>();
        }

        public Principal Member { get; private set; }
        public List<SPPermissionsLevel> Level { get; set; }
    }
}
