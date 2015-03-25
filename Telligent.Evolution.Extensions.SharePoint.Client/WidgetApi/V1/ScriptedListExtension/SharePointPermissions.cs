using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;
using ClientApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
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
            get { return "sharepoint_v1_permissions"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointPermissions>(); }
        }

        public string Name
        {
            get { return "SharePoint Permissions Extension (sharepoint_v1_permissions)"; }
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
        PagedList<SPPermissions> List(SPList list, SPListItem listItem,
            [Documentation(Name = "PageSize", Type = typeof(int)),
            Documentation(Name = "PageIndex", Type = typeof(int))]
            IDictionary options);

        /// <summary>
        /// Returns SharePoint Permissions for a ListItem
        /// </summary>
        /// <param name="list">SharePoint List</param>
        /// <param name="listItem">SharePoint List Item</param>
        /// <param name="groupOrUserId">Group or User Id</param>
        /// <returns>Permissions</returns>
        SPPermissions Get(SPList list, SPListItem listItem, int groupOrUserId);

        /// <summary>
        /// Returns true if a list item inherits security from the list.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="listItem"></param>
        /// <returns></returns>
        bool IsInherited(SPList list, SPListItem listItem);

        /// <summary>
        /// Breaks role inheritance and grant permissions directly to pointed groups and users
        /// </summary>
        /// <param name="list">SharePoint List</param>
        /// <param name="listItem">SharePoint ListItem</param>
        /// <param name="options">Permission Level ids and group ids or user logins divided by comma ','</param>
        void Update(SPList list, SPListItem listItem,
            [Documentation(Name = "PermissionLevelIds", Type = typeof(string)),
            Documentation(Name = "GroupIds", Type = typeof(string)),
            Documentation(Name = "UserNames", Type = typeof(string))]
            IDictionary options);

        /// <summary>
        /// Removes user or group permissions
        /// </summary>
        /// <param name="list">SharePoint List</param>
        /// <param name="listItem">SharePoint List Item</param>
        /// <param name="groupOrUserId">Member Ids separated by comma ','</param>
        void Remove(SPList list, SPListItem listItem, string groupOrUserId);

        /// <summary>
        /// Removes list item role assignments and inherits role assignments from the list.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="listItem"></param>
        void ResetInheritance(SPList list, SPListItem listItem);

        /// <summary>
        /// Add users to the group
        /// </summary>
        /// <param name="url">SharePoint Web URL</param>
        /// <param name="credentials">Creadentials</param>
        /// <param name="loginNames">One or more User Names divided by comma ','</param>
        /// <param name="groupId">Target Group Id</param>
        void AddUserToGroup(string url, Authentication credentials, string loginNames, int groupId);

        /// <summary>
        /// SharePoint Web Permissions Levels
        /// </summary>
        /// <param name="url">SPWeb URL</param>
        /// <param name="credentials">Authentication</param>
        /// <returns>Permission Levels</returns>
        List<SPPermissionsLevel> LevelList(string url, Authentication credentials);

        [Obsolete("Use sharepoint_v2_person or the REST endpoint api.ashx/v2/sharepoint/groups", true)]
        List<SPGroup> GroupList(string url, Authentication credentials);

        [Obsolete("Use sharepoint_v2_person or the REST endpoint api.ashx/v2/sharepoint/users", true)]
        List<SPBaseUser> UserList(string url, Authentication credentials);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointPermissions : ISharePointPermissions
    {
        private static readonly SharePointPermissionsExtension plugin = PluginManager.Get<SharePointPermissionsExtension>().FirstOrDefault();

        public const int DefaultPageSize = 20;
        private readonly ICredentialsManager credentials;

        internal SharePointPermissions()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        internal SharePointPermissions(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public PagedList<SPPermissions> List(SPList list, SPListItem listItem,
            [Documentation(Name = "PageSize", Type = typeof(int)),
            Documentation(Name = "PageIndex", Type = typeof(int))]
            IDictionary options)
        {
            var permissionsList = new PagedList<SPPermissions>();
            Validate(permissionsList.Errors, list, listItem);
            if (!permissionsList.Errors.Any())
            {
                var permissionsListOptions = new PermissionsListOptions(list.Id, listItem.ContentId)
                    {
                        Url = list.SPWebUrl,
                        PageSize = DefaultPageSize,
                        PageIndex = 0
                    };
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
                    permissionsList = ClientApi.Permissions.List(permissionsListOptions);
                }
                catch (SPInternalException ex)
                {
                    permissionsList.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.ListItemNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.List() method for ContentId: {1} ListId: {2}. The exception message is: {3}", ex.GetType(), listItem.ContentId, list.Id, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    permissionsList.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.UnknownError)));
                }
            }
            return permissionsList;
        }

        public SPPermissions Get(SPList list, SPListItem listItem, int memberId)
        {
            var permissions = new SPPermissions();
            Validate(permissions.Errors, list, listItem);
            if (!permissions.Errors.Any())
            {
                var permissionsGetOptions = new PermissionsGetOptions(list.Id, listItem.ContentId)
                {
                    Url = list.SPWebUrl
                };
                try
                {
                    permissions = ClientApi.Permissions.Get(memberId, permissionsGetOptions);
                }
                catch (SPInternalException ex)
                {
                    permissions.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.ListItemNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.Get() method for ContentId: {1} ListId: {2}. The exception message is: {3}", ex.GetType(), listItem.ContentId, list.Id, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    permissions.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.UnknownError)));
                }
            }
            return permissions;
        }

        public bool IsInherited(SPList list, SPListItem listItem)
        {
            var inheritance = new Inheritance();
            Validate(inheritance.Errors, list, listItem);
            if (!inheritance.Errors.Any())
            {
                var permissionsGetOptions = new PermissionsGetOptions(list.Id, listItem.ContentId)
                {
                    Url = list.SPWebUrl
                };
                try
                {
                    inheritance = ClientApi.Permissions.GetInheritance(permissionsGetOptions);
                }
                catch (SPInternalException ex)
                {
                    inheritance.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.ListItemNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.Get() method for ContentId: {1} ListId: {2}. The exception message is: {3}", ex.GetType(), listItem.ContentId, list.Id, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    inheritance.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.UnknownError)));
                }
            }
            return inheritance.Enabled;
        }

        public void Update(SPList list, SPListItem listItem,
            [Documentation(Name = "PermissionLevelIds", Type = typeof(string), Description = "Permission level id or ifs"),
            Documentation(Name = "GroupIds", Type = typeof(string), Description = "Group id or ids, separated by comma ','"),
            Documentation(Name = "UserNames", Type = typeof(string), Description = "User login name or names, separated by comma ','"),
            Documentation(Name = "IsGranted", Type = typeof(bool), Default = false, Description = "Not removes existing role assignments, when true")]
            IDictionary options)
        {
            var updateInfo = new AdditionalInfo();
            Validate(updateInfo.Errors, list, listItem);
            if (!updateInfo.Errors.Any())
            {
                var updateOptions = new PermissionsUpdateOptions(list.Id, listItem.ContentId)
                    {
                        CopyRoleAssignments = true,
                        ClearSubscopes = true,
                        Url = list.SPWebUrl
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
                }
                catch (SPInternalException ex)
                {
                    updateInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.ListItemNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.Update() method for ContentId: {1} ListId: {2}. The exception message is: {3}", ex.GetType(), listItem.ContentId, list.Id, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    updateInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.UnknownError)));
                }
            }
        }

        public void Remove(SPList list, SPListItem listItem, string memberIds)
        {
            var removePermissions = new AdditionalInfo();
            Validate(removePermissions.Errors, list, listItem);
            if (!removePermissions.Errors.Any())
            {
                var removeOptions = new PermissionsGetOptions(list.Id, listItem.ContentId)
                    {
                        Url = list.SPWebUrl
                    };

                try
                {
                    ClientApi.Permissions.Remove(memberIds.Split(',').Select(int.Parse).ToArray(), removeOptions);
                }
                catch (SPInternalException) { }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.Remove() method for ContentId: {1} ListId: {2}. The exception message is: {3}", ex.GetType(), listItem.ContentId, list.Id, ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
            }
        }

        public void ResetInheritance(SPList list, SPListItem listItem)
        {
            var resetInheritance = new AdditionalInfo();
            Validate(resetInheritance.Errors, list, listItem);
            if (!resetInheritance.Errors.Any())
            {
                var resetInheritanceOptions = new PermissionsGetOptions(list.Id, listItem.ContentId)
                {
                    Url = list.SPWebUrl
                };
                try
                {
                    ClientApi.Permissions.ResetInheritance(resetInheritanceOptions);
                }
                catch (SPInternalException) { }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.ResetInheritance() method for ContentId: {1} ListId: {2}. The exception message is: {3}", ex.GetType(), listItem.ContentId, list.Id, ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
            }
        }

        public void AddUserToGroup(string url, Authentication authentication, string loginNames, int groupId)
        {
            // Group Id is invalid
            if (groupId <= 0)
            {
                return;
            }

            using (var clientContext = new SPContext(url, authentication ?? credentials.Get(url)))
            {
                var group = clientContext.Web.SiteGroups.GetById(groupId);
                foreach (var loginName in loginNames.Split(','))
                {
                    var user = clientContext.Web.EnsureUser(loginName);
                    group.Users.AddUser(user);
                }
                try
                {
                    clientContext.ExecuteQuery();
                }
                catch (ServerException ex)
                {
                    //User not found
                    SPLog.RoleOperationUnavailable(ex, String.Format("A server exception occurred while adding users to the group with id '{0}'. The exception message is: {1}", groupId, ex.Message));
                }
            }
        }

        public List<SPPermissionsLevel> LevelList(string url, Authentication authentication)
        {
            try
            {
                return ClientApi.Permissions.Levels(url).ToList();
            }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.LevelList() method URL: {1}. The exception message is: {2}", ex.GetType(), url, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        [Obsolete("Use sharepoint_v2_person or the REST endpoint api.ashx/v2/sharepoint/groups", true)]
        public List<SPGroup> GroupList(string url, Authentication authentication)
        {
            try
            {
                using (var clientContext = new SPContext(url, authentication ?? credentials.Get(url)))
                {
                    SP.Web web = clientContext.Web;
                    IEnumerable<SP.Group> groups = clientContext.LoadQuery(web.SiteGroups);
                    clientContext.ExecuteQuery();

                    return (from g in groups
                            select new SPGroup(g.Id, g.LoginName)
                                {
                                    Description = g.Description
                                }).ToList();
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.GroupList() method for URL: {1}. The exception message is: {2}", ex.GetType(), url, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        [Obsolete("Use sharepoint_v2_person or the REST endpoint api.ashx/v2/sharepoint/users", true)]
        public List<SPBaseUser> UserList(string url, Authentication authentication)
        {
            try
            {
                using (var clientContext = new SPContext(url, authentication ?? credentials.Get(url)))
                {
                    var userInfoList = clientContext.Web.SiteUserInfoList;
                    var users = userInfoList.GetItems(CamlQuery.CreateAllItemsQuery());
                    clientContext.Load(users, userCollection => userCollection.Include(
                        u => u.Id,
                        u => u["Name"],
                        u => u.DisplayName));
                    clientContext.ExecuteQuery();

                    var userList = new List<SPBaseUser>();

                    foreach (var user in users)
                    {
                        try
                        {
                            if (user.FieldValues["Name"] != null)
                            {
                                userList.Add(new SPBaseUser(user.Id, user.FieldValues["Name"].ToString()) { Name = user.DisplayName });
                            }
                        }
                        catch (Exception ex)
                        {
                            SPLog.UnKnownError(ex, ex.Message);
                        }
                    }
                    return userList;
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointPermissions.GroupList() method for URL: {1}. The exception message is: {2}", ex.GetType(), url, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        private static void Validate(IList<Error> errors, SPList list, SPListItem listItem)
        {
            if (list == null || listItem == null)
            {
                errors.Add(new Error(typeof(ArgumentException).ToString(), plugin.Translate(SharePointPermissionsExtension.Translations.NotSpecifiedListOrListItem)));
            }

            if (list != null && list.Errors.Any())
            {
                foreach (var error in list.Errors)
                {
                    errors.Add(error);
                }
            }

            if (listItem != null && listItem.Errors.Any())
            {
                foreach (var error in listItem.Errors)
                {
                    errors.Add(error);
                }
            }
        }

    }
}
