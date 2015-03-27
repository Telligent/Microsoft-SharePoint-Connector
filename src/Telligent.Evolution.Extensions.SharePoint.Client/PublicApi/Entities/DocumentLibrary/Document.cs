using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class Document : ApiEntity, IContent
    {
        private static readonly IDocumentUrls documentUrls = ServiceLocator.Get<IDocumentUrls>();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();

        private readonly SPListItem listItem;
        private Library library;
        private string displayName, name, path, title, url;
        private bool? isFolder = null;
        private Guid contentId;

        public const string FolderContentType = "0x0120";

        public Document() { }

        public Document(Guid id)
        {
            contentId = id;
        }

        public Document(AdditionalInfo additionalInfo)
            : base(additionalInfo) { }

        public Document(IList<Warning> warnings, IList<Error> errors)
            : base(warnings, errors) { }

        public Document(SPListItem item)
        {
            listItem = item ?? new SPListItem();

            if (Author != null)
            {
                var user = TEApi.Users.Get(new UsersGetOptions { Email = Author.Email });
                CreatedByUserId = user != null ? user.Id : null;

                AvatarUrl = Author.AvatarUrl;
            }

            if (!listItem.Errors.Any()) return;

            foreach (var error in listItem.Errors)
            {
                Errors.Add(error);
            }
        }

        [Documentation(Description = "List Item counter Id")]
        public int Id { get { return listItem.Id; } }

        [Documentation(Description = "Parent Library")]
        public Library Library { get { return library = library ?? PublicApi.Libraries.Get(new LibraryGetOptions(listItem.ListId)); } }

        [Documentation(Description = "Document Title")]
        public string Title
        {
            get
            {
                if (string.IsNullOrEmpty(title))
                {
                    title = listItem.Value("Title") != null ? listItem.Value("Title").ToString() : Name;
                }
                return title;
            }
            internal set { title = value; }
        }

        [Documentation(Description = "File Name")]
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    name = listItem.Value("FileLeafRef") != null ? listItem.Value("FileLeafRef").ToString() : string.Empty;
                }
                return name;
            }
            internal set { name = value; }
        }

        private string downloadUrl = string.Empty;
        [Documentation(Description = "Download URL")]
        public string DownloadUrl
        {
            get
            {
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    downloadUrl = string.Concat(Library.SPWebUrl, Path);
                }
                return downloadUrl;
            }
            internal set { downloadUrl = value; }
        }

        [Documentation(Description = "File Name")]
        public string DisplayName
        {
            get { return string.IsNullOrEmpty(displayName) ? displayName = DocumentContentType.XExt.Replace(Name, string.Empty) : displayName; }
            internal set { displayName = value; }
        }

        [Documentation(Description = "File path in the Document Library")]
        public string Path
        {
            get
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = listItem.Value("FileRef") != null ? listItem.Value("FileRef").ToString() : string.Empty;
                }
                return path;
            }
            internal set { path = value; }
        }

        [Documentation(Description = "The type of a file icon")]
        public string DocIcon { get { return listItem.Value("DocIcon") != null ? listItem.Value("DocIcon").ToString() : string.Empty; } }

        [Documentation(Description = "Determines if object is a folder")]
        public bool IsFolder
        {
            get
            {
                if (!isFolder.HasValue)
                {
                    isFolder = listItem.Value("ContentTypeId") != null && listItem.Value("ContentTypeId").ToString().StartsWith(FolderContentType);
                }
                return isFolder.Value;
            }
        }

        [Documentation(Description = "Modified date in local date time format")]
        public DateTime Modified
        {
            get
            {
                return listItem.Modified;
            }
        }

        [Documentation(Description = "Author account name")]
        public Author Author { get { return listItem.Author; } }

        [Documentation(Description = "Editor account name")]
        public Author Editor { get { return listItem.Editor; } }

        [Documentation(Description = "Short info about file content")]
        public string MetaInfo { get { return listItem.Value("MetaInfo").ToString(); } }

        [Documentation(Description = "Returns true if the document has been checked out")]
        public bool IsCheckedOut
        {
            get
            {
                var isCheckedOut = listItem.Value("CheckedOutUserId") as FieldLookupValue;
                return isCheckedOut != null && isCheckedOut.LookupValue != null;
            }
        }

        #region IContentType Members

        [Documentation(Description = "List Item unique Id")]
        public Guid ContentId
        {
            get
            {
                if (contentId == Guid.Empty)
                {
                    contentId = listItem.UniqueId;
                }
                return contentId;
            }
        }

        public string Url
        {
            get
            {
                if (string.IsNullOrEmpty(url))
                {
                    var listBase = listDataService.Get(listItem.ListId);
                    if (listBase != null)
                    {
                        url = documentUrls.ViewDocument(listBase, new ItemUrlQuery(Id, ContentId));
                    }
                }
                return url;
            }
            internal set { url = value; }
        }

        public Guid ContentTypeId
        {
            get { return DocumentContentType.Id; }
        }

        public DateTime CreatedDate
        {
            get { return listItem.CreatedDate; }
        }

        public string HtmlName(string target)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return HttpUtility.HtmlEncode(Name);
            }
            return string.Empty;
        }

        public string HtmlDescription(string target)
        {
            if (!string.IsNullOrEmpty(DisplayName))
            {
                return HttpUtility.HtmlEncode(DisplayName);
            }
            return string.Empty;
        }

        public IApplication Application
        {
            get { return Library; }
        }

        public int? CreatedByUserId { get; private set; }

        public bool IsEnabled { get { return true; } }

        public string AvatarUrl { get; private set; }

        #endregion
    }
}
