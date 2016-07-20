using System;
using System.Collections.Generic;
using System.Web.UI.HtmlControls;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;

namespace Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil
{
    public static class AuthenticationHelper
    {
        private static AuthenticationBuilder defauldAuthBuilder = new AuthenticationBuilder(new Anonymous(), new ServiceAccount(), new Methods.Windows(), new SAML(), new Methods.OAuth());

        public static HtmlGenericControl GetPropertyControls()
        {
            return GetPropertyControls(defauldAuthBuilder);
        }

        public static HtmlGenericControl GetPropertyControls(AuthenticationBuilder authBuilder)
        {
            HtmlGenericControl propertyControls = new HtmlGenericControl("div");
            propertyControls.Attributes["class"] = "authentication";
            authBuilder.CreateMarkup(propertyControls);
            return propertyControls;
        }

        public static HtmlGenericControl SetPropertyControls(Authentication auth)
        {
            return SetPropertyControls(auth, defauldAuthBuilder);
        }

        public static HtmlGenericControl SetPropertyControls(Authentication auth, AuthenticationBuilder authBuilder)
        {
            HtmlGenericControl propertyControls = new HtmlGenericControl("div");
            propertyControls.Attributes["class"] = "authentication";
            authBuilder.CreateMarkup(propertyControls, auth);
            return propertyControls;
        }

        public static String GetAuthenticationType(HtmlGenericControl authControls)
        {
            Authentication authMethod = defauldAuthBuilder.GetSelectedControl(authControls);
            return authMethod != null ? authMethod.Text : String.Empty;
        }

        public static String ToQueryString(HtmlGenericControl authControls)
        {
            return ToQueryString(authControls, defauldAuthBuilder);
        }

        public static String ToQueryString(HtmlGenericControl authControls, AuthenticationBuilder authBuilder)
        {
            Dictionary<String, String> parameters = new Dictionary<String, String>();
            Authentication authMethod = authBuilder.GetSelectedControl(authControls);
            return authMethod.ToQueryString();
        }

        public static Authentication FromHtml(HtmlGenericControl authControls)
        {
            return defauldAuthBuilder.GetSelectedControl(authControls);
        }

        public static Authentication FromQueryString(String queryString)
        {
            return defauldAuthBuilder.GetAuthentication(queryString);
        }
    }
}
