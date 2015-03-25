using System;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class SharePointListUrlsExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v2_listUrls"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointListUrls>(); }
        }

        public string Name
        {
            get { return "SharePoint List URLs Scripted Content Fragment Extension (sharepoint_v2_listUrls)"; }
        }

        public string Description
        {
            get { return "Enables scripted content fragments to render links to list-related URLs."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointListUrls
    {
        string Import(int groupId);
        string Edit(Guid libraryId);
        string Browse(int groupId);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointListUrls : ISharePointListUrls
    {
        public string Import(int groupId)
        {
            return PublicApi.SharePointUrls.ImportList(groupId);
        }

        public string Edit(Guid listId)
        {
            return PublicApi.SharePointUrls.EditList(listId);
        }

        public string Browse(int groupId)
        {
            return PublicApi.SharePointUrls.BrowseLists(groupId);
        }
    }
}