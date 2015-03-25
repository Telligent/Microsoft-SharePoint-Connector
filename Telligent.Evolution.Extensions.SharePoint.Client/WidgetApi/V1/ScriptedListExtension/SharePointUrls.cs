using System;
using System.Collections.Generic;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Exceptions;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;
using Telligent.Evolution.Extensions.SharePoint.Client.Routing;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using ClientApi = Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointUrlsExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_urls"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointUrls>(); }
        }

        public string Name
        {
            get { return "SharePoint Urls Extension (sharepoint_v1_urls)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to work with files on the SharePoint side."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointUrls
    {
        string Current { get; }

        string Library(Guid applicationId);
        string Library(string applicationId);

        string Document(Guid contentId);
        string Document(string contentId);

        string SPList(Guid applicationId);
        string SPList(string applicationId);

        string SPListItem(Guid contentId);
        string SPListItem(string contentId);

        string View(Guid contentId, Guid contentTypeId);
        string View(string contentId, string contentTypeId);

        string Edit(Guid contentId, Guid contentTypeId);
        string Edit(string contentId, string contentTypeId);

        string Create(Guid applicationId, Guid applicationTypeId);
        string Create(string applicationId, string applicationTypeId);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointUrls : ISharePointUrls
    {
        private readonly List<SPRouteTable> routeTables = new List<SPRouteTable> { LibrariesRouteTable.Get(), DocumentsRouteTable.Get(), ListsRouteTable.Get(), ListItemsRouteTable.Get() };

        public string Current
        {
            get
            {
                try
                {
                    var pageContext = Extensibility.Api.Version1.PublicApi.Url.CurrentContext;
                    if (pageContext != null
                        && !string.IsNullOrEmpty(pageContext.UrlName)
                        && pageContext.ApplicationTypeId != null
                        && pageContext.ApplicationTypeId.Value != Guid.Empty)
                    {
                        foreach (var routeTable in routeTables)
                        {
                            var routedPage = routeTable.GetPageByUrlName(pageContext.UrlName);
                            if (routedPage != null)
                            {
                                return routedPage.ShortName;
                            }
                        }
                    }
                }
                catch (SPInternalException) { }
                catch (Exception ex)
                {
                    string message = string.Format("An exception of type {0} occurred in the $sharepoint_v1_urls.Current method. The exception message is: {1}", ex.GetType(), ex.Message);
                    SPLog.UnKnownError(ex, message);
                }
                return null;
            }
        }

        public string Document(Guid contentId)
        {
            try
            {
                return ClientApi.SharePointUrls.Document(contentId);
            }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the $sharepoint_v1_urls.Document() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        public string Document(string contentId)
        {
            Guid id;
            return Guid.TryParse(contentId, out id) ? Document(id) : null;
        }

        public string Library(Guid applicationId)
        {
            try
            {
                return ClientApi.SharePointUrls.BrowseDocuments(applicationId);
            }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the $sharepoint_v1_urls.Library() method ApplicationId: {1}. The exception message is: {2}", ex.GetType(), applicationId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        public string Library(string applicationId)
        {
            Guid id;
            return Guid.TryParse(applicationId, out id) ? Library(id) : null;
        }

        public string SPList(Guid applicationId)
        {
            try
            {
                return ClientApi.SharePointUrls.BrowseListItems(applicationId);
            }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the $sharepoint_v1_urls.SPList() method ApplicationId: {1}. The exception message is: {2}", ex.GetType(), applicationId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        public string SPList(string applicationId)
        {
            Guid id;
            return Guid.TryParse(applicationId, out id) ? SPList(id) : null;
        }

        public string SPListItem(Guid contentId)
        {
            try
            {
                return ClientApi.SharePointUrls.ListItem(contentId);
            }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the $sharepoint_v1_urls.SPListItem() method ContentId: {1}. The exception message is: {2}", ex.GetType(), contentId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        public string SPListItem(string contentId)
        {
            Guid id;
            return Guid.TryParse(contentId, out id) ? SPListItem(id) : null;
        }

        public string View(Guid contentId, Guid contentTypeId)
        {
            try
            {
                if (contentTypeId == DocumentContentType.Id)
                {
                    return PublicApi.SharePointUrls.Document(contentId);
                }
                else if (contentTypeId == ItemContentType.Id)
                {
                    return PublicApi.SharePointUrls.ListItem(contentId);
                }
            }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the $sharepoint_v1_urls.View() method ContentId: {1}, contentTypeId: {2}. The exception message is: {3}", ex.GetType(), contentId, contentTypeId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        public string View(string contentId, string contentTypeId)
        {
            Guid id, typeId;
            return Guid.TryParse(contentId, out id) && Guid.TryParse(contentTypeId, out typeId) ? View(id, typeId) : null;
        }

        public string Edit(Guid contentId, Guid contentTypeId)
        {
            try
            {
                if (contentTypeId == DocumentContentType.Id)
                {
                    return PublicApi.SharePointUrls.EditDocument(contentId);
                }
                else if (contentTypeId == ItemContentType.Id)
                {
                    return PublicApi.SharePointUrls.EditListItem(contentId);
                }
            }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the $sharepoint_v1_urls.Edit() method ContentId: {1}, contentTypeId: {2}. The exception message is: {3}", ex.GetType(), contentId, contentTypeId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        public string Edit(string contentId, string contentTypeId)
        {
            Guid id, typeId;
            return Guid.TryParse(contentId, out id) && Guid.TryParse(contentTypeId, out typeId) ? Edit(id, typeId) : null;
        }

        public string Create(Guid applicationId, Guid applicationTypeId)
        {
            try
            {
                if (applicationTypeId == LibraryApplicationType.Id)
                {
                    return PublicApi.SharePointUrls.AddDocument(applicationId);
                }
                else if (applicationTypeId == ListApplicationType.Id)
                {
                    return PublicApi.SharePointUrls.AddListItem(applicationId);
                }
            }
            catch (SPInternalException) { }
            catch (Exception ex)
            {
                string message = string.Format("An exception of type {0} occurred in the $sharepoint_v1_urls.Create() method ApplicationId: {1}, ApplicationTypeId: {2}. The exception message is: {3}", ex.GetType(), applicationId, applicationTypeId, ex.Message);
                SPLog.UnKnownError(ex, message);
            }
            return null;
        }

        public string Create(string applicationId, string applicationTypeId)
        {
            Guid id, typeId;
            return Guid.TryParse(applicationId, out id) && Guid.TryParse(applicationTypeId, out typeId) ? Create(id, typeId) : null;
        }
    }
}