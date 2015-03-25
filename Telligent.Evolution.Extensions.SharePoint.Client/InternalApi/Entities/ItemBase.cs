using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class ItemBase
    {
        public ItemBase() { }

        public ItemBase(Guid applicationId, Guid uniqueId, int id, DateTime modified)
        {
            ApplicationId = applicationId;
            UniqueId = uniqueId;
            Id = id;
            ModifiedDate = modified;
            IsIndexable = true;
        }

        public int Id { get; private set; }
        public Guid UniqueId { get; private set; }
        public Guid ApplicationId { get; private set; }
        public string ContentKey { get; set; }

        // TODO: Remove ModifiedDate, it should be created in DB
        public DateTime ModifiedDate { get; private set; }

        // TODO: Remove IsIndexable, when folders become indexable
        public bool IsIndexable { get; set; }

    }
}
