using System;
using System.Linq.Expressions;
using Telligent.Evolution.Extensibility.Rest.Entities.Version1;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Entities
{
    public class RestSPList : RestEntity<Guid>
    {
        internal static Expression<Func<SP.List, object>>[] InstanceQuery
        {
            get
            {
                return new Expression<Func<SP.List, object>>[]{
                    list => list.Id,
                    list => list.Title,
                    list => list.Description,
                    list => list.DefaultViewUrl,
                    list => list.Hidden
                };
            }
        }

        public RestSPList()
        {
        }

        internal RestSPList(SP.List splist)
        {
            Id = splist.Id;
            Title = splist.Title;
            Description = splist.Description;
            Url = String.Concat(splist.Context.Url.TrimEnd('/'), "/", splist.DefaultViewUrl.TrimStart('/'));
        }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }
    }
}
