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
using PluginManager = Telligent.Evolution.Extensibility.Version1.PluginManager;
using PublicApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List
{
    public class ListApplicationType : IApplicationType, ITranslatablePlugin, IConfigurablePlugin, ISearchableContentType, IGroupDefaultCustomNavigationPlugin, IGroupNewPostLinkPlugin, INavigableApplicationType, IApplicationNavigable, ICustomNavigationPlugin, IWebContextualApplicationType
    {
        private const int MaxNumberOfLists = 100;
        private static readonly IListService listService = ServiceLocator.Get<IListService>();

        private IApplicationStateChanges applicationStateChanges;
        private IContentStateChanges contentStateChanges;
        private ITranslatablePluginController translationController;

        public static Guid Id { get { return new Guid("82C21BA1-2BF6-49C6-B0F3-2A954EBDA03E"); } }

        #region IPlugin Members

        public string Name
        {
            get { return "SharePoint List application type"; }
        }

        public string Description
        {
            get { return "Provides application type information for SharePoint Lists in a group."; }
        }

        public void Initialize()
        {
            PublicApi.Lists.Events.AfterCreate += EventsAfterCreate;
            PublicApi.Lists.Events.AfterUpdate += EventsAfterUpdate;
            PublicApi.Lists.Events.Render += EventsRender;
            PublicApi.Lists.Events.AfterDelete += EventsAfterDelete;
            Extensibility.Api.Version1.PublicApi.Groups.Events.AfterDelete += GroupsEventsAfterDelete;
        }

        static void GroupsEventsAfterDelete(GroupAfterDeleteEventArgs e)
        {
            if (!e.Id.HasValue) return;

            var allListsInTheGroup = GetAllListsByGroupId(e.Id.Value);
            foreach (var list in allListsInTheGroup)
            {
                PublicApi.Lists.Delete(list, false);
            }
        }

        private static IEnumerable<SPList> GetAllListsByGroupId(int groupId)
        {
            var listsInTheGroup = new List<SPList>();

            const int pageSize = 10;
            int pageIndex = 0;
            int totalCount = 0;
            do
            {
                var lists = PublicApi.Lists.List(groupId, new SPListCollectionOptions
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });
                listsInTheGroup.AddRange(lists);
                totalCount = lists.TotalCount;
                pageIndex++;
            }
            while (pageSize * pageIndex < totalCount);

            return listsInTheGroup;
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
            get { return new[] { Evolution.Components.ContentTypes.Group }; }
        }

        public void AttachChangeEvents(IApplicationStateChanges stateChanges)
        {
            applicationStateChanges = stateChanges;
        }

        public IApplication Get(Guid listUniqueId)
        {
            return listService.Get(new SPListGetOptions(listUniqueId));
        }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var enUS = new Translation("en-us");

                enUS.Set("application_no_permissions", "No permissions");
                enUS.Set("application_short_type_name", "SPList");

                // Navigation items
                enUS.Set("navigationitem_name", "SharePoint List");
                enUS.Set("configuration_options", "Options");
                enUS.Set("configuration_label", "Label");
                enUS.Set("configuration_label_description", "Enter an optional label for this link.");
                enUS.Set("configuration_defaultLabel", "Lists");

                // New post
                enUS.Set("link_label", "New List Item");
                enUS.Set("content_type_name", "listItem");

                enUS.Set("upload_listitem_link_label", "New Item in {0}");
                enUS.Set("import_list_link_label", "Import a List");
                enUS.Set("application_type_name", "list");

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

                var pageSize = new Property("PageSize", "Number of Lists to display", PropertyType.Int, 0, "10");

                var minMaxRule = new PropertyRule(typeof(MinMaxValueRule), false);
                ((MinMaxValueRule)minMaxRule.Rule).MinValue = 1;
                ((MinMaxValueRule)minMaxRule.Rule).MaxValue = MaxNumberOfLists;
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
                var doc = TEApi.SearchIndexing.NewDocument(
                    list.ContentId,
                    Id,
                    "List",
                    PublicApi.SharePointUrls.BrowseListItems(list.Id),
                    list.Title,
                    PublicApi.Lists.Events.OnRender(list, "Description", list.Title, "unknown"));

                doc.AddField(TEApi.SearchIndexing.Constants.CollapseField, string.Format("list:{0}", list.ContentId));
                doc.AddField(TEApi.SearchIndexing.Constants.IsContent, true.ToString(CultureInfo.InvariantCulture));
                doc.AddField(TEApi.SearchIndexing.Constants.ContentID, list.ContentId.ToString());
                doc.AddField(TEApi.SearchIndexing.Constants.Date, TEApi.SearchIndexing.FormatDate(list.Created));
                doc.AddField(TEApi.SearchIndexing.Constants.Category, "Lists");

                searchDocuments.Add(doc);
            }

            return searchDocuments;
        }

        public string GetViewHtml(IContent content, Target target)
        {
            if (content == null || content.ContentId == Guid.Empty) return null;

            var list = PublicApi.Lists.Get(new SPListGetOptions(content.ContentId));
            if (list == null || list.Id == Guid.Empty) return null;

            var options = new RenderedSearchResultOptions
            {
                Target = target,
                Title = list.Title,
                Body = list.Description,
                Url = PublicApi.SharePointUrls.BrowseListItems(list.Id),
                Date = list.Created,
                ContainerName = list.Container.HtmlName(target.ToString()),
                ContainerUrl = list.Container.Url,
                ApplicationName = list.Title,
                ApplicationUrl = PublicApi.SharePointUrls.BrowseListItems(list.Id),
                TypeCssClass = "sharepoint-list",
                User = content.CreatedByUserId.HasValue ? TEApi.Users.Get(new UsersGetOptions { Id = content.CreatedByUserId.Value }) : null,
            };
            return content.ToRenderedSearchResult(options);
        }

        public int[] GetViewSecurityRoles(Guid contentId)
        {
            var splist = listService.Get(new SPListGetOptions(contentId));
            if (splist != null)
            {
                var group = TEApi.Groups.Get(new GroupsGetOptions { Id = splist.GroupId });
                if (!group.HasErrors())
                {
                    var groupSearchType = PluginManager.Get<ISearchableContentType>().FirstOrDefault(x => x.ContentTypeId == TEApi.Groups.ContentTypeId);
                    if (groupSearchType != null)
                        return groupSearchType.GetViewSecurityRoles(group.ContainerId);
                }
            }

            return new int[0];
        }

        public bool IsCacheable
        {
            get { return true; }
        }

        public void SetIndexStatus(Guid[] contentIds, bool isIndexed)
        {
            listService.UpdateIndexingStatus(contentIds, isIndexed);
        }

        public bool VaryCacheByUser
        {
            get { return true; }
        }

        #endregion

        #region IContentType Members

        public Guid[] ApplicationTypes
        {
            get { return new[] { TEApi.Groups.ContainerTypeId }; }
        }

        public void AttachChangeEvents(IContentStateChanges stateChanges)
        {
            contentStateChanges = stateChanges;
        }

        public Guid ContentTypeId
        {
            get { return ApplicationTypeId; }
        }

        public string ContentTypeName
        {
            get { return ApplicationTypeName; }
        }

        IContent IContentType.Get(Guid contentId)
        {
            return listService.Get(new SPListGetOptions(contentId));
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
                (int userId) => GetVisability(groupId, userId, Configuration),
                () => TEApi.Url.CurrentContext.ApplicationTypeId == Id)
            {
                Configuration = configuration,
                Plugin = this,
                CssClass = "sharepoint-lists"
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
                (int userId) => GetVisability(groupId, userId, Configuration),
                () => TEApi.Url.CurrentContext.ApplicationTypeId == Id)
            {
                Configuration = configuration,
                Plugin = this,
                CssClass = "sharepoint-lists"
            };
        }

        #endregion

        #region Event handlers

        private void EventsAfterCreate(ListAfterCreateEventArgs e)
        {

        }

        private void EventsAfterUpdate(ListAfterUpdateEventArgs e)
        {
            var appContent = listService.Get(new SPListGetOptions(e.Id));

            if (appContent == null) return;

            if (contentStateChanges != null)
                contentStateChanges.Updated(appContent);

            if (applicationStateChanges != null)
                applicationStateChanges.Updated(appContent);

            SetIndexStatus(new[] { e.Id }, false);
        }

        private void EventsRender(ListRenderEventArgs e)
        {

        }

        private void EventsAfterDelete(ListAfterDeleteEventArgs e)
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
            get { return "lists"; }
        }

        #endregion

        #region IApplicationNavigable

        public void RegisterUrls(IUrlController controller)
        {
            ListsRouteTable.Get().RegisterPages(controller);
            ListItemsRouteTable.Get().RegisterPages(controller);
        }

        #endregion

        #region IGroupNewPostLinkPlugin Members

        public IEnumerable<IGroupNewPostLink> GetNewPostLinks(int groupId, int userId)
        {
            var postLinks = new List<IGroupNewPostLink>();

            if (HasNewPostLinks(groupId, userId))
            {
                var pageName = TEApi.Url.CurrentContext.PageName;
                if (pageName.Equals("home", StringComparison.InvariantCultureIgnoreCase)
                    || pageName.Equals("sharepoint-list-list", StringComparison.InvariantCultureIgnoreCase))
                {
                    AddGroupHomeLinks(groupId, userId, postLinks);
                }
                else if (SPCoreService.ApplicationTypeId.HasValue && SPCoreService.ApplicationTypeId.Value == Id)
                {
                    AddListLinks(groupId, userId, postLinks);
                }
            }

            return postLinks;
        }

        private void AddGroupHomeLinks(int groupId, int userId, List<IGroupNewPostLink> postLinks)
        {
            // Upload to [List Name] (css = add-post)
            postLinks.AddRange(UploadToListLinks(groupId));

            var groupUser = TEApi.GroupUserMembers.Get(groupId, new GroupUserMembersGetOptions { UserId = userId });
            if (groupUser == null)
            {
                return;
            }

            var isAGroupMember = groupUser.MembershipType.Equals("owner", StringComparison.OrdinalIgnoreCase)
                || groupUser.MembershipType.Equals("manager", StringComparison.OrdinalIgnoreCase);

            if (!isAGroupMember)
            {
                return;
            }

            // Import a List (css = add-application)
            postLinks.Add(new GroupNewPostLink
            {
                CssClass = "internal-link add-application import-sharepoint-list",
                Label = translationController.GetLanguageResourceValue("import_list_link_label"),
                Url = PublicApi.SharePointUrls.ImportList(groupId),
                NewPostTypeName = translationController.GetLanguageResourceValue("application_type_name")
            });
        }

        private void AddListLinks(int groupId, int userId, List<IGroupNewPostLink> postLinks)
        {
            // List page
            var currentList = PublicApi.Lists.Get(SPCoreService.Context.ListId);
            if (currentList != null)
            {
                // List
                postLinks.Add(MakeNewPostLink(currentList));
            }
            else
            {
                // Upload to [List Name] (css = add-post)
                postLinks.AddRange(UploadToListLinks(groupId));
            }
        }

        private IEnumerable<IGroupNewPostLink> UploadToListLinks(int groupId)
        {
            var lists = PublicApi.Lists.List(groupId, new SPListCollectionOptions { PageSize = Configuration.GetInt("PageSize") });
            if (lists != null && lists.Any())
            {
                foreach (var list in lists)
                {
                    yield return MakeNewPostLink(list);
                }
            }
        }

        public bool HasNewPostLinks(int groupId, int userId)
        {
            return HasNewPostLinks(groupId, userId, Configuration);
        }

        private static bool HasNewPostLinks(int groupId, int userId, IPluginConfiguration configuration)
        {
            if (TEApi.RoleUsers.IsUserInRoles(TEApi.Users.AccessingUser.Username, new[] { "Administrators" }))
                return true;

            var group = TEApi.Groups.Get(new GroupsGetOptions { Id = groupId });
            if (group == null)
            {
                return false;
            }

            var groupUser = TEApi.GroupUserMembers.Get(groupId, new GroupUserMembersGetOptions { UserId = userId });
            if (groupUser == null)
            {
                return false;
            }

            var isAGroupMember = groupUser.MembershipType.Equals("owner", StringComparison.OrdinalIgnoreCase)
                || groupUser.MembershipType.Equals("manager", StringComparison.OrdinalIgnoreCase)
                || groupUser.MembershipType.Equals("member", StringComparison.OrdinalIgnoreCase);
            if (!isAGroupMember)
            {
                return false;
            }

            var lists = PublicApi.Lists.List(groupId, new SPListCollectionOptions { PageSize = configuration.GetInt("PageSize") });
            if (lists == null)
            {
                return false;
            }

            return lists.TotalCount > 0;
        }

        private IGroupNewPostLink MakeNewPostLink(SPList list)
        {
            return new GroupNewPostLink
            {
                CssClass = "internal-link add-post sharepoint-list-app",
                Label = string.Format(translationController.GetLanguageResourceValue("upload_listitem_link_label"), list.Title),
                Url = PublicApi.SharePointUrls.AddListItem(list.Id),
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
            var urlContext = TEApi.Url.CurrentContext.ContextItems.GetItemByApplicationType(ListApplicationType.Id);
            if (urlContext != null && urlContext.ApplicationId.HasValue)
            {
                return PublicApi.Lists.Get(new SPListGetOptions(urlContext.ApplicationId.Value));
            }
            return null;
        }

        public bool IsCurrentApplicationType(IWebContext context)
        {
            var urlContext = TEApi.Url.CurrentContext.ContextItems.GetItemByApplicationType(ListApplicationType.Id);
            return urlContext != null && urlContext.ApplicationId.HasValue;
        }

        #endregion

        private static string GetNavigationLabel(int groupId, string defaultLabel)
        {
            var lists = PublicApi.Lists.List(groupId, new SPListCollectionOptions { PageSize = 1 });
            if (lists.TotalCount == 1)
            {
                var list = lists.FirstOrDefault();
                if (list != null) return list.Title;
            }
            return defaultLabel;
        }

        private static string GetNavigationUrl(int groupId)
        {
            var lists = PublicApi.Lists.List(groupId, new SPListCollectionOptions { PageSize = 1 });
            if (lists.TotalCount == 1)
            {
                var list = lists.FirstOrDefault();
                if (list != null) return PublicApi.SharePointUrls.BrowseListItems(list.Id);
            }
            return PublicApi.SharePointUrls.BrowseLists(groupId);
        }

        private static bool GetVisability(int groupId, int userId, IPluginConfiguration configuration)
        {
            var lists = PublicApi.Lists.List(groupId, new SPListCollectionOptions { PageSize = 1 });
            return lists.TotalCount > 0 && HasNewPostLinks(groupId, userId, configuration);
        }
    }
}
