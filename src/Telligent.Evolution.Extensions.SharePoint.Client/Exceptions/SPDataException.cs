using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Exceptions
{
    public class SPDataException : SPInternalException
    {
        public SPDataException(string message)
            : base(message)
        {
        }

        public SPDataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        // The name of a stored procedure, that leads to exception
        public string SProcName { get; set; }
    }
}
