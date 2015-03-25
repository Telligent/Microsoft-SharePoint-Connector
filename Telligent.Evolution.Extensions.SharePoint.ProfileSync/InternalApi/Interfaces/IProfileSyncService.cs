using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi.Entities;

namespace Telligent.Evolution.Extensions.SharePoint.ProfileSync.InternalApi
{
    public interface IProfileSyncService : IDisposable
    {
        bool Enabled { get; }

        IEnumerable<UserFieldMapping> Fields { get; }

        List<User> List(IEnumerable<string> emails);

        void Update(User mergeUser, IEnumerable<string> fields);
    }

    public interface IFullProfileSyncService : IProfileSyncService, IDisposable
    {
        List<User> List(ref int nextIndex);
    }

    public interface IIncrementalProfileSyncService : IProfileSyncService, IDisposable
    {
        List<User> List(DateTime date);
    }
}