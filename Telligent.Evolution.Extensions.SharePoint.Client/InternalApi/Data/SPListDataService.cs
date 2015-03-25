using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IListDataService : ICacheable
    {
        void AddUpdate(ListBase list);

        ListBase Get(Guid id);
        ListBase Get(string applicationKey, int groupId);

        List<ListBase> List(int groupId);
        List<ListBase> List(int groupId, Guid typeId);
        List<ListBase> List(Guid typeId);

        void Delete(Guid id);

        PagedList<ListBase> ListsToReindex(Guid applicationTypeId, int pageSize, int pageIndex = 0);

        void UpdateIndexingStatus(Guid[] ids, bool isIndexed);
    }

    internal class SPListDataService : IListDataService
    {
        private readonly ICacheService cacheService;
        private readonly IApplicationKeyValidationService applicationKeyValidator;

        public SPListDataService(IApplicationKeyValidationService applicationKeyValidator, ICacheService cacheService)
        {
            this.applicationKeyValidator = applicationKeyValidator;
            this.cacheService = cacheService;
        }

        public SPListDataService()
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

        public void AddUpdate(ListBase list)
        {
            list.Validate();

            // Make ApplicationKey valid and unique
            if (!string.IsNullOrEmpty(list.ApplicationKey))
            {
                int groupId = list.GroupId;
                list.ApplicationKey = applicationKeyValidator.MakeValid(list.ApplicationKey.ToLowerInvariant(), applicationKey =>
                {
                    // List is duplicate when there is another list with the same application key but different Id
                    var anotherList = Get(applicationKey, groupId);
                    return anotherList != null && anotherList.Id != Guid.Empty && anotherList.Id != list.Id;
                });
            }

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_List_AddUpdate]", connection))
                    {
                        command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = list.Id;
                        command.Parameters.Add("@ApplicationKey", SqlDbType.NVarChar, 256).Value = list.ApplicationKey;
                        command.Parameters.Add("@TypeId", SqlDbType.UniqueIdentifier).Value = list.TypeId;
                        command.Parameters.Add("@GroupId", SqlDbType.Int).Value = list.GroupId;
                        command.Parameters.Add("@SPWebUrl", SqlDbType.NVarChar, 256).Value = list.SPWebUrl;
                        command.Parameters.Add("@ViewId", SqlDbType.UniqueIdentifier).Value = list.ViewId;

                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }

                // Clear Cache
                cacheService.RemoveByTags(new[] { GetTagId(list.Id) }, CacheScope.Context | CacheScope.Process);
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the SPListDataService.AddUpdate() method. The exception message is: {1}", ex.GetType(), ex.Message);
                SPLog.DataProvider(ex, message);
                throw new AddLibraryException(ex.Message, list.SPWebUrl, list.Id, list.GroupId);
            }
        }

        public ListBase Get(Guid id)
        {
            var cacheId = GetCacheId(id);
            var listBase = (ListBase)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (listBase != null) return listBase;

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_List_Get]", connection))
                    {
                        command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = id;

                        connection.Open();

                        using (var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection))
                        {
                            if (reader.HasRows && reader.Read())
                            {
                                listBase = GetListBase(reader);
                                PutInCache(listBase);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the SPListDataService.Get() method for ApplicationId: {1}. The exception message is: {2}", ex.GetType(), id, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(ex.Message, ex.InnerException);
            }
            return listBase;
        }

        public ListBase Get(string applicationKey, int groupId)
        {
            var cacheId = GetCacheId(groupId, applicationKey);
            var listBase = (ListBase)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (listBase != null) return listBase;

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_List_GetByApplicationKey]", connection))
                    {
                        command.Parameters.Add("@ApplicationKey", SqlDbType.NVarChar, 256).Value = applicationKey;
                        command.Parameters.Add("@GroupId", SqlDbType.Int).Value = groupId;

                        connection.Open();

                        using (var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection))
                        {
                            if (reader.Read())
                            {
                                listBase = GetListBase(reader);
                                PutInCache(listBase);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the SPListDataService.Get() method for ApplicationKey: {1}, GroupId: {2}. The exception message is: {3}", ex.GetType(), applicationKey, groupId, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(ex.Message, ex.InnerException);
            }
            return listBase;
        }

        public List<ListBase> List(int groupId)
        {
            var listCollection = new List<ListBase>();

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_List_ListByGroupId]", connection))
                    {
                        command.Parameters.Add("@GroupId", SqlDbType.Int).Value = groupId;

                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {

                                var listBase = GetListBase(reader);
                                PutInCache(listBase);
                                listCollection.Add(listBase);
                            }
                        }

                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the SPListDataService.List() method for GroupId: {1}. The exception message is: {2}", ex.GetType(), groupId, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }

            return listCollection;
        }

        public List<ListBase> List(int groupId, Guid typeId)
        {
            var listCollection = new List<ListBase>();

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_List_ListByGroupIdTypeId]", connection))
                    {
                        command.Parameters.Add("@GroupId", SqlDbType.Int).Value = groupId;
                        command.Parameters.Add("@TypeId", SqlDbType.UniqueIdentifier).Value = typeId;

                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var listBase = GetListBase(reader);
                                PutInCache(listBase);
                                listCollection.Add(listBase);
                            }
                        }

                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the listDataService.List() method for GroupId: {1} and ApplicationTypeId: {2}. The exception message is: {3}", ex.GetType(), groupId, typeId, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }

            return listCollection;
        }

        public List<ListBase> List(Guid typeId)
        {
            var listCollection = new List<ListBase>();

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_List_ListByTypeId]", connection))
                    {

                        command.Parameters.Add("@TypeId", SqlDbType.UniqueIdentifier).Value = typeId;

                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var listBase = GetListBase(reader);
                                PutInCache(listBase);
                                listCollection.Add(listBase);
                            }
                        }

                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the SPListDataService.List() method for ApplicationTypeId: {1}. The exception message is: {2}", ex.GetType(), typeId, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }

            return listCollection;
        }

        public void Delete(Guid id)
        {
            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_List_Delete]", connection))
                    {
                        command.Parameters.Add("@ApplicationId", SqlDbType.UniqueIdentifier).Value = id;

                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                cacheService.RemoveByTags(new[] { GetTagId(id) }, CacheScope.Context | CacheScope.Process);
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the SPListDataService.Delete() method for ApplicationId: {1}. The exception message is: {2}", ex.GetType(), id, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }
        }

        public PagedList<ListBase> ListsToReindex(Guid applicationTypeId, int pageSize, int pageIndex = 0)
        {
            var listCollection = new List<ListBase>();

            int totalCount;
            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_List_GetToReindex]", connection))
                    {
                        command.Parameters.Add("@TypeId", SqlDbType.UniqueIdentifier).Value = applicationTypeId;
                        command.Parameters.Add("@PagingBegin", SqlDbType.Int).Value = pageIndex * pageSize;
                        command.Parameters.Add("@PagingEnd", SqlDbType.Int).Value = (pageIndex + 1) * pageSize;
                        command.Parameters.Add("@TotalRecords", SqlDbType.Int).Direction = ParameterDirection.Output;

                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var listBase = GetListBase(reader);
                                PutInCache(listBase);
                                listCollection.Add(listBase);
                            }
                        }
                        connection.Close();

                        totalCount = (int)command.Parameters["@TotalRecords"].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the SPListDataService.ListsToReindex() method for ApplicationTypeId = {1}. The exception message is: {2}", ex.GetType(), applicationTypeId, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }
            return new PagedList<ListBase>(listCollection, pageSize, pageIndex, totalCount);
        }

        public void UpdateIndexingStatus(Guid[] ids, bool isIndexed)
        {
            if (ids == null || ids.Length <= 0) return;

            var applicationIds = string.Join(",", ids);

            try
            {
                using (var connection = DataHelpers.GetSqlConnection())
                {
                    using (var command = DataHelpers.CreateSprocCommand("[te_SharePoint_List_UpdateIsIndexed]", connection))
                    {
                        connection.Open();
                        command.Parameters.Add("@IsIndexed", SqlDbType.Bit).Value = isIndexed ? 1 : 0;
                        command.Parameters.Add("@ApplicationIds", SqlDbType.NVarChar).Value = applicationIds;

                        command.ExecuteNonQuery();

                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the SPListDataService.ListsToReindex() method for Application Ids: {1}. The exception message is: {2}", ex.GetType(), applicationIds, ex.Message);
                SPLog.DataProvider(ex, message);
                throw new SPDataException(message, ex);
            }
        }

        private void PutInCache(ListBase listBase)
        {
            cacheService.Put(GetCacheId(listBase.GroupId, listBase.ApplicationKey), listBase, CacheScope.Context | CacheScope.Process, new[] { GetTagId(listBase.Id) }, CacheTimeOut);
            cacheService.Put(GetCacheId(listBase.Id), listBase, CacheScope.Context | CacheScope.Process, new[] { GetTagId(listBase.Id) }, CacheTimeOut);
        }

        private static string GetCacheId(Guid applicationId)
        {
            return string.Concat("ListBase:", applicationId.ToString("N"));
        }

        private static string GetCacheId(int groupId, string applicationKey)
        {
            return string.Concat("ListBase:", groupId, ":", applicationKey);
        }

        private static string GetTagId(Guid id)
        {
            return string.Concat("ListBase_Tag:", id.ToString("N"));
        }

        private static ListBase GetListBase(SqlDataReader reader)
        {
            return new ListBase(reader.GetInt("GroupId"), reader.GetGuid("ApplicationId"), reader.GetGuid("TypeId"), reader["SPWebUrl"].ToString())
            {
                ApplicationKey = reader.GetStringOrEmpty("ApplicationKey"),
                ViewId = reader.GetGuid("ViewId")
            };
        }
    }
}
