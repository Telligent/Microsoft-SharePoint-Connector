using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using ClientApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class SharePointPermissionsExtension : IScriptedContentFragmentExtension, ITranslatablePlugin
    {
        internal static class Translations
        {
            public const string ListItemNotFound = "permissions_listitem_notfound";
            public const string NotSpecifiedListOrListItem = "permissions_listitem_or_list_notspecified";
            public const string UnknownError = "permissions_unknown_error";
        }

        private ITranslatablePluginController translationController;

        #region IScriptedContentFragmentExtension Members

        public string ExtensionName
        {
            get { return "sharepoint_v2_permissions"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointPermissions>(); }
        }

        public string Name
        {
            get { return "SharePoint Permissions Extension (sharepoint_v2_permissions)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to use SharePoint Permissions."; }
        }

        public void Initialize() { }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set(Translations.ListItemNotFound, "The list item cannot be found.");
                t.Set(Translations.NotSpecifiedListOrListItem, "The list or list item has not been specified.");
                t.Set(Translations.UnknownError, "An error has been occurred, please try again or contact your administrator.");

                return new[] { t };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        internal string Translate(string key, params object[] args)
        {
            return String.Format(translationController.GetLanguageResourceValue(key), args);
        }
    }

    public interface ISharePointPermissions
    {
        PagedList<SPPermissions> List(Guid contentId, IDictionary options);
        SPPermissions Get(Guid contentId, int groupOrUserId);
        Inheritance Inherited(Guid contentId);
        AdditionalInfo Update(Guid contentId, IDictionary options);
        AdditionalInfo Remove(Guid contentId, string memberIds);
        AdditionalInfo Reset(Guid contentId);
        ApiList<SPPermissionsLevel> Levels(string spwebUrl);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointPermissions : ISharePointPermissions
    {
        private static readonly SharePointPermissionsExtension plugin = PluginManager.Get<SharePointPermissionsExtension>().FirstOrDefault();
        private static readonly IListItemDataService listItemDataService = ServiceLocator.Get<IListItemDataService>();

        public PagedList<SPPermissions> List(Guid contentId,
            [Documentation(Name = "PageSize", Type = typeof(int)),
            Documentation(Name = "PageIndex", Type = typeof(int))]
            IDictionary options)
        {
            var permissionsListOptions = new PermissionsListOptions(EnsureListId(contentId), contentId);
            if (options != null)
            {
                int pageSize;
                if (options["PageSize"] != null && int.TryParse(options["PageSize"].ToString(), out pageSize))
                {
                    permissionsListOptions.PageSize = pageSize;
                }

                int pageIndex;
                if (options["PageIndex"] != null && int.TryParse(options["PageIndex"].ToString(), out pageIndex))
                {
                    permissionsListOptions.PageIndex = pageIndex;
                }
            }

            try
            {
                return ClientApi.Permissions.List(permissionsListOptions);
            }
            catch (SPInternalException ex)
            {
                return new PagedList<SPPermissions>(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.ListItemNotFound)));
            }
            catch (Exception ex)
            {
                SPLog.UnKnownError(ex, string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointPermissions.List() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message));
                return new PagedList<SPPermissions>(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.UnknownError)));
            }
        }

        public SPPermissions Get(Guid contentId, int groupOrUserId)
        {
            try
            {
                return ClientApi.Permissions.Get(groupOrUserId, new PermissionsGetOptions(EnsureListId(contentId), contentId));
            }
            catch (SPInternalException ex)
            {
                return ItemNotFoundError<SPPermissions>(ex);
            }
            catch (Exception ex)
            {
                SPLog.UnKnownError(ex, string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointPermissions.Get() method for ContentId: {1}, groupOrUserId: {2}. The exception message is: {3}", ex.GetType(), contentId, groupOrUserId, ex.Message));
                return UnknownError<SPPermissions>(ex);
            }
        }

        public Inheritance Inherited(Guid contentId)
        {
            try
            {
                return ClientApi.Permissions.GetInheritance(new PermissionsGetOptions(EnsureListId(contentId), contentId));
            }
            catch (SPInternalException ex)
            {
                return ItemNotFoundError<Inheritance>(ex);
            }
            catch (Exception ex)
            {
                SPLog.UnKnownError(ex, string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointPermissions.Inherited() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message));
                return UnknownError<Inheritance>(ex);
            }
        }

        public AdditionalInfo Update(Guid contentId,
            [Documentation(Name = "PermissionLevelIds", Type = typeof(string), Description = "Permission level id or ifs"),
            Documentation(Name = "GroupIds", Type = typeof(string), Description = "Group id or ids, separated by comma ','"),
            Documentation(Name = "UserNames", Type = typeof(string), Description = "User login name or names, separated by comma ','"),
            Documentation(Name = "IsGranted", Type = typeof(bool), Default = false, Description = "Not removes existing role assignments, when true")]
            IDictionary options)
        {
            var updateOptions = new PermissionsUpdateOptions(EnsureListId(contentId), contentId)
            {
                CopyRoleAssignments = true,
                ClearSubscopes = true
            };

            if (options["PermissionLevelIds"] != null && !String.IsNullOrEmpty(options["PermissionLevelIds"].ToString()))
            {
                updateOptions.Levels = options["PermissionLevelIds"].ToString().Split(',').Select(int.Parse).ToArray();
            }

            bool isGranted;
            if (options["IsGranted"] != null && !String.IsNullOrEmpty(options["IsGranted"].ToString()) && bool.TryParse(options["IsGranted"].ToString(), out isGranted))
            {
                updateOptions.Overwrite = !isGranted;
            }

            if (options["GroupIds"] != null && !String.IsNullOrEmpty(options["GroupIds"].ToString()))
            {
                updateOptions.GroupIds = options["GroupIds"].ToString().Split(',').Select(int.Parse).ToArray();
            }

            if (options["UserNames"] != null && !String.IsNullOrEmpty(options["UserNames"].ToString()))
            {
                updateOptions.LoginNames = options["UserNames"].ToString().Split(',');
            }

            try
            {
                ClientApi.Permissions.Update(updateOptions);
                return new AdditionalInfo();
            }
            catch (SPInternalException ex)
            {
                return new AdditionalInfo(GetItemNotFoundError(ex));
            }
            catch (Exception ex)
            {
                SPLog.UnKnownError(ex, string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointPermissions.Update() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message));
                return new AdditionalInfo(GetUnknownError(ex));
            }
        }

        public AdditionalInfo Remove(Guid contentId, string memberIds)
        {
            try
            {
                ClientApi.Permissions.Remove(memberIds.Split(',').Select(int.Parse).ToArray(), new PermissionsGetOptions(EnsureListId(contentId), contentId));
                return new AdditionalInfo();
            }
            catch (SPInternalException ex)
            {
                return new AdditionalInfo(GetItemNotFoundError(ex));
            }
            catch (Exception ex)
            {
                SPLog.UnKnownError(ex, string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointPermissions.Remove() method for ContentId: {1}, memberIds: {2}. The exception message is: {3}", ex.GetType(), contentId, memberIds, ex.Message));
                return new AdditionalInfo(GetUnknownError(ex));
            }
        }

        public AdditionalInfo Reset(Guid contentId)
        {
            try
            {
                ClientApi.Permissions.ResetInheritance(new PermissionsGetOptions(EnsureListId(contentId), contentId));
                return new AdditionalInfo();
            }
            catch (SPInternalException ex)
            {
                return new AdditionalInfo(GetItemNotFoundError(ex));
            }
            catch (Exception ex)
            {
                SPLog.UnKnownError(ex, string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointPermissions.Reset() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message));
                return new AdditionalInfo(GetUnknownError(ex));
            }
        }

        public ApiList<SPPermissionsLevel> Levels(string url)
        {
            try
            {
                return ClientApi.Permissions.Levels(url);
            }
            catch (SPInternalException ex)
            {
                return new ApiList<SPPermissionsLevel>(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.ListItemNotFound)));
            }
            catch (Exception ex)
            {
                SPLog.UnKnownError(ex, string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.LevelList() method URL: {1}. The exception message is: {2}", ex.GetType(), url, ex.Message));
                return new ApiList<SPPermissionsLevel>(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.UnknownError)));
            }
        }

        private T ItemNotFoundError<T>(Exception ex) where T : ApiEntity, new()
        {
            var obj = new T();
            obj.Errors.Add(GetItemNotFoundError(ex));
            return obj;
        }


        private T UnknownError<T>(Exception ex) where T : ApiEntity, new()
        {
            var obj = new T();
            obj.Errors.Add(GetUnknownError(ex));
            return obj;
        }

        private static Error GetItemNotFoundError(Exception ex)
        {
            return new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.ListItemNotFound));
        }

        private static Error GetUnknownError(Exception ex)
        {
            return new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.UnknownError));
        }

        private Guid EnsureListId(Guid contentId)
        {
            var listId = SPCoreService.Context.ListId;
            if (listId != Guid.Empty) return listId;

            var itemBase = listItemDataService.Get(contentId);
            if (itemBase != null)
            {
                listId = itemBase.ApplicationId;
            }
            return listId;
        }
    }
}
