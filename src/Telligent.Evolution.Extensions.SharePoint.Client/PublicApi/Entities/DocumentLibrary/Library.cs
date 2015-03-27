using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    [Documentation(Description = "A Document Library entity")]
    public class Library : ApiEntity, IApplication, IContent
    {
        public enum SortBy
        {
            Name,
            ItemCount,
            Created,
            Modified
        }

        internal static IEnumerable<Library> Order(IEnumerable<Library> collection, SortBy sortBy, string sortOrder)
        {
            IEnumerable<Library> query = Enumerable.Empty<Library>();
            switch (sortBy)
            {
                case SortBy.Name: query = collection.OrderBy(lib => lib.Name); break;
                case SortBy.Created: query = collection.OrderBy(lib => lib.Created); break;
                case SortBy.Modified: query = collection.OrderBy(lib => lib.Modified); break;
                case SortBy.ItemCount: query = collection.OrderBy(lib => lib.ItemCount); break;
            }
            return String.Compare(sortOrder, "Descending", true, CultureInfo.InvariantCulture) == 0 ? query.Reverse().ToList() : query.ToList();
        }

        public Library() { }

        public Library(SPList list)
        {
            if (list == null)
                return;

            Initialize(list);
        }

        [Documentation(Description = "SharePoint List Id")]
        public Guid Id { get; set; }

        [Documentation(Description = "Name of the Document Library")]
        public string Name { get; internal set; }

        [Documentation(Description = "Description of the Document Library")]
        public string Description { get; internal set; }

        [Documentation(Description = "Returns true when versioning was enabled for a Document Library")]
        public bool VersioningEnabled { get; internal set; }

        [Documentation(Description = "The Date of Creation")]
        public DateTime Created { get; internal set; }

        [Documentation(Description = "Last Date Modified")]
        public DateTime Modified { get; internal set; }

        [Documentation(Description = "A count of all Documents")]
        public int ItemCount { get; internal set; }

        [Documentation(Description = "Root folder name")]
        public string Root { get; internal set; }

        [Documentation(Description = "The container group Id")]
        public int GroupId { get; set; }

        [Documentation(Description = "SharePoint Web Url")]
        public string SPWebUrl { get; set; }

        [Documentation(Description = "Default SharePoint View Url")]
        public string SPViewUrl { get; set; }

        [Documentation(Description = "Default View Id")]
        public Guid ViewId { get; set; }

        #region IApplication Members

        public Guid ApplicationId
        {
            get { return Id; }
        }

        public Guid ApplicationTypeId
        {
            get { return LibraryApplicationType.Id; }
        }

        public string AvatarUrl
        {
            get { return null; }
        }

        public IContainer Container
        {
            get
            {
                return TEApi.Groups.Get(new GroupsGetOptions
                {
                    Id = GroupId
                });
            }
        }

        public string HtmlName(string target)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return HttpUtility.HtmlEncode(Name);
            }
            return string.Empty;
        }

        public string HtmlDescription(string target)
        {
            if (!string.IsNullOrEmpty(Description))
            {
                return HttpUtility.HtmlEncode(Description);
            }
            return string.Empty;
        }

        public bool IsEnabled { get; internal set; }

        public string Url { get; internal set; }

        #endregion

        #region IContent Members

        public IApplication Application
        {
            get { return this; }
        }

        public Guid ContentId
        {
            get { return ApplicationId; }
        }

        public Guid ContentTypeId
        {
            get { return ApplicationTypeId; }
        }

        public int? CreatedByUserId
        {
            get { return null; }
        }

        DateTime IContent.CreatedDate { get { return Created; } }

        #endregion

        private static readonly IDocumentUrls documentUrls = ServiceLocator.Get<IDocumentUrls>();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();

        private void Initialize(SPList list)
        {
            Created = list.Created;
            Description = list.Description;
            GroupId = list.GroupId;
            Id = list.Id;
            IsEnabled = true;
            ItemCount = list.ItemCount;
            Modified = list.Modified;
            Name = list.Title;
            Root = list.RootFolder;
            SPViewUrl = list.SPViewUrl;
            SPWebUrl = list.SPWebUrl;

            var listBase = listDataService.Get(Id);
            Url = listBase != null ? documentUrls.BrowseDocuments(listBase) : SPWebUrl;

            VersioningEnabled = list.EnableVersioning;
            ViewId = list.ViewId;

            foreach (var error in list.Errors)
            {
                Errors.Add(error);
            }

            foreach (var warning in list.Warnings)
            {
                Warnings.Add(warning);
            }
        }
    }
}
