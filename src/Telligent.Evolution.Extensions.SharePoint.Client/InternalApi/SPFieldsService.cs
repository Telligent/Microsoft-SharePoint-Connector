using System;
using Telligent.Evolution.Extensions.SharePoint.Components;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface IFieldsService
    {
        Microsoft.SharePoint.Client.Field Get(string url, Guid listId, Guid fieldId);
    }

    internal class SPFieldsService : IFieldsService
    {
        private readonly ICredentialsManager credentials;

        public SPFieldsService() : this(ServiceLocator.Get<ICredentialsManager>()) { }
        public SPFieldsService(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public Microsoft.SharePoint.Client.Field Get(string url, Guid listId, Guid fieldId)
        {
            using (var spcontext = new SPContext(url, credentials.Get(url)))
            {
                var list = spcontext.Web.Lists.GetById(listId);
                var field = list.Fields.GetById(fieldId);
                spcontext.Load(field);
                spcontext.ExecuteQuery();
                return field;
            }
        }
    }
}
