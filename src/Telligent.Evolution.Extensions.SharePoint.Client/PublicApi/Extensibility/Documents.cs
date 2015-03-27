using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class DocumentCreateOptions
    {
        private DocumentCreateOptions(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name could not be null or whitespace.");

            FileName = fileName;
        }

        public DocumentCreateOptions(string fileName, byte[] data)
            : this(fileName)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("File Data could not be null or empty.");

            Data = data;
        }

        public DocumentCreateOptions(string fileName, string cfsFileStore, string cfsFilePath, string cfsFileName)
            : this(fileName)
        {
            if (string.IsNullOrWhiteSpace(cfsFileStore))
                throw new ArgumentException("Central File Storage Name could not be null or whitespace.");
            CFSFileStore = cfsFileStore;

            if (string.IsNullOrWhiteSpace(cfsFilePath))
                throw new ArgumentException("Central File Storage File Path could not be null or whitespace.");
            CFSFilePath = cfsFilePath;

            if (string.IsNullOrWhiteSpace(cfsFileName))
                throw new ArgumentException("Central File Storage File Name could not be null or whitespace.");
            CFSFileName = cfsFileName;
        }

        public string FileName { get; private set; }
        public string CFSFileName { get; private set; }
        public string CFSFileStore { get; private set; }
        public string CFSFilePath { get; private set; }
        public byte[] Data { get; private set; }

        public string FolderPath { get; set; }
        public bool Overwrite { get; set; }
        public string Url { get; set; }

        public string FilePath
        {
            get
            {
                return String.Concat(FolderPath.TrimEnd('/'), '/', FileName.TrimStart('/'));
            }
        }
    }

    public class DocumentGetOptions
    {
        public DocumentGetOptions(Guid contentId)
        {
            if (contentId == Guid.Empty)
                throw new ArgumentException("Invalid Content Id.");

            ContentId = contentId;
        }

        public DocumentGetOptions(int id)
        {
            if (id < 0)
                throw new ArgumentException("Invalid Item Id.");

            Id = id;
        }

        public Guid ContentId { get; private set; }
        public int? Id { get; private set; }
        public string Url { get; set; }
    }

    public class DocumentUpdateOptions : DocumentGetOptions
    {
        public DocumentUpdateOptions(Guid contentId) : base(contentId) { }
        public DocumentUpdateOptions(int id) : base(id) { }

        public IDictionary Fields { get; set; }
    }

    public class DocumentListOptions
    {
        public DocumentListOptions()
        {
            PageSize = 20;
        }

        public int PageSize { get; set; }
        public int PageIndex { get; set; }
        public string SortBy { get; set; }
        public SortOrder SortOrder { get; set; }
        public string FolderPath { get; set; }
        public string Url { get; set; }
    }

    public class DocumentCheckInOptions : DocumentGetOptions
    {
        public DocumentCheckInOptions(Guid contentId) : base(contentId) { }
        public DocumentCheckInOptions(int id) : base(id) { }

        public bool KeepCheckOut { get; set; }
        public string CheckinType { get; set; }
        public string Comment { get; set; }
    }

    public class DocumentRestoreOptions : DocumentGetOptions
    {
        public DocumentRestoreOptions(Guid contentId) : base(contentId) { }
        public DocumentRestoreOptions(int id) : base(id) { }

        public string Version { get; set; }
    }

    public interface IDocuments : ICacheable
    {
        DocumentEvents Events { get; }
        Document Create(Guid libraryId, DocumentCreateOptions options);
        Document Get(Guid libraryId, DocumentGetOptions options);
        PagedList<Document> List(Guid libraryId, DocumentListOptions options = null);
        Document Update(Guid libraryId, DocumentUpdateOptions options);
        void Delete(Guid libraryId, DocumentGetOptions options);
        void CheckIn(Guid libraryId, DocumentCheckInOptions options);
        void CheckOut(Guid libraryId, DocumentGetOptions options);
        void UndoCheckOut(Guid libraryId, DocumentGetOptions options);
        void Restore(Guid libraryId, DocumentRestoreOptions options);
        ApiList<SPDocumentVersion> GetVersions(Guid libraryId, DocumentGetOptions options);
        SPDocumentInfo GetDetails(Guid libraryId, DocumentGetOptions options);
        void ExpireTags(Guid libraryId);
    }

    public class Documents : IDocuments
    {
        private const int DefaultPageSize = 20;

        internal static readonly string[] ViewFields = { "UniqueId", "Title", "FileLeafRef", "FileRef", "Created", "Modified", "DocIcon", "ContentTypeId", "MetaInfo", "Author", "Editor", "CheckedOutUserId" };

        private readonly IListDataService listDataService;
        private readonly IListItemService listItemService;
        private readonly IFileService fileService;
        private readonly ICacheService cacheService;

        public Documents()
            : this(ServiceLocator.Get<IListDataService>(), ServiceLocator.Get<IListItemService>(), ServiceLocator.Get<IFileService>(), ServiceLocator.Get<ICacheService>())
        {
        }

        internal Documents(IListDataService listDataService, IListItemService listItemService, IFileService fileService, ICacheService cacheService)
        {
            this.listDataService = listDataService;
            this.listItemService = listItemService;
            this.fileService = fileService;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        private DocumentEvents events;
        public DocumentEvents Events
        {
            get { return events ?? (events = new DocumentEvents()); }
        }

        public Document Create(Guid libraryId, DocumentCreateOptions options)
        {
            try
            {
                Events.OnBeforeCreate(new Document { Url = options.Url, Name = options.FileName });
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Documents.Events.OnBeforeCreate() method for LibraryId: {1}. The exception message is: {2}", ex.GetType(), libraryId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            var document = new Document();
            var splistItem = listItemService.Create(libraryId, options);
            if (splistItem != null)
            {
                document = new Document(splistItem);
                ExpireTags(libraryId);
            }

            if (!document.HasErrors())
            {
                cacheService.Put(GetCacheId(document.ContentId), document, CacheScope.Context | CacheScope.Process, new[] { Tag(document.Library.Id) }, CacheTimeOut);
                try
                {
                    Events.OnAfterCreate(document);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the PublicApi.Documents.Events.OnAfterCreate() method for LibraryId: {1}. The exception message is: {2}", ex.GetType(), libraryId, ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
            }

            return document;
        }

        public Document Get(Guid libraryId, DocumentGetOptions options)
        {
            var cacheId = GetCacheId(libraryId, options);
            var document = (Document)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (document == null)
            {
                if (string.IsNullOrEmpty(options.Url))
                {
                    options.Url = GetUrl(libraryId);
                }

                document = new Document(listItemService.Get(libraryId, options));
                cacheService.Put(cacheId, document, CacheScope.Context | CacheScope.Process, new[] { Tag(libraryId) }, CacheTimeOut);
            }
            return document;
        }

        public PagedList<Document> List(Guid libraryId, DocumentListOptions options = null)
        {
            if (options == null)
            {
                options = new DocumentListOptions { PageSize = DefaultPageSize };
            }

            if (string.IsNullOrEmpty(options.Url))
            {
                options.Url = GetUrl(libraryId);
            }

            var cacheId = GetCacheId(libraryId, options);
            var documents = (PagedList<Document>)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (documents == null)
            {
                var listItems = listItemService.List(libraryId, options);
                documents = new PagedList<Document>(listItems.Select(_ => new Document(_)))
                                {
                                    PageSize = options.PageSize,
                                    PageIndex = options.PageIndex,
                                    TotalCount = listItems.TotalCount
                                };

                cacheService.Put(cacheId, documents, CacheScope.Context | CacheScope.Process, new[] { Tag(libraryId) }, CacheTimeOut);
            }
            return documents;
        }

        public Document Update(Guid libraryId, DocumentUpdateOptions options)
        {
            var documentId = options.Id.HasValue ? options.Id.Value.ToString(CultureInfo.InvariantCulture) : options.ContentId.ToString("N");
            var documentBeforeUpdate = Get(libraryId, options);
            if (documentBeforeUpdate == null) return null;

            try
            {
                Events.OnBeforeUpdate(documentBeforeUpdate);
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the PublicApi.Documents.Events.OnBeforeUpdate() method for LibraryId: {1}, DocumentId: {2}. The exception message is: {3}", ex.GetType(), libraryId, documentId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            listItemService.Update(libraryId, options);
            ExpireTags(libraryId);

            var document = Get(libraryId, options);
            if (document != null)
            {
                try
                {
                    Events.OnAfterUpdate(document);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the PublicApi.Documents.Events.OnAfterUpdate() method for LibraryId: {1}, DocumentId: {2}. The exception message is: {3}", ex.GetType(), libraryId, documentId, ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
            }
            return document;
        }

        public void Delete(Guid libraryId, DocumentGetOptions options)
        {
            var documentId = options.Id.HasValue ? options.Id.Value.ToString(CultureInfo.InvariantCulture) : options.ContentId.ToString("N");
            var documentBeforeDelete = Get(libraryId, options);
            if (documentBeforeDelete == null) return;

            try
            {
                Events.OnBeforeDelete(documentBeforeDelete);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Documents.Events.OnBeforeDelete() method for LibraryId: {1}, DocumentId: {2}. The exception message is: {3}", ex.GetType(), libraryId, documentId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            listItemService.Delete(libraryId, options);
            ExpireTags(libraryId);

            try
            {
                Events.OnAfterDelete(documentBeforeDelete);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Documents.Events.OnAfterDelete() method for LibraryId: {1}, DocumentId: {2}. The exception message is: {3}", ex.GetType(), libraryId, documentId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
        }

        public void CheckIn(Guid libraryId, DocumentCheckInOptions options)
        {
            var document = Get(libraryId, options);
            fileService.CheckIn(document.Library.SPWebUrl, document.Library.Id, document.Id, options);
            ExpireTags(document.Library.Id);
        }

        public void CheckOut(Guid libraryId, DocumentGetOptions options)
        {
            var document = Get(libraryId, options);
            fileService.CheckOut(document.Library.SPWebUrl, document.Library.Id, document.Id);
            ExpireTags(document.Library.Id);
        }

        public void UndoCheckOut(Guid libraryId, DocumentGetOptions options)
        {
            var document = Get(libraryId, options);
            fileService.UndoCheckOut(document.Library.SPWebUrl, document.Library.Id, document.Id);
            ExpireTags(document.Library.Id);
        }

        public void Restore(Guid libraryId, DocumentRestoreOptions options)
        {
            var document = Get(libraryId, options);
            fileService.Restore(document.Library.SPWebUrl, document.Library.Id, document.Id, options.Version);
            ExpireTags(document.Library.Id);
        }

        public ApiList<SPDocumentVersion> GetVersions(Guid libraryId, DocumentGetOptions options)
        {
            var versionsCacheId = GetVersionsCacheId(libraryId, options);
            var documentVersionList = (ApiList<SPDocumentVersion>)cacheService.Get(versionsCacheId, CacheScope.Context | CacheScope.Process);
            if (documentVersionList == null)
            {
                var document = Get(libraryId, options);
                documentVersionList = new ApiList<SPDocumentVersion>(fileService.GetVersions(document.Library.SPWebUrl, document.Library.Id, document.Id));
                cacheService.Put(versionsCacheId, documentVersionList, CacheScope.Context | CacheScope.Process, new[] { DocumentPropertiesTag(document.ContentId) }, CacheTimeOut);
            }
            return documentVersionList;
        }

        public SPDocumentInfo GetDetails(Guid libraryId, DocumentGetOptions options)
        {
            var detailsCacheId = GetDetailsCacheId(libraryId, options);
            var documentDetails = (SPDocumentInfo)cacheService.Get(detailsCacheId, CacheScope.Context | CacheScope.Process);
            if (documentDetails == null)
            {
                var document = Get(libraryId, options);
                documentDetails = fileService.GetDetails(document.Library.SPWebUrl, document.Library.Id, document.Id);
                cacheService.Put(detailsCacheId, documentDetails, CacheScope.Context | CacheScope.Process, new[] { DocumentPropertiesTag(document.ContentId) }, CacheTimeOut);
            }
            return documentDetails;
        }

        #region Cache-related methods

        public void ExpireTags(Guid applicationId)
        {
            cacheService.RemoveByTags(new[] { Tag(applicationId), Folders.Tag(applicationId), ListItems.Tag(applicationId) }, CacheScope.Context | CacheScope.Process);
        }

        internal static string Tag(Guid applicationId)
        {
            return string.Concat("SharePoint_Document_TAG:", applicationId.ToString("N"));
        }
        
        private static string GetCacheId(Guid contentId)
        {
            return string.Concat("SharePoint_Document:", contentId.ToString("N"));
        }

        private static string GetDocumentId(DocumentGetOptions options)
        {
            return options.ContentId != Guid.Empty ? options.ContentId.ToString("N") : options.Id.GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
        }
        
        private static string GetVersionsCacheId(Guid applicationId, DocumentGetOptions options)
        {
            return string.Concat("Documents.Versions:", applicationId.ToString("N"), ":", GetDocumentId(options));
        }

        private static string GetDetailsCacheId(Guid applicationId, DocumentGetOptions options)
        {
            return string.Concat("Documents.Details:", applicationId.ToString("N"), ":", GetDocumentId(options));
        }

        private static string GetCacheId(Guid applicationId, DocumentGetOptions options)
        {
            return string.Concat("Documents.Get:", applicationId.ToString("N"), ":", GetDocumentId(options));
        }

        private static string GetCacheId(Guid applicationId, DocumentListOptions options)
        {
            return string.Join(":",
                                new[]
                                {
                                    "Documents.List",
                                    applicationId.ToString("N"),
                                    options.PageSize.ToString(CultureInfo.InvariantCulture),
                                    options.PageIndex.ToString(CultureInfo.InvariantCulture),
                                    options.SortBy,
                                    options.SortOrder.ToString(),
                                    options.FolderPath??string.Empty
                                });
        }
        
        private static string DocumentPropertiesTag(Guid contentId)
        {
            return string.Concat("SharePoint_Document_Properties_TAG:", contentId.ToString("N"));
        }

        #endregion

        private string GetUrl(Guid listId)
        {
            var listBase = listDataService.Get(listId);
            if (listBase != null)
            {
                return listBase.SPWebUrl;
            }
            return null;
        }
    }
}
