using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Exceptions
{
    public class SPFileAlreadyExistsException : SPInternalException
    {
        public SPFileAlreadyExistsException(string message)
            : base(message)
        {
        }

        public SPFileAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public string FilePath { get; set; }
    }
}
