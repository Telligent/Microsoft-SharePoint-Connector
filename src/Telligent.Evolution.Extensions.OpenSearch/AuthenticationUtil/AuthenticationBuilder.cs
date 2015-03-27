using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil
{
    public class AuthenticationBuilder
    {
        private readonly List<Authentication> authMethods;

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
            const string authRadioGroupName = "authentication";
            foreach (var auth in authMethods)
            {
                bool isChecked = auth.Name == authentication.Name;
                var divwrapper = new HtmlGenericControl("div");
                var radioBtn = new RadioButton { ID = auth.Name, GroupName = authRadioGroupName, Text = auth.Text, Checked = isChecked, CssClass = auth.Name.ToLower() };
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
            return (from auth in authMethods 
                    let currentAuth = container.FindControl(auth.Name) as RadioButton 
                    where currentAuth != null && currentAuth.Checked 
                    select ((Authentication) Activator.CreateInstance(auth.GetType())).ParseMarkup(container))
                    .FirstOrDefault();
        }

        public Authentication GetAuthentication(string queryString)
        {
            return Authentication.QueryToObject(queryString, authMethods);
        }
    }
}
