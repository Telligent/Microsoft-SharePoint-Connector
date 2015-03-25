using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class Folder : ApiEntity
    {
        public Folder() { }

        public Folder(AdditionalInfo additionalInfo) : base(additionalInfo) { }

        public Folder(IList<Warning> warnings, IList<Error> errors) : base(warnings, errors) { }

        public Folder(string name, string path, int itemCount, Guid libraryId)
            : this()
        {
            LibraryId = libraryId;

            Name = name;
            Path = path;
            ItemCount = itemCount;
        }

        public Guid LibraryId { get; private set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int ItemCount { get; set; }
    }
}
