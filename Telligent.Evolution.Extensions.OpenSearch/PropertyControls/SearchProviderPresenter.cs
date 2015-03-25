using System;
using System.Web;
using Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.OpenSearch.Controls;
using Telligent.Evolution.Extensions.OpenSearch.Controls.Layout;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class SearchProviderPresenter
    {
        // Modal settings
        const string Width = "350";
        const string Height = "400";
        const string EditPageRelativeUrl = "/SharePoint/OpenSearch/OpenSearchProviderPage.aspx";

        private readonly SearchProvider provider;
        private readonly SearchProvidersList providerList;
        private readonly string baseUrl;

        [ItemCollection(IsId = true)]
        public string Id { get { return provider.Id; } }

        [ItemCollection(Style = "font-weight: bold; cursor: pointer;", Order = 0, Region = Region.Title, Filtered = true)]
        public String Name { get { return HttpUtility.HtmlDecode(provider.Name); } }

        [ItemCollection(CssClass = "description", Order = 0, Region = Region.SubTitle)]
        public String OSDX_URL { get { return HttpUtility.HtmlDecode(provider.OpenSearchUrl); } }

        [ItemCollection(CssClass = "flags", Order = 2, Region = Region.SubTitle)]
        public String AuthName
        {
            get
            {
                if (provider.Authentication is Windows)
                    return "Windows";
                if (provider.Authentication is ServiceAccount)
                    return "Service";
                return "Anonymous";
            }
        }

        [ItemCollection( Text = "Edit", Style = "float:right;", Order = 1, Region = Region.HoverButtons, CssClass = "CommonTextButton")]
        public string Edit
        {
            get
            {
                return EditBtnLink();
            }
        }

        [ItemCollection(Text = "Delete", Style = "float:right;", Order = 1, Region = Region.HoverButtons, CssClass = "CommonTextButton")]
        public string Delete
        {
            get
            {
                return DeleteBtnLink();
            }
        }

        public SearchProviderPresenter(SearchProvider provider, SearchProvidersList providerList, string baseUrl)
        {
            this.provider = provider;
            this.providerList = providerList;
            this.baseUrl = baseUrl;
        }

        private string EditBtnLink()
        {
            const string functionCallback = "AddProvider";
            string queryString = "?mode=edit&" + EditLinkQueryString(provider);
            return String.Format("javascript: Telligent_Modal.Open('{0}', {1}, {2}, {3})",
                String.Concat(baseUrl, EditPageRelativeUrl, queryString),
                Width,
                Height,
                functionCallback);
        }

        private string DeleteBtnLink()
        {
            return String.Format("javascript: DeleteProvider({0})", provider.Id);
        }

        private static string EditLinkQueryString(SearchProvider provider)
        {
            return String.Format("id={0}&providername={1}&{2}", provider.Id, HttpUtility.UrlEncode(HttpUtility.HtmlDecode(provider.Name)), provider.Authentication.ToQueryString());
        }
    }
}
