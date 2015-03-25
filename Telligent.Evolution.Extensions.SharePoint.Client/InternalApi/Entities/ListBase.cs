using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class ListBase
    {
        public ListBase() { }

        public ListBase(int groupId, Guid id, Guid typeId, string spwebUrl)
        {
            GroupId = groupId;
            Id = id;
            TypeId = typeId;
            SPWebUrl = spwebUrl;
        }

        public int GroupId { get; private set; }
        public Guid Id { get; private set; }
        public string ApplicationKey { get; set; }
        public Guid TypeId { get; private set; }
        public string SPWebUrl { get; private set; }
        public Guid ViewId { get; set; }
    }
}
