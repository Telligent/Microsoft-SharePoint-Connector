using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api
{
    public interface ICacheable
    {
        TimeSpan CacheTimeOut { get; set; }
    }
}
