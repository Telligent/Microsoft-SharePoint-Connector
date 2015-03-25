using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using ClientApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public class SPList : ApiEntity, IApplication, IContent
    {
        public enum SortBy
        {
            Title,
            Created,
            Modified,
            ItemCount
        }

        internal static IEnumerable<SPList> Order(IEnumerable<SPList> collection, SortBy sortBy, string sortOrder)
        {
            IEnumerable<SPList> query = Enumerable.Empty<SPList>();
            switch (sortBy)
            {
                case SortBy.Title: query = collection.OrderBy(lib => lib.Title); break;
                case SortBy.Created: query = collection.OrderBy(lib => lib.Created); break;
                case SortBy.Modified: query = collection.OrderBy(lib => lib.Modified); break;
                case SortBy.ItemCount: query = collection.OrderBy(lib => lib.ItemCount); break;
            }
            return string.Compare(sortOrder, "Descending", true, CultureInfo.InvariantCulture) == 0 ? query.Reverse().ToList() : query.ToList();
        }

        public SPList() { }

        public SPList(List splist, Guid siteId)
        {
            if (splist == null) return;

            try
            {
                SPWebUrl = splist.Context.Url;

                BaseType = splist.BaseType;
                Created = splist.Created;
                CreatedDate = splist.Created;

                var parentWebUrl = splist.ParentWebUrl.TrimStart('/');
                var spviewServerRelativeUrl = splist.DefaultViewUrl.TrimStart('/');
                if (spviewServerRelativeUrl.StartsWith(parentWebUrl))
                {
                    spviewServerRelativeUrl = spviewServerRelativeUrl.Substring(parentWebUrl.Length).TrimStart('/');
                }
                SPViewUrl = string.Concat(SPWebUrl.TrimEnd('/'), '/', spviewServerRelativeUrl);

                Description = splist.Description;
                EnableVersioning = splist.EnableVersioning;
                Fields = (splist.Fields != null) ? splist.Fields.ToList() : new List<Field>();
                Id = splist.Id;
                ItemCount = splist.ItemCount;
                Modified = splist.LastItemModifiedDate.ToLocalTime();
                ParentWeb = splist.ParentWebUrl;
                Path = splist.DefaultViewUrl;
                RootFolder = (splist.RootFolder != null) ? splist.RootFolder.ServerRelativeUrl : string.Empty;
                SiteId = siteId;
                Title = splist.Title;
                WebId = splist.ParentWeb.Id;
                IsEnabled = true;
            }
            catch (Exception ex)
            {
                SPLog.UnKnownError(ex, ex.Message);
            }
        }

        [Documentation(Description = "SharePoint List Id")]
        public Guid Id { get; internal set; }

        [Documentation(Description = "SharePoint List Template Id")]
        public BaseType BaseType { get; private set; }

        [Documentation(Description = "SharePoint List Title")]
        public string Title { get; internal set; }

        [Documentation(Description = "SharePoint List Description")]
        public string Description { get; internal set; }

        [Documentation(Description = "The Date of Creation")]
        public DateTime Created { get; private set; }

        [Documentation(Description = "Last Date Modified")]
        public DateTime Modified { get; private set; }

        [Documentation(Description = "Returns true if versioning is enabled")]
        public bool EnableVersioning { get; private set; }

        [Documentation(Description = "SharePoint Site Id")]
        public Guid SiteId { get; private set; }

        [Documentation(Description = "SharePoint Web Id")]
        public Guid WebId { get; private set; }

        [Documentation(Description = "The Items Count")]
        public int ItemCount { get; private set; }

        [Documentation(Description = "Related Group Id")]
        public int GroupId { get; set; }

        [Documentation(Description = "SharePoint List Fields")]
        public List<Field> Fields { get; private set; }

        [Documentation(Description = "SharePoint Default View URL")]
        public string SPViewUrl { get; private set; }

        [Documentation(Description = "The parent Web URL")]
        public string ParentWeb { get; private set; }

        [Documentation(Description = "Server relative URL")]
        public string Path { get; private set; }

        [Documentation(Description = "The root folder server relative URL")]
        public string RootFolder { get; private set; }

        private static readonly IListItemUrls listItemUrls = ServiceLocator.Get<IListItemUrls>();
        private static readonly IListDataService listDataService = ServiceLocator.Get<IListDataService>();
        private string _url;
        [Documentation(Description = "SharePoint List URL in Evolution")]
        public string Url
        {
            get
            {
                if (string.IsNullOrEmpty(_url))
                {
                    var listBase = listDataService.Get(Id);
                    _url = listBase != null ? listItemUrls.BrowseListItems(listBase) : SPWebUrl;
                }
                return _url;
            }
        }

        [Documentation(Description = "SharePoint List URL in SharePoint")]
        public string SPWebUrl { get; internal set; }

        public int UserId { get; set; }

        [Documentation(Description = "Default List ViewId")]
        public Guid ViewId { get; set; }

        public Field GetField(string internalName)
        {
            return Fields.FirstOrDefault(field => field.InternalName == internalName);
        }

        #region IApplication Members

        public Guid ApplicationId
        {
            get { return Id; }
            internal set { Id = value; }
        }

        public Guid ApplicationTypeId
        {
            get { return ListApplicationType.Id; }
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
            if (!string.IsNullOrEmpty(Title))
            {
                return HttpUtility.HtmlEncode(Title);
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

        public DateTime CreatedDate { get; internal set; }

        #endregion
    }
}
