using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods
{
    public class ServiceAccount : Authentication, IEncrypted
    {
        const string UserNamePattern = @"^[a-zA-Z0-9]+((_|-| |\.)*[a-zA-Z0-9]+)*$";
        const int BulletsNumber = 8;

        private AuthenticationParam userName;
        private AuthenticationParam password;
        private AuthenticationParam domain;

        public override string Name
        {
            get { return "Service"; }
        }

        public override string Text
        {
            get { return "Service Account"; }
        }

        public ServiceAccount()
        {
            this.ValidationEnabled = false;
            userName = new AuthenticationParam("Name", "User Name", String.Empty);
            password = new AuthenticationParam("Password", "Password", String.Empty);
            domain = new AuthenticationParam("Domain", "Domain", String.Empty);
        }

        public ServiceAccount(string userName, string password, string domain = null)
            : this()
        {
            this.userName.Value = userName;
            this.password.Value = password;
            this.domain.Value = domain;
        }

        public ServiceAccount(string queryString)
            : this()
        {
            NameValueCollection queryKeyValue = HttpUtility.ParseQueryString(queryString);
            userName.Value = queryKeyValue[userName.Name];
            password.Value = queryKeyValue[password.Name];
            domain.Value = queryKeyValue[domain.Name];
        }

        public override ICredentials Credentials()
        {
            NetworkCredential cred = new NetworkCredential
            {
                UserName = userName.Value,
                Password = password.Value,
                Domain = domain.Value
            };
            return cred;
        }

        public override string ToQueryString()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection.Add(Authentication.AuthKey, this.Name);
            nameValueCollection.Add(userName.Name, userName.Value);
            nameValueCollection.Add(password.Name, password.Value);
            nameValueCollection.Add(domain.Name, domain.Value);
            return Authentication.ToQueryStringUtil(nameValueCollection);
        }

        public override void CreateMarkup(Control container)
        {
            Panel panel = new Panel() { ID = this.Name + "Parameters", CssClass = "parameters" };
            container.Controls.Add(panel);

            TextBox tbUserName = new TextBox();
            AddParameter(panel, userName, tbUserName);
            panel.Controls.Add(CreateValidator(tbUserName, "The user name can not be empty!", userNameValidator_ServerValidate));

            TextBox tbPassword = new TextBox();
            tbPassword.TextMode = TextBoxMode.Password;
            tbPassword.Attributes["autocomplete"] = "off";
            AddParameter(panel, password, tbPassword);

            TextBox tbDomain = new TextBox();
            AddParameter(panel, domain, tbDomain);
        }

        private CustomValidator CreateValidator(Control controlToValidate, string errorMsg, CustomValidationMethod validationMethod)
        {
            CustomValidator customValidator = new CustomValidator
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
                Regex pattern = new Regex(UserNamePattern);
                args.IsValid = pattern.IsMatch(args.Value);
            }
        }

        public override Authentication ParseMarkup(Control markup)
        {
            return new ServiceAccount
            {
                userName = ParseValue(markup, userName),
                password = ParseValue(markup, password),
                domain = ParseValue(markup, domain)
            };
        }

        #region Utility methods
        private AuthenticationParam ParseValue(Control markup, AuthenticationParam parametr)
        {
            TextBox parametrValue = markup.FindControl(this.Name + parametr.Name) as TextBox;
            parametr.Value = (parametrValue != null) ? parametrValue.Text : String.Empty;
            return parametr;
        }

        private void AddParameter(Control panel, AuthenticationParam parametr, TextBox tb)
        {
            panel.Controls.Add(new HtmlGenericControl("div") { InnerText = parametr.Text });
            tb.ID = this.Name + parametr.Name;
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

        private bool IsBulletSeries(string password)
        {
            return password.Equals(new String(GetBullet(), BulletsNumber));
        }

        private char GetBullet()
        {
            return Encoding.GetEncoding(1252).GetString(new byte[] { 149 })[0];
        }
        #endregion

        #region IEncrypted
        public void UpdateEncryptedFields(object authentication)
        {
            ServiceAccount auth = authentication as ServiceAccount;
            if (auth != null && IsBulletSeries(this.password.Value))
            {
                this.password.Value = auth.password.Value;
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
