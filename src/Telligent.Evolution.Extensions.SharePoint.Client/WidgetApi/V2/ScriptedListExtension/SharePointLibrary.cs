using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Utility;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using PublicApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class SharePointLibraryExtension : IScriptedContentFragmentExtension, ITranslatablePlugin
    {
        internal static class Translations
        {
            public const string InvalidId = "library_invalid_id";
            public const string UrlCannotBeEmpty = "library_url_cannot_be_empty";
            public const string NotFound = "library_notfound";
            public const string NotFoundBecauseDeleted = "library_notfound_because_deleted";
            public const string InvalidGroupId = "library_invalid_groupid";
            public const string CannotBeAdded = "library_cannot_be_added";
            public const string CannotBeCreated = "library_cannot_be_created";
            public const string CannotBeUpdated = "library_cannot_be_updated";
            public const string InvalidCreateOptions = "library_invalid_createoptions";
            public const string UnknownError = "library_unknown_error";
            public const string NameCannotBeEmpty = "library_name_cannot_be_empty";
            public const string AlreadyInUse = "library_already_in_use";
        }

        private ITranslatablePluginController translationController;

        #region IScriptedContentFragmentExtension Members

        public string ExtensionName
        {
            get { return "sharepoint_v2_library"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointLibrary>(); }
        }

        public string Name
        {
            get { return "SharePoint Document Library Extension (sharepoint_v2_library)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to use the Document Library extension for a SharePoint files managing."; }
        }

        public void Initialize() { }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set("property_group_viewnotfound", "View not found. The following Views are available:");
                t.Set("property_group_spsite", "SharePoint Site");
                t.Set("property_group_spdoclib", "SharePoint Document Library");
                t.Set("property_group_splist", "SharePoint List");

                // Exceptions
                t.Set(Translations.InvalidId, "The Document Library Id is invalid.");
                t.Set(Translations.NotFound, "The Document Library cannot be found.");
                t.Set(Translations.InvalidGroupId, "The Group Id is invalid.");
                t.Set(Translations.CannotBeAdded, "The Document Library cannot be added to the group.");
                t.Set(Translations.CannotBeCreated, "The Document Library cannot be created.");
                t.Set(Translations.CannotBeUpdated, "The Document Library cannot be updated.");
                t.Set(Translations.InvalidCreateOptions, "The Document Library cannot be created, because specified create options are invalid.");
                t.Set(Translations.UrlCannotBeEmpty, "The Document Library SharePoint Web Url cannot be empty.");
                t.Set(Translations.NameCannotBeEmpty, "The Document Library Title cannot be empty.");
                t.Set(Translations.NotFoundBecauseDeleted, "The Document Library has been already deleted.");
                t.Set(Translations.UnknownError, "An error has been occurred, please try again or contact your administrator.");
                t.Set(Translations.AlreadyInUse, "The Library you are trying to import is already being used. Please select another Library.");

                return new[] { t };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        public static SharePointLibraryExtension Plugin
        {
            get
            {
                return PluginManager.Get<SharePointLibraryExtension>().FirstOrDefault();
            }
        }

        internal string Translate(string key, params object[] args)
        {
            return String.Format(translationController.GetLanguageResourceValue(key), args);
        }

        internal string ViewNotFoundMsg()
        {
            return translationController.GetLanguageResourceValue("property_group_viewnotfound");
        }

        internal string SPSiteMsg()
        {
            return translationController.GetLanguageResourceValue("property_group_spsite");
        }

        internal string SPDocLibMsg()
        {
            return translationController.GetLanguageResourceValue("property_group_spdoclib");
        }

        internal string SPListMsg()
        {
            return translationController.GetLanguageResourceValue("property_group_splist");
        }
    }

    public interface ISharePointLibrary
    {
        Library Current { get; }

        string Directory { get; set; }

        string View { get; set; }

        Library Add(int groupId, Guid libraryId, string url);

        AdditionalInfo Update(Guid libraryId, IDictionary options);

        Library Create(int groupId, string url, string name, IDictionary options = null);

        Library Get(Guid libraryId);

        PagedList<Library> List(int groupId, IDictionary options);

        AdditionalInfo Delete(Guid libraryId, bool deleteLibrary);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointLibrary : ISharePointLibrary
    {
        private static readonly SharePointLibraryExtension plugin = PluginManager.Get<SharePointLibraryExtension>().FirstOrDefault();

        private const int DefaultPageSize = 10;

        private const string DirectoryCookieId = "D";
        private const string ViewCookieId = "V";

        #region ISharePointDocumentLibrary Members

        public Library Current
        {
            get
            {
                try
                {
                    var libraryId = SPCoreService.Context.LibraryId;
                    if (libraryId != Guid.Empty)
                    {
                        return PublicApi.Libraries.Get(new LibraryGetOptions(libraryId));
                    }
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.Current property. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
                return null;
            }
        }

        public string Directory
        {
            get
            {
                try
                {
                    var directory = SPCookie.GetCookie(GetCookieId(), DirectoryCookieId);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        directory = HttpUtility.UrlDecode(directory);
                    }
                    return directory;
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.Directory get property. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
                return null;
            }
            set
            {
                try
                {
                    SPCookie.SetCookie(GetCookieId(), DirectoryCookieId, HttpUtility.UrlEncode(value));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.Directory set property. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
            }
        }

        public string View
        {
            get
            {
                try
                {
                    return SPCookie.GetCookie(GetCookieId(), ViewCookieId);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.View get property. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
                return null;
            }
            set
            {
                try
                {
                    SPCookie.SetCookie(GetCookieId(), ViewCookieId, value);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.View set property. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
            }
        }

        public Library Add(int groupId, Guid libraryId, string spwebUrl)
        {
            // Check that library was not imported before
            var library = PublicApi.Libraries.Get(new LibraryGetOptions(libraryId));
            if (library != null)
            {
                // Verify the group exists
                var group = Extensibility.Api.Version1.PublicApi.Groups.Get(new GroupsGetOptions { Id = library.GroupId });
                if (group != null)
                {
                    // The Library is already in use
                    library.Errors.Add(new Error(typeof(ArgumentException).ToString(), plugin.Translate(SharePointLibraryExtension.Translations.AlreadyInUse)));
                    return library;
                }
            }

            // Import a library
            library = new Library();
            try
            {
                PublicApi.Libraries.Add(groupId, libraryId, spwebUrl);
                library = PublicApi.Libraries.Get(new LibraryGetOptions(libraryId));
            }
            catch (InvalidOperationException ex)
            {
                var exType = ex.GetType().ToString();
                ValidateGroupId(groupId, library.Errors, exType);
                ValidateLibraryId(libraryId, library.Errors, exType);
                ValidateUrl(spwebUrl, library.Errors, exType);
            }
            catch (ArgumentException ex)
            {
                library.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.NotFound, libraryId)));
            }
            catch (SPInternalException ex)
            {
                var group = TEApi.Groups.Get(new GroupsGetOptions { Id = groupId });
                var groupName = group != null ? group.Name : groupId.ToString(CultureInfo.InvariantCulture);
                library.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.CannotBeAdded, groupName, libraryId, spwebUrl)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.Add() method for LibraryId: {1} GroupId: {2} SPWebUrl: {3}. The exception message is: {4}", ex.GetType(), libraryId, groupId, spwebUrl, ex.Message);
                SPLog.UnKnownError(ex, message);
                library.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.UnknownError)));
            }

            return library;
        }

        public AdditionalInfo Update(Guid libraryId,
            [Documentation(Name = "Title", Type = typeof(string)),
            Documentation(Name = "Description", Type = typeof(string)),
            Documentation(Name = "DefaultViewId", Type = typeof(Guid))]
            IDictionary options)
        {
            var result = new AdditionalInfo();
            if (options == null) return result;

            var updateOptions = new LibraryUpdateOptions(libraryId);
            if (options["Title"] != null)
            {
                updateOptions.Title = options["Title"].ToString();
            }
            if (options["Description"] != null)
            {
                updateOptions.Description = options["Description"].ToString();
            }
            if (options["DefaultViewId"] is Guid)
            {
                updateOptions.DefaultViewId = (Guid)options["DefaultViewId"];
            }
            if (updateOptions.Title != null
                || updateOptions.Description != null
                || updateOptions.DefaultViewId != Guid.Empty)
            {
                try
                {
                    PublicApi.Libraries.Update(updateOptions);
                }
                catch (SPInternalException ex)
                {
                    result.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.CannotBeUpdated)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.Update() method while updating library with Id: {1}. The exception message is: {2}", ex.GetType(), libraryId, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    result.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.UnknownError)));
                }
            }
            return result;
        }

        public Library Create(int groupId, string url, string name,
            [Documentation(Name = "Description", Type = typeof(string))]
            IDictionary options = null)
        {
            var library = new Library();

            var createOptions = new LibraryCreateOptions(groupId)
            {
                SPWebUrl = url,
                Title = name
            };
            if (options != null && options["Description"] != null)
            {
                createOptions.Description = options["Description"].ToString();
            }

            try
            {
                library = PublicApi.Libraries.Create(createOptions);
            }
            catch (InvalidOperationException ex)
            {
                var exType = ex.GetType().ToString();
                ValidateGroupId(groupId, library.Errors, exType);
                ValidateUrl(url, library.Errors, exType);
                if (string.IsNullOrEmpty(name))
                {
                    library.Errors.Add(new Error(exType, plugin.Translate(SharePointLibraryExtension.Translations.NameCannotBeEmpty)));
                }
            }
            catch (SPInternalException ex)
            {
                library.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.CannotBeCreated, groupId, url, name)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.Create() method while creating library with Title:{1} in SPWebUrl:{2} for GroupId:{3}. The exception message is: {4}", ex.GetType(), name, url, groupId, ex.Message);
                SPLog.UnKnownError(ex, message);
                library.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.UnknownError)));
            }
            return library;
        }

        public Library Get(Guid libraryId)
        {
            var library = new Library();

            try
            {
                library = PublicApi.Libraries.Get(new LibraryGetOptions(libraryId));
            }
            catch (InvalidOperationException ex)
            {
                library.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.InvalidId, libraryId)));
            }
            catch (SPInternalException ex)
            {
                library.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.NotFound, libraryId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.Get() method for LibraryId: {1}. The exception message is: {2}", ex.GetType(), libraryId, ex.Message);
                SPLog.UnKnownError(ex, message);
                library.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.UnknownError)));
            }

            return library;
        }

        public PagedList<Library> List(int groupId,
            [Documentation(Name = "PageIndex", Type = typeof(int)),
            Documentation(Name = "PageSize", Type = typeof(int)),
            Documentation(Name = "SortBy", Type = typeof(string)),
            Documentation(Name = "SortOrder", Type = typeof(string)),
            Documentation(Name = "SearchText", Type = typeof(string))]
            IDictionary options)
        {
            var listOptions = new LibraryListOptions
            {
                PageSize = DefaultPageSize,
                SortOrder = "Ascending",
                SortBy = Library.SortBy.Name.ToString()
            };

            if (options != null)
            {
                int pageSize;
                if (options["PageSize"] != null && int.TryParse(options["PageSize"].ToString(), out pageSize))
                {
                    listOptions.PageSize = pageSize;
                }

                int pageIndex;
                if (options["PageIndex"] != null && int.TryParse(options["PageIndex"].ToString(), out pageIndex))
                {
                    listOptions.PageIndex = pageIndex;
                }

                if (options["SearchText"] != null && !String.IsNullOrEmpty(options["SearchText"].ToString()))
                {
                    listOptions.Filter = options["SearchText"].ToString();
                }

                if (options["SortBy"] != null)
                {
                    listOptions.SortBy = options["SortBy"].ToString();
                }

                if (options["SortOrder"] != null)
                {
                    listOptions.SortOrder = options["SortOrder"].ToString();
                }
            }

            var libraries = new PagedList<Library>();
            try
            {
                libraries = PublicApi.Libraries.List(groupId, listOptions);
            }
            catch (InvalidOperationException ex)
            {
                ValidateGroupId(groupId, libraries.Errors, ex.GetType().ToString());
            }
            catch (SPInternalException ex)
            {
                libraries.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.UnknownError)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.List() method for GroupId: {1}. The exception message is: {2}", ex.GetType(), groupId, ex.Message);
                SPLog.UnKnownError(ex, message);
                libraries.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.UnknownError)));
            }
            return libraries;
        }

        public AdditionalInfo Delete(Guid libraryId, bool deleteLibrary)
        {
            var errorInfo = new AdditionalInfo();

            try
            {
                var library = PublicApi.Libraries.Get(new LibraryGetOptions(libraryId));
                PublicApi.Libraries.Delete(library, deleteLibrary);
            }
            catch (InvalidOperationException ex)
            {
                errorInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.InvalidId, libraryId)));
            }
            catch (SPInternalException ex)
            {
                // The library has been already deleted
                errorInfo = new AdditionalInfo(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.NotFoundBecauseDeleted, libraryId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.Delete() method for LibraryId: {1}. The exception message is: {2}", ex.GetType(), libraryId, ex.Message);
                SPLog.UnKnownError(ex, message);
                errorInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointLibraryExtension.Translations.UnknownError)));
            }

            return errorInfo;
        }

        #endregion

        #region Validation-related methods

        private static void ValidateGroupId(int groupId, IList<Error> errors, string exceptionType)
        {
            var group = TEApi.Groups.Get(new GroupsGetOptions { Id = groupId });
            if (group == null)
            {
                errors.Add(new Error(exceptionType, plugin.Translate(SharePointLibraryExtension.Translations.InvalidGroupId, groupId)));
            }
            else if (group.HasErrors())
            {
                foreach (var error in group.Errors)
                {
                    errors.Add(error);
                }
            }
        }

        private static void ValidateLibraryId(Guid libraryId, IList<Error> errors, string exceptionType)
        {
            if (libraryId == Guid.Empty)
            {
                errors.Add(new Error(exceptionType, plugin.Translate(SharePointLibraryExtension.Translations.InvalidId, libraryId)));
            }
        }

        private static void ValidateUrl(string spwebUrl, IList<Error> errors, string exceptionType)
        {
            if (string.IsNullOrEmpty(spwebUrl))
            {
                errors.Add(new Error(exceptionType, plugin.Translate(SharePointLibraryExtension.Translations.UrlCannotBeEmpty)));
            }
        }

        #endregion

        private string GetCookieId()
        {
            var library = PublicApi.Libraries.Get(new LibraryGetOptions(SPCoreService.Context.LibraryId));
            if (library != null && !library.HasErrors())
                return string.Concat("L", library.Id.GetHashCode() ^ library.GroupId.GetHashCode());
            return string.Empty;
        }
    }
}
