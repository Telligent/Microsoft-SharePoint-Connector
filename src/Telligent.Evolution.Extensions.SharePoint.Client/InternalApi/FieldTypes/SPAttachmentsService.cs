using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Entities;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.WebServices;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class AttachmentsGetQuery
    {
        private AttachmentsGetQuery(string fieldName)
        {
            FieldName = fieldName;
        }

        public AttachmentsGetQuery(int id, string fieldName)
            : this(fieldName)
        {
            Id = id;
        }

        public AttachmentsGetQuery(Guid contentId, string fieldName)
            : this(fieldName)
        {
            ContentId = contentId;
        }

        public string FieldName { get; private set; }
        public Guid ContentId { get; private set; }
        public int? Id { get; private set; }
    }

    internal class AttachmentsAddQuery : AttachmentsGetQuery
    {
        public AttachmentsAddQuery(int id, string fieldName) : base(id, fieldName) { }
        public AttachmentsAddQuery(Guid id, string fieldName) : base(id, fieldName) { }

        public string FileName { get; set; }
        public byte[] FileData { get; set; }
    }

    internal class AttachmentsRemoveQuery : AttachmentsGetQuery
    {
        public AttachmentsRemoveQuery(int id, string fieldName) : base(id, fieldName) { }
        public AttachmentsRemoveQuery(Guid id, string fieldName) : base(id, fieldName) { }

        public List<string> FileNames { get; set; }
    }

    internal interface IAttachmentsService
    {
        void Add(string url, Guid listId, AttachmentsAddQuery options);
        List<SPAttachment> List(string url, Guid listId, AttachmentsGetQuery options);
        void Remove(string url, Guid listId, AttachmentsRemoveQuery options);
    }

    internal class SPAttachmentsService : IAttachmentsService
    {
        private readonly ICredentialsManager credentials;

        public SPAttachmentsService()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        public SPAttachmentsService(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public void Add(string url, Guid listId, AttachmentsAddQuery options)
        {
            using (var clientContext = new SPContext(url, credentials.Get(url)))
            {
                ListItemCollection listItems = null;
                if (!options.Id.HasValue)
                {
                    listItems = clientContext.Web.Lists.GetById(listId).GetItems(CAMLQueryBuilder.GetItem(options.ContentId, new string[] { }));
                    clientContext.Load(listItems, _ => _.Include(item => item.Id));
                }

                var listRootFolder = clientContext.Web.Lists.GetById(listId).RootFolder;
                clientContext.Load(listRootFolder, f => f.ServerRelativeUrl);
                clientContext.ExecuteQuery();

                int listItemId = options.Id.HasValue ? options.Id.Value : listItems.First().Id;

                Microsoft.SharePoint.Client.Folder attachmentsFolder;
                try
                {
                    attachmentsFolder = clientContext.Web.GetFolderByServerRelativeUrl(listRootFolder.ServerRelativeUrl + "/Attachments/" + listItemId.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                    clientContext.Load(attachmentsFolder);
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
                    using (var service = new ListService(url, credentials.Get(url)))
                    {
                        service.AddAttachment(listId.ToString(), listItemId.ToString(CultureInfo.InvariantCulture), options.FileName, options.FileData);
                    }
                }
                else
                {
                    var fileUrl = string.Format("{0}/{1}", listRootFolder.ServerRelativeUrl + "/Attachments/" + listItemId.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(), options.FileName);
                    if (options.FileData != null)
                    {
                        using (var stream = new MemoryStream(options.FileData))
                        {
                            Microsoft.SharePoint.Client.File.SaveBinaryDirect(clientContext, fileUrl, stream, true);
                        }
                        clientContext.ExecuteQuery();
                    }
                }
            }
        }

        public List<SPAttachment> List(string url, Guid listId, AttachmentsGetQuery options)
        {
            using (var clientContext = new SPContext(url, credentials.Get(url)))
            {
                var site = clientContext.Site;
                clientContext.Load(site, s => s.Url);

                ListItemCollection listItems = null;
                if (!options.Id.HasValue)
                {
                    listItems = clientContext.Web.Lists.GetById(listId).GetItems(CAMLQueryBuilder.GetItem(options.ContentId, new string[] { }));
                    clientContext.Load(listItems, _ => _.Include(item => item.Id));
                }

                var listRootFolder = clientContext.Web.Lists.GetById(listId).RootFolder;
                clientContext.Load(listRootFolder, f => f.ServerRelativeUrl);
                clientContext.ExecuteQuery();

                try
                {
                    int listItemId = options.Id.HasValue ? options.Id.Value : listItems.First().Id;

                    var attachmentsFolder = clientContext.Web.GetFolderByServerRelativeUrl(listRootFolder.ServerRelativeUrl + "/Attachments/" + listItemId.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                    clientContext.Load(attachmentsFolder);
                    clientContext.Load(attachmentsFolder.Files,
                        files => files.Include(
                            f => f.ServerRelativeUrl,
                            f => f.Name,
                            f => f.Title,
                            f => f.TimeCreated,
                            f => f.Author,
                            f => f.TimeLastModified,
                            f => f.ModifiedBy));
                    clientContext.ExecuteQuery();
                    return attachmentsFolder.Files.ToList().Select(attachment => new SPAttachment(attachment.Name, new Uri(site.Url + attachment.ServerRelativeUrl))
                    {
                        Created = attachment.TimeCreated,
                        Modified = attachment.TimeLastModified,
                        CreatedBy = new SPUserPrincipal(attachment.Author),
                        ModifiedBy = new SPUserPrincipal(attachment.ModifiedBy)
                    }).ToList();
                }
                catch
                {
                    return new List<SPAttachment>();
                }
            }
        }

        public void Remove(string url, Guid listId, AttachmentsRemoveQuery options)
        {
            using (var clientContext = new SPContext(url, credentials.Get(url)))
            {
                ListItemCollection listItems = null;
                if (!options.Id.HasValue)
                {
                    listItems = clientContext.Web.Lists.GetById(listId).GetItems(CAMLQueryBuilder.GetItem(options.ContentId, new string[] { }));
                    clientContext.Load(listItems, _ => _.Include(item => item.Id));
                }

                var listRootFolder = clientContext.Web.Lists.GetById(listId).RootFolder;
                clientContext.Load(listRootFolder, f => f.ServerRelativeUrl);
                clientContext.ExecuteQuery();

                int listItemId = options.Id.HasValue ? options.Id.Value : listItems.First().Id;

                var attachmentsFolder = clientContext.Web.GetFolderByServerRelativeUrl(listRootFolder.ServerRelativeUrl + "/Attachments/" + listItemId);
                clientContext.Load(attachmentsFolder.Files);
                clientContext.ExecuteQuery();

                foreach (var file in attachmentsFolder.Files.ToList())
                {
                    if (options.FileNames.Contains(file.Name))
                    {
                        file.DeleteObject();
                    }
                }
                attachmentsFolder.Update();
                clientContext.ExecuteQuery();
            }
        }
    }
}
