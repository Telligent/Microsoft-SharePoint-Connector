using System;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Events.Version1;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public delegate void ListBeforeCreateEventHandler(ListBeforeCreateEventArgs e);
    public delegate void ListAfterCreateEventHandler(ListAfterCreateEventArgs e);
    public delegate void ListBeforeUpdateEventHandler(ListBeforeUpdateEventArgs e);
    public delegate void ListAfterUpdateEventHandler(ListAfterUpdateEventArgs e);
    public delegate void ListBeforeDeleteEventHandler(ListBeforeDeleteEventArgs e);
    public delegate void ListAfterDeleteEventHandler(ListAfterDeleteEventArgs e);
    public delegate void ListRenderEventHandler(ListRenderEventArgs e);

    public class ListEvents : EventsBase
    {
        #region Create

        private readonly object beforeCreateEvent = new object();

        public event ListBeforeCreateEventHandler BeforeCreate
        {
            add { Add(beforeCreateEvent, value); }
            remove { Remove(beforeCreateEvent, value); }
        }

        internal void OnBeforeCreate(SPList list)
        {
            var handlers = Get<ListBeforeCreateEventHandler>(beforeCreateEvent);
            if (handlers != null)
            {
                var args = new ListBeforeCreateEventArgs(list);
                handlers(args);
            }

            TEApi.Html.Events.OnBeforeCreate(GetHtmlProperties(list));
            TEApi.Content.Events.OnBeforeCreate(list);
        }

        private readonly object afterCreateEvent = new object();

        public event ListAfterCreateEventHandler AfterCreate
        {
            add { Add(afterCreateEvent, value); }
            remove { Remove(afterCreateEvent, value); }
        }

        internal void OnAfterCreate(SPList list)
        {
            var handlers = Get<ListAfterCreateEventHandler>(afterCreateEvent);
            if (handlers != null)
                handlers(new ListAfterCreateEventArgs(list));

            TEApi.Content.Events.OnAfterCreate(list);
        }

        #endregion

        #region Update

        private readonly object beforeUpdateEvent = new object();

        public event ListBeforeUpdateEventHandler BeforeUpdate
        {
            add { Add(beforeUpdateEvent, value); }
            remove { Remove(beforeUpdateEvent, value); }
        }

        internal void OnBeforeUpdate(SPList list)
        {
            var handlers = Get<ListBeforeUpdateEventHandler>(beforeUpdateEvent);
            if (handlers != null)
            {
                var args = new ListBeforeUpdateEventArgs(list);
                handlers(args);
            }

            TEApi.Html.Events.OnBeforeUpdate(GetHtmlProperties(list));
            TEApi.Content.Events.OnBeforeUpdate(list);
        }

        private readonly object afterUpdateEvent = new object();

        public event ListAfterUpdateEventHandler AfterUpdate
        {
            add { Add(afterUpdateEvent, value); }
            remove { Remove(afterUpdateEvent, value); }
        }

        internal void OnAfterUpdate(SPList list)
        {
            var handlers = Get<ListAfterUpdateEventHandler>(afterUpdateEvent);
            if (handlers != null)
                handlers(new ListAfterUpdateEventArgs(list));

            TEApi.Content.Events.OnAfterUpdate(list);
        }

        #endregion

        #region Delete

        private readonly object beforeDeleteEvent = new object();

        public event ListBeforeDeleteEventHandler BeforeDelete
        {
            add { Add(beforeDeleteEvent, value); }
            remove { Remove(beforeDeleteEvent, value); }
        }

        internal void OnBeforeDelete(SPList list)
        {
            var handlers = Get<ListBeforeDeleteEventHandler>(beforeDeleteEvent);
            if (handlers != null)
                handlers(new ListBeforeDeleteEventArgs(list));

            TEApi.Content.Events.OnBeforeDelete(list);
        }

        private readonly object afterDeleteEvent = new object();

        public event ListAfterDeleteEventHandler AfterDelete
        {
            add { Add(afterDeleteEvent, value); }
            remove { Remove(afterDeleteEvent, value); }
        }

        internal void OnAfterDelete(SPList list)
        {
            var handlers = Get<ListAfterDeleteEventHandler>(afterDeleteEvent);
            if (handlers != null)
                handlers(new ListAfterDeleteEventArgs(list));

            TEApi.Content.Events.OnAfterDelete(list);
        }

        #endregion

        #region Render

        private readonly object renderEvent = new object();

        public event ListRenderEventHandler Render
        {
            add { Add(renderEvent, value); }
            remove { Remove(renderEvent, value); }
        }

        internal string OnRender(SPList list, string propertyName, string propertyHtml, string target)
        {
            var handlers = Get<ListRenderEventHandler>(renderEvent);
            if (handlers != null)
            {
                var args = new ListRenderEventArgs(list, propertyName, propertyHtml, target);
                handlers(args);
                propertyHtml = args.RenderedHtml;
            }

            return TEApi.Html.Events.OnRender(propertyName, propertyHtml, target);
        }

        #endregion

        private HtmlProperties GetHtmlProperties(SPList internalEntity)
        {
            return new HtmlProperties()
                .Add("Name", () => internalEntity.Title, html => internalEntity.Title = html, false)
                .Add("Description", () => internalEntity.Description, html => internalEntity.Description = html, true);
        }
    }

    public abstract class ReadOnlyListEventArgsBase
    {
        internal ReadOnlyListEventArgsBase(SPList list)
        {
            InternalEntity = list;
        }

        internal SPList InternalEntity { get; private set; }

        public bool IsEnabled { get { return InternalEntity.IsEnabled; } }
        public bool VersioningEnabled { get { return InternalEntity.EnableVersioning; } }
        public DateTime Modified { get { return InternalEntity.Modified; } }
        public Guid ApplicationId { get { return InternalEntity.ApplicationId; } }
        public Guid ApplicationTypeId { get { return InternalEntity.ApplicationTypeId; } }
        public Guid Id { get { return InternalEntity.Id; } }
        public int ItemCount { get { return InternalEntity.ItemCount; } }
        public int? GroupId { get { return InternalEntity.GroupId; } }
        public string AvatarUrl { get { return InternalEntity.AvatarUrl; } }
        public string Description { get { return InternalEntity.Description; } }
        public string Name { get { return InternalEntity.Title; } }
        public string Root { get { return InternalEntity.RootFolder; } }
        public string SPViewUrl { get { return InternalEntity.SPViewUrl; } }
        public string SPWebUrl { get { return InternalEntity.SPWebUrl; } }
        public string Url { get { return InternalEntity.Url; } }
    }

    public abstract class EditableListEventArgsBase
    {
        internal EditableListEventArgsBase(SPList list)
        {
            InternalEntity = list;
        }

        internal SPList InternalEntity { get; private set; }

        public bool IsEnabled { get { return InternalEntity.IsEnabled; } }
        public bool VersioningEnabled { get { return InternalEntity.EnableVersioning; } }
        public DateTime Modified { get { return InternalEntity.Modified; } }
        public Guid ApplicationId { get { return InternalEntity.ApplicationId; } }
        public Guid ApplicationTypeId { get { return InternalEntity.ApplicationTypeId; } }
        public Guid Id { get { return InternalEntity.Id; } }
        public int ItemCount { get { return InternalEntity.ItemCount; } }
        public int? GroupId { get { return InternalEntity.GroupId; } }
        public string AvatarUrl { get { return InternalEntity.AvatarUrl; } }
        public string Description { get { return InternalEntity.Description; } }
        public string Name { get { return InternalEntity.Title; } }
        public string Root { get { return InternalEntity.RootFolder; } }
        public string SPViewUrl { get { return InternalEntity.SPViewUrl; } }
        public string SPWebUrl { get { return InternalEntity.SPWebUrl; } }
        public string Url { get { return InternalEntity.Url; } }
    }

    public class ListBeforeCreateEventArgs : EditableListEventArgsBase
    {
        internal ListBeforeCreateEventArgs(SPList list) : base(list) { }
    }

    public class ListAfterCreateEventArgs : ReadOnlyListEventArgsBase
    {
        internal ListAfterCreateEventArgs(SPList list) : base(list) { }
    }

    public class ListBeforeUpdateEventArgs : EditableListEventArgsBase
    {
        internal ListBeforeUpdateEventArgs(SPList list) : base(list) { }
    }

    public class ListAfterUpdateEventArgs : ReadOnlyListEventArgsBase
    {
        internal ListAfterUpdateEventArgs(SPList list) : base(list) { }
    }

    public class ListBeforeDeleteEventArgs : ReadOnlyListEventArgsBase
    {
        internal ListBeforeDeleteEventArgs(SPList list) : base(list) { }
    }

    public class ListAfterDeleteEventArgs : ReadOnlyListEventArgsBase
    {
        internal ListAfterDeleteEventArgs(SPList list) : base(list) { }
    }

    public class ListRenderEventArgs : ReadOnlyListEventArgsBase
    {
        internal ListRenderEventArgs(SPList list, string renderedProperty, string renderedHtml, string target)
            : base(list)
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
