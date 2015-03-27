using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class PermissionsGetQuery
    {
        public static implicit operator PermissionsGetQuery(PermissionsGetOptions options)
        {
            return new PermissionsGetQuery(options.ListId, options.ContentId)
            {
                Url = options.Url
            };
        }

        private PermissionsGetQuery(Guid contentId)
        {
            ContentId = contentId;
        }

        public PermissionsGetQuery(Guid listId, Guid itemId)
            : this(itemId)
        {
            ListId = listId;
        }

        public PermissionsGetQuery(Guid listId, int itemId)
        {
            ListId = listId;
            Id = itemId;
        }

        public int? Id { get; private set; }
        public Guid ContentId { get; private set; }
        public Guid ListId { get; private set; }

        public string Url { get; set; }
    }

    internal class PermissionsListQuery
    {
        private const int DefaultPageSize = 20;

        public static implicit operator PermissionsListQuery(PermissionsListOptions options)
        {
            var query = new PermissionsListQuery(options.ListId, options.ContentId)
            {
                PageSize = options.PageSize,
                PageIndex = options.PageIndex,
                Url = options.Url
            };
            return query;
        }

        private PermissionsListQuery()
        {
            PageSize = DefaultPageSize;
        }

        private PermissionsListQuery(Guid contentId)
            : this()
        {
            ContentId = contentId;
        }

        public PermissionsListQuery(Guid listId, Guid itemId)
            : this(itemId)
        {
            ListId = listId;
        }

        public PermissionsListQuery(Guid listId, int itemId)
            : this()
        {
            ListId = listId;
            Id = itemId;
        }

        public int? Id { get; private set; }
        public Guid ContentId { get; private set; }
        public Guid ListId { get; private set; }

        public string Url { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
    }

    internal class PermissionsUpdateQuery
    {
        public static implicit operator PermissionsUpdateQuery(PermissionsUpdateOptions options)
        {
            var query = new PermissionsUpdateQuery(options.ListId, options.ContentId)
            {
                Url = options.Url,
                Levels = options.Levels,
                GroupIds = options.GroupIds,
                LoginNames = options.LoginNames,
                Overwrite = options.Overwrite,
                CopyRoleAssignments = options.CopyRoleAssignments,
                ClearSubscopes = options.ClearSubscopes
            };

            return query;
        }

        private PermissionsUpdateQuery()
        {
            CopyRoleAssignments = true;
        }

        private PermissionsUpdateQuery(Guid contentId)
            : this()
        {
            ContentId = contentId;
        }

        public PermissionsUpdateQuery(Guid listId, Guid itemId)
            : this(itemId)
        {
            ListId = listId;
        }

        public PermissionsUpdateQuery(Guid listId, int itemId)
            : this()
        {
            ListId = listId;
            Id = itemId;
        }

        public int? Id { get; private set; }
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

    internal interface IPermissionsService
    {
        List<SPPermissionsLevel> Levels(string webUrl);
        SPPermissions Get(int userOrGroupId, PermissionsGetQuery options);
        PagedList<SPPermissions> List(PermissionsListQuery options);
        void Update(PermissionsUpdateQuery options);
        void Remove(int[] userOrGroupIds, PermissionsGetQuery options);
        Inheritance GetInheritance(PermissionsGetQuery options);
        void ResetInheritance(PermissionsGetQuery options);
    }

    internal class SPPermissionsService : IPermissionsService
    {
        private readonly ICredentialsManager credentials;
        private readonly IListDataService listDataService;
        private readonly IListItemDataService listItemDataService;

        public SPPermissionsService()
            : this(ServiceLocator.Get<ICredentialsManager>(), ServiceLocator.Get<IListDataService>(), ServiceLocator.Get<IListItemDataService>())
        {
        }

        public SPPermissionsService(ICredentialsManager credentials, IListDataService listDataService, IListItemDataService listItemDataService)
        {
            this.credentials = credentials;
            this.listDataService = listDataService;
            this.listItemDataService = listItemDataService;
        }

        public List<SPPermissionsLevel> Levels(string webUrl)
        {
            var levels = new List<SPPermissionsLevel>();
            try
            {
                using (var clientContext = new SPContext(webUrl, credentials.Get(webUrl)))
                {
                    var web = clientContext.Web;
                    clientContext.Load(web,
                        w => w.RoleDefinitions.Include(
                            rd => rd.Id,
                            rd => rd.Name,
                            rd => rd.Description));
                    clientContext.ExecuteQuery();

                    foreach (var rd in web.RoleDefinitions)
                    {
                        levels.Add(new SPPermissionsLevel(rd.Id, rd.Name) { Description = rd.Description });
                    }
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the SPPermissionsService.Levels() method for SPWebUrl: {1}. The exception message is: {2}", ex.GetType(), webUrl, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
            return levels;
        }

        public SPPermissions Get(int userOrGroupId, PermissionsGetQuery options)
        {
            var spwebUrl = EnsureUrl(options.Url, options.ListId);

            try
            {
                using (var clientContext = new SPContext(spwebUrl, credentials.Get(spwebUrl)))
                {
                    List splist = clientContext.Web.Lists.GetById(options.ListId);
                    var splistItemCollection = splist.GetItems(options.Id.HasValue ?
                        CAMLQueryBuilder.GetItem(options.Id.Value, new string[] { }) :
                        CAMLQueryBuilder.GetItem(options.ContentId, new string[] { }));

                    var listItemRoleAssignments = clientContext.LoadQuery(
                        splistItemCollection.Select(item => item.RoleAssignments.GetByPrincipalId(userOrGroupId)).Include(
                            roleAssignment => roleAssignment.Member,
                            roleAssignment => roleAssignment.RoleDefinitionBindings.Include(
                                roleDef => roleDef.Id,
                                roleDef => roleDef.Name,
                                roleDef => roleDef.Description)));

                    clientContext.ExecuteQuery();

                    return listItemRoleAssignments.First().ToPermission();
                }
            }
            catch (Exception ex)
            {
                string itemId = options.Id.HasValue ? options.Id.Value.ToString(CultureInfo.InvariantCulture) : options.ContentId.ToString();
                string message = string.Format("An exception of type {0} occurred in the SPPermissionsService.Get() method for a User or Group with Id: {1} ListId: {2} ItemId: {3}. The exception message is: {4}", ex.GetType(), userOrGroupId, options.ListId, itemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        public PagedList<SPPermissions> List(PermissionsListQuery options)
        {
            var spwebUrl = EnsureUrl(options.Url, options.ListId);

            try
            {
                using (var clientContext = new SPContext(spwebUrl, credentials.Get(spwebUrl)))
                {
                    List splist = clientContext.Web.Lists.GetById(options.ListId);
                    var splistItemCollection = splist.GetItems(options.Id.HasValue ?
                        CAMLQueryBuilder.GetItem(options.Id.Value, new string[] { }) :
                        CAMLQueryBuilder.GetItem(options.ContentId, new string[] { }));

                    var lazyListItems = clientContext.LoadQuery(splistItemCollection.Include(item => item.HasUniqueRoleAssignments, item => item.Id));
                    clientContext.ExecuteQuery();

                    var splistItem = lazyListItems.First();
                    IEnumerable<RoleAssignment> lazyRoleAssignmentList = RoleAssignmentsLoadQuery(splistItem, clientContext);
                    clientContext.ExecuteQuery();

                    var roleAssignmentList = lazyRoleAssignmentList.ToList();
                    return new PagedList<SPPermissions>(roleAssignmentList.Skip(options.PageSize * options.PageIndex).Take(options.PageSize).Select(ra => ra.ToPermission()))
                    {
                        PageSize = options.PageSize,
                        PageIndex = options.PageIndex,
                        TotalCount = roleAssignmentList.Count
                    };
                }
            }
            catch (Exception ex)
            {
                string listItemId = options.Id.HasValue ? options.Id.Value.ToString(CultureInfo.InvariantCulture) : options.ContentId.ToString();
                string message = string.Format("An exception of type {0} occurred in the SPPermissionsService.List() method for ListId: {1} ItemId: {2}. The exception message is: {3}", ex.GetType(), options.ListId, listItemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        public void Update(PermissionsUpdateQuery options)
        {
            var spwebUrl = EnsureUrl(options.Url, options.ListId);

            try
            {
                using (var clientContext = new SPContext(spwebUrl, credentials.Get(spwebUrl)))
                {
                    List splist = clientContext.ToList(options.ListId);
                    var splistItemCollection = splist.GetItems(options.Id.HasValue ?
                        CAMLQueryBuilder.GetItem(options.Id.Value, new string[] { }) :
                        CAMLQueryBuilder.GetItem(options.ContentId, new string[] { }));

                    var lazyListItems = clientContext.LoadQuery(splistItemCollection.Include(item => item.HasUniqueRoleAssignments, item => item.Id));
                    clientContext.ExecuteQuery();

                    var splistItem = lazyListItems.First();
                    splistItem.BreakRoleInheritance(options.CopyRoleAssignments, options.ClearSubscopes);

                    if (options.Levels != null)
                    {
                        var rdList = new RoleDefinitionBindingCollection(clientContext);
                        foreach (var permissionLevelId in options.Levels)
                        {
                            rdList.Add(clientContext.Web.RoleDefinitions.GetById(permissionLevelId));
                        }

                        if (options.GroupIds != null)
                        {
                            foreach (var groupId in options.GroupIds)
                            {
                                var group = clientContext.Web.SiteGroups.GetById(groupId);
                                if (options.Overwrite)
                                {
                                    splistItem.RoleAssignments.GetByPrincipal(group).DeleteObject();
                                }
                                splistItem.RoleAssignments.Add(group, rdList);
                            }
                        }

                        if (options.LoginNames != null)
                        {
                            foreach (var loginName in options.LoginNames)
                            {
                                var user = clientContext.Web.EnsureUser(loginName);
                                if (options.Overwrite)
                                {
                                    splistItem.RoleAssignments.GetByPrincipal(user).DeleteObject();
                                }
                                splistItem.RoleAssignments.Add(user, rdList);
                            }
                        }
                    }

                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                string listItemId = options.Id.HasValue ? options.Id.Value.ToString(CultureInfo.InvariantCulture) : options.ContentId.ToString();
                string message = string.Format("An exception of type {0} occurred in the SPPermissionsService.Update() method for ListId: {1} ItemId: {2}. The exception message is: {3}", ex.GetType(), options.ListId, listItemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        public void Remove(int[] userOrGroupIds, PermissionsGetQuery options)
        {
            if (userOrGroupIds != null && userOrGroupIds.Length > 0)
            {
                var spwebUrl = EnsureUrl(options.Url, options.ListId);

                try
                {
                    using (var clientContext = new SPContext(spwebUrl, credentials.Get(spwebUrl)))
                    {
                        List splist = clientContext.Web.Lists.GetById(options.ListId);
                        var splistItemCollection = splist.GetItems(options.Id.HasValue ?
                            CAMLQueryBuilder.GetItem(options.Id.Value, new string[] { }) :
                            CAMLQueryBuilder.GetItem(options.ContentId, new string[] { }));

                        var lazyListItems = clientContext.LoadQuery(splistItemCollection.Include(item => item.HasUniqueRoleAssignments, item => item.Id));
                        clientContext.ExecuteQuery();

                        var splistItem = lazyListItems.First();
                        foreach (int userOrGroupId in userOrGroupIds)
                        {
                            splistItem.RoleAssignments.GetByPrincipalId(userOrGroupId).DeleteObject();
                        }
                        clientContext.ExecuteQuery();
                    }
                }
                catch (Exception ex)
                {
                    string listItemId = options.Id.HasValue ? options.Id.Value.ToString(CultureInfo.InvariantCulture) : options.ContentId.ToString();
                    string userOrGroupIdsString = string.Join(", ", userOrGroupIds);
                    string message = string.Format("An exception of type {0} occurred in the SPPermissionsService.Remove() method for Users or Groups with Ids: {1} ListId: {2} ItemId: {3}. The exception message is: {4}", ex.GetType(), userOrGroupIdsString, options.ListId, listItemId, ex.Message);
                    SPLog.RoleOperationUnavailable(ex, message);

                    throw new SPInternalException(message, ex);
                }
            }
        }

        public Inheritance GetInheritance(PermissionsGetQuery options)
        {
            var inheritance = new Inheritance();

            var spwebUrl = EnsureUrl(options.Url, options.ListId);

            try
            {
                using (var clientContext = new SPContext(spwebUrl, credentials.Get(spwebUrl)))
                {
                    List splist = clientContext.Web.Lists.GetById(options.ListId);
                    var splistItemCollection = splist.GetItems(options.Id.HasValue ?
                        CAMLQueryBuilder.GetItem(options.Id.Value, new string[] { }) :
                        CAMLQueryBuilder.GetItem(options.ContentId, new string[] { }));

                    var lazyListItems = clientContext.LoadQuery(splistItemCollection.Include(item => item.HasUniqueRoleAssignments, item => item.Id));
                    clientContext.ExecuteQuery();

                    var splistItem = lazyListItems.First();

                    inheritance.Enabled = !splistItem.HasUniqueRoleAssignments;
                }
            }
            catch (Exception ex)
            {
                string itemId = options.Id.HasValue ? options.Id.Value.ToString(CultureInfo.InvariantCulture) : options.ContentId.ToString();
                string message = string.Format("An exception of type {0} occurred in the SPPermissionsService.GetInheritance() method for ListId: {1} ItemId: {2}. The exception message is: {3}", ex.GetType(), options.ListId, itemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
            return inheritance;
        }

        public void ResetInheritance(PermissionsGetQuery options)
        {
            var spwebUrl = EnsureUrl(options.Url, options.ListId);

            try
            {
                using (var clientContext = new SPContext(spwebUrl, credentials.Get(spwebUrl)))
                {
                    List splist = clientContext.Web.Lists.GetById(options.ListId);
                    var splistItemCollection = splist.GetItems(options.Id.HasValue ?
                        CAMLQueryBuilder.GetItem(options.Id.Value, new string[] { }) :
                        CAMLQueryBuilder.GetItem(options.ContentId, new string[] { }));

                    var lazyListItems = clientContext.LoadQuery(splistItemCollection.Include(item => item.HasUniqueRoleAssignments, item => item.Id));
                    clientContext.ExecuteQuery();

                    var splistItem = lazyListItems.First();
                    splistItem.ResetRoleInheritance();

                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                string itemId = options.Id.HasValue ? options.Id.Value.ToString(CultureInfo.InvariantCulture) : options.ContentId.ToString();
                string message = string.Format("An exception of type {0} occurred in the SPPermissionsService.ResetInheritance() method for ListId: {1} ItemId: {2}. The exception message is: {3}", ex.GetType(), options.ListId, itemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        private static IEnumerable<RoleAssignment> RoleAssignmentsLoadQuery(SecurableObject obj, ClientContext clientContext)
        {
            IEnumerable<RoleAssignment> listRoles = clientContext.LoadQuery(
                    obj.RoleAssignments.Include(
                        roleAsg => roleAsg.Member,
                        roleAsg => roleAsg.RoleDefinitionBindings.Include(
                            roleDef => roleDef.Id,
                            roleDef => roleDef.Name,
                            roleDef => roleDef.Description)));
            return listRoles;
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
