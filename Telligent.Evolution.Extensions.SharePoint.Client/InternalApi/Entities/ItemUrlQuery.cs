using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities
{
    internal class ItemUrlQuery
    {
        public static implicit operator ItemUrlQuery(ItemBase itemBase)
        {
            return new ItemUrlQuery(itemBase.Id, itemBase.UniqueId)
            {
                Key = itemBase.ContentKey
            };
        }

        public ItemUrlQuery(int id, Guid uniqueId)
        {
            Id = id;
            UniqueId = uniqueId;
        }

        public int Id { get; private set; }
        public Guid UniqueId { get; private set; }
        public string Key { get; set; }
    }
}
