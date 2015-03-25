using System;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync
{
    public class Configuration : ModalPage
    {
        private const string AddPageTitleText = "Add SharePoint Credentials";
        private const string EditPageTitleText = "Edit SharePoint Credentials";
        private const string EmptySPUrlErrorMsg = "SharePoint Site Collection url can not be empty!";

        private SPConfiguration spConfig;
        private SPProfileSyncProvider spSyncSettings;
        private SPProfileSyncProviderList spSyncSettingsList;
        private readonly AuthenticationBuilder authBuilder = new AuthenticationBuilder(new ServiceAccount());

        protected enum PageMode { Add, Edit }
        protected PageMode Mode;
        protected bool FarmSyncEnabled
        {
            get
            {
                return spConfig != null && spConfig.FarmSyncEnabled;
            }
        }

        #region Controls
        protected TextBox TbSPSiteUrl;
        protected TextBox TbUserIdFieldName;
        protected TextBox TbUserEmailFieldName;
        protected TextBox TbFarmUserEmailFieldName;
        protected TextBox TbFarmUserIdFieldName;
        protected RequiredFieldValidator ReqSPSiteUrl;
        protected CustomValidator CredentialsValidator;
        protected ValidationSummary ValidationSummary;
        protected HtmlGenericControl CtAuth;
        protected Literal ErrorMessage;
        protected LinkButton SaveBtn;
        #endregion

        #region Page Overriden
        protected override void OnInit(EventArgs e)
        {
            ErrorMessage.Visible = false;
            base.OnInit(e);
            EnsureChildControls();
            ReqSPSiteUrl.ErrorMessage = ReqSPSiteUrl.ToolTip = EmptySPUrlErrorMsg;
            CredentialsValidator.ServerValidate += CredentialsValidatorServerValidate;
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            TempStoreSPSyncSettingsList(false);
            Mode = Request.QueryString["mode"] == "add" ? PageMode.Add : PageMode.Edit;
            if (Mode == PageMode.Add)
            {
                spSyncSettings = new SPProfileSyncProvider(String.Empty, String.Empty, String.Empty, String.Empty, null);
                Header.Title = AddPageTitleText;
                CtAuth.Controls.Add(AuthenticationHelper.GetPropertyControls(authBuilder));

                TbUserIdFieldName.Text = "ID";
                TbUserEmailFieldName.Text = "EMail";
                TbFarmUserEmailFieldName.Text = "WorkEmail";
            }
            else
            {
                Header.Title = EditPageTitleText;
                int id;
                if (int.TryParse(Request.QueryString["id"], out id))
                {
                    spSyncSettings = spSyncSettingsList.Get(id);
                    spConfig = SPConfigurationService.Get(spSyncSettings.SPSiteURL, spSyncSettings.Authentication);
                    CtAuth.Controls.Add(AuthenticationHelper.SetPropertyControls(spSyncSettings.Authentication, authBuilder));
                    DisplaySyncSettings(spSyncSettings);
                }
            }

            TbFarmUserIdFieldName.Text = spSyncSettings.SPFarmUserIdFieldName;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            spSyncSettings.Authentication = AuthenticationHelper.FromHtml(CtAuth);
            var serviceAccount = spSyncSettings.Authentication as ServiceAccount;
            if (serviceAccount != null)
            {
                serviceAccount.ValidationEnabled = true;
            }
            CtAuth.Controls.Clear();
            CtAuth.Controls.Add(AuthenticationHelper.SetPropertyControls(spSyncSettings.Authentication, authBuilder));
        }
        #endregion

        protected void CredentialsValidatorServerValidate(object obj, ServerValidateEventArgs target)
        {
            var url = TbSPSiteUrl.Text;
            Authentication auth = AuthenticationHelper.FromHtml(CtAuth);
            target.IsValid = TestCredentials(url, auth);
        }

        protected void SaveBtnClick(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            TbUserIdFieldName.Text = (string.IsNullOrEmpty(TbUserIdFieldName.Text)) ? "ID" : TbUserIdFieldName.Text;
            TbUserEmailFieldName.Text = (string.IsNullOrEmpty(TbUserEmailFieldName.Text)) ? "EMail" : TbUserEmailFieldName.Text;
            TbFarmUserEmailFieldName.Text = (string.IsNullOrEmpty(TbFarmUserEmailFieldName.Text)) ? "WorkEmail" : TbFarmUserEmailFieldName.Text;

            try
            {
                Authentication auth = AuthenticationHelper.FromHtml(CtAuth);

                if (Mode == PageMode.Add)
                {
                    spSyncSettings = new SPProfileSyncProvider(TbSPSiteUrl.Text, TbUserIdFieldName.Text, TbUserEmailFieldName.Text, TbFarmUserEmailFieldName.Text, auth);
                }
                else
                {
                    spSyncSettings.SPSiteURL = TbSPSiteUrl.Text;
                    spSyncSettings.SPUserIdFieldName = TbUserIdFieldName.Text;
                    spSyncSettings.SPUserEmailFieldName = TbUserEmailFieldName.Text;
                    spSyncSettings.SPFarmUserEmailFieldName = TbFarmUserEmailFieldName.Text;
                    spSyncSettings.Authentication = auth;
                }

                const string script = @"setTimeout(function(){{CloseWindow('{0}');}},100);";
                CSControlUtility.Instance().RegisterClientScriptBlock(this, GetType(), "closechildwindow",
                    string.Format(script, JavaScript.Encode(spSyncSettings.ToXml())), true);

                TempStoreSPSyncSettingsList(true);
            }
            catch (Exception)
            {
                ShowErrorMessage();
            }
        }

        private static bool TestCredentials(string url, Authentication auth)
        {
            try
            {
                using (var spcontext = new SPContext(url, auth))
                {
                    spcontext.Load(spcontext.Web, w => w.Id);
                    spcontext.Load(spcontext.Site, s => s.Id);
                    spcontext.ExecuteQuery();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ShowErrorMessage()
        {
            ErrorMessage.Visible = true;
            ErrorMessage.Text = "Input data is invalid!";
        }

        #region Utility
        private void DisplaySyncSettings(SPProfileSyncProvider syncSettings)
        {
            TbSPSiteUrl.Text = syncSettings.SPSiteURL;
            TbUserIdFieldName.Text = syncSettings.SPUserIdFieldName;
            TbUserEmailFieldName.Text = syncSettings.SPUserEmailFieldName;
            TbFarmUserEmailFieldName.Text = syncSettings.SPFarmUserEmailFieldName;
        }

        private void TempStoreSPSyncSettingsList(bool delete)
        {
            var spProfileSyncSettingsListKey = Request.QueryString[SPProfileSyncControl.SettingsListKeyName];
            if (!String.IsNullOrEmpty(spProfileSyncSettingsListKey))
            {
                spSyncSettingsList = new SPProfileSyncProviderList(TemporaryStore.Get(new Guid(spProfileSyncSettingsListKey), delete));
            }
        }
        #endregion
    }
}
