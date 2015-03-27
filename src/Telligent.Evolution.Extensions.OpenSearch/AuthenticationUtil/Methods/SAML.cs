using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web.UI;

namespace Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil.Methods
{
    public class SAML : Authentication
    {
        public override string Name
        {
            get { return "SAML"; }
        }

        public override string Text
        {
            get { return "SAML Authentication"; }
        }

        public SAML() { }
        public SAML(string query) { }

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
            return new SAML();
        }
    }
}
