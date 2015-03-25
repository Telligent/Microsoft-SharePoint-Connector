using System;
using System.Collections.Specialized;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods
{
    public class OAuth : Authentication, IEncrypted
    {
        const string UserNamePattern = @"^[a-zA-Z0-9]+((_|-| |\.|@)*[a-zA-Z0-9]+)*$";
        const int BulletsNumber = 8;

        private AuthenticationParam userName;
        private AuthenticationParam password;

        public override string Name
        {
            get { return "OAuth"; }
        }

        public override string Text
        {
            get { return "OAuth"; }
        }

        public OAuth()
        {
            ValidationEnabled = false;
            userName = new AuthenticationParam("Name", "Login", String.Empty);
            password = new AuthenticationParam("Password", "Password", String.Empty);
        }

        public OAuth(string userName, string password, string domain = null)
            : this()
        {
            this.userName.Value = userName;
            this.password.Value = password;
        }

        public OAuth(string queryString)
            : this()
        {
            var queryKeyValue = HttpUtility.ParseQueryString(queryString);
            userName.Value = queryKeyValue[userName.Name];
            password.Value = queryKeyValue[password.Name];
        }

        public override ICredentials Credentials()
        {
            var securePassword = new SecureString();
            foreach (char c in password.Value)
            {
                securePassword.AppendChar(c);
            }

            return new SharePointOnlineCredentials(userName.Value, securePassword);
        }

        public override string ToQueryString()
        {
            var nameValueCollection = new NameValueCollection
                {
                    {AuthKey, Name},
                    {userName.Name, userName.Value},
                    {password.Name, password.Value}
                };
            return ToQueryStringUtil(nameValueCollection);
        }

        public override void CreateMarkup(Control container)
        {
            var panel = new Panel { ID = Name + "Parameters", CssClass = "parameters" };
            container.Controls.Add(panel);

            var tbUserName = new TextBox();
            AddParameter(panel, userName, tbUserName);
            panel.Controls.Add(CreateValidator(tbUserName, "The user name can not be empty!", userNameValidator_ServerValidate));

            var tbPassword = new TextBox { TextMode = TextBoxMode.Password };
            tbPassword.Attributes["autocomplete"] = "off";
            AddParameter(panel, password, tbPassword);
        }

        private CustomValidator CreateValidator(Control controlToValidate, string errorMsg, CustomValidationMethod validationMethod)
        {
            var customValidator = new CustomValidator
            {
                ControlToValidate = controlToValidate.ID,
                ErrorMessage = errorMsg,
                Text = "*",
                ValidateEmptyText = true
            };
            customValidator.ToolTip = customValidator.ErrorMessage;
            customValidator.ServerValidate += new ServerValidateEventHandler(validationMethod);
            return customValidator;
        }

        void userNameValidator_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (ValidationEnabled)
            {
                var pattern = new Regex(UserNamePattern);
                args.IsValid = pattern.IsMatch(args.Value);
            }
        }

        public override Authentication ParseMarkup(Control markup)
        {
            return new OAuth
            {
                userName = ParseValue(markup, userName),
                password = ParseValue(markup, password),
            };
        }

        #region Utility methods
        private AuthenticationParam ParseValue(Control markup, AuthenticationParam parametr)
        {
            var parametrValue = markup.FindControl(Name + parametr.Name) as TextBox;
            parametr.Value = (parametrValue != null) ? parametrValue.Text : String.Empty;
            return parametr;
        }

        private void AddParameter(Control panel, AuthenticationParam parametr, TextBox tb)
        {
            panel.Controls.Add(new HtmlGenericControl("div") { InnerText = parametr.Text });
            tb.ID = Name + parametr.Name;
            if (tb.TextMode == TextBoxMode.Password)
            {
                EncodePassword(tb, parametr.Value);
            }
            else
            {
                tb.Text = parametr.Value;
            }
            panel.Controls.Add(tb);
        }

        private void EncodePassword(TextBox tb, string value)
        {
            tb.Text = value;
            tb.Attributes.Add("value", value);
        }

        private bool IsBulletSeries(string pass)
        {
            return pass.Equals(new String(GetBullet(), BulletsNumber));
        }

        private char GetBullet()
        {
            return Encoding.GetEncoding(1252).GetString(new byte[] { 149 })[0];
        }
        #endregion

        #region IEncrypted
        public void UpdateEncryptedFields(object authentication)
        {
            var auth = authentication as OAuth;
            if (auth != null && IsBulletSeries(password.Value))
            {
                password.Value = auth.password.Value;
            }
        }

        public void InvokeEncryption()
        {
            password.Value = !String.IsNullOrEmpty(password.Value) ? new String(GetBullet(), BulletsNumber) : String.Empty;
        }
        #endregion

        private delegate void CustomValidationMethod(object source, ServerValidateEventArgs args);
    }
}
