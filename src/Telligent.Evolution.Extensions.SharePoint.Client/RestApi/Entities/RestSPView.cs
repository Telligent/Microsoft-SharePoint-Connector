using System;
using System.Linq.Expressions;
using Telligent.Evolution.Extensibility.Rest.Entities.Version1;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Rest.Entities
{
    public class RestSPView : RestEntity<Guid>
    {
        internal static Expression<Func<SP.View, object>>[] InstanceQuery
        {
            get
            {
                return new Expression<Func<SP.View, object>>[]{
                    view => view.Id,
                    view => view.Title};
            }
        }

        public RestSPView()
        {
        }

        internal RestSPView(SP.View view)
        {
            Id = view.Id;
            Title = view.Title;
        }

        public string Title { get; set; }
    }
}
