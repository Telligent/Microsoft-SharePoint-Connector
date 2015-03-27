using System;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Events.Version1;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public delegate void LibraryBeforeCreateEventHandler(LibraryBeforeCreateEventArgs e);
    public delegate void LibraryAfterCreateEventHandler(LibraryAfterCreateEventArgs e);
    public delegate void LibraryBeforeUpdateEventHandler(LibraryBeforeUpdateEventArgs e);
    public delegate void LibraryAfterUpdateEventHandler(LibraryAfterUpdateEventArgs e);
    public delegate void LibraryBeforeDeleteEventHandler(LibraryBeforeDeleteEventArgs e);
    public delegate void LibraryAfterDeleteEventHandler(LibraryAfterDeleteEventArgs e);
    public delegate void LibraryRenderEventHandler(LibraryRenderEventArgs e);

    public class LibraryEvents : EventsBase
    {
        #region Create

        private readonly object beforeCreateEvent = new object();

        public event LibraryBeforeCreateEventHandler BeforeCreate
        {
            add { Add(beforeCreateEvent, value); }
            remove { Remove(beforeCreateEvent, value); }
        }

        internal void OnBeforeCreate(Library library)
        {
            var handlers = Get<LibraryBeforeCreateEventHandler>(beforeCreateEvent);
            if (handlers != null)
            {
                var args = new LibraryBeforeCreateEventArgs(library);
                handlers(args);
            }

            TEApi.Html.Events.OnBeforeCreate(GetHtmlProperties(library));
            TEApi.Content.Events.OnBeforeCreate(library);
        }

        private readonly object afterCreateEvent = new object();

        public event LibraryAfterCreateEventHandler AfterCreate
        {
            add { Add(afterCreateEvent, value); }
            remove { Remove(afterCreateEvent, value); }
        }

        internal void OnAfterCreate(Library library)
        {
            var handlers = Get<LibraryAfterCreateEventHandler>(afterCreateEvent);
            if (handlers != null)
                handlers(new LibraryAfterCreateEventArgs(library));

            TEApi.Content.Events.OnAfterCreate(library);
        }

        #endregion

        #region Update

        private readonly object beforeUpdateEvent = new object();

        public event LibraryBeforeUpdateEventHandler BeforeUpdate
        {
            add { Add(beforeUpdateEvent, value); }
            remove { Remove(beforeUpdateEvent, value); }
        }

        internal void OnBeforeUpdate(Library library)
        {
            var handlers = Get<LibraryBeforeUpdateEventHandler>(beforeUpdateEvent);
            if (handlers != null)
            {
                var args = new LibraryBeforeUpdateEventArgs(library);
                handlers(args);
            }

            TEApi.Html.Events.OnBeforeUpdate(GetHtmlProperties(library));
            TEApi.Content.Events.OnBeforeUpdate(library);
        }

        private readonly object afterUpdateEvent = new object();

        public event LibraryAfterUpdateEventHandler AfterUpdate
        {
            add { Add(afterUpdateEvent, value); }
            remove { Remove(afterUpdateEvent, value); }
        }

        internal void OnAfterUpdate(Library library)
        {
            var handlers = Get<LibraryAfterUpdateEventHandler>(afterUpdateEvent);
            if (handlers != null)
                handlers(new LibraryAfterUpdateEventArgs(library));

            TEApi.Content.Events.OnAfterUpdate(library);
        }

        #endregion

        #region Delete

        private readonly object beforeDeleteEvent = new object();

        public event LibraryBeforeDeleteEventHandler BeforeDelete
        {
            add { Add(beforeDeleteEvent, value); }
            remove { Remove(beforeDeleteEvent, value); }
        }

        internal void OnBeforeDelete(Library library)
        {
            var handlers = Get<LibraryBeforeDeleteEventHandler>(beforeDeleteEvent);
            if (handlers != null)
                handlers(new LibraryBeforeDeleteEventArgs(library));

            TEApi.Content.Events.OnBeforeDelete(library);
        }

        private readonly object afterDeleteEvent = new object();

        public event LibraryAfterDeleteEventHandler AfterDelete
        {
            add { Add(afterDeleteEvent, value); }
            remove { Remove(afterDeleteEvent, value); }
        }

        internal void OnAfterDelete(Library library)
        {
            var handlers = Get<LibraryAfterDeleteEventHandler>(afterDeleteEvent);
            if (handlers != null)
                handlers(new LibraryAfterDeleteEventArgs(library));

            TEApi.Content.Events.OnAfterDelete(library);
        }

        #endregion

        #region Render

        private readonly object renderEvent = new object();

        public event LibraryRenderEventHandler Render
        {
            add { Add(renderEvent, value); }
            remove { Remove(renderEvent, value); }
        }

        internal string OnRender(Library library, string propertyName, string propertyHtml, string target)
        {
            var handlers = Get<LibraryRenderEventHandler>(renderEvent);
            if (handlers != null)
            {
                var args = new LibraryRenderEventArgs(library, propertyName, propertyHtml, target);
                handlers(args);
                propertyHtml = args.RenderedHtml;
            }

            return TEApi.Html.Events.OnRender(propertyName, propertyHtml, target);
        }

        #endregion

        private HtmlProperties GetHtmlProperties(Library internalEntity)
        {
            return new HtmlProperties()
                .Add("Name", () => internalEntity.Name, html => internalEntity.Name = html, false)
                .Add("Description", () => internalEntity.Description, html => internalEntity.Description = html, true);
        }
    }

    public abstract class ReadOnlyLibraryEventArgsBase
    {
        internal ReadOnlyLibraryEventArgsBase(Library library)
        {
            InternalEntity = library;
        }

        internal Library InternalEntity { get; private set; }

        public bool IsEnabled { get { return InternalEntity.IsEnabled; } }
        public bool VersioningEnabled { get { return InternalEntity.VersioningEnabled; } }
        public DateTime Modified { get { return InternalEntity.Modified; } }
        public Guid ApplicationId { get { return InternalEntity.ApplicationId; } }
        public Guid ApplicationTypeId { get { return InternalEntity.ApplicationTypeId; } }
        public Guid Id { get { return InternalEntity.Id; } }
        public int ItemCount { get { return InternalEntity.ItemCount; } }
        public int? GroupId { get { return InternalEntity.GroupId; } }
        public string AvatarUrl { get { return InternalEntity.AvatarUrl; } }
        public string Description { get { return InternalEntity.Description; } }
        public string Name { get { return InternalEntity.Name; } }
        public string Root { get { return InternalEntity.Root; } }
        public string SPViewUrl { get { return InternalEntity.SPViewUrl; } }
        public string SPWebUrl { get { return InternalEntity.SPWebUrl; } }
        public string Url { get { return InternalEntity.Url; } }
    }

    public abstract class EditableLibraryEventArgsBase
    {
        internal EditableLibraryEventArgsBase(Library library)
        {
            InternalEntity = library;
        }

        internal Library InternalEntity { get; private set; }

        public bool IsEnabled { get { return InternalEntity.IsEnabled; } }
        public bool VersioningEnabled { get { return InternalEntity.VersioningEnabled; } }
        public DateTime Modified { get { return InternalEntity.Modified; } }
        public Guid ApplicationId { get { return InternalEntity.ApplicationId; } }
        public Guid ApplicationTypeId { get { return InternalEntity.ApplicationTypeId; } }
        public Guid Id { get { return InternalEntity.Id; } }
        public int ItemCount { get { return InternalEntity.ItemCount; } }
        public int? GroupId { get { return InternalEntity.GroupId; } }
        public string AvatarUrl { get { return InternalEntity.AvatarUrl; } }
        public string Description { get { return InternalEntity.Description; } }
        public string Name { get { return InternalEntity.Name; } }
        public string Root { get { return InternalEntity.Root; } }
        public string SPViewUrl { get { return InternalEntity.SPViewUrl; } }
        public string SPWebUrl { get { return InternalEntity.SPWebUrl; } }
        public string Url { get { return InternalEntity.Url; } }
    }

    public class LibraryBeforeCreateEventArgs : EditableLibraryEventArgsBase
    {
        internal LibraryBeforeCreateEventArgs(Library library) : base(library) { }
    }

    public class LibraryAfterCreateEventArgs : ReadOnlyLibraryEventArgsBase
    {
        internal LibraryAfterCreateEventArgs(Library library) : base(library) { }
    }

    public class LibraryBeforeUpdateEventArgs : EditableLibraryEventArgsBase
    {
        internal LibraryBeforeUpdateEventArgs(Library library) : base(library) { }
    }

    public class LibraryAfterUpdateEventArgs : ReadOnlyLibraryEventArgsBase
    {
        internal LibraryAfterUpdateEventArgs(Library library) : base(library) { }
    }

    public class LibraryBeforeDeleteEventArgs : ReadOnlyLibraryEventArgsBase
    {
        internal LibraryBeforeDeleteEventArgs(Library library) : base(library) { }
    }

    public class LibraryAfterDeleteEventArgs : ReadOnlyLibraryEventArgsBase
    {
        internal LibraryAfterDeleteEventArgs(Library library) : base(library) { }
    }

    public class LibraryRenderEventArgs : ReadOnlyLibraryEventArgsBase
    {
        internal LibraryRenderEventArgs(Library library, string renderedProperty, string renderedHtml, string target)
            : base(library)
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
