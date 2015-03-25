using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using User = Microsoft.SharePoint.Client.User;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPUserPrincipal : IApiEntity
    {
        public SPUserPrincipal(IEnumerable<Error> errors)
        {
            Errors = new List<Error>(errors);
        }

        public SPUserPrincipal(User user)
        {
            Id = user.Id;
            LoginName = user.LoginName;
            DisplayName = user.Title;
            Email = user.Email;
        }

        public int Id { get; private set; }

        public string LoginName { get; private set; }

        public string DisplayName { get; set; }

        public string Email { get; set; }

        #region IApiEntity Members

        public bool HasErrors
        {
            get
            {
                return Errors.Any();
            }
        }

        public IList<Error> Errors { get; set; }

        public IList<Warning> Warnings { get; set; }

        #endregion
    }
}
