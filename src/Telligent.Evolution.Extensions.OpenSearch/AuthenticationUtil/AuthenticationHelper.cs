using System;
using System.Web.UI.HtmlControls;
using Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil.Methods;

namespace Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil
{
    public static class AuthenticationHelper
    {
        private static readonly AuthenticationBuilder authBuilder = new AuthenticationBuilder(new Anonymous(), new ServiceAccount(), new Windows()/*, new SAML()*/);

        public static HtmlGenericControl GetPropertyControls()
        {
            var propertyControls = new HtmlGenericControl("div");
            propertyControls.Attributes["class"] = "authentication";
            authBuilder.CreateMarkup(propertyControls);
            return propertyControls;
        }

        public static HtmlGenericControl SetPropertyControls(Authentication auth)
        {
            var propertyControls = new HtmlGenericControl("div");
            propertyControls.Attributes["class"] = "authentication";
            authBuilder.CreateMarkup(propertyControls, auth);
            return propertyControls;
        }

        public static String GetAuthenticationType(HtmlGenericControl authControls)
        {
            Authentication authMethod = authBuilder.GetSelectedControl(authControls);
            return authMethod != null ? authMethod.Text : String.Empty;
        }

        public static String ToQueryString(HtmlGenericControl authControls)
        {
            return authBuilder.GetSelectedControl(authControls).ToQueryString();
        }

        public static Authentication FromHtml(HtmlGenericControl authControls)
        {
            return authBuilder.GetSelectedControl(authControls);
        }

        public static Authentication FromQueryString(String queryString)
        {
            return authBuilder.GetAuthentication(queryString);
        }
    }
}
