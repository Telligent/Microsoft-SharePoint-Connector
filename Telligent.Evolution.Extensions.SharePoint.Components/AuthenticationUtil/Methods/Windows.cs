using System;
using System.Net;
using System.Web.UI;

namespace Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods
{
    public class Windows : Authentication
    {
        public override string Name
        {
            get { return "Windows"; }
        }

        public override string Text
        {
            get { return "Windows Authentication"; }
        }

        public Windows() : base() { }
        public Windows(string query) { }

        public override ICredentials Credentials()
        {
            return CredentialCache.DefaultCredentials;
        }

        public override string ToQueryString()
        {
            return String.Format("{0}={1}", Authentication.AuthKey, this.Name);
        }

        public override void CreateMarkup(Control container) { }
        public override Authentication ParseMarkup(Control markup)
        {
            return new Windows();
        }
    }
}
