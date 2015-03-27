using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.Model;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync
{
    public class Administration : ModalPage
    {
        private const string PageTitleText = "User Profile Administration";

        private delegate string Formatter(ProfileField f);

        #region Controls

        // SP Site User Profile fields
        protected DropDownList ddlSPSiteProfileFields;

        // SP Farm User Profile fields
        protected DropDownList ddlSPFarmProfileFields;

        // TE User Profile fields
        protected DropDownList ddlTEProfileFields;

        protected HiddenField hdnSiteProfileFieldsMap;

        protected HiddenField hdnFarmProfileFieldsMap;

        protected CheckBox cbFarmSyncEnable;

        protected CheckBox cbSyncEnable;

        protected Literal ErrorMessage;

        protected LinkButton SaveBtn;

        #endregion

        private SPConfiguration spConfig;

        private SPProfileSyncProvider spSyncSettings;

        private SPProfileSyncProviderList spSyncSettingsList;

        #region Page Overriden

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            Header.Title = PageTitleText;
            ErrorMessage.Visible = false;

            EnsureChildControls();
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            TempStoreSPSyncSettingsList(false);

            int id;
            if (int.TryParse(Request.QueryString["id"], out id))
            {
                spSyncSettings = spSyncSettingsList.Get(id);
                spConfig = SPConfigurationService.Get(spSyncSettings.SPSiteURL, spSyncSettings.Authentication);
                if (spSyncSettings.SyncConfig != null)
                {
                    spConfig.FarmProfileMappedFields = spSyncSettings.SyncConfig.FarmProfileMappedFields;
                    spConfig.SiteProfileMappedFields = spSyncSettings.SyncConfig.SiteProfileMappedFields;
                    spConfig.SyncEnabled = spSyncSettings.SyncConfig.SyncEnabled;
                    spConfig.FarmSyncEnabled = spSyncSettings.SyncConfig.FarmSyncEnabled;
                }
            }
            else
            {
                ShowErrorMessage("Incorrect request. Please try again or contact your administrator.");
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!IsPostBack)
            {
                try
                {
                    // Data Binding
                    cbSyncEnable.Checked = spConfig.SyncEnabled;
                    cbFarmSyncEnable.Checked = spConfig.FarmSyncEnabled;
                    BindDropDownListData(ddlSPSiteProfileFields, spConfig.SiteProfileFields.OrderBy(f => f.Title).ToList(), f => f.Title);
                    BindDropDownListData(ddlSPFarmProfileFields, spConfig.FarmProfileFields.OrderBy(f => f.Title), f => f.Title);
                    BindDropDownListData(ddlTEProfileFields, TEUserProfileFieldsHelper.GetFields().OrderBy(f => f.Name).ToList(), f => f.Title);
                    hdnSiteProfileFieldsMap.Value = GetJSONMapping(spConfig.SiteProfileMappedFields);
                    hdnFarmProfileFieldsMap.Value = GetJSONMapping(spConfig.FarmProfileMappedFields);
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex.Message);
                    SPLog.RoleOperationUnavailable(ex, ex.Message);
                }
            }
        }

        #endregion

        protected void ApplyBtnClick(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                ShowErrorMessage("Invalid Data");
                return;
            }

            try
            {
                spConfig.SyncEnabled = cbSyncEnable.Checked;
                spConfig.FarmSyncEnabled = cbFarmSyncEnable.Checked;

                spConfig.SiteProfileMappedFields = new List<UserFieldMapping>();
                ProcessPostedData(spConfig.SiteProfileMappedFields, hdnSiteProfileFieldsMap.Value);

                spConfig.FarmProfileMappedFields = new List<UserFieldMapping>();
                ProcessPostedData(spConfig.FarmProfileMappedFields, hdnFarmProfileFieldsMap.Value);

                // Save Configuration to a syncSettings object
                spSyncSettings.SyncConfig = new SPBaseConfig
                {
                    FarmProfileMappedFields = spConfig.FarmProfileMappedFields,
                    SiteProfileMappedFields = spConfig.SiteProfileMappedFields,
                    SyncEnabled = spConfig.SyncEnabled,
                    FarmSyncEnabled = spConfig.FarmSyncEnabled
                };

                BindDropDownListData(ddlSPSiteProfileFields, spConfig.SiteProfileFields.OrderBy(f => f.Title).ToList(), f => String.Format("{0} - {1}", f.Title, f.Name));
                BindDropDownListData(ddlSPFarmProfileFields, spConfig.FarmProfileFields.OrderBy(f => f.Title), f => f.Title);
                BindDropDownListData(ddlTEProfileFields, TEUserProfileFieldsHelper.GetFields().OrderBy(f => f.Name).ToList(), f => f.Name);

                const string script = @"setTimeout(function(){{parent.window.frames[0].AddSyncSettings('{0}');}},100);";
                CSControlUtility.Instance().RegisterClientScriptBlock(this, GetType(), "applychildwindow", string.Format(script, JavaScript.Encode(spSyncSettings.ToXml())), true);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
                SPLog.RoleOperationUnavailable(ex, ex.Message);
            }
        }

        protected void SaveBtnClick(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                ShowErrorMessage("Invalid Data");
                return;
            }

            try
            {
                spConfig.SyncEnabled = cbSyncEnable.Checked;
                spConfig.FarmSyncEnabled = cbFarmSyncEnable.Checked;

                spConfig.SiteProfileMappedFields = new List<UserFieldMapping>();
                ProcessPostedData(spConfig.SiteProfileMappedFields, hdnSiteProfileFieldsMap.Value);

                spConfig.FarmProfileMappedFields = new List<UserFieldMapping>();
                ProcessPostedData(spConfig.FarmProfileMappedFields, hdnFarmProfileFieldsMap.Value);

                // Save Configuration to a syncSettings object
                spSyncSettings.SyncConfig = new SPBaseConfig
                {
                    FarmProfileMappedFields = spConfig.FarmProfileMappedFields,
                    SiteProfileMappedFields = spConfig.SiteProfileMappedFields,
                    SyncEnabled = spConfig.SyncEnabled,
                    FarmSyncEnabled = spConfig.FarmSyncEnabled
                };

                const string script = @"setTimeout(function(){{CloseWindow('{0}');}},100);";
                CSControlUtility.Instance().RegisterClientScriptBlock(this, GetType(), "closechildwindow", string.Format(script, JavaScript.Encode(spSyncSettings.ToXml())), true);

                TempStoreSPSyncSettingsList(true);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
                SPLog.RoleOperationUnavailable(ex, ex.Message);
            }
        }

        private bool IsValid(SPConfiguration config)
        {
            if (config.FarmSyncEnabled && config.FarmProfileMappedFields.Count > 0)
            {
                return true;
            }

            if (!config.FarmSyncEnabled && config.SiteProfileMappedFields.Count > 0)
            {
                return true;
            }
            return false;
        }

        private void ProcessPostedData(List<UserFieldMapping> mapping, string hdnFieldValue)
        {
            // try parse json data
            if (!String.IsNullOrEmpty(hdnFieldValue) && hdnFieldValue != "null")
            {
                var js = new JavaScriptSerializer();
                mapping.AddRange(js.Deserialize<UserFieldMapping[]>(hdnFieldValue));
            }
        }

        #region Utility

        private void ShowErrorMessage(string msg)
        {
            ErrorMessage.Visible = true;
            ErrorMessage.Text = msg;
        }

        private void TempStoreSPSyncSettingsList(bool delete)
        {
            var spProfileSyncSettingsListKey = Request.QueryString[SPProfileSyncControl.SettingsListKeyName];
            if (!String.IsNullOrEmpty(spProfileSyncSettingsListKey))
            {
                spSyncSettingsList = new SPProfileSyncProviderList(TemporaryStore.Get(new Guid(spProfileSyncSettingsListKey), delete));
            }
        }

        private void BindDropDownListData(DropDownList control, IEnumerable<ProfileField> fields, Formatter formatter)
        {
            control.Items.Add(new ListItem());
            foreach (var f in fields)
            {
                var item = new ListItem(formatter(f), f.Name);
                if (!f.ImportAvailable)
                {
                    item.Attributes.Add("noimp", "1");
                }
                
                item.Attributes.Add("title", f.Name);

                control.Items.Add(item);
            }
        }

        private string GetJSONMapping(List<UserFieldMapping> mapping)
        {
            if (mapping != null && mapping.Count > 0)
            {
                var js = new JavaScriptSerializer();
                return js.Serialize(mapping);
            }
            return "null";
        }

        #endregion
    }
}

