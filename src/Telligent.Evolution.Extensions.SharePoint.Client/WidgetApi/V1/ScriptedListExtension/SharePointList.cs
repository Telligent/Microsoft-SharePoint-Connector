using Microsoft.SharePoint.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Caching.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;
using Telligent.Evolution.MediaGalleries.Components;
using Core = Telligent.Evolution.Components;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointListExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_list"; }
        }
        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointList>(); }
        }

        public string Name
        {
            get { return "SharePoint List Extension (sharepoint_v1_list)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to use the SharePoint Client Object Model."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointList : ICacheable
    {
        SPList Current { get; }

        SPList Get(IDictionary options);

        ApiList<SPList> List(IDictionary options);

        bool CanEdit(string url, string listId);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointList : ISharePointList
    {
        private const string GetList = "SharePointList_GetList";
        private readonly ICredentialsManager credentials;
        private readonly InternalApi.ICacheService cacheService;

        internal SharePointList() : this(ServiceLocator.Get<ICredentialsManager>(), ServiceLocator.Get<InternalApi.ICacheService>()) { }
        internal SharePointList(ICredentialsManager credentials, InternalApi.ICacheService cacheService)
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


        #region ISharePointListScriptedContentFragment
        [Obsolete("Use sharepoint_v2_list", true)]
        public SPList Current
        {
            get
            {
                var currentGallery = CoreContext.Instance().GetCurrent<MediaGallery>();
                if (currentGallery != null)
                {
                    var listId = currentGallery.GetExtendedAttribute("SPListId");
                    if (!string.IsNullOrEmpty(listId))
                    {
                        var args = new Dictionary<string, string> { { "ById", listId } };
                        return Get(args);
                    }
                }
                return null;
            }
        }

        [Obsolete("Use sharepoint_v2_list", true)]
        public SPList Get(
            [Documentation(Name = "WebUrl", Type = typeof(string)),
            Documentation(Name = "ById", Type = typeof(string)),
            Documentation(Name = "ByTitle", Type = typeof(string))]
            IDictionary options)
        {
            var url = CurrentUrl(options);
            var byId = options["ById"] as string;
            var byTitle = options["ByTitle"] as string;

            if (String.IsNullOrEmpty(url))
            {
                return null;
            }

            if (string.IsNullOrEmpty(byId) &&
               string.IsNullOrEmpty(byTitle))
            {
                return null;
            }

            var cacheOptions = string.Join("_", options.Values.Cast<string>());
            var cacheId = string.Concat(GetList, cacheOptions);
            var cacheList = (SPList)cacheService.Get(cacheId, CacheScope.Context | CacheScope.Process);
            if (cacheList == null)
            {
                try
                {
                    using (var clientContext = new SPContext(url, credentials.Get(url)))
                    {
                        var site = clientContext.Site;
                        clientContext.Load(site, s => s.Id);

                        var web = clientContext.Web;
                        clientContext.Load(web, w => w.Id);

                        if (!string.IsNullOrEmpty(byId))
                        {
                            var splist = clientContext.ToList(Guid.Parse(byId));
                            clientContext.Load(splist, SPListService.NoHiddenFieldsInstanceQuery);
                            clientContext.ExecuteQuery();

                            var newSPlistById = new SPList(splist, site.Id);
                            cacheService.Put(cacheId, newSPlistById, CacheScope.Context | CacheScope.Process, new string[0], CacheTimeOut);
                            return newSPlistById;
                        }
                        if (!string.IsNullOrEmpty(byTitle))
                        {
                            var splist = clientContext.Web.Lists.GetByTitle(byTitle);
                            clientContext.Load(splist, SPListService.NoHiddenFieldsInstanceQuery);
                            clientContext.ExecuteQuery();

                            var newSPListByTitle = new SPList(splist, site.Id);
                            cacheService.Put(cacheId, newSPListByTitle, CacheScope.Context | CacheScope.Process, new string[0], CacheTimeOut);
                            return newSPListByTitle;
                        }
                    }

                    var currentSPList = PublicApi.Lists.Get(SPCoreService.Context.ListId);
                    cacheService.Put(cacheId, currentSPList, CacheScope.Context | CacheScope.Process, new string[0], CacheTimeOut);
                    return currentSPList;
                }
                catch (Exception ex)
                {
                    SPLog.AccessDenied(ex, ex.Message);
                    var spList = new SPList();
                    spList.Errors.Add(new Error(ex.GetType().ToString(), ex.Message));
                    return spList;
                }
            }

            return cacheList;
        }

        [Obsolete("Use sharepoint_v2_list", true)]
        public ApiList<SPList> List(
            [Documentation(Name = "WebUrl", Type = typeof(string)),
            Documentation(Name = "Type", Type = typeof(string))]
            IDictionary options)
        {
            string url = CurrentUrl(options);
            if (String.IsNullOrEmpty(url))
            {
                return null;
            }

            using (var clientContext = new SPContext(url, credentials.Get(url)))
            {

                try
                {
                    var site = clientContext.Site;
                    clientContext.Load(site, s => s.Id);

                    var web = clientContext.Web;
                    clientContext.Load(web, w => w.Id);

                    if (options != null && !string.IsNullOrEmpty((string)options["Type"]))
                    {
                        var lookUpTemplate = (int)GetTemplateType(options["Type"].ToString());
                        var spListCollection = clientContext.LoadQuery(clientContext.Web.Lists
                            .Where(list => list.BaseTemplate == lookUpTemplate)
                            .Include(SPListService.NoHiddenFieldsInstanceQuery));
                        clientContext.ExecuteQuery();
                        return spListCollection.ToApiList(site.Id);
                    }
                    clientContext.Load(clientContext.Web.Lists, SPListService.NoHiddenFieldsListInstanceQuery);
                    clientContext.ExecuteQuery();
                    return clientContext.Web.Lists.ToApiList(site.Id);
                }
                catch (Exception ex)
                {
                    SPLog.AccessDenied(ex, ex.Message);
                    return new ApiList<SPList>(new Error(ex.GetType().ToString(), ex.Message));
                }
            }
        }

        [Obsolete("Use sharepoint_v2_list", true)]
        public bool CanEdit(string url, string listId)
        {
            using (var clientContext = new SPContext(url, credentials.Get(url)))
            {
                try
                {
                    var sharepointList = clientContext.ToList(Guid.Parse(listId));
                    clientContext.Load(sharepointList, l => l.EffectiveBasePermissions);
                    clientContext.ExecuteQuery();
                    var permissions = sharepointList.EffectiveBasePermissions;
                    return permissions.Has(PermissionKind.EditListItems);
                }
                catch (Exception ex)
                {
                    SPLog.AccessDenied(ex, ex.Message);
                    return false;
                }
            }
        }
        #endregion

        #region Utility methods
        private string CurrentUrl(IDictionary options)
        {
            string url = String.Empty;
            if (options != null && options["WebUrl"] != null)
            {
                url = options["WebUrl"].ToString();
            }
            if (String.IsNullOrEmpty(url))
            {
                url = UrlFromPartnership();
            }
            return url;
        }

        private string UrlFromPartnership()
        {
            // if partnership has been established we can get url and credentials from the IntegrationManager plugin
            var integrationManagerPlugin = IntegrationManagerPlugin.Plugin;
            if (integrationManagerPlugin != null)
            {
                var integrationManagerList = new IntegrationProviders(integrationManagerPlugin.Configuration.GetString(IntegrationManagerPlugin.PropertyId.SPObjectManager));
                var currentGroup = CoreContext.Instance().GetCurrent<Core.Group>();
                if (!String.IsNullOrEmpty(currentGroup.GetExtendedAttribute("SPSiteId")) && !String.IsNullOrEmpty(currentGroup.GetExtendedAttribute("SPWebId")))
                {
                    // this group is partnered with SiteCollection
                    Guid siteId;
                    Guid webId;
                    if (Guid.TryParse(currentGroup.GetExtendedAttribute("SPSiteId"), out siteId) && Guid.TryParse(currentGroup.GetExtendedAttribute("SPWebId"), out webId))
                    {
                        var spobjectManager = integrationManagerList.GetAllProviders().FirstOrDefault(item => item.SPSiteID == siteId && item.SPWebID == webId);
                        if (spobjectManager != null)
                        {
                            return spobjectManager.SPSiteURL;
                        }
                    }
                }

                var manager = integrationManagerList.GetByGroupId(currentGroup.ID);

                if (manager != null)
                    return manager.SPSiteURL;

            }
            return String.Empty;
        }

        private ListTemplateType GetTemplateType(string templateType)
        {
            return (ListTemplateType)Enum.Parse(typeof(ListTemplateType), templateType, true);
        }
        #endregion
    }
}
