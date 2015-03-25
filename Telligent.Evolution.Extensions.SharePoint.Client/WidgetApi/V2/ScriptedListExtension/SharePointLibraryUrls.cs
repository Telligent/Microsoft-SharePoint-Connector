using System;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class SharePointLibraryUrlsExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v2_libraryUrls"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointLibraryUrls>(); }
        }

        public string Name
        {
            get { return "SharePoint Library URLs Scripted Content Fragment Extension (sharepoint_v2_libraryUrls)"; }
        }

        public string Description
        {
            get { return "Enables scripted content fragments to render links to library-related URLs."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointLibraryUrls
    {
        string Create(int groupId);
        string Import(int groupId);
        string Edit(Guid libraryId);
        string Browse(int groupId);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointLibraryUrls : ISharePointLibraryUrls
    {
        public string Create(int groupId)
        {
            return PublicApi.SharePointUrls.CreateLibrary(groupId);
        }

        public string Import(int groupId)
        {
            return PublicApi.SharePointUrls.ImportLibrary(groupId);
        }

        public string Edit(Guid libraryId)
        {
            return PublicApi.SharePointUrls.EditLibrary(libraryId);
        }

        public string Browse(int groupId)
        {
            return PublicApi.SharePointUrls.BrowseLibraries(groupId);
        }
    }
}