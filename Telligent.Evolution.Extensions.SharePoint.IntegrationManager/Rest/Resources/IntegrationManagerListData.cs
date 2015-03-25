using System.Collections.Generic;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Resources
{
    public class IntegrationManagerListData
    {
        public IntegrationManagerListData()
            : base()
        {
            Items = new List<RestIntegrationManager>();
            TotalCount = 0;
        }

        public IntegrationManagerListData(IEnumerable<RestIntegrationManager> items, int totalCount)
        {
            Items = new List<RestIntegrationManager>(items);
            TotalCount = totalCount;
        }

        public List<RestIntegrationManager> Items { get; set; }
        public int TotalCount { get; set; }
    }
}
