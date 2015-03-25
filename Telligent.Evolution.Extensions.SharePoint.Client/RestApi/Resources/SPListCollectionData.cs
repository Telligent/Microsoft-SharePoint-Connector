using System.Collections.Generic;
using Telligent.Evolution.Extensions.SharePoint.Client.Rest.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources
{
    public class SPListCollectionData
    {
        public SPListCollectionData()
        {
            Items = new List<RestSPList>();
            TotalCount = 0;
        }

        public SPListCollectionData(IEnumerable<RestSPList> items, int totalCount)
        {
            Items = new List<RestSPList>(items);
            TotalCount = totalCount;
        }

        public List<RestSPList> Items { get; set; }
        public int TotalCount { get; set; }
    }
}
