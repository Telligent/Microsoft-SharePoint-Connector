using System;
using System.Web;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Utility
{
    public class SPCookie
    {
        public static string GetCookie(string cookieId, string valueId)
        {
            var cookie = HttpContext.Current.Request.Cookies[cookieId];
            if (cookie != null && cookie[valueId] != null)
            {
                return cookie[valueId];
            }
            return null;
        }

        public static void SetCookie(string cookieId, string valueId, string value)
        {
            var cookie = HttpContext.Current.Request.Cookies[cookieId] ?? new HttpCookie(cookieId);
            if (cookie.Values[valueId] != null)
            {
                cookie.Values[valueId] = value;
            }
            else
            {
                cookie.Values.Add(valueId, value);
            }
            HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }
}
