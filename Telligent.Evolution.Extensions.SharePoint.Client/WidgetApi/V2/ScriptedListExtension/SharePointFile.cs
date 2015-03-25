using System;
using System.Collections;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using PublicApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class SharePointFileExtension : IScriptedContentFragmentExtension, ITranslatablePlugin
    {
        internal static class Translations
        {
            public const string CannotBeCheckedOut = "document_cannot_be_checkedout";
            public const string CannotBeCheckedIn = "document_cannot_be_checkedin";
            public const string CannotBeCreated = "document_cannot_be_created";
            public const string AlreadyExists = "document_already_exists";
            public const string CannotBeUpdated = "document_cannot_be_updated";
            public const string InvalidCreateOptions = "document_invalid_createoptions";
            public const string InvalidUpdateOptions = "document_invalid_updateoptions";
            public const string InvalidId = "document_invalid_id";
            public const string InvalidLibraryId = "document_invalid_libraryid";
            public const string LibraryNotFound = "document_library_notfound";
            public const string NotFound = "document_notfound";
            public const string NotFoundBecauseDeleted = "document_notfound_because_deleted";
            public const string EmailSendError = "document_emailsend_error";
            public const string UnknownError = "document_unknown_error";
        }

        private ITranslatablePluginController translationController;

        #region IScriptedContentFragmentExtension Members

        public string ExtensionName
        {
            get { return "sharepoint_v2_file"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointFile>(); }
        }

        public string Name
        {
            get { return "SharePoint File Extension (sharepoint_v2_file)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to work with files on the SharePoint side."; }
        }

        public void Initialize() { }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set(Translations.CannotBeCheckedOut, "The document cannot be checked out.");
                t.Set(Translations.CannotBeCheckedIn, "The document cannot be checked in.");
                t.Set(Translations.CannotBeCreated, "The document cannot be created.");
                t.Set(Translations.AlreadyExists, "The file already exists.");
                t.Set(Translations.CannotBeUpdated, "The document cannot be updated.");
                t.Set(Translations.InvalidCreateOptions, "A document cannot be created, because specified options are invalid.");
                t.Set(Translations.InvalidUpdateOptions, "The document cannot be updated, because specified options are invalid.");
                t.Set(Translations.InvalidId, "The document Id is invalid.");
                t.Set(Translations.InvalidLibraryId, "The document library Id is invalid.");
                t.Set(Translations.LibraryNotFound, "The document library cannot be found.");
                t.Set(Translations.NotFound, "The document cannot be found.");
                t.Set(Translations.NotFoundBecauseDeleted, "The document has been already deleted.");
                t.Set(Translations.UnknownError, "An error has been occurred, please try again or contact your administrator.");
                t.Set(Translations.EmailSendError, "An error has been occurred while sending the Email.");

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

    public interface ISharePointFile
    {
        Document Current { get; }

        Document Get(Guid contentId);

        PagedList<Document> List(Guid applicationId);
        PagedList<Document> List(Guid applicationId, IDictionary options);

        Document Create(Guid applicationId, string name, IDictionary options);

        Document Update(Guid contentId, IDictionary options);

        AdditionalInfo Delete(Guid contentId);

        AdditionalInfo CheckIn(Guid contentId);
        AdditionalInfo CheckIn(Guid contentId, IDictionary options);

        AdditionalInfo CheckOut(Guid contentId);

        AdditionalInfo UndoCheckOut(Guid contentId);

        bool IsCheckedOut(Guid contentId);

        PagedList<SPDocumentVersion> GetVersions(Guid contentId);
        PagedList<SPDocumentVersion> GetVersions(Guid contentId, IDictionary options);

        SPDocumentInfo GetInfo(Guid contentId);

        AdditionalInfo Restore(Guid contentId, string version);

        AdditionalInfo SendEmail(Guid contentId, IDictionary options);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointFile : ISharePointFile
    {
        private static readonly SharePointFileExtension plugin = PluginManager.Get<SharePointFileExtension>().FirstOrDefault();
        private static readonly IListItemDataService listItemDataService = ServiceLocator.Get<IListItemDataService>();

        private const int DefaultPageSize = 20;

        #region ISharePointFile

        public Document Current
        {
            get
            {
                try
                {
                    var documentId = SPCoreService.Context.DocumentId;
                    if (documentId != Guid.Empty)
                    {
                        var libraryId = EnsureLibraryId(documentId);
                        if (libraryId != Guid.Empty)
                            return PublicApi.Documents.Get(libraryId, new DocumentGetOptions(documentId));
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.Current property. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
                return null;
            }
        }

        public Document Get(Guid contentId)
        {
            var libraryId = EnsureLibraryId(contentId);
            if (libraryId == Guid.Empty) return null;

            var document = new Document();

            try
            {
                document = PublicApi.Documents.Get(libraryId, new DocumentGetOptions(contentId));
            }
            catch (InvalidOperationException ex)
            {
                document.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                document.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.NotFound, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.Get() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                document.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return document;
        }

        public PagedList<Document> List(Guid applicationId)
        {
            var documents = new PagedList<Document>();
            try
            {
                documents = PublicApi.Documents.List(applicationId);
            }
            catch (InvalidOperationException ex)
            {
                documents.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidLibraryId, applicationId)));
            }
            catch (SPInternalException ex)
            {
                documents.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.LibraryNotFound, applicationId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.List() method for LibraryId: {1}. The exception message is: {2}", ex.GetType(), applicationId, ex.Message);
                SPLog.UnKnownError(ex, message);
                documents.Errors.Add(new Error(ex.GetType().ToString(), ex.Message));
            }
            return documents;
        }

        public PagedList<Document> List(Guid applicationId,
            [Documentation(Name = "FolderPath", Type = typeof(string), Description = "Folder server relative Url"),
            Documentation(Name = "PageIndex", Type = typeof(int)),
            Documentation(Name = "PageSize", Type = typeof(int)),
            Documentation(Name = "SortBy", Type = typeof(string)),
            Documentation(Name = "SortOrder", Type = typeof(string), Options = new[] { "Ascending", "Descending" })]
            IDictionary options)
        {
            var documents = new PagedList<Document>();
            try
            {
                documents = PublicApi.Documents.List(applicationId, ProcessDocumentListOptions(options));
            }
            catch (InvalidOperationException ex)
            {
                documents.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidLibraryId, applicationId)));
            }
            catch (SPInternalException ex)
            {
                documents.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.LibraryNotFound, applicationId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.List() method for LibraryId: {1}. The exception message is: {2}", ex.GetType(), applicationId, ex.Message);
                SPLog.UnKnownError(ex, message);
                documents.Errors.Add(new Error(ex.GetType().ToString(), ex.Message));
            }
            return documents;
        }

        public Document Create(Guid applicationId, string name,
            [Documentation(Name = "Data", Type = typeof(byte[]), Description = "File Data"),
            Documentation(Name = "DataUrl", Type = typeof(string), Description = "Base64 encoded Data"),
            Documentation(Name = "Url", Type = typeof(string), Description = "CFS File Url (use $core_v2_uploadedFile.Get() method to get a file url)"),
            Documentation(Name = "FolderPath", Type = typeof(string), Description = "Folder server relative Url"),
            Documentation(Name = "Overwrite", Type = typeof(bool))]
            IDictionary options)
        {
            var document = new Document();

            try
            {
                DocumentCreateOptions documentCreateOptions = null;
                if (options["Data"] is byte[])
                {
                    documentCreateOptions = new DocumentCreateOptions(name, (byte[])options["Data"]);
                }
                else if (options["DataUrl"] != null && !string.IsNullOrEmpty(options["DataUrl"].ToString()))
                {
                    var data = options["DataUrl"].ToString();
                    const string base64Prefix = "base64,";
                    var startIndex = data.IndexOf(base64Prefix, StringComparison.InvariantCultureIgnoreCase);
                    if (startIndex != -1)
                    {
                        data = data.Substring(startIndex + base64Prefix.Length);
                    }
                    documentCreateOptions = new DocumentCreateOptions(name, Convert.FromBase64String(data));
                }
                else if (options["Url"] != null)
                {
                    var cfsOptions = CFSOptions.Get(options["Url"].ToString());
                    if (!cfsOptions.HasErrors())
                    {
                        documentCreateOptions = new DocumentCreateOptions(name, cfsOptions.FileStore, cfsOptions.FilePath, cfsOptions.FileName);
                    }
                    else
                    {
                        foreach (var error in cfsOptions.Errors)
                        {
                            document.Errors.Add(error);
                        }
                    }
                }
                else
                {
                    document.Errors.Add(new Error(typeof(ArgumentNullException).ToString(), "The Data or Url parameters have not been specified for a new document uploading."));
                }

                if (!document.Errors.Any() && documentCreateOptions != null)
                {
                    if (options["FolderPath"] != null)
                    {
                        documentCreateOptions.FolderPath = options["FolderPath"].ToString();
                    }

                    bool overwrite;
                    if (options["Overwrite"] != null && bool.TryParse(options["Overwrite"].ToString(), out overwrite))
                    {
                        documentCreateOptions.Overwrite = overwrite;
                    }

                    document = PublicApi.Documents.Create(applicationId, documentCreateOptions);
                }
            }
            catch (InvalidOperationException ex)
            {
                document.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidCreateOptions, applicationId, name)));
            }
            catch (SPFileAlreadyExistsException ex)
            {
                document.Warnings.Add(new Warning(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.AlreadyExists, applicationId, name)));
            }
            catch (SPInternalException ex)
            {
                document.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.CannotBeCreated, applicationId, name)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.Create() method while uploading a new document LibraryId:{1} Name:{2}. The exception message is: {3}", ex.GetType(), applicationId, name, ex.Message);
                SPLog.UnKnownError(ex, message);
                document.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return document;
        }

        public Document Update(Guid contentId,
            [Documentation(Name = "Fields", Description = "A collection of field names and values.", Type = typeof(IDictionary))]
            IDictionary options)
        {
            var document = new Document();

            try
            {
                if (options["Fields"] is IDictionary)
                {
                    document = PublicApi.Documents.Update(EnsureLibraryId(contentId), new DocumentUpdateOptions(contentId)
                        {
                            Fields = (IDictionary)options["Fields"]
                        });
                }
            }
            catch (InvalidOperationException ex)
            {
                document.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidUpdateOptions, contentId)));
            }
            catch (SPInternalException ex)
            {
                document.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.CannotBeUpdated, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.Update() method ContetnId:{1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                document.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return document;
        }

        public AdditionalInfo Delete(Guid contentId)
        {
            var deleteInfo = new AdditionalInfo();

            try
            {
                PublicApi.Documents.Delete(EnsureLibraryId(contentId), new DocumentGetOptions(contentId));
            }
            catch (InvalidOperationException ex)
            {
                deleteInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                deleteInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.NotFoundBecauseDeleted, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.Delete() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                deleteInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return deleteInfo;
        }

        public AdditionalInfo CheckIn(Guid contentId)
        {
            var checkInInfo = new AdditionalInfo();

            try
            {
                PublicApi.Documents.CheckIn(EnsureLibraryId(contentId), new DocumentCheckInOptions(contentId));
            }
            catch (InvalidOperationException ex)
            {
                checkInInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                checkInInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.CannotBeCheckedIn, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.CheckIn() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                checkInInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return checkInInfo;
        }

        public AdditionalInfo CheckIn(Guid contentId,
            [Documentation(Name = "KeepCheckedOut", Type = typeof(bool)),
            Documentation(Name = "CheckInType", Type = typeof(string)),
            Documentation(Name = "Comment", Type = typeof(string))]
            IDictionary options)
        {
            var checkInInfo = new AdditionalInfo();

            try
            {
                var checkInOptions = new DocumentCheckInOptions(contentId);
                bool keepCOut;
                if (options["KeepCheckedOut"] != null && bool.TryParse(options["KeepCheckedOut"].ToString(), out keepCOut))
                {
                    checkInOptions.KeepCheckOut = keepCOut;
                }
                if (options["Comment"] != null)
                {
                    checkInOptions.Comment = options["Comment"].ToString();
                }
                if (options["CheckinType"] != null)
                {
                    checkInOptions.CheckinType = options["CheckinType"].ToString();
                }
                PublicApi.Documents.CheckIn(EnsureLibraryId(contentId), checkInOptions);
            }
            catch (InvalidOperationException ex)
            {
                checkInInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                checkInInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.CannotBeCheckedIn, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.CheckIn() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                checkInInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return checkInInfo;
        }

        public AdditionalInfo CheckOut(Guid contentId)
        {
            var checkOutInfo = new AdditionalInfo();

            try
            {
                PublicApi.Documents.CheckOut(EnsureLibraryId(contentId), new DocumentGetOptions(contentId));
            }
            catch (InvalidOperationException ex)
            {
                checkOutInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                checkOutInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.CannotBeCheckedOut, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.CheckOut() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                checkOutInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return checkOutInfo;
        }

        public AdditionalInfo UndoCheckOut(Guid contentId)
        {
            var undoCheckOutInfo = new AdditionalInfo();

            try
            {
                PublicApi.Documents.UndoCheckOut(EnsureLibraryId(contentId), new DocumentGetOptions(contentId));
            }
            catch (InvalidOperationException ex)
            {
                undoCheckOutInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                undoCheckOutInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.CannotBeCheckedIn, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.CheckOut() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                undoCheckOutInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return undoCheckOutInfo;
        }

        public bool IsCheckedOut(Guid contentId)
        {
            try
            {
                return Get(contentId).IsCheckedOut;
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.IsCheckedOut() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return false;
        }

        public SPDocumentInfo GetInfo(Guid contentId)
        {
            var documentInfo = new SPDocumentInfo();

            try
            {
                documentInfo = PublicApi.Documents.GetDetails(EnsureLibraryId(contentId), new DocumentGetOptions(contentId));
            }
            catch (InvalidOperationException ex)
            {
                documentInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                documentInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.NotFound, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.GetInfo() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                documentInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return documentInfo;
        }

        public PagedList<SPDocumentVersion> GetVersions(Guid contentId)
        {
            var documentVersions = new PagedList<SPDocumentVersion>();

            try
            {
                documentVersions = new PagedList<SPDocumentVersion>(PublicApi.Documents.GetVersions(EnsureLibraryId(contentId), new DocumentGetOptions(contentId)));
            }
            catch (InvalidOperationException ex)
            {
                documentVersions.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                documentVersions.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.NotFound, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.GetVersions() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                documentVersions.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return documentVersions;
        }

        public PagedList<SPDocumentVersion> GetVersions(Guid contentId,
            [Documentation(Name = "PageSize", Type = typeof(int)),
            Documentation(Name = "PageIndex", Type = typeof(int))]
            IDictionary options)
        {
            var documentVersions = new PagedList<SPDocumentVersion>();

            try
            {
                int pageSize = DefaultPageSize;
                if (options["PageSize"] != null)
                {
                    pageSize = Convert.ToInt32(options["PageSize"]);
                }

                int pageIndex = 0;
                if (options["PageIndex"] != null)
                {
                    pageIndex = Convert.ToInt32(options["PageIndex"]);
                }

                var allVersions = PublicApi.Documents.GetVersions(EnsureLibraryId(contentId), new DocumentGetOptions(contentId));
                documentVersions = new PagedList<SPDocumentVersion>(allVersions.Skip(pageSize * pageIndex).Take(pageSize))
                    {
                        PageSize = pageSize,
                        PageIndex = pageIndex,
                        TotalCount = allVersions.Count
                    };
            }
            catch (InvalidOperationException ex)
            {
                documentVersions.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                documentVersions.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.NotFound, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.GetVersions() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                documentVersions.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }

            return documentVersions;
        }

        public AdditionalInfo Restore(Guid contentId, string version)
        {
            var restoreInfo = new AdditionalInfo();
            try
            {
                PublicApi.Documents.Restore(EnsureLibraryId(contentId), new DocumentRestoreOptions(contentId)
                    {
                        Version = version
                    });
            }
            catch (InvalidOperationException ex)
            {
                restoreInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.InvalidId, contentId)));
            }
            catch (SPInternalException ex)
            {
                restoreInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.NotFound, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointFile.Restore() method ContentId: {1} Version: {2}. The exception message is: {3}", ex.GetType(), contentId, version, ex.Message);
                SPLog.UnKnownError(ex, message);
                restoreInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.UnknownError)));
            }
            return restoreInfo;
        }

        public AdditionalInfo SendEmail(Guid contentId,
            [Documentation(Name = "Subject", Type = typeof(string), Description = "Subject of the email message."),
            Documentation(Name = "Body", Type = typeof(string), Description = "Body of the email message."),
            Documentation(Name = "UserIds", Type = typeof(string), Description = "One or more comma separated user Ids."),
            Documentation(Name = "UserEmails", Type = typeof(string), Description = "One or more comma separated emails.")]
            IDictionary options)
        {
            var result = new AdditionalInfo();
            var subject = string.Empty;
            if (options["Subject"] == null || string.IsNullOrWhiteSpace(options["Subject"].ToString()))
            {
                result.Errors.Add(new Error(typeof(FormatException).ToString(), "Email subject cannot be empty."));
            }
            else
            {
                subject = options["Subject"].ToString();
            }

            var body = string.Empty;
            if (options["Body"] == null || string.IsNullOrWhiteSpace(options["Body"].ToString()))
            {
                result.Errors.Add(new Error(typeof(FormatException).ToString(), "Email body cannot be empty."));
            }
            else
            {
                body = options["Body"].ToString();
            }

            if (result.HasErrors()) return result;

            int fromUserId = Extensibility.Api.Version1.PublicApi.Users.AccessingUser.Id.Value;
            if (options["UserIds"] != null && !string.IsNullOrWhiteSpace(options["UserIds"].ToString()))
            {
                try
                {
                    var userIds = options["UserIds"].ToString().Split(',').Select(int.Parse);
                    foreach (var userId in userIds)
                    {
                        var sended = Extensibility.Api.Version1.PublicApi.SendEmail.Send(
                            new Extensibility.Api.Version1.SendEmailOptions
                                {
                                    Subject = subject,
                                    Body = body,
                                    FromUserId = fromUserId,
                                    ToUserId = userId,
                                });
                        if (!sended)
                        {
                            result.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFileExtension.Translations.EmailSendError)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.EmailSendError)));
                }
            }

            if (options["UserEmails"] != null && !string.IsNullOrWhiteSpace(options["UserEmails"].ToString()))
            {
                try
                {
                    var userEmails = options["UserEmails"].ToString().Split(',');
                    foreach (var userEmail in userEmails)
                    {
                        var sended = Extensibility.Api.Version1.PublicApi.SendEmail.Send(
                            new Extensibility.Api.Version1.SendEmailOptions
                                {
                                    Subject = subject,
                                    Body = body,
                                    FromUserId = fromUserId,
                                    ToEmail = userEmail,
                                });
                        if (!sended) result.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFileExtension.Translations.EmailSendError)));
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFileExtension.Translations.EmailSendError)));
                }
            }
            return result;
        }

        #endregion

        private Guid EnsureLibraryId(Guid contentId)
        {
            var libraryId = SPCoreService.Context.LibraryId;
            if (libraryId == Guid.Empty)
            {
                var itemBase = listItemDataService.Get(contentId);
                if (itemBase != null)
                {
                    libraryId = itemBase.ApplicationId;
                }
            }
            return libraryId;
        }

        private static DocumentListOptions ProcessDocumentListOptions(IDictionary options)
        {
            int pageSize = DefaultPageSize;
            if (options["PageSize"] != null)
            {
                pageSize = Convert.ToInt32(options["PageSize"]);
            }

            int pageIndex = 0;
            if (options["PageIndex"] != null)
            {
                pageIndex = Convert.ToInt32(options["PageIndex"]);
            }

            string sortBy = options["SortBy"] != null ? options["SortBy"].ToString() : String.Empty;
            var sortOrder = SortOrder.Ascending;
            if (options["SortOrder"] != null)
            {
                Enum.TryParse(options["SortOrder"].ToString(), true, out sortOrder);
            }

            string folderPath = String.Empty;
            if (options["FolderPath"] != null)
            {
                folderPath = options["FolderPath"].ToString();
            }

            return new DocumentListOptions
            {
                PageSize = pageSize,
                PageIndex = pageIndex,
                SortBy = sortBy,
                SortOrder = sortOrder,
                FolderPath = folderPath
            };
        }

        internal class CFSOptions : ApiEntity
        {
            private CFSOptions(string cfsFileUrl)
            {
                ProcessCFSFileUrl(cfsFileUrl);
            }

            public string FileStore { get; private set; }
            public string FilePath { get; private set; }
            public string FileName { get; private set; }

            public static CFSOptions Get(string cfsFileUrl)
            {
                return new CFSOptions(cfsFileUrl);
            }

            private void ProcessCFSFileUrl(string fileUrl)
            {
                if (string.IsNullOrEmpty(fileUrl))
                {
                    throw new Exception("CFS File Url is null or empty.");
                }

                int cfsFileStoreStartIndex = fileUrl.IndexOf("key", StringComparison.InvariantCulture) + "key".Length;
                if (cfsFileStoreStartIndex == -1)
                {
                    throw new Exception("Invalid CFS file store key.");
                }

                fileUrl = fileUrl.Substring(cfsFileStoreStartIndex).Trim('/');
                if (string.IsNullOrEmpty(fileUrl))
                {
                    throw new Exception("Invalid CFS file store key.");
                }

                int cfsFilePathStartIndex = fileUrl.IndexOf('/');
                if (cfsFilePathStartIndex == -1)
                {
                    throw new Exception("Invalid CFS file path.");
                }

                int cfsFileNameStartIndex = fileUrl.LastIndexOf('/');
                if (cfsFileNameStartIndex == -1)
                {
                    throw new Exception("Invalid CFS file name.");
                }

                FileStore = fileUrl.Substring(0, cfsFilePathStartIndex).Trim('/');
                FilePath = fileUrl.Substring(cfsFilePathStartIndex, cfsFileNameStartIndex - cfsFilePathStartIndex).Trim('/');
                FileName = fileUrl.Substring(cfsFileNameStartIndex).Trim('/');
            }
        }
    }
}