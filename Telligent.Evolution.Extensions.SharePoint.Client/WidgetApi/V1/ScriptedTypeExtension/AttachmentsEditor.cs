using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;
using Telligent.Evolution.Extensions.SharePoint.WebServices;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class AttachmentsEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_attachments"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IAttachmentsEditor>(); }
        }

        public string Name
        {
            get { return "Attachments Editor (sharepoint_v1_attachments)"; }
        }

        public string Description
        {
            get { return "Attachments Editor functionality."; }
        }

        public void Initialize() { }
    }

    public interface IAttachmentsEditor
    {
        SP.FileCollection Attachments(SPListItem item, SPList list, object value);

        string AttachmentNames(SP.FileCollection attachments);

        string Render(SP.FileCollection attachments, string url);

        void Remove(SPList list, SPListItem listItem, string fileNames);

        void Add(SPList list, SPListItem listItem, string fileName, byte[] fileStream);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class AttachmentsEditor : IAttachmentsEditor
    {
        private readonly ICredentialsManager credentials;

        internal AttachmentsEditor()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        internal AttachmentsEditor(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        #region IAttachmentsEditor implementation
        public SP.FileCollection Attachments(SPListItem listItem, SPList list, object value)
        {
            if (value == null || !(bool)value)
                return null;

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                SP.Web web = clientContext.Web;
                clientContext.Load(web);
                SP.List splist = clientContext.ToList(list.Id);
                clientContext.Load(splist);
                SP.ListItem splistItem = splist.GetItemById(listItem.Id);
                clientContext.Load(splistItem);
                clientContext.ExecuteQuery();

                SP.Folder listFolder = splistItem.ParentList.RootFolder;
                clientContext.Load(listFolder, f => f.ServerRelativeUrl);
                clientContext.ExecuteQuery();

                SP.Folder attachmentsFolder = web.GetFolderByServerRelativeUrl(listFolder.ServerRelativeUrl + "/attachments/" + splistItem.Id.ToString());
                clientContext.Load(attachmentsFolder);
                var attachments = attachmentsFolder.Files;
                clientContext.Load(attachments);
                clientContext.ExecuteQuery();

                return attachments;
            }
        }

        public string Render(SP.FileCollection attachments, string url)
        {
            var baseUrl = (new Uri(url)).GetLeftPart(UriPartial.Authority);
            var attachmentsMarkup = new StringBuilder();

            foreach (var attachment in attachments)
            {
                attachmentsMarkup.AppendFormat("<a href='{0}' target='_blank'>{1}</a><br/>", baseUrl + attachment.ServerRelativeUrl, attachment.Name);
            }
            return attachmentsMarkup.ToString();
        }

        public string AttachmentNames(SP.FileCollection attachments)
        {
            if (attachments != null)
            {
                var names = new List<string>();

                foreach (SP.File file in attachments)
                {
                    names.Add(file.Name);
                }

                return string.Join("|", names.ToArray());
            }
            return string.Empty;
        }

        public void Remove(SPList list, SPListItem listItem, string fileNames)
        {
            if (String.IsNullOrEmpty(fileNames))
                return;

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                var site = clientContext.Site;
                clientContext.Load(site, s => s.Url);

                var web = clientContext.Web;
                clientContext.Load(web, w => w.Folders, w => w.ServerRelativeUrl);

                var splist = web.Lists.GetById(list.Id);
                var listFolder = splist.RootFolder;

                clientContext.Load(listFolder, f => f.ServerRelativeUrl, f => f.Folders);

                clientContext.ExecuteQuery();

                SP.Folder attachmentsFolder;
                var attachmentsFolderUrl = listFolder.ServerRelativeUrl + "/Attachments/" + listItem.Id;

                try
                {
                    attachmentsFolder = web.GetFolderByServerRelativeUrl(attachmentsFolderUrl);
                    clientContext.Load(attachmentsFolder);
                    clientContext.Load(attachmentsFolder.Files);
                    clientContext.ExecuteQuery();
                }
                catch
                {
                    attachmentsFolder = null;
                }

                var isUpdated = false;

                if (attachmentsFolder != null)
                {
                    var prevFileNames = fileNames.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    var filePool = new Stack<SP.File>();

                    foreach (var file in attachmentsFolder.Files)
                    {
                        if (prevFileNames.Contains(file.Name))
                        {
                            filePool.Push(file);
                            isUpdated = true;
                        }
                    }

                    if (isUpdated)
                    {
                        foreach (var file in filePool)
                        {
                            file.Recycle();
                        }
                        attachmentsFolder.Update();
                        clientContext.ExecuteQuery();
                    }
                }
            }
        }

        public void Add(SPList list, SPListItem listItem, string fileName, byte[] fileStream)
        {
            if (String.IsNullOrEmpty(fileName) && fileStream != null)
                return;

            var authentication = credentials.Get(list.SPWebUrl);

            using (var clientContext = new SPContext(list.SPWebUrl, authentication))
            {
                var site = clientContext.Site;
                clientContext.Load(site, s => s.Url);

                var web = clientContext.Web;
                clientContext.Load(web, w => w.Folders, w => w.ServerRelativeUrl);

                var splist = web.Lists.GetById(list.Id);
                var listFolder = splist.RootFolder;
                clientContext.Load(listFolder, f => f.ServerRelativeUrl, f => f.Folders);

                clientContext.ExecuteQuery();

                SP.Folder attachmentsFolder;
                var attachmentsFolderUrl = listFolder.ServerRelativeUrl + "/Attachments/" + listItem.Id;

                try
                {
                    attachmentsFolder = web.GetFolderByServerRelativeUrl(attachmentsFolderUrl);
                    clientContext.Load(attachmentsFolder);
                    clientContext.Load(attachmentsFolder.Files);
                    clientContext.ExecuteQuery();
                }
                catch
                {
                    attachmentsFolder = null;
                }

                // add
                var useService = (attachmentsFolder == null);

                if (useService)
                {
                    //There is no way to create attachments folder using client object model.
                    var baseUrl = (new Uri(clientContext.Site.Url)).GetLeftPart(UriPartial.Authority);
                    var webUrl = baseUrl.TrimEnd('/') + web.ServerRelativeUrl;

                    using (var service = new ListService(webUrl, authentication))
                    {
                        service.AddAttachment(list.Id.ToString(), listItem.Id.ToString(CultureInfo.InvariantCulture), fileName, fileStream);
                    }
                }
                else
                {
                    var fileUrl = string.Format("{0}/{1}", attachmentsFolderUrl, fileName);

                    if (fileStream != null)
                    {
                        using (var mst = new MemoryStream(fileStream))
                        {
                            SP.File.SaveBinaryDirect(clientContext, fileUrl, mst, true);
                        }
                        clientContext.ExecuteQuery();
                    }
                }
            }
        }
        #endregion
    }
}
