using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using SP = Microsoft.SharePoint.Client;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class LibraryCreateOptions
    {
        public LibraryCreateOptions(int groupId)
        {
            GroupId = groupId;
        }

        public int GroupId { get; private set; }
        public string Description { get; set; }
        public string SPWebUrl { get; set; }
        public string Title { get; set; }
    }

    public class LibraryGetOptions
    {
        public LibraryGetOptions(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public int GroupId { get; set; }
        public string Url { get; set; }
    }

    public class LibraryListOptions
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string Filter { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
    }

    public class LibraryUpdateOptions
    {
        public LibraryUpdateOptions(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public Guid? DefaultViewId { get; set; }
    }

    public interface ILibraries : ICacheable
    {
        LibraryEvents Events { get; }
        void Add(int groupId, Guid id, string url);
        void Update(LibraryUpdateOptions options);
        Library Create(LibraryCreateOptions options);
        Library Get(LibraryGetOptions options);
        PagedList<Library> List(int groupId, LibraryListOptions options);
        void Delete(Library library, bool deleteLibrary);
    }

    public class Libraries : ILibraries
    {
        private readonly ICacheService cacheService;
        private readonly IListService listService;

        public Libraries() :
            this(ServiceLocator.Get<IListService>(), ServiceLocator.Get<ICacheService>())
        {
        }

        internal Libraries(IListService listService, ICacheService cacheService)
        {
            this.listService = listService;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        private readonly LibraryEvents events = new LibraryEvents();
        public LibraryEvents Events
        {
            get { return events; }
        }

        public void Add(int groupId, Guid id, string url)
        {
            var list = new ListBase(groupId, id, LibraryApplicationType.Id, url);
            listService.Add(list);

            var group = TEApi.Groups.Get(new Extensibility.Api.Version1.GroupsGetOptions { Id = groupId });
            SecurityService.RecalculatePermissions(list.Id, LibraryApplicationType.Id, group.ApplicationId);

            ExpireTags(groupId);
        }

        public void Update(LibraryUpdateOptions options)
        {
            listService.Update(options);

            var list = listService.Get(new ListGetQuery(options.Id, LibraryApplicationType.Id)
            {
                Url = options.Url
            });
            ExpireTags(list.GroupId);
        }

        public Library Create(LibraryCreateOptions options)
        {
            try
            {
                Events.OnBeforeCreate(new Library { SPWebUrl = options.SPWebUrl, Name = options.Title, Description = options.Description });
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Libraries.Events.OnBeforeCreate() method GroupId: {1}. The exception message is: {2}", ex.GetType(), options.GroupId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            var library = new Library(listService.Create((int)SP.ListTemplateType.DocumentLibrary, options));
            ExpireTags(options.GroupId);

            var group = TEApi.Groups.Get(new Extensibility.Api.Version1.GroupsGetOptions { Id = library.GroupId });
            SecurityService.RecalculatePermissions(library.Id, LibraryApplicationType.Id, group.ApplicationId);

            cacheService.Put(CacheKey(library.Id), library, CacheScope.Context | CacheScope.Process, new[] { Tag(library.GroupId) }, CacheTimeOut);
            try
            {
                Events.OnAfterCreate(library);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Libraries.Events.OnAfterCreate() method GroupId: {1} LibraryId: {2} SPWebUrl: {3}. The exception message is: {4}", ex.GetType(), options.GroupId, library.Id, library.SPWebUrl, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            return library;
        }

        public Library Get(LibraryGetOptions options)
        {
            var library = (Library)cacheService.Get(CacheKey(options.Id), CacheScope.Context | CacheScope.Process);
            if (library == null)
            {
                var spList = listService.Get(options);
                if (spList != null)
                {
                    library = new Library(spList);
                    cacheService.Put(CacheKey(library.Id), library, CacheScope.Context | CacheScope.Process, new[] { Tag(library.GroupId) }, CacheTimeOut);
                }
            }
            return library;
        }

        public Library Get(Guid id)
        {
            var library = (Library)cacheService.Get(CacheKey(id), CacheScope.Context | CacheScope.Process);
            if (library == null)
            {
                var spList = listService.Get(new LibraryGetOptions(id));
                if (spList != null)
                {
                    library = new Library(spList);
                    cacheService.Put(CacheKey(library.Id), library, CacheScope.Context | CacheScope.Process, new[] { Tag(library.GroupId) }, CacheTimeOut);
                }
            }
            return library;
        }

        public PagedList<Library> List(int groupId, LibraryListOptions options)
        {
            var libraries = (List<Library>)cacheService.Get(CacheKey(groupId), CacheScope.Context | CacheScope.Process);
            if (libraries == null)
            {
                libraries = new List<Library>();
                var lists = listService.List(groupId, options);
                try
                {
                    libraries.AddRange(lists.Select(list => new Library(list)));
                    cacheService.Put(CacheKey(groupId), libraries, CacheScope.Context | CacheScope.Process, new[] { Tag(groupId) }, CacheTimeOut);
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the PublicApi.Libraries.List() method for GroupId: {1}. The exception message is: {2}", ex.GetType(), groupId, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    throw new SPInternalException(message, ex);
                }
            }

            if (!String.IsNullOrEmpty(options.Filter))
            {
                libraries = libraries.Where(library => library.Name.Contains(options.Filter, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }

            Library.SortBy sortBy;
            Enum.TryParse(options.SortBy, out sortBy);

            return new PagedList<Library>(libraries.Order(sortBy, options.SortOrder).Skip(options.PageSize * options.PageIndex).Take(options.PageSize))
                {
                    PageSize = options.PageSize,
                    PageIndex = options.PageIndex,
                    TotalCount = libraries.Count
                };
        }

        public void Delete(Library library, bool deleteLibrary)
        {
            if (library == null)
                throw new NullReferenceException("Library cannot be null.");
            if (library.Id == Guid.Empty)
                throw new InvalidOperationException("Id cannot be empty.");
            if (string.IsNullOrEmpty(library.SPWebUrl))
                throw new InvalidOperationException("SPWebUrl cannot be empty.");

            try
            {
                Events.OnBeforeDelete(library);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Libraries.Events.OnBeforeDelete() method GroupId: {1} ListId: {2} SPWebUrl: {3} Delete: {4}. The exception message is: {5}", ex.GetType(), library.GroupId, library.Id, library.SPWebUrl, deleteLibrary, ex.Message);
                SPLog.UnKnownError(ex, message);
            }

            listService.Delete(library.Id, deleteLibrary);
            ExpireTags(library.GroupId);

            try
            {
                Events.OnAfterDelete(library);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PublicApi.Libraries.Events.OnAfterDelete() method GroupId: {1} ListId: {2} SPWebUrl: {3} Delete: {4}. The exception message is: {5}", ex.GetType(), library.GroupId, library.Id, library.SPWebUrl, deleteLibrary, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
        }

        #region Cache-related methods

        private void ExpireTags(int? groupId)
        {
            cacheService.RemoveByTags(new[] { Tag(groupId) }, CacheScope.Context | CacheScope.Process);
        }

        private static string CacheKey(Guid id)
        {
            return string.Concat("SharePoint_Library:", id.ToString("N"));
        }

        private static string CacheKey(int groupId)
        {
            return string.Concat("SharePoint_Libraries:", groupId, ":", LibraryApplicationType.Id);
        }

        private static string Tag(int? groupId)
        {
            return string.Concat("SharePoint_Library_TAG:", groupId);
        }

        #endregion
    }
}
