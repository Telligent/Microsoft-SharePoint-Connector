using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IIndexable<T>
    {
        void UpdateIndexingStatus(Guid[] contentIds, bool isIndexed);
        PagedList<T> ListItemsToReindex(Guid typeId, int batchSize, string[] viewFields = null);
    }

    internal interface IExternalSource<in S, T>
    {
        List<T> ListItemsFromExternalSource(S source);
    }
}
