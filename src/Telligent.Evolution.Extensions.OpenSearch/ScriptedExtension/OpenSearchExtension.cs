using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil;
using Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.OpenSearch.Model.Specification;
using Telligent.Evolution.SecurityModules;

namespace Telligent.Evolution.Extensions.OpenSearch.ScriptedExtension
{
    public interface IOpenSearch
    {
        string Url(SearchProvider provider, SearchWidgetConfiguration configuration, IDictionary options);

        SearchWidgetConfiguration Configuration(string xml);

        SearchProvider Provider(string providerId);

        Dictionary<string, int> Summary(List<SearchResult> searchResultList);

        SearchResultsList Search(SearchWidgetConfiguration configuration, IDictionary options);

        string Favicon(SearchProvider provider);
    }

    [Documentation(Category = "OpenSearch")]
    public class OpenSearch : IOpenSearch
    {
        private static readonly OpenSearchSpecification Specification = new OpenSearchSpecification(new OpenSearchV1_1());

        private static readonly List<string> FAVIconList = new List<string>
        {
            "favicon.ico",
            "_layouts/images/favicon.ico"
        };

        private WindowsImpersonationContext impersonate;

        [Documentation(Description = "External url for current search")]
        public string Url(
            SearchProvider provider,
            SearchWidgetConfiguration configuration,
            [Documentation(Name = "Query", Type = typeof(string)),
            Documentation(Name = "PageIndex", Type = typeof(string)),
            Documentation(Name = "PageSize", Type = typeof(string))]
            IDictionary options)
        {
            var specification = new OpenSearchSpecification(new OpenSearchV1_1());
            var parameters = SearchParametersAdapter(options);
            return !String.IsNullOrEmpty(provider.MoreResultsUrl) && configuration.ShowMoreResultsLink ? specification.ParseUrl(provider.MoreResultsUrl, parameters) : null;
        }

        public SearchWidgetConfiguration Configuration(string xml)
        {
            return !String.IsNullOrEmpty(xml) ? new SearchWidgetConfiguration(HttpUtility.HtmlDecode(xml)) : null;
        }

        public SearchProvider Provider(string providerId)
        {
            return OpenSearchPlugin.GetSearchProvidersList.Get(providerId);
        }

        public Dictionary<string, int> Summary(List<SearchResult> searchResultList)
        {
            if (searchResultList == null)
                return null;

            return (from SearchResult item in searchResultList
                    where item.FileExtension != null
                    orderby item.FileExtension
                    group item by item.FileExtension.TrimStart('.').ToLower() into groupedItems
                    select new { Name = groupedItems.Key, Count = groupedItems.Count() }).ToDictionary(item => item.Name, item => item.Count);
        }

        [Documentation(Description = "The method executes search using query string values")]
        public SearchResultsList Search(
            SearchWidgetConfiguration configuration,
            [Documentation(Name = "Query", Type = typeof(string)),
            Documentation(Name = "PageIndex", Type = typeof(int)),
            Documentation(Name = "PageSize", Type = typeof(int))]
            IDictionary options)
        {
            var provider = Provider(configuration.ProviderId);
            var credentials = provider.Authentication;
            Dictionary<string, string> parameters;
            string openSearchUrl;
            if (options != null && options["PageIndex"] == null)
            {
                var allresults = new SearchResultsList { Items = new List<SearchResult>() };

                int index = 0;
                const int maxIndex = 10;
                int pageSize = 100;
                if (options["PageSize"] != null)
                {
                    int.TryParse(options["PageSize"].ToString(), out pageSize);
                }
                options["PageSize"] = pageSize;
                SearchResultsList current;
                do
                {
                    options["PageIndex"] = index;
                    parameters = SearchParametersAdapter(options);
                    openSearchUrl = Specification.ParseUrl(provider.OpenSearchUrl, parameters);
                    try
                    {
                        current = ExecuteSearch(openSearchUrl, credentials, configuration);
                        allresults.Items.AddRange(current.GetItems());
                    }
                    catch (WebException)
                    {
                        // format of xml file is invalid
                        return allresults;
                    }
                    index++;
                }
                while (current.GetItems().Count >= pageSize && index < maxIndex);
                return allresults;
            }
            parameters = SearchParametersAdapter(options);
            openSearchUrl = Specification.ParseUrl(provider.OpenSearchUrl, parameters);
            try
            {
                SearchResultsList results = ExecuteSearch(openSearchUrl, credentials, configuration);
                //this.CurrentResults = results;
                return results;
            }
            catch (WebException)
            {
                // format of xml file is invalid
                return null;
            }
        }

        public string Favicon(SearchProvider provider)
        {
            var checkedProperties = new List<string>
            {
                provider.MoreResultsUrl, 
                provider.MoreLinkTemplate, 
                provider.OpenSearchUrl
            };
            string favicon = null;
            for (int i = 0; i < checkedProperties.Count && String.IsNullOrEmpty(favicon); i++)
            {
                if (String.IsNullOrEmpty(checkedProperties[i]))
                    continue;
                try
                {
                    favicon = SearchFavicon(checkedProperties[i]);
                }
                catch (UriFormatException) { }
            }
            return favicon;
        }

        #region Utility private methods
        private string SearchFavicon(string url)
        {
            var searchuri = new Uri(url);
            string extBaseUrl = String.Format("{0}://{1}/", searchuri.Scheme, searchuri.Authority);
            foreach (var favicon in FAVIconList)
            {
                string faviconUrl = extBaseUrl + favicon;
                if (PageExists(faviconUrl))
                    return faviconUrl;
            }
            return String.Empty;
        }

        private Dictionary<string, string> SearchParametersAdapter(IDictionary options)
        {
            string searchTerms = options["Query"] != null ? options["Query"].ToString() : String.Empty;
            var parameters = new Dictionary<string, string> { { "searchTerms", searchTerms } };
            if (options["PageIndex"] != null && options["PageSize"] != null)
            {
                int pageIndex = Convert.ToInt32(options["PageIndex"]);
                int pageSize = Convert.ToInt32(options["PageSize"]);
                int startIndex = pageIndex * pageSize + 1;
                parameters.Add("startIndex", startIndex.ToString(CultureInfo.InvariantCulture));
                parameters.Add("startPage", (pageIndex + 1).ToString(CultureInfo.InvariantCulture));
                parameters.Add("count", pageSize.ToString(CultureInfo.InvariantCulture));
            }
            return parameters;
        }

        private SearchResultsList ExecuteSearch(string url, Authentication credentials, SearchWidgetConfiguration configuration)
        {
            var xmlResponse = GetResponse(url, credentials);
            return new SearchResultsList(xmlResponse);
        }

        private string GetResponse(string url, Authentication credentials)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            if (credentials != null)
            {
                if (credentials is Windows)
                {
                    request.UseDefaultCredentials = true;
                    impersonate = Impersonate();
                }

                if (credentials is ServiceAccount)
                {
                    request.UseDefaultCredentials = false;
                    request.Credentials = credentials.Credentials() as NetworkCredential;
                }

                //Fix for SharePoint Mixed Mode Authentication
                request.Headers = new WebHeaderCollection { { "X-FORMS_BASED_AUTH_ACCEPTED", "f" } };
            }

            return GetResponse(request);
        }

        private string GetResponse(HttpWebRequest request)
        {
            String xmlResults = String.Empty;
            try
            {
                request.Method = "GET";

                WebResponse response = request.GetResponse();
                using (Stream receiveStream = response.GetResponseStream())
                {
                    Encoding encode = Encoding.GetEncoding("utf-8");
                    if (receiveStream != null)
                    {
                        var readStream = new StreamReader(receiveStream, encode);
                        xmlResults = readStream.ReadToEnd();
                    }
                }
            }
            catch
            {
                //wrong credentials or request
            }

            if (impersonate != null)
                impersonate.Undo();

            return xmlResults;
        }

        private WindowsImpersonationContext Impersonate()
        {
            try
            {
                var ssop = (SingleSignOnPrincipal)CSContext.Current.Context.User;
                var principal = (WindowsPrincipal)ssop.OriginalPrincipal;
                var id = (WindowsIdentity)principal.Identity;

                return id.Impersonate();
            }
            catch (Exception ex)
            {
                string msg = string.Format("Enable Windows Authentication and set roleManager enabled=\"false\" : {0} {1}", ex.Message, ex.StackTrace);
                throw new Exception(msg);
            }

            return null;
        }

        private bool PageExists(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Head;
                var response = (HttpWebResponse)request.GetResponse();
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException)
            {
                // resource is not found
                return false;
            }
        }
        #endregion
    }
}
