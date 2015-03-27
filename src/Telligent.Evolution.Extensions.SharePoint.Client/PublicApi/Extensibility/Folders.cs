using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components.Cache;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class FolderGetOptions
    {
        public FolderGetOptions(string path)
        {
            Path = path;
        }

        public string Path { get; set; }
        public string SPWebUrl { get; set; }
    }

    public class FolderCreateOptions
    {
        public FolderCreateOptions(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
        public string Path { get; set; }
        public string SPWebUrl { get; set; }
    }

    public class FolderRenameOptions
    {
        public FolderRenameOptions(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; private set; }
        public string Path { get; private set; }
        public string SPWebUrl { get; set; }
    }

    public class FolderListOptions
    {
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
        public string Path { get; set; }
        public string SPWebUrl { get; set; }
    }

    public interface IFolders : ICacheable
    {
        Folder Create(Guid libraryId, FolderCreateOptions options);
        Folder Rename(Guid libraryId, FolderRenameOptions options);
        Folder Get(Guid libraryId, FolderGetOptions options);
        Folder GetParent(Guid libraryId, FolderGetOptions options);
        PagedList<Folder> List(Guid listId, FolderListOptions options);
        Folder Delete(Guid libraryId, FolderGetOptions options);
    }

    public class Folders : IFolders
    {
        private const int DefaultPageSize = 20;

        private readonly IFolderService folders;
        private readonly IListDataService listDataService;
        private readonly ICacheService cacheService;

        public Folders() :
            this(ServiceLocator.Get<IFolderService>(), ServiceLocator.Get<IListDataService>(), ServiceLocator.Get<ICacheService>())
        {
        }


        internal Folders(IFolderService folders, IListDataService listDataService, ICacheService cacheService)
        {
            this.cacheService = cacheService;
            this.listDataService = listDataService;
            this.folders = folders;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        public Folder Create(Guid libraryId, FolderCreateOptions options)
        {
            // TODO: OnBeforeCreate

            var url = !string.IsNullOrEmpty(options.SPWebUrl) ? options.SPWebUrl : GetUrl(libraryId);
            var folder = folders.Create(url, libraryId, options);
            ExpireTags(libraryId);

            // TODO: OnAfterCreate

            return Get(libraryId, new FolderGetOptions(folder.Path));
        }

        public Folder Rename(Guid libraryId, FolderRenameOptions options)
        {
            // TODO: OnBeforeRename

            var url = !string.IsNullOrEmpty(options.SPWebUrl) ? options.SPWebUrl : GetUrl(libraryId);
            var folder = folders.Rename(url, libraryId, options.Path, options);
            ExpireTags(libraryId);

            // TODO: OnAfterRename

            return Get(libraryId, new FolderGetOptions(folder.Path));
        }

        public Folder Get(Guid libraryId, FolderGetOptions options)
        {
            var cacheId = CacheKey(libraryId, options.Path);
            var folderBox = (CacheBox<Folder>)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (folderBox == null)
            {
                var url = !string.IsNullOrEmpty(options.SPWebUrl) ? options.SPWebUrl : GetUrl(libraryId);
                var folder = folders.GetParent(url, libraryId, options.Path);
                folderBox = new CacheBox<Folder>(folder);
                cacheService.Put(cacheId, folderBox, CacheScope.Context | CacheScope.Process, new[] { Tag(libraryId) }, CacheTimeOut);
            }
            return folderBox.Data;
        }

        public Folder GetParent(Guid libraryId, FolderGetOptions options)
        {
            var cacheId = ParentFolderCacheKey(libraryId, options.Path);
            var folderBox = (CacheBox<Folder>)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (folderBox == null)
            {
                var url = !string.IsNullOrEmpty(options.SPWebUrl) ? options.SPWebUrl : GetUrl(libraryId);
                var folder = folders.GetParent(url, libraryId, options.Path);
                folderBox = new CacheBox<Folder>(folder);
                cacheService.Put(cacheId, folderBox, CacheScope.Context | CacheScope.Process, new[] { Tag(libraryId) }, CacheTimeOut);
            }
            return folderBox.Data;
        }

        public PagedList<Folder> List(Guid libraryId, FolderListOptions options)
        {
            if (options == null)
            {
                options = new FolderListOptions { PageSize = DefaultPageSize };
            }

            var cacheId = ListFoldersCacheKey(libraryId, options.Path);
            var folderList = (List<Folder>)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (folderList == null)
            {
                var url = !string.IsNullOrEmpty(options.SPWebUrl) ? options.SPWebUrl : GetUrl(libraryId);
                folderList = folders.List(url, libraryId, options.Path);
                cacheService.Put(cacheId, folderList, CacheScope.Context | CacheScope.Process, new[] { Tag(libraryId) }, CacheTimeOut);
            }

            return new PagedList<Folder>(folderList.Skip(options.PageIndex * options.PageSize).Take(options.PageSize))
            {
                PageSize = options.PageSize,
                PageIndex = options.PageIndex,
                TotalCount = folderList.Count
            };
        }

        public Folder Delete(Guid libraryId, FolderGetOptions options)
        {
            var folder = Get(libraryId, options);
            if (folder != null)
            {
                var url = !String.IsNullOrEmpty(options.SPWebUrl) ? options.SPWebUrl : GetUrl(libraryId);
                folders.Delete(url, libraryId, options.Path);
                ExpireTags(libraryId);
            }
            return folder;
        }

        #region Cache Methods

        private static string CacheKey(Guid libraryId, string path)
        {
            return string.Concat("Folders.Get:", libraryId.ToString("N"), ":", path);
        }

        private static string ParentFolderCacheKey(Guid libraryId, string path)
        {
            return string.Concat("Folders.GetParent:", libraryId.ToString("N"), ":", path);
        }

        private static string ListFoldersCacheKey(Guid libraryId, string path)
        {
            return string.Concat("Folders.List:", libraryId.ToString("N"), ":", path);
        }

        internal static string Tag(Guid libraryId)
        {
            return string.Concat("SharePoint_Folder_TAG:", libraryId);
        }

        private void ExpireTags(Guid libraryId)
        {
            cacheService.RemoveByTags(new[] { Tag(libraryId), Documents.Tag(libraryId), ListItems.Tag(libraryId) }, CacheScope.Context | CacheScope.Process);
        }

        #endregion

        private string GetUrl(Guid listId)
        {
            var list = listDataService.Get(listId);
            if (list != null)
                return list.SPWebUrl;
            return null;
        }
    }
}
