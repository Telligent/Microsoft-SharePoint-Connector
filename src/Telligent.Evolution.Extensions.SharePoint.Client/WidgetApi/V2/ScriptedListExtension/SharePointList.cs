using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using PublicApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class SharePointListExtension : IScriptedContentFragmentExtension, ITranslatablePlugin
    {
        internal static class Translations
        {
            public const string CannotBeAdded = "list_cannot_be_added";
            public const string CannotBeCreated = "list_cannot_be_created";
            public const string CannotBeUpdated = "list_cannot_be_updated";
            public const string InvalidCreateOptions = "list_invalid_createoptions";
            public const string InvalidGroupId = "list_invalid_groupid";
            public const string InvalidId = "list_invalid_id";
            public const string NameCannotBeEmpty = "list_name_cannot_be_empty";
            public const string NotFound = "list_notfound";
            public const string NotFoundBecauseDeleted = "list_notfound_because_deleted";
            public const string UnknownError = "list_unknown_error";
            public const string UrlCannotBeEmpty = "list_url_cannot_be_empty";
            public const string AlreadyInUse = "list_already_in_use";
        }

        private ITranslatablePluginController translationController;

        #region IScriptedContentFragmentExtension Members

        public string ExtensionName
        {
            get { return "sharepoint_v2_list"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointList>(); }
        }

        public string Name
        {
            get { return "SharePoint List Extension (sharepoint_v2_list)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to work with SharePoint Lists."; }
        }

        public void Initialize() { }

        #endregion

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set(Translations.CannotBeAdded, "The List cannot be added to the group.");
                t.Set(Translations.CannotBeCreated, "The List cannot be created.");
                t.Set(Translations.CannotBeUpdated, "The List cannot be updated.");
                t.Set(Translations.InvalidCreateOptions, "The List cannot be created, because specified create options are invalid.");
                t.Set(Translations.InvalidGroupId, "The Group Id is invalid.");
                t.Set(Translations.InvalidId, "The List Id is invalid.");
                t.Set(Translations.NameCannotBeEmpty, "The List title cannot be empty.");
                t.Set(Translations.NotFound, "The List cannot be found.");
                t.Set(Translations.NotFoundBecauseDeleted, "The List has been already deleted.");
                t.Set(Translations.UnknownError, "An error has been occurred, please try again or contact your administrator.");
                t.Set(Translations.UrlCannotBeEmpty, "The SharePoint Web Url cannot be empty.");
                t.Set(Translations.AlreadyInUse, "The List you are trying to import is already being used. Please select another List.");

                return new[] { t };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        public static SharePointListExtension Plugin
        {
            get
            {
                return PluginManager.Get<SharePointListExtension>().FirstOrDefault();
            }
        }

        internal string Translate(string key, params object[] args)
        {
            return String.Format(translationController.GetLanguageResourceValue(key), args);
        }
    }

    public interface ISharePointList
    {
        SPList Current { get; }

        SPList Add(int groupId, Guid listId, string url);

        AdditionalInfo Update(Guid listId, IDictionary options);

        SPList Create(int groupId, string url, string name, IDictionary options = null);

        SPList Get(Guid listId);

        PagedList<SPList> List(int groupId, IDictionary options);

        AdditionalInfo Delete(Guid listId, bool deleteList);

        bool CanEdit(Guid listId);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointList : ISharePointList
    {
        private static readonly SharePointListExtension plugin = PluginManager.Get<SharePointListExtension>().FirstOrDefault();

        private const int DefaultPageSize = 10;

        #region ISharePointDocumentLibrary Members

        public SPList Current
        {
            get
            {
                try
                {
                    var listId = SPCoreService.Context.ListId;
                    if (listId != Guid.Empty)
                    {
                        return PublicApi.Lists.Get(listId);
                    }
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointList.Current property. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
                return null;
            }
        }

        public SPList Add(int groupId, Guid listId, string url)
        {
            // Check that list was not imported before
            var list = PublicApi.Lists.Get(new SPListGetOptions(listId));
            if (list != null)
            {
                // Verify the group exists
                var group = Extensibility.Api.Version1.PublicApi.Groups.Get(new GroupsGetOptions { Id = list.GroupId });
                if (group != null)
                {
                    // The List is already in use
                    list.Errors.Add(new Error(typeof(ArgumentException).ToString(), plugin.Translate(SharePointListExtension.Translations.AlreadyInUse)));
                    return list;
                }
            }

            // Import a list
            list = new SPList();
            try
            {
                PublicApi.Lists.Add(groupId, listId, url);
                list = PublicApi.Lists.Get(new SPListGetOptions(listId));
            }
            catch (InvalidOperationException ex)
            {
                var exType = ex.GetType().ToString();
                ValidateGroupId(groupId, list.Errors, exType);
                ValidateListId(listId, list.Errors, exType);
                ValidateUrl(url, list.Errors, exType);
            }
            catch (ArgumentException ex)
            {
                list.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.NotFound, listId)));
            }
            catch (SPInternalException ex)
            {
                var group = TEApi.Groups.Get(new GroupsGetOptions { Id = groupId });
                var groupName = group != null ? group.Name : groupId.ToString(CultureInfo.InvariantCulture);
                list.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.CannotBeAdded, groupName, listId, url)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointList.Add() method for ListId: {1} GroupId: {2} SPWebUrl: {3}. The exception message is: {4}", ex.GetType(), listId, groupId, url, ex.Message);
                SPLog.UnKnownError(ex, message);
                list.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.UnknownError)));
            }

            return list;
        }

        public AdditionalInfo Update(Guid listId,
            [Documentation(Name = "Title", Type = typeof(string)),
            Documentation(Name = "Description", Type = typeof(string)),
            Documentation(Name = "DefaultViewId", Type = typeof(Guid))]
            IDictionary options)
        {
            var result = new AdditionalInfo();
            if (options == null) return result;

            var updateOptions = new ListUpdateOptions(listId);
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
                    PublicApi.Lists.Update(updateOptions);
                }
                catch (SPInternalException ex)
                {
                    result.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.CannotBeUpdated)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointList.Update() method while updating list with Id: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    result.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.UnknownError)));
                }
            }
            return result;
        }

        public SPList Create(int groupId, string url, string name,
            [Documentation(Name = "Description", Type = typeof(string)),
            Documentation(Name = "Template", Type = typeof(string), Description = @"
AdminTasks, Agenda, Announcements, CallTrack, Categories, Circulation, Comments, Contacts, CustomGrid, DataConnectionLibrary,
DataSources, Decision, DiscussionBoard, Events, ExternalList, Facility, GanttTasks, GenericList, HealthReports, HealthRules,
Holidays, HomePageLibrary, IMEDic, IssueTracking, Links, ListTemplateCatalog, MasterPageCatalog, MeetingObjective, Meetings,
MeetingUser, NoCodePublic, NoCodeWorkflows, PictureLibrary, Posts, SolutionCatalog, Survey, Tasks, TextBox, ThemeCatalog,
ThingsToBring, Timecard, UserInformation, WebPageLibrary, WebPartCatalog, ListTemplateCatalog, WebTemplateCatalog, Whereabouts,
WorkflowHistory, WorkflowProcess, XMLForm")]
            IDictionary options = null)
        {
            var list = new SPList();

            var createOptions = new SPListCreateOptions(groupId)
            {
                SPWebUrl = url,
                Title = name
            };

            if (options != null)
            {
                if (options["Description"] != null)
                {
                    createOptions.Description = options["Description"].ToString();
                }

                if (options["Template"] != null)
                {
                    createOptions.Template = options["Template"].ToString();
                }
            }

            try
            {
                list = PublicApi.Lists.Create(createOptions);
            }
            catch (InvalidOperationException ex)
            {
                var exType = ex.GetType().ToString();
                ValidateGroupId(groupId, list.Errors, exType);
                ValidateUrl(url, list.Errors, exType);
                if (string.IsNullOrEmpty(name))
                {
                    list.Errors.Add(new Error(exType, plugin.Translate(SharePointListExtension.Translations.NameCannotBeEmpty)));
                }
            }
            catch (SPInternalException ex)
            {
                list.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.CannotBeCreated, groupId, url, name)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointList.Create() method while creating list with Title:{1} in SPWebUrl:{2} for GroupId:{3}. The exception message is: {4}", ex.GetType(), name, url, groupId, ex.Message);
                SPLog.UnKnownError(ex, message);
                list.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.UnknownError)));
            }
            return list;
        }

        public SPList Get(Guid listId)
        {
            var list = new SPList();

            try
            {
                list = PublicApi.Lists.Get(new SPListGetOptions(listId));
            }
            catch (InvalidOperationException ex)
            {
                list.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.InvalidId, listId)));
            }
            catch (SPInternalException ex)
            {
                list.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.NotFound, listId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointList.Get() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.UnKnownError(ex, message);
                list.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.UnknownError)));
            }

            return list;
        }

        public PagedList<SPList> List(int groupId,
            [Documentation(Name = "PageIndex", Type = typeof(int)),
            Documentation(Name = "PageSize", Type = typeof(int)),
            Documentation(Name = "SortBy", Type = typeof(string)),
            Documentation(Name = "SortOrder", Type = typeof(string)),
            Documentation(Name = "SearchText", Type = typeof(string))]
            IDictionary options)
        {

            var listOptions = new SPListCollectionOptions
            {
                PageSize = DefaultPageSize,
                SortOrder = "Ascending",
                SortBy = SPList.SortBy.Title.ToString()
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

            var lists = new PagedList<SPList>();
            try
            {
                lists = PublicApi.Lists.List(groupId, listOptions);
            }
            catch (InvalidOperationException ex)
            {
                ValidateGroupId(groupId, lists.Errors, ex.GetType().ToString());
            }
            catch (SPInternalException ex)
            {
                lists.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.UnknownError)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointLibrary.List() method for GroupId: {1}. The exception message is: {2}", ex.GetType(), groupId, ex.Message);
                SPLog.UnKnownError(ex, message);
                lists.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.UnknownError)));
            }
            return lists;
        }

        public AdditionalInfo Delete(Guid listId, bool deleteList)
        {
            var errorInfo = new AdditionalInfo();

            try
            {
                var list = PublicApi.Lists.Get(new SPListGetOptions(listId));
                PublicApi.Lists.Delete(list, deleteList);
            }
            catch (InvalidOperationException ex)
            {
                errorInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.InvalidId, listId)));
            }
            catch (SPInternalException ex)
            {
                // The list has been already deleted
                errorInfo = new AdditionalInfo(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.NotFoundBecauseDeleted, listId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointList.Delete() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.UnKnownError(ex, message);
                errorInfo.Errors.Add(new Error(ex.GetType().ToString(), plugin.Translate(SharePointListExtension.Translations.UnknownError)));
            }

            return errorInfo;
        }

        public bool CanEdit(Guid listId)
        {
            bool canEdit = false;
            try
            {
                canEdit = PublicApi.Lists.CanEdit(listId);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.SharePointList.CanEdit() method for ListId: {1}. The exception message is: {2}", ex.GetType(), listId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return canEdit;
        }

        #endregion

        #region Validation-related methods

        private static void ValidateGroupId(int groupId, IList<Error> errors, string exceptionType)
        {
            var group = TEApi.Groups.Get(new GroupsGetOptions { Id = groupId });
            if (group == null)
            {
                errors.Add(new Error(exceptionType, SharePointListExtension.Plugin.Translate(SharePointListExtension.Translations.InvalidGroupId, groupId)));
            }
            else if (group.HasErrors())
            {
                foreach (var error in group.Errors)
                {
                    errors.Add(error);
                }
            }
        }

        private static void ValidateListId(Guid listId, IList<Error> errors, string exceptionType)
        {
            if (listId == Guid.Empty)
            {
                errors.Add(new Error(exceptionType, SharePointListExtension.Plugin.Translate(SharePointListExtension.Translations.InvalidId, listId)));
            }
        }

        private static void ValidateUrl(string spwebUrl, IList<Error> errors, string exceptionType)
        {
            if (string.IsNullOrEmpty(spwebUrl))
            {
                errors.Add(new Error(exceptionType, SharePointListExtension.Plugin.Translate(SharePointListExtension.Translations.UrlCannotBeEmpty)));
            }
        }

        #endregion
    }
}
