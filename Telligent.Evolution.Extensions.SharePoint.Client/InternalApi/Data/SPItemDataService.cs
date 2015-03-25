using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IListItemDataService : ICacheable
    {
        void AddUpdate(ItemBase item);
        void AddUpdate(IEnumerable<ItemBase> items);

        ItemBase Get(Guid contentId);
        ItemBase Get(string contentKey, Guid applicationId);
        ItemBase Get(int itemId, Guid applicationId);

        List<ItemBase> List(Guid applicationId);

        void Delete(Guid contentId);

        PagedList<ItemBase> ListItemsToReindex(int batchSize, Guid[] enabledListIds);

        void UpdateIndexingStatus(Guid[] ids, bool isIndexed);
    }

    internal class SPItemDataService : IListItemDataService
    {
        private readonly ICacheService cacheService;
        private readonly IApplicationKeyValidationService applicationKeyValidator;

        public SPItemDataService(IApplicationKeyValidationService applicationKeyValidator, ICacheService cacheService)
        {
            this.applicationKeyValidator = applicationKeyValidator;
            this.cacheService = cacheService;
        }

        public SPItemDataService()
            : this(ServiceLocator.Get<IApplicationKeyValidationService>(), ServiceLocator.Get<ICacheService>())
        {
        }

        #region ICacheService

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        #endregion

        public void AddUpdate(ItemBase item)
        {
            item.Validate();

            // Clear Cache
            cacheService.RemoveByTags(new[] { GetTagId(item.UniqueId) }, CacheScope.Context | CacheScope.Process);

            // Make ContentKey valid and unique
            if (!string.IsNullOrEmpty(item.ContentKey))
            {
                var applicationId = item.ApplicationId;
                item.ContentKey = applicationKeyValidator.MakeValid(item.ContentKey.ToLowerInvariant(), contentKey =>
                {
                    // Item is a duplicate when there is another item with the same key but different Id
                    var anotherItem = Get(contentKey, applicationId);
                    return anotherItem != null && anotherItem.UniqueId != Guid.Empty && anotherItem.UniqueId != item.UniqueId;
                });
            }

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_Item_AddUpdate]", connection))
                    {
                        command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = item.ApplicationId;
                        command.Parameters.Add("@ContentKey", SqlDbType.NVarChar, 256).Value = item.ContentKey;
                        command.Parameters.Add("@ContentId", SqlDbType.UniqueIdentifier).Value = item.UniqueId;
                        command.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.Id;
                        command.Parameters.Add("@IsIndexed", SqlDbType.Int).Value = item.IsIndexable ? 0 : -1;

                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the SPItemDataService.AddUpdate() method. The exception message is: {1}", ex.GetType(), ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }
        }

        public void AddUpdate(IEnumerable<ItemBase> items)
        {
            var itemList = items.ToList();
            foreach (var item in itemList)
            {
                item.Validate();

                // Clear Cache
                cacheService.RemoveByTags(new[] { GetTagId(item.UniqueId) }, CacheScope.Context | CacheScope.Process);

                // Make ContentKey valid and unique
                if (!string.IsNullOrEmpty(item.ContentKey))
                {
                    var applicationId = item.ApplicationId;
                    item.ContentKey = applicationKeyValidator.MakeValid(item.ContentKey.ToLowerInvariant(), contentKey =>
                    {
                        // Item is a duplicate when there is another item with the same key but different Id
                        var anotherItem = Get(contentKey, applicationId);
                        return (anotherItem != null && anotherItem.UniqueId != Guid.Empty && anotherItem.UniqueId != item.UniqueId)
                            || itemList.Any(newItem => newItem != null && newItem.UniqueId != item.UniqueId && newItem.ApplicationId == applicationId && string.Compare(newItem.ContentKey, contentKey, StringComparison.InvariantCultureIgnoreCase) == 0);
                    });
                }
            }

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_Item_AddBatch]", connection))
                    {
                        var xitems = new XDocument(
                            new XDeclaration("1.0", "utf-8", "yes"),
                            new XElement("items",
                                itemList.Select(item => new XElement("item",
                                    new XAttribute("applicationId", item.ApplicationId),
                                    new XAttribute("id", item.Id),
                                    new XAttribute("contentId", item.UniqueId),
                                    new XAttribute("contentKey", item.ContentKey ?? string.Empty),
                                    new XAttribute("isIndexed", item.IsIndexable ? 0 : -1)))
                            )
                        );

                        var itemsXml = new StringBuilder();
                        using (TextWriter writer = new StringWriter(itemsXml))
                        {
                            xitems.Save(writer);
                        }

                        command.Parameters.Add("@ItemsXml", SqlDbType.Xml).Value = itemsXml.ToString();

                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the SPItemDataService.AddUpdate() method. The exception message is: {1}", ex.GetType(), ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }
        }

        public ItemBase Get(Guid contentId)
        {
            var cacheId = GetCacheId(contentId);
            var itemBase = (ItemBase)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (itemBase != null) return itemBase;

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    connection.Open();
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_Item_Get]", connection))
                    {
                        command.Parameters.Add("@ContentId", SqlDbType.UniqueIdentifier).Value = contentId;

                        using (var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection))
                        {
                            if (reader.HasRows && reader.Read())
                            {
                                itemBase = GetItemBase(reader);
                                PutInCache(itemBase);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemDataService.Get() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }

            return itemBase;
        }

        public ItemBase Get(string contentKey, Guid applicationId)
        {
            var cacheId = GetCacheId(applicationId, contentKey);
            var itemBase = (ItemBase)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (itemBase == null)
            {
                try
                {
                    using (var connection = DataHelpers.GetSqlConnection())
                    {
                        connection.Open();
                        using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_Item_GetByContentKey]", connection))
                        {
                            command.Parameters.Add("@ContentKey", SqlDbType.NVarChar, 256).Value = contentKey;
                            command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = applicationId;
                            using (var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection))
                            {
                                if (reader.HasRows && reader.Read())
                                {
                                    itemBase = GetItemBase(reader);
                                    PutInCache(itemBase);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the SPItemDataService.Get() method for ContentKey: {1}, ApplicationId: {2}. The exception message is: {3}", ex.GetType(), contentKey, applicationId, ex.Message);
                    SPLog.DataProvider(ex, message);
                    throw new SPDataException(message, ex);
                }
            }
            return itemBase;
        }

        public ItemBase Get(int itemId, Guid applicationId)
        {
            var cacheId = GetCacheId(applicationId, itemId.ToString());
            var itemBase = (ItemBase)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (itemBase == null)
            {
                try
                {
                    using (var connection = DataHelpers.GetSqlConnection())
                    {
                        connection.Open();
                        using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_Item_GetByItemId]", connection))
                        {
                            command.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemId;
                            command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = applicationId;
                            using (var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection))
                            {
                                if (reader.HasRows && reader.Read())
                                {
                                    itemBase = GetItemBase(reader);
                                    PutInCache(itemBase);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the SPItemDataService.Get() method for ItemId: {1}, ApplicationId: {2}. The exception message is: {3}", ex.GetType(), itemId, applicationId, ex.Message);
                    SPLog.DataProvider(ex, message);
                    throw new SPDataException(message, ex);
                }
            }
            return itemBase;
        }

        private void PutInCache(ItemBase itemBase)
        {
            cacheService.Put(GetCacheId(itemBase.UniqueId), itemBase, CacheScope.Context | CacheScope.Process, new[] { GetTagId(itemBase.UniqueId) });
            cacheService.Put(GetCacheId(itemBase.ApplicationId, itemBase.ContentKey), itemBase, CacheScope.Context | CacheScope.Process, new[] { GetTagId(itemBase.UniqueId) });
            cacheService.Put(GetCacheId(itemBase.ApplicationId, itemBase.Id.ToString()), itemBase, CacheScope.Context | CacheScope.Process, new[] { GetTagId(itemBase.UniqueId) });
        }

        public List<ItemBase> List(Guid applicationId)
        {
            var items = new List<ItemBase>();

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    connection.Open();
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_Item_List]", connection))
                    {
                        command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = applicationId;

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.HasRows && reader.Read())
                            {
                                var itemBase = GetItemBase(reader);
                                items.Add(itemBase);
                                PutInCache(itemBase);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the SPItemDataService.List() method for ApplicationId: {1}. The exception message is: {2}", ex.GetType(), applicationId, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }

            return items;
        }

        public void Delete(Guid contentId)
        {
            try
            {
                // Remove from DB
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_Item_Delete]", connection))
                    {
                        command.Parameters.Add("@ContentId", SqlDbType.UniqueIdentifier).Value = contentId;

                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                // Clear Cache
                cacheService.RemoveByTags(new[] { GetTagId(contentId) }, CacheScope.Context | CacheScope.Process);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPItemDataService.Delete() method for ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }
        }

        public PagedList<ItemBase> ListItemsToReindex(int batchSize, Guid[] enabledListIds)
        {
            if (enabledListIds == null || enabledListIds.Length == 0) return new PagedList<ItemBase>();

            var items = new List<ItemBase>();

            int totalCount;
            var applicationIds = string.Join(", ", enabledListIds);
            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_Item_GetToReindex]", connection))
                    {
                        command.Parameters.Add("@EnabledListIds", SqlDbType.NVarChar).Value = string.Join(",", enabledListIds);
                        command.Parameters.Add("@PagingBegin", SqlDbType.Int).Value = 0;
                        command.Parameters.Add("@PagingEnd", SqlDbType.Int).Value = batchSize;
                        command.Parameters.Add("@TotalRecords", SqlDbType.Int).Direction = ParameterDirection.Output;

                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.HasRows && reader.Read())
                            {
                                var itemBase = GetItemBase(reader);
                                PutInCache(itemBase);
                                items.Add(itemBase);
                            }
                        }
                        totalCount = (int)command.Parameters["@TotalRecords"].Value;
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the SPItemDataService.ListItemsToReindex() method for ListIds: {1} batchSize: {2}. The exception message is: {3}", ex.GetType(), applicationIds, batchSize, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }

            return new PagedList<ItemBase>(items, batchSize, 0, totalCount);
        }

        public void UpdateIndexingStatus(Guid[] ids, bool isIndexed)
        {
            if (ids == null || ids.Length == 0) return;

            var contentIds = string.Join(",", ids);

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_Item_UpdateIsIndexed]", connection))
                    {
                        command.Parameters.Add("@IsIndexed", SqlDbType.Int).Value = isIndexed ? 1 : 0;
                        command.Parameters.Add("@ContentIds", SqlDbType.NVarChar).Value = contentIds;

                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the PItemDataService.UpdateIndexingStatus() method for Content Ids: {1} isIndexed: {2}. The exception message is: {3}", ex.GetType(), contentIds, isIndexed, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }
        }

        private static string GetCacheId(Guid contentId)
        {
            return string.Concat("ItemBase:", contentId.ToString("N"));
        }

        private static string GetCacheId(Guid applicationId, string contentKey)
        {
            return string.Concat("ItemBase:", applicationId.ToString("N"), ":", contentKey);
        }

        private static string GetTagId(Guid contentId)
        {
            return string.Concat("ItemBase_Tag:", contentId.ToString("N"));
        }

        private static ItemBase GetItemBase(SqlDataReader reader)
        {
            return new ItemBase(reader.GetGuid("ApplicationId"), reader.GetGuid("ContentId"), reader.GetInt("ItemId"), reader.GetDate("UpdatedDate"))
            {
                ContentKey = reader.GetStringOrEmpty("ContentKey")
            };
        }
    }
}
