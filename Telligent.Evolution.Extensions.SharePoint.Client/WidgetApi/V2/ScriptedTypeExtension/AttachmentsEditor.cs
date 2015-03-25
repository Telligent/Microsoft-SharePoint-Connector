using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class AttachmentsExtension : IScriptedContentFragmentExtension, ITranslatablePlugin
    {
        internal static class Translations
        {
            public const string ListItemNotFound = "listItem_notfound";
            public const string FileNameCannotBeEmpty = "filename_empty";
            public const string FileContentCannotBeEmpty = "filecontent_empty";
            public const string UnknownError = "unknown_error";
        }

        private ITranslatablePluginController translationController;

        public string ExtensionName
        {
            get { return "sharepoint_v2_attachments"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IAttachmentsEditor>(); }
        }

        public string Name
        {
            get { return "Attachments (sharepoint_v2_attachments)"; }
        }

        public string Description
        {
            get { return "Allows to work with Attachments Field Types."; }
        }

        public void Initialize() { }

        #region ITranslatablePlugin Members

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set(Translations.ListItemNotFound, "The list item cannot be found.");
                t.Set(Translations.FileNameCannotBeEmpty, "File name cannot be empty or white spaces.");
                t.Set(Translations.FileContentCannotBeEmpty, "File content cannot be empty.");
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

    public interface IAttachmentsEditor
    {
        ApiList<SPAttachment> List(Guid contentId, string fieldName = null);
        AdditionalInfo Add(Guid contentId, IDictionary options);
        AdditionalInfo Remove(Guid contentId, IDictionary options);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class AttachmentsEditor : IAttachmentsEditor
    {
        private static readonly AttachmentsExtension Plugin = PluginManager.Get<AttachmentsExtension>().FirstOrDefault();
        private static readonly IListItemDataService ListItemDataService = ServiceLocator.Get<IListItemDataService>();

        public ApiList<SPAttachment> List(Guid contentId, string fieldName = null)
        {
            var attachments = new ApiList<SPAttachment>();
            try
            {
                attachments = new ApiList<SPAttachment>(PublicApi.Attachments.List(EnsureListId(contentId), new AttachmentsGetOptions(contentId, fieldName ?? "Attachments")));
            }
            catch (InvalidOperationException ex)
            {
                attachments.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(AttachmentsExtension.Translations.ListItemNotFound, contentId)));
            }
            catch (SPInternalException ex)
            {
                attachments.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(AttachmentsExtension.Translations.ListItemNotFound, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.AttachmentsEditor.List() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                attachments.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(AttachmentsExtension.Translations.UnknownError, contentId)));
            }
            return attachments;
        }

        public AdditionalInfo Add(Guid contentId,
            [Documentation(Name = "FieldName", Type = typeof(string), Description = "\"Attachments\" by default"),
            Documentation(Name = "File", Type = typeof(string), Description = "File Name"),
            Documentation(Name = "Data", Type = typeof(byte[]), Description = "Byte array file content")]
            IDictionary options)
        {
            var result = new AdditionalInfo();

            string fieldName = "Attachments";
            if (options["FieldName"] != null)
            {
                fieldName = options["FieldName"].ToString();
            }

            string fileName = null;
            if (options["File"] != null)
            {
                fileName = options["File"].ToString();
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.Errors.Add(new Error(typeof(ArgumentException).ToString(), Plugin.Translate(AttachmentsExtension.Translations.FileNameCannotBeEmpty)));
            }

            var data = options["Data"] as byte[];
            if (data == null)
            {
                result.Errors.Add(new Error(typeof(ArgumentException).ToString(), Plugin.Translate(AttachmentsExtension.Translations.FileContentCannotBeEmpty)));
            }

            if (!result.HasErrors())
            {
                try
                {
                    PublicApi.Attachments.Add(EnsureListId(contentId), new AttachmentsAddOptions(contentId, fieldName)
                    {
                        Files = new Dictionary<string, byte[]> { { fileName, data } }
                    });
                }
                catch (InvalidOperationException ex)
                {
                    result.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(AttachmentsExtension.Translations.ListItemNotFound, contentId)));
                }
                catch (SPInternalException ex)
                {
                    result.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(AttachmentsExtension.Translations.ListItemNotFound, contentId)));
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.AttachmentsEditor.Add() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                    SPLog.UnKnownError(ex, message);
                    result.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(AttachmentsExtension.Translations.UnknownError, contentId)));
                }
            }

            return result;
        }

        public AdditionalInfo Remove(Guid contentId,
            [Documentation(Name = "FieldName", Type = typeof(string), Description = "\"Attachments\" by default"),
            Documentation(Name = "FileNames", Type = typeof(string), Description = "File names to be removed splitted by ';'.")]
            IDictionary options)
        {
            var result = new AdditionalInfo();

            string fieldName = "Attachments";
            if (options["FieldName"] != null)
            {
                fieldName = options["FieldName"].ToString();
            }

            string fileNames = null;
            if (options["FileNames"] != null)
            {
                fileNames = options["FileNames"].ToString();
            }
            try
            {
                var files = fileNames != null ? fileNames.Split(';') : new string[0];
                if (files.Length > 0)
                {
                    PublicApi.Attachments.Remove(EnsureListId(contentId), new AttachmentsRemoveOptions(contentId, fieldName)
                    {
                        FileNames = files.ToList()
                    });
                }
            }
            catch (InvalidOperationException ex)
            {
                result.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(AttachmentsExtension.Translations.ListItemNotFound, contentId)));
            }
            catch (SPInternalException ex)
            {
                result.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(AttachmentsExtension.Translations.ListItemNotFound, contentId)));
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the WidgetApi.V2.AttachmentsEditor.Remove() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
                result.Errors.Add(new Error(ex.GetType().ToString(), Plugin.Translate(AttachmentsExtension.Translations.UnknownError, contentId)));
            }

            return result;
        }

        private Guid EnsureListId(Guid contentId)
        {
            var listId = SPCoreService.Context.ListId;
            if (listId != Guid.Empty) return listId;

            var itemBase = ListItemDataService.Get(contentId);
            if (itemBase != null)
            {
                listId = itemBase.ApplicationId;
            }
            return listId;
        }
    }
}
