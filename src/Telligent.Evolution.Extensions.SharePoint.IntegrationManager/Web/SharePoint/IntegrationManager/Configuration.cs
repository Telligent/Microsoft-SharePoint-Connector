using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager
{
    public class Configuration : ModalPage
    {
        private const string AddPageTitleText = "Add SharePoint Credentials";
        private const string EditPageTitleText = "Edit SharePoint Credentials";
        private const string EmptySPUrlErrorMsg = "SharePoint Site Collection URL can not be empty!";
        private const string WrongSPUrlOrCredentialsErrorMsg = "SharePoint Site Collection was not found or wrong authentication was used!";
        private const string WrongSPSiteCollectionMappingErrorMsg = "SharePoint Site Collection provider already exists!";

        private enum PageMode { Add, Edit }

        private PageMode pageMode;
        private IntegrationProviders allProviders;
        private IntegrationProvider currentProvider;

        protected LinkButton SaveBtn;
        protected TextBox TbSPSiteUrl;
        protected HtmlGenericControl CtAuth;
        protected CheckBox CbIsDefault;
        protected CustomValidator SPSiteCustomValidator;
        protected RequiredFieldValidator SPSiteRequiredFieldValidator;
        protected ValidationSummary ValidationSummary;

        #region Page Overriden

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            allProviders = FromTempStore();
            pageMode = Request.QueryString["mode"] == "add" ? PageMode.Add : PageMode.Edit;
            if (pageMode == PageMode.Add)
            {
                Page.Header.Title = AddPageTitleText;
                currentProvider = new IntegrationProvider();
                CtAuth.Controls.Add(AuthenticationHelper.GetPropertyControls());
            }
            else
            {
                Page.Header.Title = EditPageTitleText;
                currentProvider = allProviders.GetById(Request.QueryString["id"]);
                CtAuth.Controls.Add(AuthenticationHelper.SetPropertyControls(currentProvider.Authentication));
            }
            TbSPSiteUrl.Text = currentProvider.SPSiteURL;
            CbIsDefault.Checked = currentProvider.IsDefault;

            SPSiteRequiredFieldValidator.ErrorMessage = EmptySPUrlErrorMsg;
            SPSiteRequiredFieldValidator.ToolTip = SPSiteRequiredFieldValidator.ErrorMessage;
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            // The IsDefault option should be available only if it was not used in another provider
            var defaultProviderExists = allProviders.Collection.Any(provider => provider.IsDefault && provider.Id != currentProvider.Id);
            CbIsDefault.Enabled = !defaultProviderExists;
        }

        #endregion

        protected void SaveBtnClick(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            try
            {
                UpdateCurrentProvider(TbSPSiteUrl.Text, AuthenticationHelper.FromHtml(CtAuth));
                ClearTempStore();
            }
            catch (Exception ex)
            {
                SPLog.DataProvider(ex, "Error getting Saving: {0} {1}", ex.Message, ex.StackTrace);
            }
        }

        protected void SPSiteUrlServerValidate(object source, ServerValidateEventArgs args)
        {
            var url = args.Value;
            var auth = AuthenticationHelper.FromHtml(CtAuth);
            var spsite = new SPSite(url, auth);

            // Check that SharePoint Site Exists
            if (!spsite.IsSite())
            {
                args.IsValid = false;
                SPSiteCustomValidator.ErrorMessage = WrongSPUrlOrCredentialsErrorMsg;
                SPSiteCustomValidator.ToolTip = SPSiteCustomValidator.ErrorMessage;
                return;
            }

            // Check that SharePoint Site has no duplicates
            if (HasDuplicates(spsite))
            {
                args.IsValid = false;
                SPSiteCustomValidator.ErrorMessage = WrongSPSiteCollectionMappingErrorMsg;
                SPSiteCustomValidator.ToolTip = SPSiteCustomValidator.ErrorMessage;
            }
        }

        private void UpdateCurrentProvider(string spSiteUrl, Authentication auth)
        {
            if (pageMode == PageMode.Add)
            {
                currentProvider = new IntegrationProvider(spSiteUrl, 0, auth);
            }
            else
            {
                currentProvider.SPSiteURL = spSiteUrl;
                currentProvider.TEGroupId = 0;
                currentProvider.Authentication = auth;
                currentProvider.Initialize();
            }

            currentProvider.IsDefault = CbIsDefault.Checked;

            const string script = @"setTimeout(function(){{CloseWindow('{0}');}},100);";

            CSControlUtility.Instance().RegisterClientScriptBlock(this, typeof(Configuration), "closechildwindow",
                string.Format(script, JavaScript.Encode(currentProvider.ToXml())), true);
        }

        #region Utility

        private bool HasDuplicates(SPSite spsite)
        {
            var existedWebs = GetExistedWebs();
            if (existedWebs.Count == 0) return false;

            var newWeb = spsite.OpenWeb();
            return Enumerable.Contains(existedWebs, newWeb);
        }

        private List<SPWeb> GetExistedWebs()
        {
            return (from provider in allProviders.Collection
                    where provider.Id != currentProvider.Id
                    select new SPWeb(provider.SPSiteID, provider.SPWebID)).ToList();
        }

        private IntegrationProviders FromTempStore(bool delete = false)
        {
            var key = Request.QueryString["spobjectmanagerlist"];
            if (key != null)
            {
                return allProviders = new IntegrationProviders(TemporaryStore.Get(new Guid(key), delete));
            }
            return null;
        }

        private void ClearTempStore()
        {
            var key = Request.QueryString["spobjectmanagerlist"];
            if (key != null)
            {
                TemporaryStore.Get(new Guid(key), delete: true);
            }
        }

        #endregion
    }
}
