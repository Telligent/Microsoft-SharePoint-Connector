using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Exceptions
{
    public class AddLibraryException : SPDataException
    {
        public AddLibraryException(string message, Exception innerException) : base(message, innerException) { }
        public AddLibraryException(string message, string spwebUrl, Guid libraryId, int groupId)
            : base(message)
        {
            SPWebUrl = spwebUrl;
            LibraryId = libraryId;
            GroupId = groupId;
        }

        public AddLibraryException(string message, string spwebUrl, Guid libraryId, int groupId, Exception innerException)
            : base(message, innerException)
        {
            SPWebUrl = spwebUrl;
            LibraryId = libraryId;
            GroupId = groupId;
        }

        public string SPWebUrl { get; private set; }

        public Guid LibraryId { get; private set; }

        public int GroupId { get; private set; }
    }
}
