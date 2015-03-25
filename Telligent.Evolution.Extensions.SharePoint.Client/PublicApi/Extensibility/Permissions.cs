using System;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class PermissionsGetOptions
    {
        private PermissionsGetOptions(Guid contentId)
        {
            ContentId = contentId;
        }

        public PermissionsGetOptions(Guid listId, Guid itemId)
            : this(itemId)
        {
            ListId = listId;
        }

        public Guid ContentId { get; private set; }
        public Guid ListId { get; private set; }

        public string Url { get; set; }
    }

    public class PermissionsListOptions
    {
        private PermissionsListOptions(Guid contentId)
        {
            ContentId = contentId;
        }

        public PermissionsListOptions(Guid listId, Guid itemId)
            : this(itemId)
        {
            ListId = listId;
        }

        public Guid ContentId { get; private set; }
        public Guid ListId { get; private set; }

        public string Url { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
    }

    public class PermissionsUpdateOptions
    {
        private PermissionsUpdateOptions()
        {
            CopyRoleAssignments = true;
        }

        private PermissionsUpdateOptions(Guid contentId)
            : this()
        {
            ContentId = contentId;
        }

        public PermissionsUpdateOptions(Guid listId, Guid itemId)
            : this(itemId)
        {
            ListId = listId;
        }

        public Guid ContentId { get; private set; }
        public Guid ListId { get; private set; }

        public string Url { get; set; }
        public int[] Levels { get; set; }
        public int[] GroupIds { get; set; }
        public string[] LoginNames { get; set; }
        public bool Overwrite { get; set; }
        /// <summary>
        /// Returns true by default
        /// </summary>
        public bool CopyRoleAssignments { get; set; }
        public bool ClearSubscopes { get; set; }
    }

    public interface IPermissions : ICacheable
    {
        ApiList<SPPermissionsLevel> Levels(string webUrl);
        SPPermissions Get(int userOrGroupId, PermissionsGetOptions options);
        PagedList<SPPermissions> List(PermissionsListOptions options);
        void Update(PermissionsUpdateOptions options);
        void Remove(int[] userOrGroupIds, PermissionsGetOptions options);
        Inheritance GetInheritance(PermissionsGetOptions options);
        void ResetInheritance(PermissionsGetOptions options);
    }

    public class Permissions : IPermissions
    {
        private readonly IPermissionsService permissionsService;
        private readonly ICacheService cacheService;

        public Permissions()
            : this(ServiceLocator.Get<IPermissionsService>(), ServiceLocator.Get<ICacheService>())
        {
        }

        internal Permissions(IPermissionsService permissionsService, ICacheService cacheService)
        {
            this.permissionsService = permissionsService;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get
            {
                return cacheTimeOut;
            }
            set
            {
                cacheTimeOut = value;
            }
        }

        public ApiList<SPPermissionsLevel> Levels(string webUrl)
        {
            var cacheKey = String.Format("SharePoint_Levels:{0}", webUrl.Trim('/').GetHashCode());
            var levels = (ApiList<SPPermissionsLevel>)cacheService.Get(cacheKey, CacheScope.Context | CacheScope.Process);
            if (levels == null)
            {
                levels = new ApiList<SPPermissionsLevel>(permissionsService.Levels(webUrl));
                cacheService.Put(cacheKey, levels, CacheScope.Context | CacheScope.Process, new string[] { }, CacheTimeOut);
            }
            return levels;
        }

        public SPPermissions Get(int userOrGroupId, PermissionsGetOptions options)
        {
            var permissions = (SPPermissions)cacheService.Get(CacheKey(options.ContentId, userOrGroupId), CacheScope.Context | CacheScope.Process);
            if (permissions == null)
            {
                permissions = permissionsService.Get(userOrGroupId, options);
                cacheService.Put(CacheKey(options.ContentId, userOrGroupId), permissions, CacheScope.Context | CacheScope.Process, new string[] { }, CacheTimeOut);
            }
            return permissions;
        }

        public PagedList<SPPermissions> List(PermissionsListOptions options)
        {
            var permissionsList = (PagedList<SPPermissions>)cacheService.Get(CacheKey(options), CacheScope.Context | CacheScope.Process);
            if (permissionsList == null)
            {
                permissionsList = permissionsService.List(options);

                cacheService.Put(CacheKey(options), permissionsList, CacheScope.Context | CacheScope.Process, new[] { Tag(options.ContentId) }, CacheTimeOut);
            }
            return permissionsList;
        }

        public void Update(PermissionsUpdateOptions options)
        {
            ExpireTags(options.ContentId);

            permissionsService.Update(options);
        }

        public void Remove(int[] userOrGroupIds, PermissionsGetOptions options)
        {
            ExpireTags(options.ContentId);

            permissionsService.Remove(userOrGroupIds, options);
        }

        public Inheritance GetInheritance(PermissionsGetOptions options)
        {
            var cacheKey = string.Concat("SharePoint_PermissionsInheritance:", options.ContentId.ToString("N"));
            var inheritance = (Inheritance)cacheService.Get(cacheKey, CacheScope.Context | CacheScope.Process);
            if (inheritance == null)
            {
                inheritance = permissionsService.GetInheritance(options);
                cacheService.Put(cacheKey, inheritance, CacheScope.Context | CacheScope.Process, new[] { Tag(options.ContentId) }, CacheTimeOut);
            }
            return inheritance;
        }

        public void ResetInheritance(PermissionsGetOptions options)
        {
            ExpireTags(options.ContentId);

            permissionsService.ResetInheritance(options);
        }

        #region Cache-related Methods

        private static string CacheKey(Guid contentId, int userOrGroupId)
        {
            return string.Concat("SharePoint_Permissions:", contentId.ToString("N"), ":", userOrGroupId);
        }

        private static string CacheKey(PermissionsListQuery options)
        {
            return string.Concat("SharePoint_Permissions:", options.ContentId.ToString("N"), ":", options.PageSize, ":", options.PageIndex);
        }

        private static string Tag(Guid contentId)
        {
            return string.Concat("SharePoint_Permissions_TAG:", contentId.ToString("N"));
        }

        private void ExpireTags(Guid contentId)
        {
            cacheService.RemoveByTags(new[] { Tag(contentId) }, CacheScope.Context | CacheScope.Process);
        }

        #endregion
    }
}
