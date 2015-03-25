using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using User = Microsoft.SharePoint.Client.User;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPDocumentCheckOutInfo : IApiEntity
    {
        public bool IsCheckedOut { get; set; }
        public User CheckedOutByUser { get; set; }
        public bool EnableVersioning { get; set; }
        public bool EnableMinorVersions { get; set; }
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }

        #region IApiEntity Members

        public IList<Error> Errors
        {
            get { throw new NotImplementedException(); }
        }

        public IList<Warning> Warnings
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}
