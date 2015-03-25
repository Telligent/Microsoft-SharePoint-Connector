using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Data.Log;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version2
{
    public class PersonOrGroupEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v2_person"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IPersonOrGroupEditor>(); }
        }

        public string Name
        {
            get { return "Person or Group (sharepoint_v2_person)"; }
        }

        public string Description
        {
            get { return "Provides Person or Group editor functionality."; }
        }

        public void Initialize() { }
    }

    public interface IPersonOrGroupEditor
    {
        SPUser Get(string url, string name);

        IEnumerable<SPUser> GetValues(string url, string listId, int listItemId, string internalName, bool allowMultipleValues);

        object FromUser(bool allowMultipleValues, string users);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class PersonOrGroupEditor : IPersonOrGroupEditor
    {
        private readonly ICredentialsManager credentials;

        internal PersonOrGroupEditor()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        internal PersonOrGroupEditor(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public SPUser Get(string url, string name)
        {
            using (var clientContext = new SPContext(url, credentials.Get(url)))
            {
                SP.Web web = clientContext.Web;

                // find users with equal account names
                SP.ListItemCollection usersByEqualQuery = web.SiteUserInfoList.GetItems(new SP.CamlQuery
                {
                    ViewXml = ViewQueryWhere(EqualQuery("Name", name, "Text"))
                });

                IEnumerable<SP.ListItem> userItemsByEqualQuery = clientContext.LoadQuery(SP.ClientObjectQueryableExtension.Include(usersByEqualQuery, SPUser.InstanceQuery));

                // find users with the same account names
                SP.ListItemCollection usersByContainsQuery = web.SiteUserInfoList.GetItems(new SP.CamlQuery
                {
                    ViewXml = ViewQueryWhere(ContainsQuery("Name", name, "Text"))
                });
                IEnumerable<SP.ListItem> userItemsByContainsQuery = clientContext.LoadQuery(SP.ClientObjectQueryableExtension.Include(usersByContainsQuery, SPUser.InstanceQuery));

                // find users by display name
                SP.ListItemCollection usersByTitleQuery = web.SiteUserInfoList.GetItems(new SP.CamlQuery
                {
                    ViewXml = ViewQueryWhere(EqualQuery("Title", name, "Text"))
                });
                IEnumerable<SP.ListItem> userItemsByTitleQuery = clientContext.LoadQuery(SP.ClientObjectQueryableExtension.Include(usersByTitleQuery, SPUser.InstanceQuery));

                try
                {
                    clientContext.ExecuteQuery();
                }
                catch (SP.ServerException ex)
                {
                    SPLog.RoleOperationUnavailable(ex, String.Format("A server exception occurred while getting the user with name {0}'. The exception message is: {1}", name, ex.Message));
                    return null;
                }

                SPUser user = TryGet(userItemsByEqualQuery);
                if (user != null)
                {
                    return user;
                }

                user = TryGet(userItemsByContainsQuery);
                if (user != null)
                {
                    return user;
                }

                user = TryGet(userItemsByTitleQuery);
                if (user != null)
                {
                    return user;
                }
                return null;
            }
        }

        public IEnumerable<SPUser> GetValues(string url, string listId, int listItemId, string internalName, bool allowMultipleValues)
        {
            object value = PublicApi.ListItems.Get(Guid.Parse(listId), new SPListItemGetOptions(listItemId) { Url = url }).Value(internalName);

            if (value != null)
            {
                List<SP.FieldUserValue> valueList = allowMultipleValues ? ((SP.FieldUserValue[])value).ToList() : new List<SP.FieldUserValue> { (SP.FieldUserValue)value };
                var userList = valueList.Select(userValue => new SPUser(userValue.LookupId)).ToList();

                using (var clientContext = new SPContext(url, credentials.Get(url)))
                {
                    SP.Web web = clientContext.Web;
                    SP.ListItemCollection users = web.SiteUserInfoList.GetItems(new SP.CamlQuery
                    {
                        ViewXml = CamlQueryBuilder(userList.Select(user => user.LookupId), "ID", "Counter")
                    });
                    IEnumerable<SP.ListItem> userListItems = clientContext.LoadQuery(SP.ClientObjectQueryableExtension.Include(users, SPUser.InstanceQuery));

                    try
                    {
                        clientContext.ExecuteQuery();
                    }
                    catch (SP.ServerException ex)
                    {
                        SPLog.RoleOperationUnavailable(ex, string.Format("A server exception occurred while getting users from '{0}' field. The exception message is: {1}", internalName, ex.Message));
                        return null;
                    }

                    Dictionary<int, SP.ListItem> userHash = userListItems.ToDictionary(item => item.Id);

                    foreach (SPUser user in userList)
                    {
                        if (userHash.ContainsKey(user.LookupId))
                        {
                            user.Initialize(userHash[user.LookupId]);
                        }
                    }

                    return userList;
                }
            }

            return Enumerable.Empty<SPUser>();
        }

        public object FromUser(bool allowMultipleValues, string users)
        {
            object value = null;

            if (!string.IsNullOrEmpty(users))
            {
                if (!allowMultipleValues)
                {
                    value = SP.FieldUserValue.FromUser(users.Trim(';'));
                }
                else
                {
                    var names = users.Trim(';').Split(';');
                    var fieldValue = new SP.FieldUserValue[names.Count()];
                    for (int i = 0; i < names.Length; i++)
                    {
                        fieldValue[i] = SP.FieldUserValue.FromUser(names[i]);
                    }
                    value = fieldValue;
                }
            }
            return value;
        }

        private SPUser TryGet(IEnumerable<SP.ListItem> userItems)
        {
            List<SP.ListItem> userList = userItems.ToList();
            if (userList.Count == 1)
            {
                return new SPUser(userList.FirstOrDefault());
            }
            return null;
        }

        private string CamlQueryBuilder<T>(IEnumerable<T> items, string fieldName, string valueType)
        {
            T[] itemsArr = items.ToArray();
            if (itemsArr.Length == 0) return String.Empty;

            var query = new StringBuilder(EqualQuery(fieldName, itemsArr[0].ToString(), valueType));
            for (int i = 1; i < itemsArr.Length; i++)
            {
                query.Insert(0, "<Or>" + EqualQuery(fieldName, itemsArr[i].ToString(), valueType));
                query.Append("</Or>");
            }
            return ViewQueryWhere(query.ToString());
        }

        private string ViewQueryWhere(string query)
        {
            return String.Format(@"<View><Query><Where>{0}</Where></Query></View>", query);
        }

        private string EqualQuery(string fieldName, string fieldValue, string valueType)
        {
            return String.Format("<Eq><FieldRef Name='{0}' /><Value Type='{2}'>{1}</Value></Eq>", fieldName, fieldValue, valueType);
        }

        private string ContainsQuery(string fieldName, string fieldValue, string valueType)
        {
            return String.Format("<Contains><FieldRef Name='{0}' /><Value Type='{2}'>{1}</Value></Contains>", fieldName, fieldValue, valueType);
        }
    }
}
