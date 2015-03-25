using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Routing
{
    internal class LibrariesRouteTable : SPRouteTable
    {
        private static readonly LibrariesRouteTable instance = new LibrariesRouteTable();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();

        public RoutedPage Create { get; protected set; }

        private LibrariesRouteTable()
        {
            Add = new RoutedPage
                    {
                        ShortName = "AddLibrary",
                        PageName = "sharepoint-library-add",
                        UrlName = "sharepoint.library.add",
                        UrlPattern = "import",
                        ParseContext = pageContext => pageContext.ContextItems.Put(BuildContextItem())
                    };

            Create = new RoutedPage
                    {
                        ShortName = "CreateLibrary",
                        PageName = "sharepoint-library-create",
                        UrlName = "sharepoint.library.create",
                        UrlPattern = "create",
                        ParseContext = pageContext => pageContext.ContextItems.Put(BuildContextItem())
                    };

            Edit = new RoutedPage
                    {
                        ShortName = "EditLibrary",
                        PageName = "sharepoint-library-edit",
                        UrlName = "sharepoint.library.edit",
                        UrlPattern = "{libraryId}/edit",
                        ParseContext = ParseLibraryContextItem
                    };

            Show = new RoutedPage
                    {
                        ShortName = "Library",
                        PageName = "sharepoint-library-show",
                        UrlName = "sharepoint.library.show",
                        UrlPattern = "{libraryId}/show",
                        ParseContext = ParseLibraryContextItem
                    };

            List = new RoutedPage
                    {
                        ShortName = "Libraries",
                        PageName = "sharepoint-library-list",
                        UrlName = "sharepoint.library.list",
                        UrlPattern = "",
                        ParseContext = pageContext => pageContext.ContextItems.Put(BuildContextItem())
                    };
        }

        public override void RegisterPages(IUrlController controller)
        {
            base.RegisterPages(controller);
            RegisterPage(Create, controller);
        }

        public Dictionary<string, string> BuildUrlTokens(ListUrlQuery list)
        {
            return new Dictionary<string, string>
                {
                    {"libraryId", GetListTokenValue(list)}
                };
        }

        public static Guid ParseLibraryId(PageContext pageContext)
        {
            Guid applicationId = Guid.Empty;
            var tokenValue = pageContext.GetTokenValue("libraryId");
            if (tokenValue != null)
            {
                var applicationKey = tokenValue.ToString();
                if (!string.IsNullOrEmpty(applicationKey))
                {
                    var groupId = int.Parse(pageContext.ContextItems.GetItemByContentType(Extensibility.Api.Version1.PublicApi.Groups.ContentTypeId).Id);
                    var list = listDataService.Get(applicationKey, groupId);
                    if (list != null && list.Id != Guid.Empty)
                    {
                        applicationId = list.Id;
                    }
                    else
                    {
                        Guid libraryId;
                        if (Guid.TryParse(applicationKey, out libraryId) && listDataService.Get(libraryId) != null)
                        {
                            applicationId = libraryId;
                        }
                    }
                }
            }
            return applicationId;
        }

        public static void ParseLibraryContextItem(PageContext pageContext)
        {
            var applicationId = ParseLibraryId(pageContext);
            if (applicationId != Guid.Empty)
            {
                pageContext.ContextItems.Put(BuildContextItem(applicationId));
            }
        }

        public static ContextItem BuildContextItem(Guid applicationId)
        {
            var libraryContext = BuildContextItem();
            libraryContext.ApplicationId = applicationId;
            return libraryContext;
        }

        public static ContextItem BuildContextItem()
        {
            return new ContextItem
            {
                TypeName = "Library",
                ApplicationTypeId = LibraryApplicationType.Id
            };
        }

        public static LibrariesRouteTable Get()
        {
            return instance;
        }
    }
}
