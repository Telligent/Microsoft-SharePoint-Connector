using System;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using File = Microsoft.SharePoint.Client.File;
using User = Microsoft.SharePoint.Client.User;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPDocumentVersion : ApiEntity
    {
        private string spwebUrl;

        public string FileName { get; set; }
        public bool IsCurrentVersion { get; set; }
        public int ID { get; private set; }
        public string CheckInComment { get; private set; }
        public DateTime Created { get; private set; }
        public string CreatedBy { get; private set; }
        public User Profile { get; private set; }
        public string Url { get; private set; }
        public string VersionLabel { get; private set; }
        public int Size { get; set; }

        public SPDocumentVersion() { }

        public SPDocumentVersion(FileVersion version, string fileName, string parentWebUrl)
            : this()
        {
            spwebUrl = version.Context.Url;
            ID = version.ID;
            CheckInComment = version.CheckInComment;
            var fileServerRelativeUrl = version.Url.TrimStart('/');
            if (fileServerRelativeUrl.StartsWith(parentWebUrl.TrimStart('/')))
            {
                fileServerRelativeUrl = fileServerRelativeUrl.Substring(parentWebUrl.TrimStart('/').Length).TrimStart('/');
            }
            Url = string.Concat(spwebUrl, '/', fileServerRelativeUrl);
            Created = version.Created;
            CreatedBy = SiteUrls.Instance().UserProfile(version.CreatedBy.Title);
            Profile = version.CreatedBy;
            VersionLabel = version.VersionLabel;
            IsCurrentVersion = version.IsCurrentVersion;
            FileName = fileName;
        }

        public SPDocumentVersion(File file, string fileName, string parentWebUrl)
        {
            spwebUrl = file.Context.Url;
            CheckInComment = file.CheckInComment;
            var fileServerRelativeUrl = file.ServerRelativeUrl.TrimStart('/');
            if (fileServerRelativeUrl.StartsWith(parentWebUrl.TrimStart('/')))
            {
                fileServerRelativeUrl = fileServerRelativeUrl.Substring(parentWebUrl.TrimStart('/').Length).TrimStart('/');
            }
            Url = string.Concat(file.Context.Url.TrimEnd('/'), '/', fileServerRelativeUrl);
            Created = file.TimeLastModified;
            CreatedBy = SiteUrls.Instance().UserProfile(file.ModifiedBy.Title);
            Profile = file.ModifiedBy;
            VersionLabel = file.UIVersionLabel;
            IsCurrentVersion = true;
            FileName = fileName;
        }
    }

}
