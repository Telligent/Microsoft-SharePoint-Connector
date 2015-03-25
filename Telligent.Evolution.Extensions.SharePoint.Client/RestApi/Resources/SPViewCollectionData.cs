using System.Collections.Generic;
using Telligent.Evolution.Extensions.SharePoint.Client.Rest.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources
{
    public class SPViewCollectionData
    {
        public SPViewCollectionData()
        {
            Items = new List<RestSPView>();
            TotalCount = 0;
        }

        public SPViewCollectionData(IEnumerable<RestSPView> items, int totalCount)
        {
            Items = new List<RestSPView>(items);
            TotalCount = totalCount;
        }

        public List<RestSPView> Items { get; set; }
        public int TotalCount { get; set; }
    }
}
