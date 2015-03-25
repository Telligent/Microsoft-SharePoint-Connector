using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Controls.PropertyRules;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing;
using IContent = Telligent.Evolution.Extensibility.Content.Version1.IContent;
using INavigableApplicationType = Telligent.Evolution.Extensibility.Urls.Version1.INavigableApplicationType;
using PublicApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary
{
    public class LibraryApplicationType : IApplicationType, IConfigurablePlugin, ITranslatablePlugin, ISearchableContentType, IGroupDefaultCustomNavigationPlugin, IGroupNewPostLinkPlugin, INavigableApplicationType, IApplicationNavigable, IWebContextualApplicationType, ISecuredContentType
    {
        private const int MaxNumberOfLibraries = 100;
        private static readonly IListService listService = ServiceLocator.Get<IListService>();

        private IApplicationStateChanges applicationStateChanges;
        private IContentStateChanges contentStateChanges;
        private ITranslatablePluginController translationController;

        public static Guid Id { get { return new Guid("85D101BB-6B9B-4CFE-BCDE-9443BEEDD282"); } }

        #region IPlugin Members

        public string Name
        {
            get { return "Document Library Application Type"; }
        }

        public string Description
        {
            get { return "Provides application type information for Document Libraries in a group."; }
        }

        public void Initialize()
        {
            PublicApi.Libraries.Events.AfterCreate += EventsAfterCreate;
            PublicApi.Libraries.Events.AfterUpdate += EventsAfterUpdate;
            PublicApi.Libraries.Events.Render += EventsRender;
            PublicApi.Libraries.Events.AfterDelete += EventsAfterDelete;
            Extensibility.Api.Version1.PublicApi.Groups.Events.AfterDelete += GroupsEventsAfterDelete;
        }

        static void GroupsEventsAfterDelete(GroupAfterDeleteEventArgs e)
        {
            if (!e.Id.HasValue) return;

            var allLibrariesInTheGroup = GetAllLibrariesByGroupId(e.Id.Value);
            foreach (var library in allLibrariesInTheGroup)
            {
                PublicApi.Libraries.Delete(library, deleteLibrary: false);
            }
        }

        private static IEnumerable<Library> GetAllLibrariesByGroupId(int groupId)
        {
            var librariesInTheGroup = new List<Library>();

            const int pageSize = 10;
            int pageIndex = 0;
            int totalCount = 0;
            do
            {
                var libraries = PublicApi.Libraries.List(groupId, new LibraryListOptions
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });
                librariesInTheGroup.AddRange(libraries);
                totalCount = libraries.TotalCount;
                pageIndex++;
            }
            while (pageSize * pageIndex < totalCount);

            return librariesInTheGroup;
        }

        #endregion

        #region IApplicationType Members

        public Guid ApplicationTypeId
        {
            get { return Id; }
        }

        public string ApplicationTypeName
        {
            get { return translationController.GetLanguageResourceValue("application_type_name"); }
        }

        public Guid[] ContainerTypes
        {
            get { return new[] { Telligent.Evolution.Components.ContentTypes.Group }; }
        }

        public void AttachChangeEvents(IApplicationStateChanges stateChanges)
        {
            applicationStateChanges = stateChanges;
        }

        public IApplication Get(Guid applicationId)
        {
            return PublicApi.Libraries.Get(new LibraryGetOptions(applicationId));
        }

        #endregion

        #region IContentType Members

        public Guid[] ApplicationTypes
        {
            get { return new[] { ApplicationTypeId }; }
        }

        public void AttachChangeEvents(IContentStateChanges stateChanges)
        {
            contentStateChanges = stateChanges;
        }

        public Guid ContentTypeId
        {
            get { return Id; }
        }

        public string ContentTypeName
        {
            get { return ApplicationTypeName; }
        }

        IContent IContentType.Get(Guid contentId)
        {
            return PublicApi.Libraries.Get(new LibraryGetOptions(contentId));
        }

        #endregion

        #region ISecuredContentType Members

        IContent ISecuredContentType.Get(Guid contentId)
        {
            return PublicApi.Libraries.Get(new LibraryGetOptions(contentId));
        }

        public Guid GetSecurableId(IContent content)
        {
            return content.ContentId;
        }

        public Guid GetContentPermissionId(IContent content)
        {
            return ContentTypeId;
        }

        public Guid DefaultContentPermissionId
        {
            get { return ContentTypeId; }
        }

        public Guid DefaultPermissionId
        {
            get { return SharePointPermissionIds.ViewLibrary; }
        }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var enUS = new Translation("en-us");

                enUS.Set("application_no_permissions", "No permissions");
                enUS.Set("application_short_type_name", "Library");


                // Navigation items
                enUS.Set("navigationitem_name", "Document Library");
                enUS.Set("configuration_options", "Options");
                enUS.Set("configuration_label", "Label");
                enUS.Set("configuration_label_description", "Enter an optional label for this link.");
                enUS.Set("configuration_defaultLabel", "Libraries");

                // New post
                enUS.Set("link_label", "Upload a Document");
                enUS.Set("content_type_name", "document");

                enUS.Set("upload_document_link_label", "Upload to {0}");
                enUS.Set("create_library_link_label", "Create a Library");
                enUS.Set("import_library_link_label", "Import a Library");
                enUS.Set("application_type_name", "library");


                return new[] { enUS };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        #region IConfigurablePlugin

        protected IPluginConfiguration Configuration { get; private set; }

        public void Update(IPluginConfiguration configuration)
        {
            Configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var groups = new[] { new PropertyGroup("options", "Options", 0) };

                var pageSize = new Property("PageSize", "Number of Libraries to display", PropertyType.Int, 0, "10");
                var minMaxRule = new PropertyRule(typeof(MinMaxValueRule), false);
                ((MinMaxValueRule)minMaxRule.Rule).MinValue = 1;
                ((MinMaxValueRule)minMaxRule.Rule).MaxValue = MaxNumberOfLibraries;
                pageSize.Rules.Add(minMaxRule);

                groups[0].Properties.Add(pageSize);

                return groups;
            }
        }

        #endregion

        #region ISearchableContentType Members

        public IList<SearchIndexDocument> GetContentToIndex()
        {
            const int batchSize = 500;
            var lists = listService.ListItemsToReindex(Id, batchSize);
            var searchDocuments = new List<SearchIndexDocument>();

            foreach (var list in lists)
            {
                var library = new Library(list);
                var doc = TEApi.SearchIndexing.NewDocument(
                    library.ContentId,
                    Id,
                    "Library",
                    PublicApi.SharePointUrls.BrowseDocuments(library.Id),
                    library.Name,
                    PublicApi.Libraries.Events.OnRender(library, "Description", library.Name, "unknown"));

                doc.AddField(TEApi.SearchIndexing.Constants.CollapseField, string.Format("library:{0}", library.ContentId));
                doc.AddField(TEApi.SearchIndexing.Constants.IsContent, true.ToString(CultureInfo.InvariantCulture));
                doc.AddField(TEApi.SearchIndexing.Constants.ContentID, library.ContentId.ToString());
                doc.AddField(TEApi.SearchIndexing.Constants.Date, TEApi.SearchIndexing.FormatDate(library.Created));
                doc.AddField(TEApi.SearchIndexing.Constants.Category, "Libraries");

                searchDocuments.Add(doc);
            }

            return searchDocuments;
        }

        public string GetViewHtml(IContent content, Target target)
        {
            if (content == null || content.ContentId == Guid.Empty) return null;

            var library = PublicApi.Libraries.Get(new LibraryGetOptions(content.ContentId));
            if (library == null || library.Id == Guid.Empty) return null;

            var options = new RenderedSearchResultOptions
            {
                Target = target,
                Title = library.Name,
                Body = library.Description,
                Url = PublicApi.SharePointUrls.BrowseDocuments(library.Id),
                Date = library.Created,
                ContainerName = library.Container.HtmlName(target.ToString()),
                ContainerUrl = library.Container.Url,
                ApplicationName = library.Name,
                ApplicationUrl = PublicApi.SharePointUrls.BrowseDocuments(library.Id),
                TypeCssClass = "sharepoint-library",
                User = content.CreatedByUserId.HasValue ? TEApi.Users.Get(new UsersGetOptions { Id = content.CreatedByUserId.Value }) : null,
            };
            return content.ToRenderedSearchResult(options);
        }

        public int[] GetViewSecurityRoles(Guid contentId)
        {
            var library = PublicApi.Libraries.Get(new LibraryGetOptions(contentId));
            if (library == null) return new int[] { };

            var group = TEApi.Groups.Get(new GroupsGetOptions { Id = library.GroupId });
            if (group == null) return new int[] { };

            var roles = TEApi.Roles.List(group.ApplicationId, SharePointPermissionIds.ViewLibrary);
            return roles.Any() ? roles.Select(r => r.Id.GetValueOrDefault()).ToArray() : new int[] { };
        }

        public bool IsCacheable
        {
            get { return true; }
        }

        public void SetIndexStatus(Guid[] contentIds, bool isIndexed)
        {
            if (contentIds != null && contentIds.Length > 0)
            {
                listService.UpdateIndexingStatus(contentIds, isIndexed);
            }
        }

        public bool VaryCacheByUser
        {
            get { return true; }
        }

        #endregion

        #region IGroupCustomNavigationPlugin Members

        public PropertyGroup[] GetConfigurationProperties(int groupId)
        {
            var group = new PropertyGroup("config", "", 1);

            var p = new Property("group", "", PropertyType.Custom, 1, "") { ControlType = typeof(GroupSelectionList) };
            group.Properties.Add(p);

            if (groupId > 0)
            {
                p.Editable = false;
                p.DefaultValue = "Group=0";
                p.Visible = false;
                p.Attributes["IncludeCurrentGroup"] = "true";
            }

            p = new Property("label", "", PropertyType.String, 2, "")
            {
                Text = translationController.GetLanguageResourceValue("configuration_label"),
                ControlType = typeof(ContentFragmentTokenStringControl)
            };
            group.Properties.Add(p);

            return new[] { group };
        }

        public ICustomNavigationItem GetNavigationItem(Guid itemId, ICustomNavigationItemConfiguration configuration)
        {
            int groupId;
            if (!int.TryParse(HttpUtility.ParseQueryString(configuration.GetStringValue("group", ""))["Group"], out groupId))
                return null;

            if (groupId <= 0)
            {
                var group = CoreContext.Instance().CurrentGroup;
                if (group != null)
                    groupId = group.ID;
                else
                    return null;
            }

            string label = configuration.GetStringValue("label", "");
            if (string.IsNullOrEmpty(label))
            {
                label = translationController.GetLanguageResourceValue("configuration_defaultLabel");
            }

            return new CustomNavigationItem(Id,
                () => GetNavigationLabel(groupId, label),
                () => GetNavigationUrl(groupId),
                (int userId) => GetIsVisible(groupId, userId, Configuration),
                () => TEApi.Url.CurrentContext.ApplicationTypeId == Id)
            {
                Configuration = configuration,
                Plugin = this,
                CssClass = "sharepoint-libraries"
            };
        }

        public string NavigationTypeName
        {
            get { return translationController.GetLanguageResourceValue("navigationitem_name"); }
        }

        #endregion

        #region IGroupDefaultCustomNavigationPlugin Members

        public int DefaultOrderNumber
        {
            get { return int.MaxValue; }
        }

        public ICustomNavigationItem GetDefaultNavigationItem(int groupId, ICustomNavigationItemConfiguration configuration)
        {
            string label = translationController.GetLanguageResourceValue("configuration_defaultLabel");
            return new CustomNavigationItem(Id,
                () => GetNavigationLabel(groupId, label),
                () => GetNavigationUrl(groupId),
                (int userId) => GetIsVisible(groupId, userId, Configuration),
                () => TEApi.Url.CurrentContext.ApplicationTypeId == Id)
            {
                Configuration = configuration,
                Plugin = this,
                CssClass = "sharepoint-libraries"
            };
        }

        #endregion

        #region Event handlers

        private void EventsAfterCreate(LibraryAfterCreateEventArgs e)
        {

        }

        private void EventsAfterUpdate(LibraryAfterUpdateEventArgs e)
        {
            var appContent = PublicApi.Libraries.Get(new LibraryGetOptions(e.Id));

            if (contentStateChanges != null)
                contentStateChanges.Updated(appContent);

            if (applicationStateChanges != null)
                applicationStateChanges.Updated(appContent);

            SetIndexStatus(new[] { e.Id }, false);
        }

        private void EventsRender(LibraryRenderEventArgs e)
        {

        }

        private void EventsAfterDelete(LibraryAfterDeleteEventArgs e)
        {
            if (contentStateChanges != null)
                contentStateChanges.Deleted(e.Id);

            if (applicationStateChanges != null)
                applicationStateChanges.Deleted(e.Id);

            TEApi.Search.Delete(e.Id.ToString());
        }

        #endregion

        #region INavigableApplicationType

        public string PathDelimiter
        {
            get { return "libraries"; }
        }

        #endregion

        #region IApplicationNavigable

        public void RegisterUrls(IUrlController controller)
        {
            LibrariesRouteTable.Get().RegisterPages(controller);
            DocumentsRouteTable.Get().RegisterPages(controller);
        }

        #endregion

        #region IGroupNewPostLinkPlugin Members

        public IEnumerable<IGroupNewPostLink> GetNewPostLinks(int groupId, int userId)
        {
            var postLinks = new List<IGroupNewPostLink>();

            var pageName = TEApi.Url.CurrentContext.PageName;
            if (pageName.Equals("home", StringComparison.InvariantCultureIgnoreCase)
                || pageName.Equals("sharepoint-library-list", StringComparison.InvariantCultureIgnoreCase))
            {
                AddGroupHomeLinks(groupId, userId, postLinks);
            }
            else if (SPCoreService.ApplicationTypeId.HasValue && SPCoreService.ApplicationTypeId.Value == Id)
            {
                AddLibraryLinks(groupId, userId, postLinks);
            }

            return postLinks;
        }

        private void AddGroupHomeLinks(int groupId, int userId, List<IGroupNewPostLink> postLinks)
        {
            var permissions = GetUserPermissions(groupId, userId);
            if (permissions.HasFlag(UserPermissions.CreateLibrary))
            {
                // Create a Library (css = add-application)
                postLinks.Add(new GroupNewPostLink
                {
                    CssClass = "internal-link add-application create-sharepoint-library",
                    Label = translationController.GetLanguageResourceValue("create_library_link_label"),
                    Url = PublicApi.SharePointUrls.CreateLibrary(groupId),
                    NewPostTypeName = translationController.GetLanguageResourceValue("application_type_name")
                });
            }

            if (permissions.HasFlag(UserPermissions.ImportLibrary))
            {
                // Import a Library (css = add-application)
                postLinks.Add(new GroupNewPostLink
                {
                    CssClass = "internal-link add-application import-sharepoint-library",
                    Label = translationController.GetLanguageResourceValue("import_library_link_label"),
                    Url = PublicApi.SharePointUrls.ImportLibrary(groupId),
                    NewPostTypeName = translationController.GetLanguageResourceValue("application_type_name")
                });
            }

            if (permissions.HasFlag(UserPermissions.UploadDocument))
            {
                // Upload to [Library Name] (css = add-post)
                postLinks.AddRange(UploadToLibraryLinks(groupId));
            }
        }

        private void AddLibraryLinks(int groupId, int userId, List<IGroupNewPostLink> postLinks)
        {
            var permissions = GetUserPermissions(groupId, userId);
            if (permissions.HasFlag(UserPermissions.UploadDocument))
            {
                var currentLibrary = PublicApi.Libraries.Get(new LibraryGetOptions(SPCoreService.Context.LibraryId));
                if (currentLibrary != null)
                {
                    // Upload to this Library
                    postLinks.Add(MakeNewPostLink(currentLibrary));
                }
                else
                {
                    // Upload to [Library Name] (css = add-post)
                    postLinks.AddRange(UploadToLibraryLinks(groupId));
                }
            }
        }

        private IEnumerable<IGroupNewPostLink> UploadToLibraryLinks(int groupId)
        {
            var libraries = PublicApi.Libraries.List(groupId, new LibraryListOptions { PageSize = Configuration.GetInt("PageSize") });
            if (libraries != null && libraries.Any())
            {
                foreach (var library in libraries)
                {
                    yield return MakeNewPostLink(library);
                }
            }
        }

        public bool HasNewPostLinks(int groupId, int userId)
        {
            return GetNewPostLinks(groupId, userId).Any();
        }

        private IGroupNewPostLink MakeNewPostLink(Library library)
        {
            return new GroupNewPostLink
            {
                CssClass = "internal-link add-post new-sharepoint-document",
                Label = string.Format(translationController.GetLanguageResourceValue("upload_document_link_label"), library.Name),
                Url = PublicApi.SharePointUrls.AddDocument(library.Id),
                NewPostTypeName = translationController.GetLanguageResourceValue("content_type_name")
            };
        }

        public class GroupNewPostLink : IGroupNewPostLink
        {
            public string NewPostTypeName { get; set; }
            public string Label { get; set; }
            public string Url { get; set; }
            public string CssClass { get; set; }
        }

        #endregion

        #region IWebContextualApplicationType

        public IApplication GetCurrentApplication(IWebContext context)
        {
            var urlContext = TEApi.Url.CurrentContext.ContextItems.GetItemByApplicationType(LibraryApplicationType.Id);
            if (urlContext != null && urlContext.ApplicationId.HasValue)
            {
                return PublicApi.Libraries.Get(new LibraryGetOptions(urlContext.ApplicationId.Value));
            }
            return null;
        }

        public bool IsCurrentApplicationType(IWebContext context)
        {
            var urlContext = TEApi.Url.CurrentContext.ContextItems.GetItemByApplicationType(LibraryApplicationType.Id);
            return urlContext != null && urlContext.ApplicationId.HasValue;
        }

        #endregion

        private static string GetNavigationLabel(int groupId, string defaultLabel)
        {
            var libraries = PublicApi.Libraries.List(groupId, new LibraryListOptions { PageSize = 1 });
            if (libraries.TotalCount == 1)
            {
                var library = libraries.FirstOrDefault();
                if (library != null) return library.Name;
            }
            return defaultLabel;
        }

        private static string GetNavigationUrl(int groupId)
        {
            var libraries = PublicApi.Libraries.List(groupId, new LibraryListOptions { PageSize = 1 });
            if (libraries.TotalCount == 1)
            {
                var library = libraries.FirstOrDefault();
                if (library != null) return PublicApi.SharePointUrls.BrowseDocuments(library.Id);
            }
            return PublicApi.SharePointUrls.BrowseLibraries(groupId);
        }

        private static bool GetIsVisible(int groupId, int userId, IPluginConfiguration configuration)
        {
            var libraries = PublicApi.Libraries.List(groupId, new LibraryListOptions { PageSize = 1 });
            return libraries != null && libraries.TotalCount > 0;
        }

        [Flags]
        private enum UserPermissions
        {
            None = 0,
            CreateLibrary = 1,
            ImportLibrary = 2,
            UploadDocument = 4,
        }

        private static UserPermissions GetUserPermissions(int groupId, int userId)
        {
            var user = TEApi.Users.Get(new UsersGetOptions { Id = userId });
            if (user == null) return UserPermissions.None;

            var group = TEApi.Groups.Get(new GroupsGetOptions { Id = groupId });
            if (group == null) return UserPermissions.None;

            var isAdmin = TEApi.RoleUsers.IsUserInRoles(user.Username, new[] { "Administrators" });
            if (isAdmin)
            {
                return UserPermissions.CreateLibrary
                    | UserPermissions.ImportLibrary
                    | UserPermissions.UploadDocument;
            }
            else
            {
                var groupUser = TEApi.GroupUserMembers.Get(groupId, new GroupUserMembersGetOptions { UserId = userId });
                if (groupUser == null) return UserPermissions.None;

                var userPermissions = UserPermissions.None;
                var canImportCreateLibrary = groupUser.MembershipType.Equals("owner", StringComparison.OrdinalIgnoreCase)
                    || groupUser.MembershipType.Equals("manager", StringComparison.OrdinalIgnoreCase);
                if (canImportCreateLibrary)
                {
                    userPermissions |= UserPermissions.ImportLibrary;
                    userPermissions |= UserPermissions.CreateLibrary;
                }

                var canUploadDocuments = groupUser.MembershipType.Equals("owner", StringComparison.OrdinalIgnoreCase)
                    || groupUser.MembershipType.Equals("manager", StringComparison.OrdinalIgnoreCase)
                    || groupUser.MembershipType.Equals("member", StringComparison.OrdinalIgnoreCase);
                if (canUploadDocuments)
                {
                    userPermissions |= UserPermissions.UploadDocument;
                }
                return userPermissions;
            }
        }
    }
}
