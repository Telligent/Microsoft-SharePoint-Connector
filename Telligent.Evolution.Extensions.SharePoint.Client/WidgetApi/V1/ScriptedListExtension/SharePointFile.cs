using Microsoft.SharePoint.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Telligent.Evolution.Extensions.SharePoint.WebServices;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointFileExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_file"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointFile>(); }
        }

        public string Name
        {
            get { return "SharePoint File Extension (sharepoint_v1_file)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to work with files on the SharePoint side."; }
        }

        public void Initialize() { }
    }


    public interface ISharePointFile : ICacheable
    {
        bool IsCheckedOut(SPList list, SPListItem listItem);

        void CheckOutFile(SPList list, SPListItem listItem);

        void CheckInFile(SPList list, SPListItem listItem);

        void UndoCheckOut(SPList list, SPListItem listItem);

        SPDocumentVersionList Versions(SPList list, SPListItem listItem, IDictionary options);

        void RestoreVersion(SPList list, string fileName, string fileVersion);

        SPDocumentCheckOutInfo GetFileInfo(SPList list, SPListItem listItem);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointFile : ISharePointFile
    {
        private readonly ICredentialsManager credentials;
        private readonly ICacheService cacheService;

        public SharePointFile(): this(ServiceLocator.Get<ICredentialsManager>(), ServiceLocator.Get<ICacheService>()){}
        internal SharePointFile(ICredentialsManager credentials, ICacheService cacheService)
        {
            this.credentials = credentials;
            this.cacheService = cacheService;
        }

        private TimeSpan cacheTimeOut = TimeSpan.FromSeconds(15);
        public TimeSpan CacheTimeOut
        {
            get { return cacheTimeOut; }
            set { cacheTimeOut = value; }
        }

        [Obsolete("Use sharepoint_v2_file", true)]
        public bool IsCheckedOut(SPList list, SPListItem listItem)
        {
            var cacheId = string.Format("SharePointFile:{0}_{1}", list.Id, listItem.Id);
            var isCheckedOut = (bool?)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (isCheckedOut == null)
            {
                using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
                {
                    var spfile = clientContext.ToFile(list.Id, listItem.Id);
                    clientContext.Load(spfile, item => item.CheckedOutByUser);
                    clientContext.ExecuteQuery();
                    isCheckedOut = !(spfile.CheckedOutByUser.ServerObjectIsNull ?? true);
                    cacheService.Put(cacheId, isCheckedOut, CacheScope.Context | CacheScope.Process, new string[0], CacheTimeOut);
                }
            }
            return isCheckedOut.Value;
        }

        [Obsolete("Use sharepoint_v2_file", true)]
        public void CheckOutFile(SPList list, SPListItem listItem)
        {
            RemoveFileCache(list, listItem);

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                var spfile = clientContext.ToList(list.Id).GetItemById(listItem.Id).File;
                spfile.CheckOut();
                clientContext.ExecuteQuery();
            }
        }

        [Obsolete("Use sharepoint_v2_file", true)]
        public void CheckInFile(SPList list, SPListItem listItem)
        {
            RemoveFileCache(list, listItem);

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                var spfile = clientContext.ToList(list.Id).GetItemById(listItem.Id).File;
                spfile.CheckIn(String.Empty, CheckinType.MajorCheckIn);
                clientContext.ExecuteQuery();
            }
        }

        [Obsolete("Use sharepoint_v2_file", true)]
        public void CheckInFile(SPList list, SPListItem listItem, string checkinType, bool keepCOut, string comment)
        {
            RemoveFileCache(list, listItem);

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                var spfile = clientContext.ToList(list.Id).GetItemById(listItem.Id).File;
                var type = (CheckinType)Enum.Parse(typeof(CheckinType), checkinType);
                spfile.CheckIn(comment, type);
                if (keepCOut)
                    spfile.CheckOut();
                clientContext.ExecuteQuery();
            }
        }

        [Obsolete("Use sharepoint_v2_file", true)]
        public void UndoCheckOut(SPList list, SPListItem listItem)
        {
            RemoveFileCache(list, listItem);

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                var spfile = clientContext.ToList(list.Id).GetItemById(listItem.Id).File;
                spfile.UndoCheckOut();
                clientContext.ExecuteQuery();
            }
        }

        [Obsolete("Use sharepoint_v2_file", true)]
        public SPDocumentCheckOutInfo GetFileInfo(SPList list, SPListItem listItem)
        {
            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                var spList = clientContext.ToList(list.Id);
                var spfile = spList.GetItemById(listItem.Id).File;
                clientContext.Load(spList);
                clientContext.Load(spfile);
                clientContext.Load(spfile, f => f.CheckedOutByUser);
                clientContext.ExecuteQuery();
                User user = null;
                if (!spfile.CheckedOutByUser.ServerObjectIsNull.GetValueOrDefault(true))
                {
                    user = spfile.CheckedOutByUser;
                    clientContext.Load(user);
                    clientContext.ExecuteQuery();
                }
                return new SPDocumentCheckOutInfo
                {
                    IsCheckedOut = !(spfile.CheckedOutByUser.ServerObjectIsNull ?? true),
                    CheckedOutByUser = user,
                    EnableVersioning = spList.EnableVersioning,
                    EnableMinorVersions = spList.EnableMinorVersions,
                    MajorVersion = spfile.MajorVersion,
                    MinorVersion = spfile.MinorVersion
                };
            }
        }

        [Obsolete("Use sharepoint_v2_file", true)]
        public SPDocumentVersionList Versions(SPList list, SPListItem listItem,
                   [Documentation(Name = "PageSize", Type = typeof(int)),
                   Documentation(Name = "PageIndex", Type = typeof(int))]
            IDictionary options)
        {
            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                var splist = clientContext.Web.Lists.GetById(list.Id);
                clientContext.Load(splist, item => item.ParentWebUrl);

                var splistItem = splist.GetItemById(listItem.Id);
                var spfile = splistItem.File;
                clientContext.Load(spfile);
                clientContext.Load(spfile, item => item.ModifiedBy, item => item.TimeLastModified);
                var versions = clientContext.LoadQuery(spfile.Versions.IncludeWithDefaultProperties(item => item.CreatedBy, item => item.IsCurrentVersion));
                clientContext.ExecuteQuery();

                string fileName = spfile.ServerRelativeUrl;

                // There is no way to get file size using Client Object Model
                var fileSize = new Dictionary<string, int>();
                using (var service = new VersionService(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
                {
                    var nodeList = service.GetVersions(fileName);
                    foreach (XmlNode node in nodeList.ChildNodes)
                    {
                        if (node.Name == "result" && node.Attributes != null)
                        {
                            string v = node.Attributes["version"].Value.Replace("@", "");
                            int size;
                            int.TryParse(node.Attributes["size"].Value, out size);
                            fileSize.Add(v, size);
                        }
                    }
                }

                var result = new List<SPDocumentVersion>
                {
                    new SPDocumentVersion(spfile, fileName, splist.ParentWebUrl)
                    {
                        Size = fileSize[spfile.UIVersionLabel]
                    }
                };
                result.AddRange(versions.Select(version => new SPDocumentVersion(version, fileName, splist.ParentWebUrl)
                {
                    Size = fileSize[version.VersionLabel]
                }));

                // pagination
                var versioning = new SPDocumentVersionList { TotalCount = result.Count };
                int pageSize = 10;
                if (options != null && options["PageSize"] != null)
                {
                    int.TryParse(options["PageSize"].ToString(), out pageSize);
                }
                int pageIndex = 0;
                if (options != null && options["PageIndex"] != null)
                {
                    int.TryParse(options["PageIndex"].ToString(), out pageIndex);
                }
                int startIndex = pageIndex * pageSize;
                int count = Math.Min(versioning.TotalCount - startIndex, pageSize);
                var comparer = new SPDocumentVersionComparer();
                versioning.AddRange(result.OrderByDescending(item => item.VersionLabel, comparer).ToList().GetRange(startIndex, count));
                return versioning;
            }
        }

        [Obsolete("Use sharepoint_v2_file", true)]
        public void RestoreVersion(SPList list, string fileName, string fileVersion)
        {
            // There is no way to restore file using Client Object Model
            using (var service = new VersionService(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                service.RestoreVersion(fileName, fileVersion);
            }
        }

        private void RemoveFileCache(SPList list, SPListItem listItem)
        {
            cacheService.Remove(string.Format("SharePointFile:{0}_{1}", list.Id, listItem.Id), CacheScope.Context | CacheScope.Process);
        }
    }
}