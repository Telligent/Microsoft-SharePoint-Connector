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
    public class SharePointFileUrlsExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v2_fileUrls"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointFileUrls>(); }
        }

        public string Name
        {
            get { return "SharePoint File URLs Scripted Content Fragment Extension (sharepoint_v2_fileUrls)"; }
        }

        public string Description
        {
            get { return "Enables scripted content fragments to render links to document-related URLs."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointFileUrls
    {
        string Browse(Guid libraryId);
        string Add(Guid libraryId);
        string Edit(Guid contentId);
        string Edit(Guid contentId, IDictionary options);
        string Show(Guid contentId);
        string Show(Guid contentId, IDictionary options);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointFileUrls : ISharePointFileUrls
    {
        private static readonly IDocumentUrls documentUrls = ServiceLocator.Get<IDocumentUrls>();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();

        public string Browse(Guid libraryId)
        {
            return PublicApi.SharePointUrls.BrowseDocuments(libraryId);
        }

        public string Add(Guid libraryId)
        {
            return PublicApi.SharePointUrls.AddDocument(libraryId);
        }

        public string Edit(Guid contentId)
        {
            return Edit(contentId, null);
        }

        public string Edit(Guid contentId,
            [Documentation(Name = "ApplicationId", Type = typeof(Guid)),
            Documentation(Name = "ItemId", Type = typeof(int))]
            IDictionary options)
        {
            var url = PublicApi.SharePointUrls.EditDocument(contentId);
            if (string.IsNullOrEmpty(url) && options != null)
            {
                var applicationId = (Guid)options["ApplicationId"];
                var itemId = (int)options["ItemId"];
                ListBase listBase;
                if (applicationId != Guid.Empty
                    && (listBase = listDataService.Get(applicationId)) != null)
                {
                    url = documentUrls.EditDocument(listBase, new ItemUrlQuery(itemId, contentId));
                }
            }
            return url;
        }

        public string Show(Guid contentId)
        {
            return Show(contentId, null);
        }

        public string Show(Guid contentId,
            [Documentation(Name = "ApplicationId", Type = typeof(Guid)),
            Documentation(Name = "ItemId", Type = typeof(int))]
            IDictionary options)
        {
            var url = PublicApi.SharePointUrls.Document(contentId);
            if (string.IsNullOrEmpty(url) && options != null)
            {
                var applicationId = (Guid)options["ApplicationId"];
                var itemId = (int)options["ItemId"];
                ListBase listBase;
                if (applicationId != Guid.Empty
                    && (listBase = listDataService.Get(applicationId)) != null)
                {
                    url = documentUrls.ViewDocument(listBase, new ItemUrlQuery(itemId, contentId));
                }
            }
            return url;
        }
    }
}