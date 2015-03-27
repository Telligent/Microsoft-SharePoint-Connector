using System;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IContextService
    {
        Guid LibraryId { get; }
        Guid DocumentId { get; }
        Guid ListId { get; }
        Guid ListItemId { get; }
    }

    internal class SPContextService : IContextService
    {
        #region Document Library

        public Guid LibraryId
        {
            get
            {
                var currentContext = Extensibility.Api.Version1.PublicApi.Url.CurrentContext;
                if (currentContext != null && currentContext.ContextItems != null)
                {
                    var context = currentContext.ContextItems.GetItemByApplicationType(LibraryApplicationType.Id) ?? currentContext.ContextItems.GetItemByContentType(DocumentContentType.Id);
                    if (context != null && context.ApplicationId.HasValue)
                        return context.ApplicationId.Value;
                }
                return Guid.Empty;
            }
        }

        public Guid DocumentId
        {
            get
            {
                var currentContext = Extensibility.Api.Version1.PublicApi.Url.CurrentContext;
                if (currentContext != null && currentContext.ContextItems != null)
                {
                    var context = currentContext.ContextItems.GetItemByContentType(DocumentContentType.Id);
                    if (context != null && context.ContentId.HasValue)
                        return context.ContentId.Value;
                }
                return Guid.Empty;
            }
        }

        #endregion

        #region ListItems

        public Guid ListId
        {
            get
            {
                var currentContext = Extensibility.Api.Version1.PublicApi.Url.CurrentContext;
                if (currentContext != null && currentContext.ContextItems != null)
                {
                    var context = currentContext.ContextItems.GetItemByApplicationType(ListApplicationType.Id) ?? currentContext.ContextItems.GetItemByContentType(ItemContentType.Id) ?? currentContext.ContextItems.GetItemByApplicationType(LibraryApplicationType.Id) ?? currentContext.ContextItems.GetItemByContentType(DocumentContentType.Id);
                    if (context != null && context.ApplicationId.HasValue)
                        return context.ApplicationId.Value;
                }
                return Guid.Empty;
            }
        }

        public Guid ListItemId
        {
            get
            {
                var currentContext = Extensibility.Api.Version1.PublicApi.Url.CurrentContext;
                if (currentContext != null && currentContext.ContextItems != null)
                {
                    var context = currentContext.ContextItems.GetItemByContentType(ItemContentType.Id) ?? currentContext.ContextItems.GetItemByContentType(DocumentContentType.Id);
                    if (context != null && context.ContentId.HasValue)
                        return context.ContentId.Value;
                }
                return Guid.Empty;
            }
        }

        #endregion
    }
}
