using System;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Urls.Routing;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    internal static class RoutingUtility
    {
        public static IContextItem GetItemByContentType(this IContextCollection collection, Guid contentTypeId)
        {
            var item = collection.Find(
                       c => c.ContentTypeId == contentTypeId && string.IsNullOrEmpty(c.Relationship));
            return item;
        }

        public static IContextItem GetItemByApplicationType(this IContextCollection collection, Guid applicationTypeId)
        {
            var item = collection.Find(
                       c => c.ApplicationTypeId == applicationTypeId && string.IsNullOrEmpty(c.Relationship));
            return item;
        }

        public static IContextItem GetItemByContainerType(this IContextCollection collection, Guid containerTypeId)
        {
            var item = collection.Find(
                       c => c.ContainerTypeId == containerTypeId && string.IsNullOrEmpty(c.Relationship));
            return item;
        }

        public static IContextItem GetItemByTypeName(this IContextCollection collection, string typeName)
        {
            var item = collection.Find(
                       c => c.TypeName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
            return item;
        }

        public static Guid GetItemUniqueId(this IListItemDataService listItemDataService, string token, Guid listId)
        {
            if (string.IsNullOrEmpty(token)) return Guid.Empty;

            ItemBase listItem;

            // Try get item by incremental id
            int lookupId;
            if (int.TryParse(token, out lookupId)
                && (listItem = listItemDataService.Get(lookupId, listId)) != null)
            {
                return listItem.UniqueId;
            }

            // Try get item by contentKey
            if ((listItem = listItemDataService.Get(token, listId)) != null)
            {
                return listItem.UniqueId;
            }

            return Guid.Empty;
        }

        public static Guid GetItemUniqueId(this IListItemService listItemService, string token, Guid listId)
        {
            if (string.IsNullOrEmpty(token)) return Guid.Empty;

            SPListItem listItem;

            // Try get item by incremental id
            int lookupId;
            if (int.TryParse(token, out lookupId)
                && (listItem = listItemService.Get(listId, new ItemGetQuery(lookupId))) != null)
            {
                listItemService.Add(listId, new ItemImportQuery(listItem.ContentId));
                return listItem.UniqueId;
            }

            return Guid.Empty;
        }
    }
}
