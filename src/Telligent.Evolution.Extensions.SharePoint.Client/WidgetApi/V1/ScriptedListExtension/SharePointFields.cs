using Microsoft.SharePoint.Client;
using System;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointFieldsExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_fields"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointFields>(); }
        }

        public string Name
        {
            get { return "SharePoint List Fields Extension (sharepoint_v1_fields)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to work with SharePoint Fields."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointFields
    {
        Field Get(Guid listId, Guid fieldId);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointFields : ISharePointFields
    {
        public Field Get(Guid listId, Guid fieldId)
        {
            return Api.Version1.PublicApi.Fields.Get(listId, fieldId);
        }
    }
}
