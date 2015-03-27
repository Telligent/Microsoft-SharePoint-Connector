using System;
using System.Collections;
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

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointFolderExtension : IScriptedContentFragmentExtension, ITranslatablePlugin
    {
        internal static class Translations
        {
            public const string CannotBeCreated = "folder_cannot_be_created";
            public const string FolderNotFound = "folder_notfound";
            public const string InvalidGetOptions = "folder_invalid_getoptions";
            public const string InvalidCreateOptions = "folder_invalid_createoptions";
            public const string InvalidRenameOptions = "folder_invalid_renameoptions";
            public const string InvalidLibraryId = "folder_invalid_libraryid";
            public const string LibraryNotFound = "folder_library_notfound";
            public const string NotFoundBecauseDeleted = "folder_notfound_because_deleted";
            public const string UnknownError = "folder_unknown_error";
        }

        private ITranslatablePluginController translationController;

        #region IScriptedContentFragmentExtension Members

        public string ExtensionName
        {
            get { return "sharepoint_v1_folder"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointFolder>(); }
        }

        public string Name
        {
            get { return "SharePoint Folder Extension (sharepoint_v1_folder)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to work with SharePoint Folders."; }
        }

        public void Initialize() { }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set(Translations.CannotBeCreated, "The folder cannot be created.");
                t.Set(Translations.FolderNotFound, "The folder cannot be found.");
                t.Set(Translations.FolderNotFound, "The folder cannot be found.");
                t.Set(Translations.InvalidGetOptions, "The folder cannot be found, because specified options are invalid.");
                t.Set(Translations.InvalidRenameOptions, "The folder cannot be renamed, because specified options are invalid.");
                t.Set(Translations.InvalidLibraryId, "The document library Id is invalid.");
                t.Set(Translations.LibraryNotFound, "The document library cannot be found.");
                t.Set(Translations.NotFoundBecauseDeleted, "The folder has been already deleted.");
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

    public interface ISharePointFolder
    {
        Folder Create(string applicationId, string folderPath, string folderName);
        Folder Create(string url, string libraryId, string folderPath, string folderName);

        Folder Rename(string applicationId, string folderPath, string folderName);
        Folder Rename(string url, string libraryId, string folderPath, string folderName);

        Folder Get(string applicationId, string folderPath);
        Folder Get(string url, string libraryId, string folderPath);

        Folder GetParent(string applicationId, string folderPath);
        Folder GetParent(string url, string libraryId, string folderPath);

        PagedList<Folder> List(string applicationId, IDictionary options);
        PagedList<Folder> List(string url, string libraryId, IDictionary options);

        Folder Delete(string applicationId, string folderPath);
        Folder Delete(string url, string libraryId, string folderPath);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointFolder : ISharePointFolder
    {
        private static readonly SharePointFolderExtension plugin = PluginManager.Get<SharePointFolderExtension>().FirstOrDefault();

        #region ISharePointFolder

        public Folder Create(string applicationId, string folderPath, string folderName)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.Create(libraryId, new FolderCreateOptions(folderName)
                    {
                        Path = folderPath
                    });
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidCreateOptions)));
                }
                catch (SPInternalException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.CannotBeCreated)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.Create() method for ApplicationId: {1} FolderPath: '{2}' FolderName: '{3}'. The exception message is: {4}",
                        ex.GetType(), applicationId, folderPath, folderName, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        public Folder Create(string url, string applicationId, string folderPath, string folderName)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.Create(libraryId, new FolderCreateOptions(folderName)
                        {
                            Path = folderPath,
                            SPWebUrl = url
                        });
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidCreateOptions)));
                }
                catch (SPInternalException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.CannotBeCreated)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.Create() method for ApplicationId: {1} FolderPath: '{2}' FolderName: '{3}' SPWebUrl: '{4}'. The exception message is: {5}",
                        ex.GetType(), applicationId, folderPath, folderName, url, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        public Folder Rename(string applicationId, string folderPath, string folderName)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.Rename(libraryId, new FolderRenameOptions(folderName, folderPath));
                }
                catch (ArgumentException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidRenameOptions)));
                }
                catch (SPInternalException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.FolderNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.Rename() method for ApplicationId: {1} FolderPath: {2} FolderName: {3}. The exception message is: {4}",
                        ex.GetType(), applicationId, folderPath, folderName, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        public Folder Rename(string url, string applicationId, string folderPath, string folderName)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.Rename(libraryId, new FolderRenameOptions(folderName, folderPath)
                        {
                            SPWebUrl = url
                        });
                }
                catch (ArgumentException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidRenameOptions)));
                }
                catch (SPInternalException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.FolderNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.Rename() method for ApplicationId: {1} FolderPath: '{2}' FolderName: '{3}' SPWebUrl: '{4}'. The exception message is: {5}",
                        ex.GetType(), applicationId, folderPath, folderName, url, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        public Folder Get(string applicationId, string folderPath)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.Get(libraryId, new FolderGetOptions(folderPath));
                }
                catch (ArgumentException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidGetOptions)));
                }
                catch (SPInternalException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.FolderNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.Get() method for ApplicationId: {1} FolderPath: '{2}'. The exception message is: {3}",
                        ex.GetType(), applicationId, folderPath, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        public Folder Get(string url, string applicationId, string folderPath)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.Get(libraryId, new FolderGetOptions(folderPath)
                        {
                            SPWebUrl = url
                        });
                }
                catch (ArgumentException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidGetOptions)));
                }
                catch (SPInternalException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.FolderNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.Get() method for ApplicationId: {1} FolderPath: '{2}' SPWebUrl: '{3}'. The exception message is: {4}",
                        ex.GetType(), applicationId, folderPath, url, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        public Folder GetParent(string applicationId, string folderPath)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.GetParent(libraryId, new FolderGetOptions(folderPath));
                }
                catch (ArgumentException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidGetOptions)));
                }
                catch (SPInternalException)
                {
                    return null;
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.GetParent() method for ApplicationId: {1} FolderPath: '{2}'. The exception message is: {3}",
                        ex.GetType(), applicationId, folderPath, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        public Folder GetParent(string url, string applicationId, string folderPath)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.GetParent(libraryId, new FolderGetOptions(folderPath)
                    {
                        SPWebUrl = url
                    });
                }
                catch (ArgumentException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidGetOptions)));
                }
                catch (SPInternalException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.FolderNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.GetParent() method for ApplicationId: {1} FolderPath: '{2}' SPWebUrl: '{3}'. The exception message is: {4}",
                        ex.GetType(), applicationId, folderPath, url, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        public PagedList<Folder> List(string applicationId,
            [Documentation(Name = "FolderPath", Type = typeof(string), Description = "Folder server relative Url"),
            Documentation(Name = "PageIndex", Type = typeof(int)),
            Documentation(Name = "PageSize", Type = typeof(int))]
            IDictionary options)
        {
            var folders = new PagedList<Folder>();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folders = ClientApi.Folders.List(libraryId, ProcessFolderListOptions(options));
                }
                catch (ArgumentException ex)
                {
                    folders.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (InvalidOperationException ex)
                {
                    folders.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (SPInternalException ex)
                {
                    folders.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.FolderNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.List() method for ApplicationId: {1}. The exception message is: {2}",
                        ex.GetType(), applicationId, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folders.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folders.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folders;
        }

        public PagedList<Folder> List(string url, string applicationId,
            [Documentation(Name = "FolderPath", Type = typeof(string), Description = "Folder server relative Url"),
            Documentation(Name = "PageIndex", Type = typeof(int)),
            Documentation(Name = "PageSize", Type = typeof(int))]
            IDictionary options)
        {
            var folders = new PagedList<Folder>();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    var listOptions = ProcessFolderListOptions(options);
                    listOptions.SPWebUrl = url;
                    folders = ClientApi.Folders.List(libraryId, listOptions);
                }
                catch (ArgumentException ex)
                {
                    folders.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (InvalidOperationException ex)
                {
                    folders.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (SPInternalException ex)
                {
                    folders.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.FolderNotFound)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.List() method for ApplicationId: {1} SPWebUrl: '{2}'. The exception message is: {3}",
                        ex.GetType(), applicationId, url, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folders.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folders.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folders;
        }

        public Folder Delete(string applicationId, string folderPath)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.Delete(libraryId, new FolderGetOptions(folderPath));
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId)));
                }
                catch (SPInternalException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.NotFoundBecauseDeleted)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.Delete() method for ApplicationId: {1} FolderPath: '{2}'. The exception message is: {3}",
                        ex.GetType(), applicationId, folderPath, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        public Folder Delete(string url, string applicationId, string folderPath)
        {
            var folder = new Folder();

            Guid libraryId;
            if (Guid.TryParse(applicationId, out libraryId))
            {
                try
                {
                    folder = ClientApi.Folders.Delete(libraryId, new FolderGetOptions(folderPath)
                    {
                        SPWebUrl = url
                    });
                }
                catch (ArgumentException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.LibraryNotFound)));
                }
                catch (InvalidOperationException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidGetOptions)));
                }
                catch (SPInternalException ex)
                {
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.NotFoundBecauseDeleted)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V1.SharePointFolder.Delete() method for ApplicationId: {1} FolderPath: '{2}' SPWebUrl: '{3}'. The exception message is: {4}",
                        ex.GetType(), applicationId, folderPath, url, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    folder.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointFolderExtension.Translations.UnknownError)));
                }
            }
            else
            {
                folder.Errors.Add(new Error(typeof(InvalidOperationException).ToString(), plugin.Translate(SharePointFolderExtension.Translations.InvalidLibraryId, applicationId)));
            }
            return folder;
        }

        #endregion

        private static FolderListOptions ProcessFolderListOptions(IDictionary options)
        {
            int pageSize = 100;
            if (options["PageSize"] != null)
            {
                pageSize = Convert.ToInt32(options["PageSize"]);
            }

            int pageIndex = 0;
            if (options["PageIndex"] != null)
            {
                pageIndex = Convert.ToInt32(options["PageIndex"]);
            }

            string folderPath = String.Empty;
            if (options["FolderPath"] != null)
            {
                folderPath = options["FolderPath"].ToString();
            }

            return new FolderListOptions
            {
                PageSize = pageSize,
                PageIndex = pageIndex,
                Path = folderPath
            };
        }
    }
}
