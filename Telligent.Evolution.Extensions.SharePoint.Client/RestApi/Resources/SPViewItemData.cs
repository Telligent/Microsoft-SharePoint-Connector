using System;
using Telligent.Evolution.Extensions.SharePoint.Client.Rest.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources
{
    public class SPViewItemData
    {
        public Guid SiteId { get; set; }
        public Guid WebId { get; set; }
        public RestSPView Item { get; set; }
    }
}
