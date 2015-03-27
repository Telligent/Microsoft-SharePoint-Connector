using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;
using System;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Entities
{
    public class RestIntegrationManager
    {
        public RestIntegrationManager()
        {
        }

        public RestIntegrationManager(IntegrationProvider manager)
        {
            Id = manager.Id;
            SPSiteId = manager.SPSiteID;
            SPWebId = manager.SPWebID;
            SPSiteUrl = manager.SPSiteURL;
            SPSiteName = manager.SPSiteName;
            TEGroupId = manager.TEGroupId;
            TEGroupName = manager.TEGroupName;
            IsDefault = manager.IsDefault;
        }

        public string Id { get; set; }
        public Guid SPSiteId { get; set; }
        public Guid SPWebId { get; set; }
        public string SPSiteUrl { get; set; }
        public string SPSiteName { get; set; }
        public int TEGroupId { get; set; }
        public string TEGroupName { get; set; }
        public bool IsDefault { get; set; }
    }
}
