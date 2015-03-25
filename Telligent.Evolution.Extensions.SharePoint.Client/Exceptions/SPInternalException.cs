using System;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Exceptions
{
    public class SPInternalException : Exception
    {
        public SPInternalException(string message) : base(message) { }
        public SPInternalException(string message, Exception innerException) : base(message, innerException) { }
    }
}
