using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.WebServices;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class FileCheckInOptions
    {
        public static implicit operator FileCheckInOptions(DocumentCheckInOptions options)
        {
            return new FileCheckInOptions
                {
                    CheckinType = options.CheckinType,
                    Comment = options.Comment,
                    KeepCheckOut = options.KeepCheckOut
                };
        }

        public bool KeepCheckOut { get; set; }
        public string CheckinType { get; set; }
        public string Comment { get; set; }
    }

    internal interface IFileService
    {
        void CheckIn(string url, Guid libraryId, int itemId, FileCheckInOptions options);
        void CheckOut(string url, Guid libraryId, int itemId);
        void UndoCheckOut(string url, Guid libraryId, int itemId);
        void Restore(string url, Guid libraryId, int itemId, string version);
        bool IsCheckedOut(string url, Guid libraryId, int itemId);
        List<SPDocumentVersion> GetVersions(string url, Guid libraryId, int itemId);
        SPDocumentInfo GetDetails(string url, Guid libraryId, int itemId);
    }

    internal class SPFileService : IFileService
    {
        private readonly ICredentialsManager credentials;

        public SPFileService()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        public SPFileService(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public void CheckIn(string url, Guid libraryId, int itemId, FileCheckInOptions options)
        {
            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var spfile = clientContext.Web.Lists.GetById(libraryId).GetItemById(itemId).File;
                    var type = !string.IsNullOrEmpty(options.CheckinType) ? (CheckinType)Enum.Parse(typeof(CheckinType), options.CheckinType) : CheckinType.MajorCheckIn;

                    spfile.CheckIn(options.Comment, type);

                    if (options.KeepCheckOut)
                    {
                        spfile.CheckOut();
                    }

                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPFileService.CheckIn() method for URL: {1}, LibraryId: {2}, ItemId: {3}. The exception message is: {4}", ex.GetType(), url, libraryId, itemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message);
            }
        }

        public void CheckOut(string url, Guid libraryId, int itemId)
        {
            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var spfile = clientContext.Web.Lists.GetById(libraryId).GetItemById(itemId).File;
                    spfile.CheckOut();

                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPFileService.CheckOut() method for URL: {1}, LibraryId: {2}, ItemId: {3}. The exception message is: {4}", ex.GetType(), url, libraryId, itemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message);
            }
        }

        public void UndoCheckOut(string url, Guid libraryId, int itemId)
        {
            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var spfile = clientContext.Web.Lists.GetById(libraryId).GetItemById(itemId).File;
                    spfile.UndoCheckOut();

                    clientContext.ExecuteQuery();
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPFileService.UndoCheckOut() method for URL: {1}, LibraryId: {2}, ItemId: {3}. The exception message is: {4}", ex.GetType(), url, libraryId, itemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message);
            }
        }

        public void Restore(string url, Guid libraryId, int itemId, string version)
        {
            try
            {
                var auth = credentials.Get(url);
                using (var clientContext = new SPContext(url, auth))
                {
                    var spfile = clientContext.Web.Lists.GetById(libraryId).GetItemById(itemId).File;
                    clientContext.Load(spfile, f => f.ServerRelativeUrl);
                    clientContext.ExecuteQuery();

                    var fileName = spfile.ServerRelativeUrl;
                    // There is no way to restore file using Client Object Model
                    using (var service = new VersionService(url, auth))
                    {
                        service.RestoreVersion(fileName, version);
                    }
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPFileService.Restore() method for URL: {1}, LibraryId: {2}, ItemId: {3}, Version: {4}. The exception message is: {5}", ex.GetType(), url, libraryId, itemId, version, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message);
            }
        }

        public bool IsCheckedOut(string url, Guid libraryId, int itemId)
        {
            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var spfile = clientContext.Web.Lists.GetById(libraryId).GetItemById(itemId).File;
                    clientContext.Load(spfile, i => i.CheckedOutByUser);
                    clientContext.ExecuteQuery();

                    return !(spfile.CheckedOutByUser.ServerObjectIsNull ?? true);
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPFileService.IsCheckedOut() method for URL: {1}, LibraryId: {2}, ItemId: {3}. The exception message is: {4}", ex.GetType(), url, libraryId, itemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message);
            }
        }

        public List<SPDocumentVersion> GetVersions(string url, Guid libraryId, int itemId)
        {
            var documentVersionList = new List<SPDocumentVersion>();
            try
            {
                var auth = credentials.Get(url);
                using (var clientContext = new SPContext(url, auth))
                {
                    var splist = clientContext.Web.Lists.GetById(libraryId);
                    clientContext.Load(splist, l => l.ParentWebUrl);

                    var splistItem = splist.GetItemById(itemId);
                    var spfile = splistItem.File;
                    clientContext.Load(spfile);
                    clientContext.Load(spfile, i => i.ModifiedBy, i => i.TimeLastModified);

                    var versions = clientContext.LoadQuery(spfile.Versions.IncludeWithDefaultProperties(i => i.CreatedBy, i => i.IsCurrentVersion));

                    clientContext.ExecuteQuery();

                    // There is no way to get file size using Client Object Model
                    var fileSizePool = new Dictionary<string, int>();

                    using (var service = new VersionService(url, auth))
                    {
                        var nodeList = service.GetVersions(spfile.ServerRelativeUrl);
                        foreach (XmlNode node in nodeList.ChildNodes)
                        {
                            if (node.Name == "result")
                            {
                                if (node.Attributes != null)
                                {
                                    var version = node.Attributes["version"];
                                    var v = version.Value.Replace("@", "");
                                    int size;
                                    int.TryParse(node.Attributes["size"].Value, out size);
                                    fileSizePool.Add(v, size);
                                }
                            }
                        }
                    }

                    documentVersionList.Add(new SPDocumentVersion(spfile, spfile.ServerRelativeUrl, splist.ParentWebUrl)
                        {
                            Size = fileSizePool[spfile.UIVersionLabel]
                        });
                    documentVersionList.AddRange(
                        versions
                            .OrderByDescending(v => v.VersionLabel, new SPDocumentVersionComparer())
                            .Select(version => new SPDocumentVersion(version, spfile.ServerRelativeUrl, splist.ParentWebUrl)
                                {
                                    Size = fileSizePool[version.VersionLabel]
                                })
                    );
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPFileService.GetVersions() method for URL: {1}, LibraryId: {2}, ItemId: {3}. The exception message is: {4}", ex.GetType(), url, libraryId, itemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message);
            }
            return documentVersionList;
        }

        public SPDocumentInfo GetDetails(string url, Guid libraryId, int itemId)
        {
            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var spList = clientContext.Web.Lists.GetById(libraryId);
                    var spFile = spList.GetItemById(itemId).File;
                    clientContext.Load(spList);
                    clientContext.Load(spFile);
                    clientContext.Load(spFile, f => f.CheckedOutByUser);
                    clientContext.ExecuteQuery();

                    return new SPDocumentInfo(spList, spFile, spFile.CheckedOutByUser);
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("An exception of type {0} occurred in the InternalApi.SPFileService.GetDetails() method for URL: {1}, LibraryId: {2}, ItemId: {3}. The exception message is: {4}", ex.GetType(), url, libraryId, itemId, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message);
            }
        }
    }
}
