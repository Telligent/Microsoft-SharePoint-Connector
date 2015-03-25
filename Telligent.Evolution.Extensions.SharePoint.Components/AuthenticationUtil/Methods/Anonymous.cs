using System;
using System.Net;
using System.Web.UI;

namespace Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods
{
    public class Anonymous : Authentication
    {
        public override string Name
        {
            get { return "Anonymous"; }
        }

        public override string Text
        {
            get { return "Anonymous"; }
        }
        
        public Anonymous() { }
        public Anonymous(string query) { }

        public override ICredentials Credentials()
        {
            return null;
        }

        public override string ToQueryString()
        {
            return String.Format("{0}={1}", Authentication.AuthKey, this.Name);
        }

        public override void CreateMarkup(Control container) { }
        public override Authentication ParseMarkup(Control markup)
        {
            return new Anonymous();
        }
    }
}
