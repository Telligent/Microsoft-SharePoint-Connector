using System;
using System.Collections.Generic;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Events.Version1;
using TEApi = Telligent.Evolution.Extensibility.Api.Version1.PublicApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public delegate void ListItemBeforeCreateEventHandler(ListItemBeforeCreateEventArgs e);
    public delegate void ListItemAfterCreateEventHandler(ListItemAfterCreateEventArgs e);
    public delegate void ListItemBeforeUpdateEventHandler(ListItemBeforeUpdateEventArgs e);
    public delegate void ListItemAfterUpdateEventHandler(ListItemAfterUpdateEventArgs e);
    public delegate void ListItemBeforeDeleteEventHandler(ListItemBeforeDeleteEventArgs e);
    public delegate void ListItemAfterDeleteEventHandler(ListItemAfterDeleteEventArgs e);
    public delegate void ListItemRenderEventHandler(ListItemRenderEventArgs e);

    public class ListItemEvents : EventsBase
    {
        #region Create

        private readonly object beforeCreateEvent = new object();

        public event ListItemBeforeCreateEventHandler BeforeCreate
        {
            add { Add(beforeCreateEvent, value); }
            remove { Remove(beforeCreateEvent, value); }
        }

        internal void OnBeforeCreate(SPListItem listItem)
        {
            var handlers = Get<ListItemBeforeCreateEventHandler>(beforeCreateEvent);
            if (handlers != null)
            {
                var args = new ListItemBeforeCreateEventArgs(listItem);
                handlers(args);
            }

            TEApi.Html.Events.OnBeforeCreate(GetHtmlProperties(listItem));
            TEApi.Content.Events.OnBeforeCreate(listItem);
        }

        private readonly object afterCreateEvent = new object();

        public event ListItemAfterCreateEventHandler AfterCreate
        {
            add { Add(afterCreateEvent, value); }
            remove { Remove(afterCreateEvent, value); }
        }

        internal void OnAfterCreate(SPListItem listItem)
        {
            var handlers = Get<ListItemAfterCreateEventHandler>(afterCreateEvent);
            if (handlers != null)
                handlers(new ListItemAfterCreateEventArgs(listItem));

            TEApi.Content.Events.OnAfterCreate(listItem);
        }

        #endregion

        #region Update

        private readonly object beforeUpdateEvent = new object();

        public event ListItemBeforeUpdateEventHandler BeforeUpdate
        {
            add { Add(beforeUpdateEvent, value); }
            remove { Remove(beforeUpdateEvent, value); }
        }

        internal void OnBeforeUpdate(SPListItem listItem)
        {
            var handlers = Get<ListItemBeforeUpdateEventHandler>(beforeUpdateEvent);
            if (handlers != null)
            {
                var args = new ListItemBeforeUpdateEventArgs(listItem);
                handlers(args);
            }

            TEApi.Html.Events.OnBeforeUpdate(GetHtmlProperties(listItem));
            TEApi.Content.Events.OnBeforeUpdate(listItem);
        }

        private readonly object afterUpdateEvent = new object();

        public event ListItemAfterUpdateEventHandler AfterUpdate
        {
            add { Add(afterUpdateEvent, value); }
            remove { Remove(afterUpdateEvent, value); }
        }

        internal void OnAfterUpdate(SPListItem listItem)
        {
            var handlers = Get<ListItemAfterUpdateEventHandler>(afterUpdateEvent);
            if (handlers != null)
                handlers(new ListItemAfterUpdateEventArgs(listItem));

            TEApi.Content.Events.OnAfterUpdate(listItem);
        }

        #endregion

        #region Delete

        private readonly object beforeDeleteEvent = new object();

        public event ListItemBeforeDeleteEventHandler BeforeDelete
        {
            add { Add(beforeDeleteEvent, value); }
            remove { Remove(beforeDeleteEvent, value); }
        }

        internal void OnBeforeDelete(SPListItem listItem)
        {
            var handlers = Get<ListItemBeforeDeleteEventHandler>(beforeDeleteEvent);
            if (handlers != null)
                handlers(new ListItemBeforeDeleteEventArgs(listItem));

            TEApi.Content.Events.OnBeforeDelete(listItem);
        }

        private readonly object afterDeleteEvent = new object();

        public event ListItemAfterDeleteEventHandler AfterDelete
        {
            add { Add(afterDeleteEvent, value); }
            remove { Remove(afterDeleteEvent, value); }
        }

        internal void OnAfterDelete(SPListItem listItem)
        {
            var handlers = Get<ListItemAfterDeleteEventHandler>(afterDeleteEvent);
            if (handlers != null)
                handlers(new ListItemAfterDeleteEventArgs(listItem));

            TEApi.Content.Events.OnAfterDelete(listItem);
        }

        #endregion

        #region Render

        private readonly object renderEvent = new object();

        public event ListItemRenderEventHandler Render
        {
            add { Add(renderEvent, value); }
            remove { Remove(renderEvent, value); }
        }

        internal string OnRender(SPListItem listItem, string propertyName, string propertyHtml, string target)
        {
            var handlers = Get<ListItemRenderEventHandler>(renderEvent);
            if (handlers != null)
            {
                var args = new ListItemRenderEventArgs(listItem, propertyName, propertyHtml, target);
                handlers(args);
                propertyHtml = args.RenderedHtml;
            }

            return TEApi.Html.Events.OnRender(propertyName, propertyHtml, target);
        }

        #endregion

        private HtmlProperties GetHtmlProperties(SPListItem internalEntity)
        {
            return new HtmlProperties()
                .Add("Name", () => internalEntity.DisplayName, html => internalEntity.DisplayName = html, false);
        }
    }

    public abstract class ReadOnlyListItemEventArgsBase
    {
        internal ReadOnlyListItemEventArgsBase(SPListItem listItem)
        {
            InternalEntity = listItem;
        }

        internal SPListItem InternalEntity { get; private set; }

        public Author Author { get { return InternalEntity != null ? InternalEntity.Author : null; } }
        public Author Editor { get { return InternalEntity != null ? InternalEntity.Editor : null; } }
        public DateTime CreatedDate { get { return InternalEntity != null ? InternalEntity.CreatedDate : DateTime.Now; } }
        public Guid ContentId { get { return InternalEntity != null ? InternalEntity.ContentId : Guid.Empty; } }
        public int Id { get { return InternalEntity != null ? InternalEntity.Id : -1; } }
        public int? CreatedByUserId { get { return InternalEntity != null ? InternalEntity.CreatedByUserId : null; } }
        public Guid ListId { get { return InternalEntity != null ? InternalEntity.ListId : Guid.Empty; } }
        public string AvatarUrl { get { return InternalEntity != null ? InternalEntity.AvatarUrl : null; } }
        public string Title { get { return InternalEntity != null ? InternalEntity.DisplayName : null; } }
        public string Url { get { return InternalEntity != null ? InternalEntity.Url : null; } }
        public List<Field> EditableFields() { return InternalEntity != null ? InternalEntity.EditableFields() : null; }
        public string ValueAsText(string fieldName) { return InternalEntity != null ? InternalEntity.ValueAsText(fieldName) : null; }
    }

    public abstract class EditableListItemEventArgsBase
    {
        internal EditableListItemEventArgsBase(SPListItem listItem)
        {
            InternalEntity = listItem;
        }

        internal SPListItem InternalEntity { get; private set; }

        public Author Author { get { return InternalEntity.Author; } }
        public Author Editor { get { return InternalEntity.Editor; } }
        public DateTime CreatedDate { get { return InternalEntity.CreatedDate; } }
        public Guid ContentId { get { return InternalEntity.ContentId; } }
        public int Id { get { return InternalEntity.Id; } }
        public int? CreatedByUserId { get { return InternalEntity.CreatedByUserId; } }
        public Guid ListId { get { return InternalEntity.ListId; } }
        public string AvatarUrl { get { return InternalEntity.AvatarUrl; } }
        public string Title { get { return InternalEntity.DisplayName; } }
        public string Url { get { return InternalEntity.Url; } }
    }

    public class ListItemBeforeCreateEventArgs : EditableListItemEventArgsBase
    {
        internal ListItemBeforeCreateEventArgs(SPListItem listItem) : base(listItem) { }
    }

    public class ListItemAfterCreateEventArgs : ReadOnlyListItemEventArgsBase
    {
        internal ListItemAfterCreateEventArgs(SPListItem listItem) : base(listItem) { }
    }

    public class ListItemBeforeUpdateEventArgs : EditableListItemEventArgsBase
    {
        internal ListItemBeforeUpdateEventArgs(SPListItem listItem) : base(listItem) { }
    }

    public class ListItemAfterUpdateEventArgs : ReadOnlyListItemEventArgsBase
    {
        internal ListItemAfterUpdateEventArgs(SPListItem listItem) : base(listItem) { }
    }

    public class ListItemBeforeDeleteEventArgs : ReadOnlyListItemEventArgsBase
    {
        internal ListItemBeforeDeleteEventArgs(SPListItem listItem) : base(listItem) { }
    }

    public class ListItemAfterDeleteEventArgs : ReadOnlyListItemEventArgsBase
    {
        internal ListItemAfterDeleteEventArgs(SPListItem listItem) : base(listItem) { }
    }

    public class ListItemRenderEventArgs : ReadOnlyListItemEventArgsBase
    {
        internal ListItemRenderEventArgs(SPListItem listItem, string renderedProperty, string renderedHtml, string target)
            : base(listItem)
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
