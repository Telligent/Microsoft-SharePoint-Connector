using System;
using System.Net;
using System.Security.Principal;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.SecurityModules;

namespace Telligent.Evolution.Extensions.SharePoint.WebServices
{
    public class ListService : SPListsService.Lists
    {
        private readonly Authentication authentication;
        private readonly WindowsImpersonationContext impersonate;
        private readonly string siteUrl;

        public ListService(string siteUrl, Authentication authentication)
        {
            Url = siteUrl.TrimEnd('/') + "/_vti_bin/Lists.asmx";

            this.authentication = authentication;
            this.siteUrl = siteUrl;
            
            Credentials = this.authentication.Credentials();
            
            if (this.authentication is Windows)
            {
                impersonate = Impersonate();
            }
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            var wr = base.GetWebRequest(uri);
            wr.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");

            if (authentication is SAML)
            {
                wr.Headers.Add("Cookie", string.Format("FedAuth={0}", SAML.GetToken(siteUrl)));
            }

            return wr;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (impersonate != null)
                impersonate.Undo();
        }

        private WindowsImpersonationContext Impersonate()
        {
            try
            {
                var ssop = (SingleSignOnPrincipal)CSContext.Current.Context.User;
                var principal = (WindowsPrincipal)ssop.OriginalPrincipal;
                var id = (WindowsIdentity)principal.Identity;

                return id.Impersonate();
            }
            catch (Exception ex)
            {
                SPLog.UserInvalidCredentials(ex, string.Format("Enable Windows Authentication and set roleManager enabled=\"false\" : {0} {1}", ex.Message, ex.StackTrace));
            }

            return null;
        }
    }
}
