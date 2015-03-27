using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal class FolderBaseQuery
    {
        private const string FolderNameForbiddenCharactersPattern = "(^\\.|\\.$)|(\\.\\.+)|[~\"#%&*:<>\\?\\/\\\\{{|}}]";

        protected void Validate(string name)
        {
            if (String.IsNullOrEmpty(name))
                throw new InvalidOperationException("Invalid Folder Name.");

            if (Regex.Match(name, FolderNameForbiddenCharactersPattern, RegexOptions.IgnoreCase).Success)
                throw new InvalidOperationException(string.Format("The folder name \"{0}\" contains invalid characters. Please use a different name. Valid folder names cannot begin or end with a dot, cannot contain consecutive dots and cannot contain any of the following characters: ~ \" # % & * : < > ? / \\ {{ | }}.", name));
        }
    }

    internal class FolderCreateQuery : FolderBaseQuery
    {
        public static implicit operator FolderCreateQuery(FolderCreateOptions options)
        {
            return new FolderCreateQuery(options.Name)
            {
                Path = options.Path
            };
        }

        public FolderCreateQuery(string name)
        {
            Validate(name);
            Name = name;
        }

        public string Name { get; private set; }
        public string Path { get; set; }
    }

    internal class FolderRenameQuery : FolderBaseQuery
    {
        public static implicit operator FolderRenameQuery(FolderRenameOptions options)
        {
            return new FolderRenameQuery(options.Name);
        }

        public FolderRenameQuery(string name)
        {
            Validate(name);
            Name = name;
        }

        public string Name { get; private set; }
    }

    internal interface IFolderService
    {
        Folder Create(string url, Guid libraryId, FolderCreateQuery folderCreateQuery);

        Folder Rename(string url, Guid libraryId, string folderPath, FolderRenameQuery folderRenameQuery);

        Folder Get(string url, Guid libraryId, string folderPath);

        Folder GetParent(string url, Guid libraryId, string folderPath);

        List<Folder> List(string url, Guid libraryId, string folderPath);

        List<Folder> UpToParent(string url, Guid libraryId, string folderPath);

        Folder Delete(string url, Guid libraryId, string folderPath);

        Guid Recycle(string url, string folderPath);
    }

    internal class SPFolderService : IFolderService
    {
        private readonly ICredentialsManager credentials;
        private readonly IListItemDataService listItemDataService;

        public SPFolderService()
            : this(ServiceLocator.Get<ICredentialsManager>(), ServiceLocator.Get<IListItemDataService>())
        {
        }

        public SPFolderService(ICredentialsManager credentials, IListItemDataService listItemDataService)
        {
            this.credentials = credentials;
            this.listItemDataService = listItemDataService;
        }

        public Folder Create(string url, Guid libraryId, FolderCreateQuery folderCreateQuery)
        {
            Folder folder;

            try
            {
                SP.ListItem folderListItem;

                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var splist = clientContext.Web.Lists.GetById(libraryId);
                    var parentFolder = !String.IsNullOrEmpty(folderCreateQuery.Path) ? clientContext.Web.GetFolderByServerRelativeUrl(folderCreateQuery.Path) : splist.RootFolder;
                    
                    clientContext.Load(parentFolder);

                    // check that new folder name is unique
                    var folderName = folderCreateQuery.Name;
                    var subfolderWithTheSameName = clientContext.LoadQuery(parentFolder.Folders.Where(f => f.Name == folderName));
                    try
                    {
                        clientContext.ExecuteQuery();
                    }
                    catch (Exception ex)
                    {
                        var message = string.Format("An exception of type {0} occurred in the InternalApi.SPFolderService.Create() method for SharePoint ServerRelativeUrl: '{1}' in SPWebUrl: '{2}'. The exception message is: {3}", ex.GetType(), folderCreateQuery.Path, url, ex.Message);
                        SPLog.FileNotFound(ex, message);

                        throw new SPInternalException(message, ex);
                    }

                    if (subfolderWithTheSameName != null && subfolderWithTheSameName.Any())
                    {
                        throw new InvalidOperationException(string.Format("The folder with '{0}' name already exists at ServerRelativeUrl: '{1}' in SPWebUrl: '{2}'.", folderName, folderCreateQuery.Path, url));
                    }

                    var newFolder = parentFolder.Folders.Add(folderCreateQuery.Name);
                    clientContext.Load(newFolder, f => f.Name, f => f.ServerRelativeUrl, f => f.ItemCount);

                    parentFolder.Update();
                    clientContext.ExecuteQuery();

                    var query = GetFolderByServerRelativeUrl(newFolder.ServerRelativeUrl);
                    var folderListItems = clientContext.LoadQuery(splist.GetItems(query));
                    clientContext.ExecuteQuery();

                    folder = new Folder(newFolder.Name, newFolder.ServerRelativeUrl, newFolder.ItemCount, libraryId);
                    folderListItem = folderListItems.First();
                }

                listItemDataService.AddUpdate(new ItemBase(libraryId,
                    Guid.Parse(folderListItem["UniqueId"].ToString()),
                    folderListItem.Id,
                    Convert.ToDateTime(folderListItem["Modified"]))
                        {
                            IsIndexable = false
                        });
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (SPInternalException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPFolderService.Create() method while creating a new folder with the Name: '{1}' ServerRelativeUrl: '{2}' in SPWebUrl: '{3}'. The exception message is: {4}", ex.GetType(), folderCreateQuery.Name, folderCreateQuery.Path, url, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }

            return folder;
        }

        public Folder Rename(string url, Guid libraryId, string folderPath, FolderRenameQuery folderRenameQuery)
        {
            Folder folder;

            if (string.IsNullOrEmpty(folderPath))
            {
                throw new InvalidOperationException("ServerRelativeUrl cannot be empty.");
            }

            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var spfolder = clientContext.Web.GetFolderByServerRelativeUrl(folderPath);
                    var parent = spfolder.ParentFolder;
                    clientContext.Load(spfolder);

                    // check that new folder name is unique
                    string folderName = folderRenameQuery.Name;
                    var subfolderWithTheSameName = clientContext.LoadQuery(parent.Folders.Where(f => f.Name == folderName));
                    try
                    {
                        clientContext.ExecuteQuery();
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("An exception of type {0} occurred in the InternalApi.SPFolderService.Rename() method for SharePoint ServerRelativeUrl: '{1}' in SPWebUrl: '{2}'. The exception message is: {3}", ex.GetType(), folderPath, url, ex.Message);
                        SPLog.FileNotFound(ex, message);

                        throw new SPInternalException(message, ex);
                    }

                    if (subfolderWithTheSameName != null && subfolderWithTheSameName.Any())
                    {
                        throw new InvalidOperationException(string.Format("The folder with '{0}' name already exists at ServerRelativeUrl: '{1}' in SPWebUrl: '{2}'.", folderName, folderPath, url));
                    }

                    var folderToRenameQuery = clientContext.Web.Lists.GetById(libraryId).GetItems(GetFolderByServerRelativeUrl(spfolder.ServerRelativeUrl));
                    var spfolderListItems = clientContext.LoadQuery(SP.ClientObjectQueryableExtension.IncludeWithDefaultProperties(folderToRenameQuery, f => f["FileLeafRef"]));
                    clientContext.ExecuteQuery();

                    var spfolderListItem = spfolderListItems != null ? spfolderListItems.FirstOrDefault() : null;
                    if (spfolderListItem == null)
                    {
                        throw new Exception("The folder cannot be loaded, because CAML request failed or invalid.");
                    }

                    spfolderListItem["FileLeafRef"] = folderRenameQuery.Name;
                    spfolderListItem.Update();

                    clientContext.Load(spfolder);

                    clientContext.ExecuteQuery();

                    folder = new Folder(spfolder.Name, spfolder.ServerRelativeUrl, spfolder.ItemCount, libraryId);
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (SPInternalException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPFolderService.Rename() method FolderName: '{1}' ServerRelativeUrl: '{2}' in SPWebUrl: '{3}'. The exception message is: {4}", ex.GetType(), folderRenameQuery.Name, folderPath, url, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }

            return folder;
        }

        public Folder Get(string url, Guid libraryId, string folderPath)
        {
            Folder folder;
            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var spfolder = clientContext.Web.GetFolderByServerRelativeUrl(folderPath);
                    clientContext.Load(spfolder, f => f.Name, f => f.ServerRelativeUrl, f => f.ItemCount);

                    clientContext.ExecuteQuery();

                    folder = new Folder(spfolder.Name, spfolder.ServerRelativeUrl, spfolder.ItemCount, libraryId);
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPFolderService.Get() method LibraryId: {1} ServerRelativeUrl: '{2}' SPWebUrl: '{3}'. The exception message is: {4}", ex.GetType(), libraryId, folderPath, url, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
            return folder;
        }

        public Folder GetParent(string url, Guid libraryId, string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(folderPath.Trim('/'))) return null;

            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    SP.List splist = clientContext.Web.Lists.GetById(libraryId);

                    SP.Folder rootFolder = splist.RootFolder;
                    clientContext.Load(rootFolder, f => f.ServerRelativeUrl);
                    clientContext.ExecuteQuery();

                    if (rootFolder.ServerRelativeUrl.Trim('/').Equals(folderPath.Trim('/'), StringComparison.InvariantCultureIgnoreCase))
                    {
                        // This is a root folder, so it has no parents
                        return null;
                    }

                    var spfolder = clientContext.Web.GetFolderByServerRelativeUrl(folderPath);
                    clientContext.Load(spfolder, f => f.ServerRelativeUrl);
                    clientContext.Load(spfolder.ParentFolder, p => p.Name, p => p.ServerRelativeUrl, p => p.ItemCount);
                    clientContext.ExecuteQuery();

                    bool pathIsNotEmpty = spfolder != null && !String.IsNullOrEmpty(spfolder.ServerRelativeUrl.Trim('/'));
                    bool isARootFolder = spfolder == null || rootFolder.ServerRelativeUrl.Trim('/').Equals(spfolder.ServerRelativeUrl.Trim('/'));
                    bool hasParentFolder = pathIsNotEmpty && !isARootFolder;
                    if (!hasParentFolder)
                    {
                        return null;
                    }

                    if (spfolder.ParentFolder != null)
                    {
                        return new Folder(spfolder.ParentFolder.Name, spfolder.ParentFolder.ServerRelativeUrl, spfolder.ParentFolder.ItemCount, libraryId);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new SPInternalException(ex.Message, ex);
            }
        }

        public List<Folder> List(string url, Guid libraryId, string folderPath)
        {
            var folderList = new List<Folder>();

            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var list = clientContext.Web.Lists.GetById(libraryId);
                    var rootFolder = list.RootFolder;
                    clientContext.Load(rootFolder, f => f.ServerRelativeUrl);

                    var spfolder = clientContext.Web.GetFolderByServerRelativeUrl(folderPath);
                    var folderCollection = clientContext.LoadQuery(SP.ClientObjectQueryableExtension.Include(spfolder.Folders, f => f.Name, f => f.ServerRelativeUrl, f => f.ItemCount));

                    clientContext.ExecuteQuery();

                    folderList.AddRange(from f in folderCollection
                                        where !IsHiddenFolder(f.ServerRelativeUrl, rootFolder.ServerRelativeUrl)
                                        select new Folder(f.Name, f.ServerRelativeUrl, f.ItemCount, libraryId));
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPFolderService.List() method LibraryId: {1} ServerRelativeUrl: '{2}' SPWebUrl: '{3}'. The exception message is: {4}", ex.GetType(), libraryId, folderPath, url, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
            return folderList;
        }

        public List<Folder> UpToParent(string url, Guid libraryId, string folderPath)
        {
            var folderList = new List<Folder>();

            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var list = clientContext.Web.Lists.GetById(libraryId);
                    var rootFolder = list.RootFolder;
                    clientContext.Load(rootFolder, f => f.Name, f => f.ServerRelativeUrl, f => f.ItemCount);
                    clientContext.ExecuteQuery();

                    bool pathIsNotEmpty = !String.IsNullOrEmpty(folderPath.Trim('/'));
                    bool isARootFolder = rootFolder.ServerRelativeUrl.Trim('/').Equals(folderPath.Trim('/'));
                    bool hasParentFolder = pathIsNotEmpty && !isARootFolder;
                    while (hasParentFolder)
                    {
                        var spfolder = clientContext.Web.GetFolderByServerRelativeUrl(folderPath);
                        clientContext.Load(spfolder, f => f.Name, f => f.ServerRelativeUrl, f => f.ItemCount);
                        clientContext.Load(spfolder.ParentFolder, p => p.ServerRelativeUrl);
                        clientContext.ExecuteQuery();

                        folderList.Add(new Folder(spfolder.Name, spfolder.ServerRelativeUrl, spfolder.ItemCount, libraryId));

                        folderPath = spfolder.ParentFolder.ServerRelativeUrl;

                        pathIsNotEmpty = !String.IsNullOrEmpty(folderPath.Trim('/'));
                        isARootFolder = rootFolder.ServerRelativeUrl.Trim('/').Equals(folderPath.Trim('/'));
                        hasParentFolder = pathIsNotEmpty && !isARootFolder;
                    }
                    folderList.Add(new Folder(rootFolder.Name, rootFolder.ServerRelativeUrl, rootFolder.ItemCount, libraryId));
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPFolderService.UpToParent() method LibraryId: {1} ServerRelativeUrl: '{2}' SPWebUrl: '{3}'. The exception message is: {4}", ex.GetType(), libraryId, folderPath, url, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
            return folderList;
        }

        public Folder Delete(string url, Guid libraryId, string folderPath)
        {
            Folder folder;
            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    SP.List splist = clientContext.Web.Lists.GetById(libraryId);

                    SP.Folder rootFolder = splist.RootFolder;
                    clientContext.Load(rootFolder, f => f.ServerRelativeUrl);

                    SP.Folder spfolder = clientContext.Web.GetFolderByServerRelativeUrl(folderPath);
                    clientContext.Load(spfolder, f => f.Name, f => f.ServerRelativeUrl, f => f.ItemCount);

                    clientContext.ExecuteQuery();

                    bool pathIsNotEmpty = !String.IsNullOrEmpty(spfolder.ServerRelativeUrl.Trim('/'));
                    bool isARootFolder = rootFolder.ServerRelativeUrl.Trim('/').Equals(spfolder.ServerRelativeUrl.Trim('/'));
                    bool hasParentFolder = pathIsNotEmpty && !isARootFolder;
                    if (hasParentFolder && !IsHiddenFolder(spfolder.ServerRelativeUrl, rootFolder.ServerRelativeUrl))
                    {
                        folder = new Folder(spfolder.Name, spfolder.ServerRelativeUrl, spfolder.ItemCount, libraryId);

                        spfolder.DeleteObject();
                        clientContext.ExecuteQuery();
                    }
                    else
                    {
                        throw new SPInternalException("Folder can not be removed.");
                    }
                }
            }
            catch (SPInternalException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPFolderService.Delete() method LibraryId: {1} ServerRelativeUrl: '{2}' in SPWebUrl: '{3}'. The exception message is: {4}", ex.GetType(), libraryId, folderPath, url, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
            return folder;
        }

        /// <summary>
        /// Recycle Folder
        /// </summary>
        /// <param name="url">SPWeb Url</param>
        /// <param name="folderPath"></param>
        /// <returns>A Guid that represents the transaction ID of the delete transaction.</returns>
        public Guid Recycle(string url, string folderPath)
        {
            try
            {
                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    var spfolder = clientContext.Web.GetFolderByServerRelativeUrl(folderPath);
                    var removedItems = spfolder.Recycle();

                    clientContext.ExecuteQuery();

                    return removedItems.Value;
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the InternalApi.SPFolderService.Recycle() method ServerRelativeUrl: '{1}' in SPWebUrl: '{2}'. The exception message is: {3}", ex.GetType(), folderPath, url, ex.Message);
                SPLog.RoleOperationUnavailable(ex, message);

                throw new SPInternalException(message, ex);
            }
        }

        #region Utility

        private static bool IsHiddenFolder(string folerPath, string rootFolderPath)
        {
            string hiddenFolderPath = String.Format("{0}/Forms", rootFolderPath.Trim('/'));
            return folerPath.Trim('/').StartsWith(hiddenFolderPath);
        }

        private static SP.CamlQuery GetFolderByServerRelativeUrl(string serverRelativeUrl)
        {
            return CAMLQueryBuilder.GetQuery(1, new[] { "Id", "UniqueId", "FileRef", "Modified" }, string.Format(@"
                <Where>
                    <And>
                        <Eq>
                            <FieldRef Name='ContentType'/>
                            <Value Type='Text'>Folder</Value>
                        </Eq>
                        <Eq>
                            <FieldRef Name='FileRef'/>
                            <Value Type='Text'>{0}</Value>
                        </Eq>
                    </And>
                </Where>", serverRelativeUrl), queryAllFoldersAndSubFolders: true);
        }

        #endregion
    }
}
