using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities
{
    internal class ListUrlQuery
    {
        public static implicit operator ListUrlQuery(ListBase listBase)
        {
            return new ListUrlQuery(listBase.GroupId, listBase.Id)
            {
                ApplicationKey = listBase.ApplicationKey
            };
        }

        public ListUrlQuery(int groupId, Guid id)
        {
            GroupId = groupId;
            Id = id;
        }

        public int GroupId { get; private set; }
        public Guid Id { get; private set; }
        public string ApplicationKey { get; set; }
    }
}
