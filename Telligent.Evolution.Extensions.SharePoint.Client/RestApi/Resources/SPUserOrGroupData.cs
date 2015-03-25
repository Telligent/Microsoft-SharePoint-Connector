using System;
using Telligent.Evolution.Extensions.SharePoint.Client.Rest.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Resources
{
    public class SPUserOrGroupData
    {
        public Guid SiteId { get; set; }
        public Guid WebId { get; set; }
        public string Url { get; set; }
        public RestSPUserOrGroup[] List { get; set; }
    }
}
