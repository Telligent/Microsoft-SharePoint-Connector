using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class AttachmentsGetOptions
    {
        private AttachmentsGetOptions(string fieldName)
        {
            FieldName = fieldName;
        }

        public AttachmentsGetOptions(Guid contentId, string fieldName)
            : this(fieldName)
        {
            ContentId = contentId;
        }

        public Guid ContentId { get; private set; }
        public string FieldName { get; private set; }
        public string Url { get; set; }
    }

    public class AttachmentsAddOptions : AttachmentsGetOptions
    {
        public AttachmentsAddOptions(Guid contentId, string fieldName)
            : base(contentId, fieldName)
        {
            Files = new Dictionary<string, byte[]>();
        }

        public Dictionary<string, byte[]> Files { get; set; }
    }

    public class AttachmentsRemoveOptions : AttachmentsGetOptions
    {
        public AttachmentsRemoveOptions(Guid contentId, string fieldName)
            : base(contentId, fieldName)
        {
            FileNames = new List<string>();
        }

        public List<string> FileNames { get; set; }
    }

    public interface IAttachments : ICacheable
    {
        void Add(Guid listId, AttachmentsAddOptions options);
        List<SPAttachment> List(Guid listId, AttachmentsGetOptions options);
        void Remove(Guid listId, AttachmentsRemoveOptions options);
    }

    public class Attachments : IAttachments
    {
        private readonly IListDataService listDataService;
        private readonly IListItemDataService listItemDataService;
        private readonly IAttachmentsService attachmentsService;
        private readonly ICacheService cacheService;

        public Attachments()
            : this(ServiceLocator.Get<IListDataService>(), ServiceLocator.Get<IListItemDataService>(), ServiceLocator.Get<IAttachmentsService>(), ServiceLocator.Get<ICacheService>())
        {
        }

        internal Attachments(IListDataService listDataService, IListItemDataService listItemDataService, IAttachmentsService attachmentsService, ICacheService cacheService)
        {
            this.listDataService = listDataService;
            this.listItemDataService = listItemDataService;
            this.attachmentsService = attachmentsService;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        public void Add(Guid listId, AttachmentsAddOptions options)
        {
            if (options.Files.Count == 0) return;

            var url = EnsureUrl(options.Url, listId);
            foreach (var file in options.Files)
            {
                attachmentsService.Add(url, listId, new AttachmentsAddQuery(options.ContentId, options.FieldName)
                {
                    FileName = file.Key,
                    FileData = file.Value
                });
            }
            cacheService.Remove(CacheKey(options.ContentId, options.FieldName), CacheScope.Context | CacheScope.Process);
            cacheService.RemoveByTags(new[] { Tag(listId), ListItems.Tag(listId), Documents.Tag(listId) }, CacheScope.Context | CacheScope.Process);
        }

        public List<SPAttachment> List(Guid listId, AttachmentsGetOptions options)
        {
            var cacheKey = CacheKey(options.ContentId, options.FieldName);
            var attachments = (List<SPAttachment>)cacheService.Get(cacheKey, CacheScope.Context | CacheScope.Process);
            if (attachments == null)
            {
                var url = EnsureUrl(options.Url, listId);
                attachments = attachmentsService.List(url, listId, new AttachmentsGetQuery(options.ContentId, options.FieldName));
                cacheService.Put(cacheKey, attachments, CacheScope.Context | CacheScope.Process, new[] { Tag(listId) }, CacheTimeOut);
            }
            return attachments;
        }

        public void Remove(Guid listId, AttachmentsRemoveOptions options)
        {
            if (options.FileNames.Count == 0) return;

            var url = EnsureUrl(options.Url, listId);
            attachmentsService.Remove(url, listId, new AttachmentsRemoveQuery(options.ContentId, options.FieldName)
            {
                FileNames = options.FileNames
            });

            cacheService.Remove(CacheKey(options.ContentId, options.FieldName), CacheScope.Context | CacheScope.Process);
            cacheService.RemoveByTags(new[] { Tag(listId), ListItems.Tag(listId), Documents.Tag(listId) }, CacheScope.Context | CacheScope.Process);
        }

        public static string Tag(Guid listId)
        {
            return string.Format("Attachments::{0}", listId.ToString("N"));
        }

        private static string CacheKey(Guid contentId, string fieldName)
        {
            return string.Format("Attachments.List::{0}::{1}", contentId.ToString("N"), fieldName != null ? fieldName.ToLowerInvariant() : "attachments");
        }

        private string EnsureUrl(string url, Guid listId)
        {
            var notEmptyUrl = !String.IsNullOrEmpty(url) ? url : GetUrlByListId(listId);

            if (string.IsNullOrEmpty(notEmptyUrl))
                throw new InvalidOperationException("Url cannot be empty.");

            return notEmptyUrl;
        }

        private string GetUrlByListId(Guid listId)
        {
            var list = listDataService.Get(listId);
            if (list != null)
            {
                list.Validate();
                return list.SPWebUrl;
            }
            return null;
        }
    }
}
