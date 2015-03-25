using System;
using System.Collections;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class SharePointListItemUrlsExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v2_listItemUrls"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointListItemUrls>(); }
        }

        public string Name
        {
            get { return "SharePoint ListItem URLs Scripted Content Fragment Extension (sharepoint_v2_listItemUrls)"; }
        }

        public string Description
        {
            get { return "Enables scripted content fragments to render links to listItem-related URLs."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointListItemUrls
    {
        string Browse(Guid listId);
        string Add(Guid listId);
        string Edit(Guid contentId, IDictionary options = null);
        string Show(Guid contentId, IDictionary options = null);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointListItemUrls : ISharePointListItemUrls
    {
        private static readonly IListItemUrls listItemUrls = ServiceLocator.Get<IListItemUrls>();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();

        public string Browse(Guid listId)
        {
            return PublicApi.SharePointUrls.BrowseListItems(listId);
        }

        public string Add(Guid listId)
        {
            return PublicApi.SharePointUrls.AddListItem(listId);
        }

        public string Edit(Guid contentId,
            [Documentation(Name = "ApplicationId", Type = typeof(Guid)),
            Documentation(Name = "ItemId", Type = typeof(int))]
            IDictionary options = null)
        {
            var url = PublicApi.SharePointUrls.EditListItem(contentId);
            if (string.IsNullOrEmpty(url) && options != null)
            {
                var applicationId = (Guid)options["ApplicationId"];
                var itemId = (int)options["ItemId"];
                ListBase listBase;
                if (applicationId != Guid.Empty
                    && (listBase = listDataService.Get(applicationId)) != null)
                {
                    url = listItemUrls.EditListItem(listBase, new ItemUrlQuery(itemId, contentId));
                }
            }
            return url;
        }

        public string Show(Guid contentId,
            [Documentation(Name = "ApplicationId", Type = typeof(Guid)),
            Documentation(Name = "ItemId", Type = typeof(int))]
            IDictionary options = null)
        {
            var url = PublicApi.SharePointUrls.ListItem(contentId);
            if (string.IsNullOrEmpty(url) && options != null)
            {
                var applicationId = (Guid)options["ApplicationId"];
                var itemId = (int)options["ItemId"];
                ListBase listBase;
                if (applicationId != Guid.Empty
                    && (listBase = listDataService.Get(applicationId)) != null)
                {
                    url = listItemUrls.ViewListItem(listBase, new ItemUrlQuery(itemId, contentId));
                }
            }
            return url;
        }
    }
}