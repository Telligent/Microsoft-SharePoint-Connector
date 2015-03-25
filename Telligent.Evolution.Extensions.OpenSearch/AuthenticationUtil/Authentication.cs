using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.Collections.Specialized;
using System.Web.UI;

namespace Telligent.Evolution.Extensions.OpenSearch.AuthenticationUtil
{
    public abstract class Authentication
    {
        protected const string AuthKey = "authtype";

        /// <summary>
        /// The unique name of authentication method
        /// </summary> 
        public abstract String Name { get; }

        /// <summary>
        /// Text message for User Interface
        /// </summary> 
        public abstract String Text { get; }

        public bool ValidationEnabled { get; set; }

        public abstract ICredentials Credentials();

        public abstract string ToQueryString();

        public abstract void CreateMarkup(Control container);

        public abstract Authentication ParseMarkup(Control markup);

        public static Authentication QueryToObject(string queryString, List<Authentication> authentications)
        {
            string authName = HttpUtility.ParseQueryString(queryString)[AuthKey];
            foreach (var auth in authentications)
            {
                if (auth.Name == authName)
                    return (Authentication)Activator.CreateInstance(auth.GetType(), queryString);
            }
            return null;
        }

        protected static String ToQueryStringUtil(NameValueCollection parameters)
        {
            string[] queryKeyValue = (from key in parameters.AllKeys
                                      select key + "=" + parameters[key]).ToArray();
            return String.Join("&", queryKeyValue);
        }
    }
}
