using System.Collections.Generic;
using System.Globalization;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Routing
{
    internal abstract class SPRouteTable
    {
        private readonly object obj = new object();
        private readonly Dictionary<string, RoutedPage> registeredPages = new Dictionary<string, RoutedPage>();

        public RoutedPage Add { get; protected set; }
        public RoutedPage Edit { get; protected set; }
        public RoutedPage Show { get; protected set; }
        public RoutedPage List { get; protected set; }

        public virtual void RegisterPages(IUrlController controller)
        {
            RegisterPage(Add, controller);
            RegisterPage(Edit, controller);
            RegisterPage(Show, controller);
            RegisterPage(List, controller);
        }

        public RoutedPage GetPageByUrlName(string urlName)
        {
            var name = urlName.ToLowerInvariant();
            if (registeredPages.ContainsKey(name))
            {
                return registeredPages[name];
            }
            return null;
        }

        protected void RegisterPage(RoutedPage page, IUrlController controller)
        {
            controller.AddPage(page.UrlName, page.UrlPattern, null, page.ParameterConstraints, page.PageName, new PageDefinitionOptions { ParseContext = page.ParseContext });
            var pageName = page.UrlName.ToLowerInvariant();
            if (!registeredPages.ContainsKey(pageName))
            {
                lock (obj)
                {
                    if (!registeredPages.ContainsKey(pageName))
                    {
                        registeredPages.Add(pageName, page);
                    }
                }
            }
        }

        #region Helpers

        protected static string GetListTokenValue(ListUrlQuery list)
        {
            return !string.IsNullOrEmpty(list.ApplicationKey) ? list.ApplicationKey : list.Id.ToString("N");
        }

        protected static string GetItemTokenValue(ItemUrlQuery item)
        {
            return item.Id.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
