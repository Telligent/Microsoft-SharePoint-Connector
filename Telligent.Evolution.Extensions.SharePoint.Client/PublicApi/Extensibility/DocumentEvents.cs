using System;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Events.Version1;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public delegate void DocumentBeforeCreateEventHandler(DocumentBeforeCreateEventArgs e);
    public delegate void DocumentAfterCreateEventHandler(DocumentAfterCreateEventArgs e);
    public delegate void DocumentBeforeUpdateEventHandler(DocumentBeforeUpdateEventArgs e);
    public delegate void DocumentAfterUpdateEventHandler(DocumentAfterUpdateEventArgs e);
    public delegate void DocumentBeforeDeleteEventHandler(DocumentBeforeDeleteEventArgs e);
    public delegate void DocumentAfterDeleteEventHandler(DocumentAfterDeleteEventArgs e);
    public delegate void DocumentRenderEventHandler(DocumentRenderEventArgs e);

    public class DocumentEvents : EventsBase
    {
        #region Create

        private readonly object beforeCreateEvent = new object();

        public event DocumentBeforeCreateEventHandler BeforeCreate
        {
            add { Add(beforeCreateEvent, value); }
            remove { Remove(beforeCreateEvent, value); }
        }

        internal void OnBeforeCreate(Document document)
        {
            var handlers = Get<DocumentBeforeCreateEventHandler>(beforeCreateEvent);
            if (handlers != null)
            {
                var args = new DocumentBeforeCreateEventArgs(document);
                handlers(args);
            }

            TEApi.Html.Events.OnBeforeCreate(GetHtmlProperties(document));
            TEApi.Content.Events.OnBeforeCreate(document);
        }

        private readonly object afterCreateEvent = new object();

        public event DocumentAfterCreateEventHandler AfterCreate
        {
            add { Add(afterCreateEvent, value); }
            remove { Remove(afterCreateEvent, value); }
        }

        internal void OnAfterCreate(Document document)
        {
            var handlers = Get<DocumentAfterCreateEventHandler>(afterCreateEvent);
            if (handlers != null)
                handlers(new DocumentAfterCreateEventArgs(document));

            TEApi.Content.Events.OnAfterCreate(document);
        }

        #endregion

        #region Update

        private readonly object beforeUpdateEvent = new object();

        public event DocumentBeforeUpdateEventHandler BeforeUpdate
        {
            add { Add(beforeUpdateEvent, value); }
            remove { Remove(beforeUpdateEvent, value); }
        }

        internal void OnBeforeUpdate(Document document)
        {
            var handlers = Get<DocumentBeforeUpdateEventHandler>(beforeUpdateEvent);
            if (handlers != null)
            {
                var args = new DocumentBeforeUpdateEventArgs(document);
                handlers(args);
            }

            TEApi.Html.Events.OnBeforeUpdate(GetHtmlProperties(document));
            TEApi.Content.Events.OnBeforeUpdate(document);
        }

        private readonly object afterUpdateEvent = new object();

        public event DocumentAfterUpdateEventHandler AfterUpdate
        {
            add { Add(afterUpdateEvent, value); }
            remove { Remove(afterUpdateEvent, value); }
        }

        internal void OnAfterUpdate(Document document)
        {
            var handlers = Get<DocumentAfterUpdateEventHandler>(afterUpdateEvent);
            if (handlers != null)
                handlers(new DocumentAfterUpdateEventArgs(document));

            TEApi.Content.Events.OnAfterUpdate(document);
        }

        #endregion

        #region Delete

        private readonly object beforeDeleteEvent = new object();

        public event DocumentBeforeDeleteEventHandler BeforeDelete
        {
            add { Add(beforeDeleteEvent, value); }
            remove { Remove(beforeDeleteEvent, value); }
        }

        internal void OnBeforeDelete(Document document)
        {
            var handlers = Get<DocumentBeforeDeleteEventHandler>(beforeDeleteEvent);
            if (handlers != null)
                handlers(new DocumentBeforeDeleteEventArgs(document));

            TEApi.Content.Events.OnBeforeDelete(document);
        }

        private readonly object afterDeleteEvent = new object();

        public event DocumentAfterDeleteEventHandler AfterDelete
        {
            add { Add(afterDeleteEvent, value); }
            remove { Remove(afterDeleteEvent, value); }
        }

        internal void OnAfterDelete(Document document)
        {
            var handlers = Get<DocumentAfterDeleteEventHandler>(afterDeleteEvent);
            if (handlers != null)
                handlers(new DocumentAfterDeleteEventArgs(document));

            TEApi.Content.Events.OnAfterDelete(document);
        }

        #endregion

        #region Render

        private readonly object renderEvent = new object();

        public event DocumentRenderEventHandler Render
        {
            add { Add(renderEvent, value); }
            remove { Remove(renderEvent, value); }
        }

        internal string OnRender(Document document, string propertyName, string propertyHtml, string target)
        {
            var handlers = Get<DocumentRenderEventHandler>(renderEvent);
            if (handlers != null)
            {
                var args = new DocumentRenderEventArgs(document, propertyName, propertyHtml, target);
                handlers(args);
                propertyHtml = args.RenderedHtml;
            }

            return TEApi.Html.Events.OnRender(propertyName, propertyHtml, target);
        }

        #endregion

        private HtmlProperties GetHtmlProperties(Document internalEntity)
        {
            return new HtmlProperties()
                .Add("Name", () => internalEntity.Name, html => internalEntity.Name = html, false)
                .Add("Title", () => internalEntity.Title, html => internalEntity.Title = html, true)
                .Add("Path", () => internalEntity.Path, html => internalEntity.Path = html, true);
        }
    }

    public abstract class ReadOnlyDocumentEventArgsBase
    {
        internal ReadOnlyDocumentEventArgsBase(Document document)
        {
            InternalEntity = document;
        }

        internal Document InternalEntity { get; private set; }

        public Author Author { get { return InternalEntity.Author; } }
        public Author Editor { get { return InternalEntity.Editor; } }
        public bool IsFolder { get { return InternalEntity.IsFolder; } }
        public DateTime CreatedDate { get { return InternalEntity.CreatedDate; } }
        public DateTime Modified { get { return InternalEntity.Modified; } }
        public Guid ContentId { get { return InternalEntity.ContentId; } }
        public int Id { get { return InternalEntity.Id; } }
        public int? CreatedByUserId { get { return InternalEntity.CreatedByUserId; } }
        public Library Library { get { return InternalEntity.Library; } }
        public string AvatarUrl { get { return InternalEntity.AvatarUrl; } }
        public string DocIcon { get { return InternalEntity.DocIcon; } }
        public string MetaInfo { get { return InternalEntity.MetaInfo; } }
        public string Name { get { return InternalEntity.Name; } }
        public string Path { get { return InternalEntity.Path; } }
        public string Title { get { return InternalEntity.Title; } }
        public string Url { get { return InternalEntity.Url; } }
    }

    public abstract class EditableDocumentEventArgsBase
    {
        internal EditableDocumentEventArgsBase(Document document)
        {
            InternalEntity = document;
        }

        internal Document InternalEntity { get; private set; }

        public Author Author { get { return InternalEntity.Author; } }
        public Author Editor { get { return InternalEntity.Editor; } }
        public bool IsFolder { get { return InternalEntity.IsFolder; } }
        public DateTime CreatedDate { get { return InternalEntity.CreatedDate; } }
        public DateTime Modified { get { return InternalEntity.Modified; } }
        public Guid ContentId { get { return InternalEntity.ContentId; } }
        public int Id { get { return InternalEntity.Id; } }
        public int? CreatedByUserId { get { return InternalEntity.CreatedByUserId; } }
        public Library Library { get { return InternalEntity.Library; } }
        public string AvatarUrl { get { return InternalEntity.AvatarUrl; } }
        public string DocIcon { get { return InternalEntity.DocIcon; } }
        public string MetaInfo { get { return InternalEntity.MetaInfo; } }
        public string Name { get { return InternalEntity.Name; } }
        public string Path { get { return InternalEntity.Path; } }
        public string Title { get { return InternalEntity.Title; } }
        public string Url { get { return InternalEntity.Url; } }
    }

    public class DocumentBeforeCreateEventArgs : EditableDocumentEventArgsBase
    {
        internal DocumentBeforeCreateEventArgs(Document document) : base(document) { }
    }

    public class DocumentAfterCreateEventArgs : ReadOnlyDocumentEventArgsBase
    {
        internal DocumentAfterCreateEventArgs(Document document) : base(document) { }
    }

    public class DocumentBeforeUpdateEventArgs : EditableDocumentEventArgsBase
    {
        internal DocumentBeforeUpdateEventArgs(Document document) : base(document) { }
    }

    public class DocumentAfterUpdateEventArgs : ReadOnlyDocumentEventArgsBase
    {
        internal DocumentAfterUpdateEventArgs(Document document) : base(document) { }
    }

    public class DocumentBeforeDeleteEventArgs : ReadOnlyDocumentEventArgsBase
    {
        internal DocumentBeforeDeleteEventArgs(Document document) : base(document) { }
    }

    public class DocumentAfterDeleteEventArgs : ReadOnlyDocumentEventArgsBase
    {
        internal DocumentAfterDeleteEventArgs(Document document) : base(document) { }
    }

    public class DocumentRenderEventArgs : ReadOnlyDocumentEventArgsBase
    {
        internal DocumentRenderEventArgs(Document document, string renderedProperty, string renderedHtml, string target)
            : base(document)
        {
            RenderedHtml = renderedHtml;
            RenderedProperty = renderedProperty;
            RenderTarget = target;
        }

        public string RenderedProperty { get; private set; }
        public string RenderedHtml { get; set; }
        public string RenderTarget { get; private set; }
    }
}
