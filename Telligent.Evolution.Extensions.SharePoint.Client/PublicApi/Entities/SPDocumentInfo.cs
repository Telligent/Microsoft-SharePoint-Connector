using System;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using File = Microsoft.SharePoint.Client.File;
using User = Microsoft.SharePoint.Client.User;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPDocumentInfo : ApiEntity
    {
        public SPDocumentInfo() { }
        public SPDocumentInfo(List spList, File spFile)
        {
            IsCheckedOut = !(bool)(spFile.CheckedOutByUser.ServerObjectIsNull ?? true);
            EnableVersioning = spList.EnableVersioning;
            EnableMinorVersions = spList.EnableMinorVersions;
            MajorVersion = spFile.MajorVersion;
            MinorVersion = spFile.MinorVersion;
        }
        public SPDocumentInfo(List spList, File spFile, User spUser)
            : this(spList, spFile)
        {
            try
            {
                CheckedOutByUser = new SPUserPrincipal(spUser);
            }
            catch (Exception ex)
            {
                CheckedOutByUser = new SPUserPrincipal(new[] { new Error(ex.GetType().ToString(), ex.Message) });
            }
        }

        public SPUserPrincipal CheckedOutByUser { get; set; }
        public bool IsCheckedOut { get; set; }
        public bool EnableVersioning { get; set; }
        public bool EnableMinorVersions { get; set; }
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
    }
}
