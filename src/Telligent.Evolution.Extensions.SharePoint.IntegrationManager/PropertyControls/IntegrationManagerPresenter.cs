using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls;
using Telligent.Evolution.Extensions.SharePoint.Components.Controls.Layout;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;
using WindowsAuth = Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods.Windows;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager
{
    public class IntegrationManagerPresenter
    {
        const string Width = "350";
        const string Height = "400";
        const string EditPageRelativeUrl = "/SharePoint/IntegrationManager/Configuration.aspx";

        private readonly IntegrationProvider provider;
        private readonly string providersKey;
        private readonly string baseUrl;

        [ItemCollection(IsId = true)]
        public string Id { get { return provider.Id; } }

        [ItemCollection(Style = "font-weight: bold;", Order = 0, Region = Region.Title, Filtered = true)]
        public String SPSiteName { get { return provider.SPSiteName; } }

        [ItemCollection(CssClass = "description", Order = 0, Region = Region.SubTitle)]
        public String SPSiteURL { get { return provider.SPSiteURL; } }

        [ItemCollection(CssClass = "flags", Order = 2, Region = Region.SubTitle)]
        public String AuthName
        {
            get
            {
                if (provider.Authentication is WindowsAuth)
                    return "Windows";
                if (provider.Authentication is ServiceAccount)
                    return "Service";
                if (provider.Authentication is SAML)
                    return "SAML";
                if (provider.Authentication is Components.AuthenticationUtil.Methods.OAuth)
                    return "OAuth";
                return "Anonymous";
            }
        }

        [ItemCollection(CssClass = "flags", Order = 2, Region = Region.SubTitle)]
        public String IsDefault
        {
            get
            {
                if (provider.IsDefault)
                    return "Default";
                return String.Empty;
            }
        }

        [ItemCollection(Text = "Edit", Style = "float:right;", Order = 1, Region = Region.HoverButtons, CssClass = "CommonTextButton")]
        public String Edit
        {
            get
            {
                return EditBtnLink();
            }
        }

        [ItemCollection(Text = "Delete", Style = "float:right;", Order = 1, Region = Region.HoverButtons, CssClass = "CommonTextButton")]
        public String Delete
        {
            get
            {
                return DeleteLinkQueryString();
            }
        }

        public IntegrationManagerPresenter(IntegrationProvider instance, string key, string url)
        {
            provider = instance;
            providersKey = key;
            baseUrl = url;
        }

        public static List<Control> HeaderButtons(IntegrationProviders providers, string contentDivId, string baseUrl)
        {
            var addBtn = new HtmlGenericControl("a");
            addBtn.Attributes["href"] = AddBtnLink(providers, baseUrl);
            addBtn.Attributes["class"] = "PanelSaveButton CommonTextButton";
            addBtn.Attributes["style"] = "float: right;";
            addBtn.InnerText = "Add";

            var deleteBtn = new HtmlGenericControl("a");
            deleteBtn.Attributes["href"] = String.Format("javascript: DeleteSelectedSPObjecManager(jQuery('#{0}'))", contentDivId);
            deleteBtn.Attributes["class"] = "PanelSaveButton CommonTextButton";
            deleteBtn.Attributes["style"] = "float: right;";
            deleteBtn.InnerText = "Delete Selected";

            // if the Root Group is mapped, then the Add Button will be hidden
            /*
            const int siteRootGroupId = 1;
            if (managers.GetManagerByGroupId(siteRootGroupId) != null)
                addBtn.Attributes["style"] = "display:none;";
            */

            return new List<Control> { addBtn, deleteBtn };
        }

        #region Utility methods

        private string EditBtnLink()
        {
            const string functionCallback = "AddManager";
            string queryString = "?mode=edit&id=" + provider.Id + "&spobjectmanagerlist=" + providersKey;
            return String.Format("javascript: Telligent_Modal.Open('{0}', {1}, {2}, {3})",
                String.Concat(baseUrl, EditPageRelativeUrl, queryString),
                Width,
                Height,
                functionCallback);
        }

        private string DeleteLinkQueryString()
        {
            return String.Format("javascript: DeleteManager({0})", provider.Id);
        }

        private static string AddBtnLink(IntegrationProviders providers, string baseUrl)
        {
            const string functionCallback = "AddManager";
            string key = TemporaryStore.Add(providers.ToXml()).ToString();
            string queryString = "?mode=add&spobjectmanagerlist=" + key;
            return String.Format("javascript: Telligent_Modal.Open('{0}', {1}, {2}, {3})",
                String.Concat(baseUrl, EditPageRelativeUrl, queryString),
                Width,
                Height,
                functionCallback);
        }

        #endregion
    }
}
