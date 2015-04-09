using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensions.SharePoint.Components.DI;
using PublicApi = Telligent.Evolution.Extensions.SharePoint.Client.Api;
using RestApi = Telligent.Evolution.Extensions.SharePoint.Client.Rest.Api;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    internal static class ServiceLocator
    {
        private static readonly object LockObject = new object();
        private static readonly Controller Controller = new Controller();
        private static readonly Dictionary<Type, object> Instances = new Dictionary<Type, object>();

        static ServiceLocator()
        {
            // Internal Api
            Controller.Bind<InternalApi.ICacheService, InternalApi.SPCacheService>();
            Controller.Bind<InternalApi.ICredentialsManager, InternalApi.SPCredentialsManager>();
            Controller.Bind<InternalApi.IListDataService, InternalApi.SPListDataService>();
            Controller.Bind<InternalApi.IListItemDataService, InternalApi.SPItemDataService>();
            Controller.Bind<InternalApi.IListService, InternalApi.SPListService>();
            Controller.Bind<InternalApi.IListItemService, InternalApi.SPItemService>();
            Controller.Bind<InternalApi.IFileService, InternalApi.SPFileService>();
            Controller.Bind<InternalApi.IFolderService, InternalApi.SPFolderService>();
            Controller.Bind<InternalApi.IPermissionsService, InternalApi.SPPermissionsService>();
            Controller.Bind<InternalApi.IAttachmentsService, InternalApi.SPAttachmentsService>();
            Controller.Bind<InternalApi.IApplicationKeyValidationService, InternalApi.ApplicationKeyValidationService>();
            Controller.Bind<InternalApi.ITaxonomiesService, InternalApi.SPTaxonomiesService>();
            Controller.Bind<InternalApi.IFieldsService, InternalApi.SPFieldsService>();
            Controller.Bind<InternalApi.IUserProfileService, InternalApi.UserProfileService>();
            Controller.Bind<InternalApi.ILibraryUrls, InternalApi.SharePointLibraryUrls>();
            Controller.Bind<InternalApi.IDocumentUrls, InternalApi.SharePointDocumentUrls>();
            Controller.Bind<InternalApi.IListUrls, InternalApi.SharePointListUrls>();
            Controller.Bind<InternalApi.IListItemUrls, InternalApi.SharePointListItemUrls>();
            Controller.Bind<InternalApi.IContextService, InternalApi.SPContextService>();

            // Public Api
            Controller.Bind<PublicApi.Version1.IDocuments, PublicApi.Version1.Documents>();
            Controller.Bind<PublicApi.Version1.IFolders, PublicApi.Version1.Folders>();
            Controller.Bind<PublicApi.Version1.ILibraries, PublicApi.Version1.Libraries>();
            Controller.Bind<PublicApi.Version1.ILists, PublicApi.Version1.Lists>();
            Controller.Bind<PublicApi.Version1.IListItems, PublicApi.Version1.ListItems>();
            Controller.Bind<PublicApi.Version1.IPermissions, PublicApi.Version1.Permissions>();
            Controller.Bind<PublicApi.Version1.ISharePointUrls, PublicApi.Version1.SharePointUrls>();
            Controller.Bind<PublicApi.Version1.IAttachments, PublicApi.Version1.Attachments>();
            Controller.Bind<PublicApi.Version1.ITaxonomies, PublicApi.Version1.Taxonomies>();
            Controller.Bind<PublicApi.Version1.IFields, PublicApi.Version1.Fields>();
            Controller.Bind<PublicApi.Version1.IUserProfiles, PublicApi.Version1.UserProfiles>();

            // REST Api
            Controller.Bind<RestApi.Version1.ISPListController, RestApi.Version1.SPListController>();
            Controller.Bind<RestApi.Version1.ISPViewController, RestApi.Version1.SPViewController>();
            Controller.Bind<RestApi.Version1.ISPUserOrGroupController, RestApi.Version1.SPUserOrGroupController>();

            // WidgetApi
            // Scripted List Extensions
            Controller.Bind<version1.ISharePointCalendar, version1.SharePointCalendar>();
            Controller.Bind<version1.ISharePointFile, version1.SharePointFile>();
            Controller.Bind<version1.ISharePointFolder, version1.SharePointFolder>();
            Controller.Bind<version1.ISharePointListItem, version1.SharePointListItem>();
            Controller.Bind<version1.ISharePointUI, version1.SharePointUI>();
            Controller.Bind<version1.ISharePointUrls, version1.SharePointUrls>();
            Controller.Bind<version1.ISharePointView, version1.SharePointView>();
            Controller.Bind<version1.ISharePointPermissions, version1.SharePointPermissions>();
            Controller.Bind<version1.ISharePointFields, version1.SharePointFields>();

            Controller.Bind<version2.ISharePointFile, version2.SharePointFile>();
            Controller.Bind<version2.ISharePointLibrary, version2.SharePointLibrary>();
            Controller.Bind<version2.ISharePointList, version2.SharePointList>();
            Controller.Bind<version2.ISharePointListItem, version2.SharePointListItem>();
            Controller.Bind<version2.ISharePointPermissions, version2.SharePointPermissions>();
            Controller.Bind<version2.ISharePointLibraryUrls, version2.SharePointLibraryUrls>();
            Controller.Bind<version2.ISharePointFileUrls, version2.SharePointFileUrls>();
            Controller.Bind<version2.ISharePointListUrls, version2.SharePointListUrls>();
            Controller.Bind<version2.ISharePointListItemUrls, version2.SharePointListItemUrls>();

            // Scripted Type Extensions
            Controller.Bind<version1.IAttachmentsEditor, version1.AttachmentsEditor>();
            Controller.Bind<version1.IChoiceEditor, version1.ChoiceEditor>();
            Controller.Bind<version1.IDateTimeEditor, version1.DateTimeEditor>();
            Controller.Bind<version1.IFieldEditor, version1.FieldEditor>();
            Controller.Bind<version1.IHyperlinkOrPictureEditor, version1.HyperlinkOrPictureEditor>();
            Controller.Bind<version1.ILookupEditor, version1.LookupEditor>();
            Controller.Bind<version1.IManagedMetadataEditor, version1.ManagedMetadataEditor>();
            Controller.Bind<version1.IMultiChoiceEditor, version1.MultiChoiceEditor>();
            Controller.Bind<version1.INumberEditor, version1.NumberEditor>();
            Controller.Bind<version1.IPersonOrGroupEditor, version1.PersonOrGroupEditor>();
            Controller.Bind<version1.IRecurrenceEditor, version1.RecurrenceEditor>();

            Controller.Bind<version2.IAttachmentsEditor, version2.AttachmentsEditor>();
            Controller.Bind<version2.IPersonOrGroupEditor, version2.PersonOrGroupEditor>();
            Controller.Bind<version2.ITaxonomies, version2.Taxonomies>();
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (!Instances.ContainsKey(type))
            {
                var instance = Controller.Get<T>();
                lock (LockObject)
                {
                    if (!Instances.ContainsKey(type))
                    {
                        Instances.Add(type, instance);
                    }
                }
            }
            return (T)Instances[type];
        }
    }
};
