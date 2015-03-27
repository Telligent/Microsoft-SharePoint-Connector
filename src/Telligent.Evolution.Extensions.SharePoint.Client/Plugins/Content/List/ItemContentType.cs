using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Telligent.Evolution.Components.Search;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.Search;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using PublicApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List
{
    public class ItemContentType : ITranslatablePlugin, ISearchableContentType, IWebContextualContentType, ISecuredContentType, ISecuredCommentViewContentType, ICommentableContentType, ITaggableContentType, IRateableContentType, IViewableContentType
    {
        private static readonly IListItemService listItemService = ServiceLocator.Get<IListItemService>();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();
        private static readonly IListItemDataService listItemDataService = ServiceLocator.Get<IListItemDataService>();

        private ITranslatablePluginController translationController;

        private IContentStateChanges contentStateChanges;

        public static Guid Id { get { return new Guid("97F36A1D-F92F-4E44-AF7A-E30A3DD8B8E8"); } }

        #region IPlugin Members

        public string Name
        {
            get { return "List Item content type"; }
        }

        public string Description
        {
            get { return "Provides content type information for SharePoint List Items."; }
        }

        public void Initialize()
        {
            PublicApi.ListItems.Events.AfterCreate += EventsAfterCreate;
            PublicApi.ListItems.Events.AfterUpdate += EventsAfterUpdate;
            PublicApi.ListItems.Events.Render += EventsRender;
            PublicApi.ListItems.Events.AfterDelete += EventsAfterDelete;
        }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var enUS = new Translation("en-us");

                enUS.Set("content_type_name", "SharePoint List Item");
                enUS.Set("application_no_permissions", "Permission denied");
                enUS.Set("application_no_permissions_description", "Sorry access to this item has been denied by SharePoint.");
                enUS.Set("application_short_type_name", "ListItem");

                return new[] { enUS };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        #region IContentType Members

        public Guid[] ApplicationTypes
        {
            get { return new[] { ListApplicationType.Id }; }
        }

        public Guid ContentTypeId
        {
            get { return Id; }
        }

        public string ContentTypeName
        {
            get { return translationController.GetLanguageResourceValue("content_type_name"); }
        }

        public void AttachChangeEvents(IContentStateChanges stateChanges)
        {
            contentStateChanges = stateChanges;
        }

        public IContent Get(Guid itemUniqueId)
        {
            var listId = EnsureListId(itemUniqueId);
            if (listId != Guid.Empty)
            {
                return PublicApi.ListItems.Get(listId, new SPListItemGetOptions(itemUniqueId));
            }
            return null;
        }

        #endregion

        #region ISecuredContentType Members

        public Guid GetSecurableId(IContent content)
        {
            return content.Application.ApplicationId;
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
            get { return SharePointPermissionIds.ViewList; }
        }

        #endregion

        #region ICommentableContentType Members

        public bool CanCreateComment(Guid contentId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewList);
        }

        public bool CanDeleteComment(Guid commentId, int userId)
        {
            var comment = TEApi.Comments.Get(commentId);
            return comment != null && HasPermission(comment.ContentId, userId, SharePointPermissionIds.ViewList);
        }

        public bool CanModifyComment(Guid commentId, int userId)
        {
            var comment = TEApi.Comments.Get(commentId);
            return comment != null && HasPermission(comment.ContentId, userId, SharePointPermissionIds.ViewList);
        }

        public bool CanReadComment(Guid commentId, int userId)
        {
            return true;
        }

        #endregion

        #region ISecuredCommentViewContentType Members

        public Guid ContentPermissionId
        {
            get { return ContentTypeId; }
        }

        public Guid PermissionId
        {
            get { return SharePointPermissionIds.ViewList; }
        }

        #endregion

        #region IRateableContentType Members

        public bool CanDeleteRating(Guid contentId, int ratingUserId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewList);
        }

        public bool CanRate(Guid contentId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewList);
        }

        #endregion

        #region ITaggableContentType Members

        public bool CanAddTags(Guid contentId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewList);
        }

        public bool CanRemoveTags(Guid contentId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewList);
        }

        #endregion

        #region ISearchableContentType Members

        public IList<SearchIndexDocument> GetContentToIndex()
        {
            var searchDocuments = new List<SearchIndexDocument>();
            var searchConfig = Telligent.Common.Services.Get<SearchConfiguration>();
            var maxFileSizeInBytes = searchConfig.MaxAttachmentFileSizeMB * 1024 * 1024;
            var credentialsManager = ServiceLocator.Get<ICredentialsManager>();
            var items = listItemService.ListItemsToReindex(ListApplicationType.Id, 500);
            foreach (var item in items)
            {
                var doc = TEApi.SearchIndexing.NewDocument(
                    item.ContentId,
                    Id,
                    "ListItem",
                    PublicApi.SharePointUrls.ListItem(item.ContentId),
                    item.DisplayName,
                    PublicApi.ListItems.Events.OnRender(item, "Description", item.DisplayName, "unknown"));

                var list = PublicApi.Lists.Get(item.ListId);

                doc.AddField(TEApi.SearchIndexing.Constants.RelatedId, item.ContentId.ToString());
                doc.AddField(TEApi.SearchIndexing.Constants.IsContent, true.ToString());
                doc.AddField(TEApi.SearchIndexing.Constants.ContentID, item.ContentId.ToString());
                doc.AddField(TEApi.SearchIndexing.Constants.ApplicationId, list.Id.ToString());
                doc.AddField(TEApi.SearchIndexing.Constants.GroupID, list.GroupId.ToString(CultureInfo.InvariantCulture));
                doc.AddField(TEApi.SearchIndexing.Constants.ContainerId, list.Container.ContainerId.ToString());

                doc.AddField(TEApi.SearchIndexing.Constants.CollapseField, string.Format("listitem:{0}", item.ContentId));
                doc.AddField(TEApi.SearchIndexing.Constants.Date, TEApi.SearchIndexing.FormatDate(item.CreatedDate));
                doc.AddField(TEApi.SearchIndexing.Constants.Category, "ListItems");

                var user = TEApi.Users.Get(new UsersGetOptions { Email = item.Author.Email });
                if (user != null && !user.HasErrors())
                {
                    doc.AddField(TEApi.SearchIndexing.Constants.UserDisplayName, user.DisplayName);
                    doc.AddField(TEApi.SearchIndexing.Constants.Username, user.Username);
                    doc.AddField(TEApi.SearchIndexing.Constants.CreatedBy, user.DisplayName);
                }

                var tags = TEApi.Tags.Get(item.ContentId, Id, null);
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        doc.AddField(TEApi.SearchIndexing.Constants.TagKeyword, tag.TagName.ToLower());
                        doc.AddField(TEApi.SearchIndexing.Constants.Tag, tag.TagName);
                    }
                }

                foreach (var field in item.EditableFields())
                {
                    if (field.FieldTypeKind == Microsoft.SharePoint.Client.FieldType.Attachments)
                    {
                        var webUrl = list.SPWebUrl.ToLowerInvariant();
                        var atachments = PublicApi.Attachments.List(item.ListId, new AttachmentsGetOptions(item.ContentId, field.InternalName));
                        foreach (var attachment in atachments)
                        {
                            string attachmentUrl = attachment.Uri.ToString().ToLowerInvariant();
                            if (attachmentUrl.StartsWith(webUrl))
                            {
                                string path = attachmentUrl.Replace(webUrl, string.Empty);
                                doc.AddField(string.Format("sp_{0}_{1}_name", field.InternalName.ToLowerInvariant(), attachment.Name.ToLowerInvariant()), attachment.Name);
                                doc.AddField(string.Format("sp_{0}_{1}_text", field.InternalName.ToLowerInvariant(), attachment.Name.ToLowerInvariant()), RemoteAttachment.GetText(webUrl, attachment.Name, path, credentialsManager, maxFileSizeInBytes));
                            }
                        }
                    }
                    else
                    {
                        var key = string.Format("sp_{0}", field.InternalName.ToLowerInvariant());
                        var value = item.ValueAsText(field.InternalName);
                        if (value != null)
                        {
                            doc.AddField(key, value.ToLowerInvariant());
                        }
                    }
                }

                searchDocuments.Add(doc);
            }

            return searchDocuments;
        }

        public int[] GetViewSecurityRoles(Guid contentId)
        {
            var listId = EnsureListId(contentId);
            if (listId != Guid.Empty)
            {
                var listItem = PublicApi.ListItems.Get(listId, new SPListItemGetOptions(contentId));
                if (listItem != null)
                {
                    var list = PublicApi.Lists.Get(new SPListGetOptions(listId));
                    var group = TEApi.Groups.Get(new GroupsGetOptions { Id = list.GroupId });
                    var roles = TEApi.Roles.List(group.ApplicationId, SharePointPermissionIds.ViewLibrary);
                    return roles.Any() ? roles.Select(r => r.Id.GetValueOrDefault()).ToArray() : new int[0];
                }
            }
            return new int[] { };
        }

        public bool IsCacheable
        {
            get { return true; }
        }

        public void SetIndexStatus(Guid[] contentIds, bool isIndexed)
        {
            listItemService.UpdateIndexingStatus(contentIds, isIndexed);
        }

        public bool VaryCacheByUser
        {
            get { return true; }
        }

        #endregion

        #region IViewableContentType

        public string GetViewHtml(IContent content, Target target)
        {
            if (content == null || content.ContentId == Guid.Empty) return null;

            var contentId = content.ContentId;
            var listId = EnsureListId(contentId);
            if (listId != Guid.Empty)
            {
                var listItem = PublicApi.ListItems.Get(listId, new SPListItemGetOptions(contentId));
                if (listItem != null)
                {
                    var options = new RenderedSearchResultOptions
                    {
                        Target = target,
                        Title = listItem.DisplayName,
                        Url = PublicApi.SharePointUrls.ListItem(listItem.ContentId),
                        Date = listItem.CreatedDate,
                        ContainerName = content.Application.Container != null ? content.Application.Container.HtmlName(target.ToString()) : null,
                        ContainerUrl = content.Application.Container != null ? content.Application.Container.Url : null,
                        ApplicationName = content.Application.HtmlName(target.ToString()),
                        ApplicationUrl = PublicApi.SharePointUrls.BrowseDocuments(listItem.ListId),
                        TypeCssClass = "sharepoint-listItem",
                        User = content.CreatedByUserId.HasValue ? TEApi.Users.Get(new UsersGetOptions { Id = content.CreatedByUserId.Value }) : null,
                        RemoteAttachments = new List<RenderedSearchResultAttachment>(
                            PublicApi.Attachments.List(listItem.ListId, new AttachmentsGetOptions(listItem.ContentId, "Attachments"))
                                .Select(_ =>
                                    new RenderedSearchResultAttachment
                                    {
                                        FileName = _.Name,
                                        Url = _.Uri.ToString()
                                    }))
                    };

                    return content.ToRenderedSearchResult(options);
                }
            }
            return null;
        }

        #endregion

        #region IWebContextualContentType

        public IContent GetCurrentContent(Extensibility.UI.Version1.IWebContext context)
        {
            var contentId = SPCoreService.Context.ListItemId;
            if (contentId != Guid.Empty)
            {
                return Get(contentId);
            }
            return null;
        }

        #endregion

        #region Event handlers

        private void EventsAfterCreate(ListItemAfterCreateEventArgs e)
        {

        }

        private void EventsAfterUpdate(ListItemAfterUpdateEventArgs e)
        {
            if (contentStateChanges != null)
                contentStateChanges.Updated(Get(e.ContentId));

            SetIndexStatus(new[] { e.ContentId }, false);
        }

        private void EventsRender(ListItemRenderEventArgs e)
        {
            var content = new StringBuilder();

            foreach (var field in e.EditableFields())
            {
                var value = e.ValueAsText(field.InternalName);
                if (string.IsNullOrEmpty(value)) continue;

                content.Append(string.Format(" {0}", value));
            }

            e.RenderedHtml = content.ToString();
        }

        private void EventsAfterDelete(ListItemAfterDeleteEventArgs e)
        {
            if (e.ContentId == Guid.Empty) return;

            if (contentStateChanges != null)
                contentStateChanges.Deleted(e.ContentId);

            try
            {
                TEApi.Search.Delete(e.ContentId.ToString());
            }
            catch (Exception)
            {
                SPLog.Event(string.Format("Warning: Could not remove index for {0}", e.ContentId));
            }
        }

        #endregion

        internal static string ViewUrl(int groupId, Guid contentId)
        {
            return (groupId > 0) ? TEApi.Url.Adjust(TEApi.GroupUrls.Custom(groupId, "list-item"), string.Concat("itemId=", contentId.ToString())) : string.Empty;
        }

        internal static string EditUrl(int groupId, Guid contentId)
        {
            return (groupId > 0) ? TEApi.Url.Adjust(TEApi.GroupUrls.Custom(groupId, "list-item-create-edit"), string.Concat("itemId=", contentId.ToString())) : string.Empty;
        }

        internal static bool HasGroupPermission(int groupId, int userId, Guid permissionId)
        {
            var group = TEApi.Groups.Get(new GroupsGetOptions { Id = groupId });

            var permission = TEApi.Permissions.Get(permissionId, userId, group.ContainerId, TEApi.Groups.ApplicationTypeId);
            return (permission != null && permission.IsAllowed);
        }

        private bool HasPermission(Guid contentId, int userId, params Guid[] permissions)
        {
            var listId = EnsureListId(contentId);
            if (listId != Guid.Empty)
            {
                var list = PublicApi.Lists.Get(listId);
                if (list != null)
                {
                    var groupId = list.GroupId;
                    return permissions.Aggregate(false, (hasPermission, permission) => hasPermission || HasGroupPermission(groupId, userId, permission));
                }
            }
            return false;
        }

        #region Utility

        private Guid EnsureListId(Guid itemUniqueId)
        {
            var listId = SPCoreService.Context.ListId;
            if (listId != Guid.Empty) return listId;

            var itemBase = listItemDataService.Get(itemUniqueId);
            if (itemBase != null)
            {
                listId = itemBase.ApplicationId;
            }
            return listId;
        }

        #endregion
    }
}
