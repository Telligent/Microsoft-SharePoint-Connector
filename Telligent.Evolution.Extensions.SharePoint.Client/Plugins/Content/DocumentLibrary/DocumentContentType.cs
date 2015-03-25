using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telligent.Evolution.Components.Search;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.Search;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using IContent = Telligent.Evolution.Extensibility.Content.Version1.IContent;
using PublicApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary
{
    public class DocumentContentType : ITranslatablePlugin, ISearchableContentType, IWebContextualContentType, ISecuredContentType, ISecuredCommentViewContentType, ICommentableContentType, ITaggableContentType, IRateableContentType, IViewableContentType
    {
        private static readonly IListItemService listItemService = ServiceLocator.Get<IListItemService>();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();
        private static readonly IListItemDataService listItemDataService = ServiceLocator.Get<IListItemDataService>();

        private ITranslatablePluginController translationController;
        private IContentStateChanges contentStateChanges;

        public static Regex XExt = new Regex(@"\.[^\.]+$", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Guid Id { get { return new Guid("F2BA56DC-271A-4001-AB52-A26454D7F113"); } }

        #region IPlugin Members

        public string Name
        {
            get { return "Document content type"; }
        }

        public string Description
        {
            get { return "Provides content type information for documents in a Document Library."; }
        }

        public void Initialize()
        {
            PublicApi.Documents.Events.AfterCreate += EventsAfterCreate;
            PublicApi.Documents.Events.AfterUpdate += EventsAfterUpdate;
            PublicApi.Documents.Events.Render += EventsRender;
            PublicApi.Documents.Events.AfterDelete += EventsAfterDelete;
        }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var enUS = new Translation("en-us");

                enUS.Set("content_type_name", "Document Library File");
                enUS.Set("application_no_permissions", "Permission denied");
                enUS.Set("application_no_permissions_description", "Sorry access to this file has been denied by SharePoint.");
                enUS.Set("application_short_type_name", "Document");

                return new[] { enUS };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        #region IContentType Members

        public Guid ContentTypeId
        {
            get { return Id; }
        }

        public string ContentTypeName
        {
            get { return translationController.GetLanguageResourceValue("content_type_name"); }
        }

        public Guid[] ApplicationTypes
        {
            get { return new[] { LibraryApplicationType.Id }; }
        }

        public void AttachChangeEvents(IContentStateChanges stateChanges)
        {
            contentStateChanges = stateChanges;
        }

        public IContent Get(Guid contentId)
        {
            var libraryId = EnsureLibraryId(contentId);
            if (libraryId != Guid.Empty)
            {
                return PublicApi.Documents.Get(libraryId, new DocumentGetOptions(contentId));
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
            get { return SharePointPermissionIds.ViewLibrary; }
        }

        #endregion

        #region ICommentableContentType Members

        public bool CanCreateComment(Guid contentId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewLibrary);
        }

        public bool CanDeleteComment(Guid commentId, int userId)
        {
            var comment = TEApi.Comments.Get(commentId);
            return comment != null && HasPermission(comment.ContentId, userId, SharePointPermissionIds.ViewLibrary);
        }

        public bool CanModifyComment(Guid commentId, int userId)
        {
            var comment = TEApi.Comments.Get(commentId);
            return comment != null && HasPermission(comment.ContentId, userId, SharePointPermissionIds.ViewLibrary);
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
            get { return SharePointPermissionIds.ViewLibrary; }
        }

        #endregion

        #region IRateableContentType Members

        public bool CanDeleteRating(Guid contentId, int ratingUserId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewLibrary);
        }

        public bool CanRate(Guid contentId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewLibrary);
        }

        #endregion

        #region ITaggableContentType Members

        public bool CanAddTags(Guid contentId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewLibrary);
        }

        public bool CanRemoveTags(Guid contentId, int userId)
        {
            return HasPermission(contentId, userId, SharePointPermissionIds.ViewLibrary);
        }

        #endregion

        #region ISearchableContentType Members

        public IList<SearchIndexDocument> GetContentToIndex()
        {
            var searchDocuments = new List<SearchIndexDocument>();
            var searchConfig = Telligent.Common.Services.Get<SearchConfiguration>();
            var maxFileSizeInBytes = searchConfig.MaxAttachmentFileSizeMB * 1024 * 1024;
            var credentialsManager = ServiceLocator.Get<ICredentialsManager>();
            var items = listItemService.ListItemsToReindex(LibraryApplicationType.Id, 500);
            foreach (var item in items)
            {
                try
                {
                    var document = new Document(item);
                    if (document.IsFolder) continue;

                    var extMatch = XExt.Match(document.Name);
                    var ext = extMatch.Success ? extMatch.Groups[0].Value.TrimStart('.') : string.Empty;

                    var doc = TEApi.SearchIndexing.NewDocument(
                        document.ContentId,
                        Id,
                        "Document",
                        PublicApi.SharePointUrls.Document(document.ContentId),
                        document.DisplayName,
                        PublicApi.Documents.Events.OnRender(document, "Description", document.Name, "unknown"));


                    doc.AddField(TEApi.SearchIndexing.Constants.RelatedId, document.ContentId.ToString());
                    doc.AddField(TEApi.SearchIndexing.Constants.IsContent, true.ToString());
                    doc.AddField(TEApi.SearchIndexing.Constants.ContentID, document.ContentId.ToString());
                    doc.AddField(TEApi.SearchIndexing.Constants.ApplicationId, document.Library.Id.ToString());
                    doc.AddField(TEApi.SearchIndexing.Constants.GroupID, document.Library.GroupId.ToString(CultureInfo.InvariantCulture));
                    doc.AddField(TEApi.SearchIndexing.Constants.ContainerId, document.Library.Container.ContainerId.ToString());

                    doc.AddField("sp_fileextension", ext.ToLowerInvariant());
                    doc.AddField(TEApi.SearchIndexing.Constants.CollapseField, string.Format("document:{0}", document.ContentId));
                    doc.AddField(TEApi.SearchIndexing.Constants.Date, TEApi.SearchIndexing.FormatDate(document.CreatedDate));
                    doc.AddField(TEApi.SearchIndexing.Constants.Category, "Documents");

                    var user = TEApi.Users.Get(new UsersGetOptions { Email = document.Author.Email });
                    if (user != null && !user.HasErrors())
                    {
                        doc.AddField(TEApi.SearchIndexing.Constants.UserDisplayName, user.DisplayName);
                        doc.AddField(TEApi.SearchIndexing.Constants.Username, user.Username);
                        doc.AddField(TEApi.SearchIndexing.Constants.CreatedBy, user.DisplayName);
                    }

                    var tags = TEApi.Tags.Get(document.ContentId, Id, null);
                    if (tags != null)
                    {
                        foreach (var tag in tags)
                        {
                            doc.AddField(TEApi.SearchIndexing.Constants.TagKeyword, tag.TagName.ToLower());
                            doc.AddField(TEApi.SearchIndexing.Constants.Tag, tag.TagName);
                        }
                    }

                    doc.AddField(TEApi.SearchIndexing.Constants.AttachmentName, document.Name);
                    doc.AddField(TEApi.SearchIndexing.Constants.AttachmentText, RemoteAttachment.GetText(document.Library.SPWebUrl, document.Name, document.Path, credentialsManager, maxFileSizeInBytes));

                    foreach (var field in item.EditableFields())
                    {
                        var key = string.Format("sp_{0}", field.InternalName.ToLowerInvariant());
                        var value = item.ValueAsText(field.InternalName);
                        if (value != null)
                        {
                            doc.AddField(key, value.ToLowerInvariant());
                        }
                    }

                    searchDocuments.Add(doc);
                }
                catch { }
            }

            return searchDocuments;
        }

        public int[] GetViewSecurityRoles(Guid contentId)
        {
            var itemBase = listItemDataService.Get(contentId);
            if (itemBase != null)
            {
                var listBase = listDataService.Get(itemBase.ApplicationId);
                if (listBase != null)
                {
                    var group = TEApi.Groups.Get(new GroupsGetOptions { Id = listBase.GroupId });
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

            var itemBase = listItemDataService.Get(content.ContentId);
            if (itemBase == null) return null;

            var document = PublicApi.Documents.Get(itemBase.ApplicationId, new DocumentGetOptions(content.ContentId));
            if (document == null || document.HasErrors() || document.ContentId == Guid.Empty) return null;

            var options = new RenderedSearchResultOptions
            {
                Target = target,
                Title = document.Title,
                Url = PublicApi.SharePointUrls.Document(document.ContentId),
                Date = document.CreatedDate,
                ContainerName = document.Library.Container != null ? content.Application.Container.HtmlName(target.ToString()) : null,
                ContainerUrl = document.Library.Container != null ? content.Application.Container.Url : null,
                ApplicationName = document.Library.Name,
                ApplicationUrl = PublicApi.SharePointUrls.BrowseDocuments(document.Library.Id),
                TypeCssClass = "sharepoint-document",
                User = content.CreatedByUserId.HasValue ? TEApi.Users.Get(new UsersGetOptions { Id = content.CreatedByUserId.Value }) : null,
                RemoteAttachments = new List<RenderedSearchResultAttachment>
                {
                    new RenderedSearchResultAttachment
                    {
                        FileName = document.Name,
                        Url = document.DownloadUrl
                    }
                }
            };
            return content.ToRenderedSearchResult(options);
        }

        #endregion

        #region IWebContextualContentType

        public IContent GetCurrentContent(Extensibility.UI.Version1.IWebContext context)
        {
            var contentId = SPCoreService.Context.DocumentId;
            if (contentId != Guid.Empty)
            {
                return Get(contentId);
            }
            return null;
        }

        #endregion

        #region Event handlers

        private void EventsAfterCreate(DocumentAfterCreateEventArgs e)
        {
        }

        private void EventsAfterUpdate(DocumentAfterUpdateEventArgs e)
        {
            if (contentStateChanges != null)
                contentStateChanges.Updated(Get(e.ContentId));

            SetIndexStatus(new[] { e.ContentId }, false);
        }

        private void EventsRender(DocumentRenderEventArgs e)
        {
            var item = PublicApi.ListItems.Get(e.Library.Id, new SPListItemGetOptions(e.ContentId));
            var content = new StringBuilder();

            foreach (var field in item.EditableFields())
            {
                var value = item.ValueAsText(field.InternalName);
                if (string.IsNullOrEmpty(value)) continue;

                content.Append(string.Format(" {0}", value));
            }

            e.RenderedHtml = content.ToString();
        }

        private void EventsAfterDelete(DocumentAfterDeleteEventArgs e)
        {
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
            return (groupId > 0) ? TEApi.Url.Adjust(TEApi.GroupUrls.Custom(groupId, "document-library-file"), string.Concat("documentId=", contentId.ToString())) : string.Empty;
        }

        internal static string EditUrl(int groupId, Guid contentId)
        {
            return (groupId > 0) ? TEApi.Url.Adjust(TEApi.GroupUrls.Custom(groupId, "document-library-edit"), string.Concat("documentId=", contentId.ToString())) : string.Empty;
        }

        internal static bool HasGroupPermission(int groupId, int userId, Guid permissionId)
        {
            var group = TEApi.Groups.Get(new GroupsGetOptions { Id = groupId });

            var permission = TEApi.Permissions.Get(permissionId, userId, group.ContainerId, TEApi.Groups.ApplicationTypeId);
            return (permission != null && permission.IsAllowed);
        }

        internal bool HasPermission(Guid contentId, int userId, params Guid[] permissions)
        {
            var libraryId = EnsureLibraryId(contentId);
            if (libraryId != Guid.Empty)
            {
                var library = PublicApi.Libraries.Get(new LibraryGetOptions(libraryId));
                if (library != null)
                {
                    var groupId = library.GroupId;
                    return permissions.Aggregate(false, (hasPermission, permission) => hasPermission || HasGroupPermission(groupId, userId, permission));
                }
            }
            return false;
        }

        #region Utility

        private Guid EnsureLibraryId(Guid contentId)
        {
            var libraryId = SPCoreService.Context.LibraryId;
            if (libraryId != Guid.Empty) return libraryId;

            var itemBase = listItemDataService.Get(contentId);
            if (itemBase != null)
            {
                libraryId = itemBase.ApplicationId;
            }
            return libraryId;
        }

        #endregion
    }
}
