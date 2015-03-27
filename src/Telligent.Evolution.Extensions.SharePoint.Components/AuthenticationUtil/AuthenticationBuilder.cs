using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil
{
    public class AuthenticationBuilder
    {
        private List<Authentication> authMethods;

        public AuthenticationBuilder(params Authentication[] authMethods)
        {
            this.authMethods = new List<Authentication>(authMethods);
        }

        public void CreateMarkup(Control container)
        {
            CreateMarkup(container, authMethods.FirstOrDefault());
        }

        public void CreateMarkup(Control container, Authentication authentication)
        {
            const string AuthRadioGroupName = "authentication";
            foreach (var auth in this.authMethods)
            {
                bool isChecked = auth.Name == authentication.Name;
                var divwrapper = new HtmlGenericControl("div");
                RadioButton radioBtn = new RadioButton() { ID = auth.Name, GroupName = AuthRadioGroupName, Text = auth.Text, Checked = isChecked, CssClass = auth.Name.ToLower() };
                divwrapper.Controls.Add(radioBtn);
                container.Controls.Add(divwrapper);
                if (isChecked)
                {
                    authentication.ValidationEnabled = true;
                    authentication.CreateMarkup(divwrapper);
                }
                else
                {
                    auth.CreateMarkup(divwrapper);
                }
            }
        }

        public Authentication GetSelectedControl(Control container)
        {
            foreach (var auth in authMethods)
            {
                RadioButton currentAuth = container.FindControl(auth.Name) as RadioButton;
                if (currentAuth != null && currentAuth.Checked)
                {
                    return ((Authentication)Activator.CreateInstance(auth.GetType())).ParseMarkup(container);
                }
            }
            return null;
        }

        public Authentication GetAuthentication(string queryString)
        {
            return Authentication.QueryToObject(queryString, this.authMethods);
        }
    }
}
