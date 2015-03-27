using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;
using Telligent.Evolution.MediaGalleries.Components;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointLibraryExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_library"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointLibrary>(); }
        }

        public string Name
        {
            get { return "SharePoint Document Library Extension (sharepoint_v1_library)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to use the Document Library extension for a SharePoint files managing."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointLibrary
    {
        string CurrentView { get; set; }

        LibraryItem Get(string siteUrl, string listId, int? groupId, int? galleryId);

        bool IsFolder(string contentType);

        bool InDirectory(string filepath, string directory, string root);

        string OpenDirectory(string directory, string root);

        string UpDirectory(string directory, string root);

        string CurrentDirectory(string directory, string root);

        string Url(string spurl, string filepath, string root);

        SPLibrary List(IDictionary options);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointLibrary : ISharePointLibrary
    {
        const char DirectoryDelimiter = '/';
        const string CurrentViewCookie = "VirtualLibrary_CurrentView";
        const string CurrentDirCookie = "SharePointLibrary";

        [Obsolete("Use sharepoint_v2_library", true)]
        public string CurrentView
        {
            get
            {
                return GetCookie(CurrentViewCookie, CurrentViewKey());
            }
            set
            {
                SetCookie(CurrentViewCookie, CurrentViewKey(), value);
            }
        }

        [Obsolete("Use sharepoint_v2_library", true)]
        public string CurrentDir
        {
            get
            {
                return GetCookie(CurrentDirCookie, CurrentDirectoryKey());
            }
            set
            {
                SetCookie(CurrentDirCookie, CurrentDirectoryKey(), value);
            }
        }

        [Obsolete("Use sharepoint_v2_library", true)]
        public LibraryItem Get(string siteUrl, string listId, int? groupId, int? galleryId)
        {
            if (!String.IsNullOrEmpty(siteUrl) && !String.IsNullOrEmpty(listId))
            {
                return new LibraryItem
                {
                    Url = siteUrl,
                    ListId = listId
                };
            }
            return Default(groupId, galleryId);
        }

        [Obsolete("Use sharepoint_v2_library", true)]
        public bool IsFolder(string contentType)
        {
            // 0x0120 is the folder content type
            return contentType.StartsWith("0x0120");
        }

        [Obsolete("Use sharepoint_v2_library", true)]
        public bool InDirectory(string filepath, string directory, string root)
        {
            directory = ActualDirectory(directory, root) + DirectoryDelimiter;
            filepath = ActualDirectory(filepath, root);
            int index = filepath.IndexOf(directory);
            if (index >= 0)
            {
                // file path is not a selected folder and intersection with directory returns file name without delimiters
                return filepath.Length > directory.Length &&
                       !filepath.Substring(index + directory.Length).Trim(DirectoryDelimiter).Contains(DirectoryDelimiter);
            }
            return false;
        }

        [Obsolete("Use sharepoint_v2_library", true)]
        public string OpenDirectory(string directory, string root)
        {
            return ActualDirectory(directory, root);
        }

        [Obsolete("Use sharepoint_v2_library", true)]
        public string UpDirectory(string directory, string root)
        {
            string actualDir = ActualDirectory(directory, root);
            int index = actualDir.LastIndexOf(DirectoryDelimiter);
            return index > 0 ? actualDir.Substring(0, index + 1) : actualDir;
        }

        [Obsolete("Use sharepoint_v2_library", true)]
        public string CurrentDirectory(string directory, string root)
        {
            CurrentDir = directory.Trim('/').Contains('/') ? directory : String.Empty;
            return ActualDirectory(directory, root);
        }

        // The method merges root folder with directory
        // (ex. root = docLibTitle/, directory = sites/docLibTitle/Folder/File.ext returns docLibTitle/Folder/File.ext)
        private string ActualDirectory(string directory, string root)
        {
            if (String.IsNullOrEmpty(directory) || String.IsNullOrEmpty(directory.Trim(DirectoryDelimiter)))
                return root.Trim(DirectoryDelimiter);

            if (String.IsNullOrEmpty(root) || String.IsNullOrEmpty(root.Trim(DirectoryDelimiter)))
                return directory.Trim(DirectoryDelimiter);

            root = root.Trim(DirectoryDelimiter);
            directory = directory.Trim(DirectoryDelimiter);
            int startIndex = directory.IndexOf(root);
            if (startIndex < 0)
                return root;

            return directory.Substring(startIndex).TrimStart(DirectoryDelimiter);
        }

        public string Url(string spurl, string filepath, string root)
        {
            return HttpUtility.UrlPathEncode(spurl.TrimEnd(DirectoryDelimiter) + DirectoryDelimiter + ActualDirectory(filepath, root));
        }

        [Obsolete("Use sharepoint_v2_library", true)]
        public SPLibrary List(
            [Documentation(Name = "LibraryItems", Type = typeof(ApiList<SPListItem>)),
            Documentation(Name = "CurrentDir", Type = typeof(string)),
            Documentation(Name = "Root", Type = typeof(string)),
            Documentation(Name = "PageIndex", Type = typeof(int)),
            Documentation(Name = "PageSize", Type = typeof(int)),
            Documentation(Name = "SortBy", Type = typeof(string))]
            IDictionary options)
        {
            if (options["LibraryItems"] == null)
            {
                return null;
            }
            string currentDir = options["CurrentDir"] != null ? options["CurrentDir"].ToString() : String.Empty;
            string root = options["Root"] != null ? options["Root"].ToString() : String.Empty;

            var listCollection = (from SPListItem item in (ApiList<SPListItem>)options["LibraryItems"]
                                  where InDirectory(item["FileRef"], currentDir, root)
                                  select item).ToList();

            int totalCount = listCollection.Count;

            var files = (from item in listCollection
                         where !IsFolder(item["ContentTypeId"])
                         select item).ToList();

            listCollection.RemoveAll(item => !IsFolder(item["ContentTypeId"]));

            if (options["SortBy"] != null)
            {
                var orderFields = SPListItemField();
                string sortBy = Convert.ToString(options["SortBy"]);
                if (!String.IsNullOrEmpty(sortBy) && orderFields.ContainsKey(sortBy))
                {
                    listCollection = listCollection.OrderBy(orderFields[sortBy]).ToList();
                    files = files.OrderBy(orderFields[sortBy]).ToList();
                }
            }
            listCollection.AddRange(files);
            if (options["PageIndex"] != null && options["PageSize"] != null)
            {
                int pageIndex = Convert.ToInt32(options["PageIndex"]);
                int pageSize = Convert.ToInt32(options["PageSize"]);
                int startIndex = pageIndex * pageSize;
                int lastIndex = startIndex + pageSize;
                if (lastIndex < listCollection.Count)
                {
                    int count = listCollection.Count - lastIndex;
                    listCollection.RemoveRange(lastIndex, count);
                }
                if (startIndex > 0)
                {
                    listCollection.RemoveRange(0, startIndex);
                }
            }
            var result = new ApiList<SPListItem>();
            foreach (var item in listCollection)
            {
                result.Add(item);
            }

            var library = new SPLibrary
                {
                    TotalCount = totalCount,
                    Collection = result
                };

            return library;
        }

        private LibraryItem Default(int? groupId, int? galleryId)
        {
            var managerList = new IntegrationProviders(IntegrationManagerPlugin.Plugin.Configuration.GetString(IntegrationManagerPlugin.PropertyId.SPObjectManager));
            var listService = ServiceLocator.Get<ISharePointList>();
            if (listService == null)
            {
                return null;
            }

            var group = TEApi.Groups.Get(new GroupsGetOptions { Id = groupId });

            if (group == null)
            {
                return null;
            }

            // Check partnership
            if (group.ExtendedAttributes["SPSiteId"] != null && group.ExtendedAttributes["SPWebId"] != null)
            {
                // The MediaGallery Partnership was established
                Guid siteId;
                Guid webId;
                if (Guid.TryParse(group.ExtendedAttributes["SPSiteId"].Value, out siteId) && Guid.TryParse(group.ExtendedAttributes["SPWebId"].Value, out webId))
                {
                    var manager = managerList.GetAllProviders().FirstOrDefault(item => item.SPWebID == webId && item.SPSiteID == siteId);
                    if (manager == null)
                    {
                        return null;
                    }

                    // Get Document Library Id from partnership
                    if (galleryId != null)
                    {
                        var gallery = TEApi.Galleries.Get(new GalleriesGetOptions { GroupId = groupId, Id = galleryId });
                        if (gallery != null && gallery.ExtendedAttributes["SPListId"] != null)
                        {
                            var listId = gallery.ExtendedAttributes["SPListId"].Value;
                            return new LibraryItem
                            {
                                ListId = listId,
                                Url = manager.SPSiteURL
                            };
                        }
                    }

                    // Get SharedDocuments
                    return GetLibraryItem(manager, listService);
                }
                return null;
            }
            else
            {
                // If this group was mapped, then we know SharePoint site URL and credentials from Integration Manager Plugin
                IntegrationProvider manager = managerList.GetByGroupId((int)group.Id);
                while (manager == null && group.ParentGroupId != -1)
                {
                    group = TEApi.Groups.Get(new GroupsGetOptions { Id = group.ParentGroupId });
                    manager = managerList.Collection.FirstOrDefault(item => item.TEGroupId == group.Id);
                }
                return GetLibraryItem(manager ?? managerList.Default(), listService);
            }
        }

        private LibraryItem GetLibraryItem(IntegrationProvider manager, ISharePointList listService)
        {
            if (manager != null)
            {
                IDictionary query = new Dictionary<string, string>();
                query.Add("WebUrl", manager.SPSiteURL);
                query.Add("ByTitle", "Shared Documents");
                var docLibList = listService.Get(query);
                if (docLibList != null)
                {
                    return new LibraryItem
                    {
                        ListId = docLibList.Id.ToString(),
                        Url = manager.SPSiteURL
                    };
                }
            }
            return null;
        }

        private static Dictionary<string, Func<SPListItem, object>> SPListItemField()
        {
            var fieldsKeyValue = new Dictionary<string, Func<SPListItem, object>>
                                     {
                                         {"Name", item => item.DisplayName},
                                         {"Author", item => item.Author},
                                         {"Date", item => item.CreatedDate}
                                     };
            return fieldsKeyValue;
        }

        private string CurrentViewKey()
        {
            var mediaGallery = CoreContext.Instance().GetCurrent<MediaGallery>();
            return mediaGallery != null ? "vl-view-" + mediaGallery.SectionID.ToString() : String.Empty;
        }

        private string CurrentDirectoryKey()
        {
            var mediaGallery = CoreContext.Instance().GetCurrent<MediaGallery>();
            return mediaGallery != null ? mediaGallery.SectionID.ToString() : String.Empty;
        }

        private string GetCookie(string cookieName, string valueKey)
        {
            var cookie = HttpContext.Current.Request.Cookies[cookieName];
            if (cookie != null && cookie[valueKey] != null)
            {
                return cookie[valueKey];
            }
            return String.Empty;
        }

        private void SetCookie(string cookieName, string valueKey, string value)
        {
            var cookie = HttpContext.Current.Response.Cookies[cookieName] ?? new HttpCookie(cookieName);
            if (cookie.Values[valueKey] != null)
                cookie.Values[valueKey] = value;
            else
                cookie.Values.Add(valueKey, value);
            WriteCookie(cookie);
        }

        private void WriteCookie(HttpCookie cookie)
        {
            string cookieDomain = CSContext.Current.SiteSettings.CookieDomain.ToString();
            string currentURL = HttpContext.Current.Request.Url.AbsoluteUri;

            //Check if we are not breaking functionality because of bad domain setting
            if ((CSRegex.MatchNotTldRegex().IsMatch(cookieDomain)) && (currentURL.IndexOf(cookieDomain) > -1))
            {
                cookie.Path = "/";
                cookie.Domain = cookieDomain;
            }
            HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }
}
