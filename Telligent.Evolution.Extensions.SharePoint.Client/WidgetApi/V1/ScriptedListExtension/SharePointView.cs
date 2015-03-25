using Microsoft.SharePoint.Client;
using System;
using System.Collections;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class SharePointViewExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_view"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISharePointView>(); }
        }

        public string Name
        {
            get { return "SharePoint View Extension (sharepoint_v1_view)"; }
        }

        public string Description
        {
            get { return "This feature allows widgets to use the SharePoint Client Object Model."; }
        }

        public void Initialize() { }
    }

    public interface ISharePointView
    {
        SPView Get(IDictionary options);

        ApiList<SPView> List(SPList list);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class SharePointView : ISharePointView
    {
        private readonly ICredentialsManager credentials;

        internal SharePointView()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        internal SharePointView(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public SPView Get(
            [Documentation(Name = "List", Type = typeof(SPList)),
            Documentation(Name = "ById", Type = typeof(string)),
            Documentation(Name = "ByTitle", Type = typeof(string))]
            IDictionary options)
        {
            SPList list = null;

            if (options != null && options["List"] != null)
            {
                list = (SPList)options["List"];
            }
            else
            {
                return null;
            }

            var byId = (options["ById"] != null) ? options["ById"].ToString() : string.Empty;
            var byTitle = (options["ByTitle"] != null) ? options["ByTitle"].ToString() : string.Empty;

            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                SP.List splist = clientContext.ToList(list.Id);
                var fieldsQuery = clientContext.LoadQuery(splist.Fields.Include(
                    field => field.Title,
                    field => field.InternalName));

                SP.View spview = null;

                if (!string.IsNullOrEmpty(byId))
                {
                    string viewId = options["ById"].ToString();
                    spview = splist.GetView(new Guid(viewId));
                    clientContext.Load(spview);
                    clientContext.Load(spview, SPView.InstanceQuery);
                    clientContext.ExecuteQuery();
                }

                if (spview == null && !string.IsNullOrEmpty(byTitle))
                {
                    string viewTitle = options["ByTitle"].ToString();
                    var viewQuery = clientContext.LoadQuery(splist.Views
                        .Where(view => view.Title == viewTitle)
                        .IncludeWithDefaultProperties(SPView.InstanceQuery));
                    clientContext.ExecuteQuery();
                    spview = viewQuery.FirstOrDefault();
                }

                if (spview != null)
                {
                    return new SPView(spview, fieldsQuery.ToDictionary(item => item.InternalName, item => item.Title));
                }
            }

            return null;
        }

        public ApiList<SPView> List(SPList list)
        {
            using (var clientContext = new SPContext(list.SPWebUrl, credentials.Get(list.SPWebUrl)))
            {
                SP.ViewCollection viewCollection = clientContext.ToList(list.Id).Views;
                var views = clientContext.LoadQuery(viewCollection.Include(SPView.InstanceQuery));
                SP.List splist = clientContext.ToList(list.Id);
                var fieldsQuery = clientContext.LoadQuery(splist.Fields.Include(
                    field => field.Title,
                    field => field.InternalName));
                clientContext.Load(splist,
                    _list => _list.ContentTypes.Include(ct => ct.Fields.SchemaXml),
                    _list => _list.SchemaXml);
                clientContext.ExecuteQuery();
                var spviewCollection = new ApiList<SPView>();
                var columns = fieldsQuery.ToDictionary(item => item.InternalName, item => item.Title);
                foreach (var view in views)
                {
                    if (!view.Hidden)
                    {
                        spviewCollection.Add(new SPView(view, columns));
                    }
                }
                return spviewCollection;
            }
        }
    }
}
