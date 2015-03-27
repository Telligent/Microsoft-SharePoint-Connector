using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Routing.Entities
{
    public class RoutedPage
    {
        /// <summary>
        /// Content page name from xml
        /// </summary>
        public string PageName { get; set; }

        /// <summary>
        /// Friendly page name for scripted widgets
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// Route URL name
        /// </summary>
        public string UrlName { get; set; }

        /// <summary>
        /// Route URL pattern
        /// </summary>
        public string UrlPattern { get; set; }

        /// <summary>
        /// Route parameters constraints
        /// </summary>
        public object ParameterConstraints { get; set; }

        public Action<PageContext> ParseContext { get; set; }

        /// <summary>
        /// Generates a relative URL based on the given parameters
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public string BuildUrl(int groupId, Dictionary<string, string> parameters = null)
        {
            return PublicApi.Url.BuildUrl(UrlName, groupId, parameters);
        }
    }
}
