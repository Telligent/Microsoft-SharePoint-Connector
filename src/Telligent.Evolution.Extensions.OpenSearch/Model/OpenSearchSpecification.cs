using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Telligent.Evolution.Extensions.OpenSearch.Model.Specification;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class OpenSearchSpecification
    {
        private static readonly Regex OptionalTag = new Regex(@"^\{.+\?\}$");
        private readonly Dictionary<string, string> defaultParameters;

        public OpenSearchSpecification(IOpenSearchSpecification specification)
        {
            defaultParameters = specification.Get();
        }

        public string ParseUrl(string url, Dictionary<string, string> userParameters)
        {
            foreach (var key in userParameters.Keys)
                InsertValue(ref url, key, userParameters[key]);
            foreach (var key in defaultParameters.Keys)
                InsertRequiredValue(ref url, key, defaultParameters[key]);
            return TrimOptionalTags(url);
        }

        #region Utility methods
        private void InsertRequiredValue(ref string query, string key, string value)
        {
            string requiredTemplateParameters = "{" + key + "}";
            query = query.Replace(requiredTemplateParameters, value);
        }

        private void InsertValue(ref string query, string key, string value)
        {
            InsertRequiredValue(ref query, key, value);
            string optionalTemplateParameters = "{" + key + "?}";
            query = query.Replace(optionalTemplateParameters, value);
        }

        private string TrimOptionalTags(string url)
        {
            var result = new Uri(url);
            NameValueCollection query = HttpUtility.ParseQueryString(result.Query);
            query.AllKeys.Where(key => OptionalTag.IsMatch(query[key])).ToList().ForEach(query.Remove);
            return String.Format("{0}?{1}",result.GetLeftPart(UriPartial.Path), ToQueryStringUtil(query));
        }

        private String ToQueryStringUtil(NameValueCollection parameters)
        {
            string[] queryKeyValue = (from key in parameters.AllKeys
                                      select key + "=" + parameters[key]).ToArray();
            return String.Join("&", queryKeyValue);
        }
        #endregion
    }
}
