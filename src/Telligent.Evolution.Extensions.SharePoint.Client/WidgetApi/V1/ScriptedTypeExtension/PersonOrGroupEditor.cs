using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;
using SP = Microsoft.SharePoint.Client;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class PersonOrGroupEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_person"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IPersonOrGroupEditor>(); }
        }

        public string Name
        {
            get { return "Person or Group Editor (sharepoint_v1_person)"; }
        }

        public string Description
        {
            get { return "Person or Group Editor functionality."; }
        }

        public void Initialize() { }
    }

    public interface IPersonOrGroupEditor
    {
        string ListJS(Dictionary<int, string> userCollection);

        Dictionary<int, string> UserCollection(SPList currentList);

        List<string> GetValues(SP.FieldUser field, SPListItem listItem, Dictionary<int, string> userCollection);

        object FromUser(SP.FieldUser field, string users);
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

        [Obsolete("Use sharepoint_v2_person", true)]
        public Dictionary<int, string> UserCollection(SPList currentList)
        {
            var _userCollection = new Dictionary<int, string>();
            using (var clientContext = new SPContext(currentList.SPWebUrl, credentials.Get(currentList.SPWebUrl)))
            {
                SP.Web web = clientContext.Web;
                var usersList = web.SiteUserInfoList.GetItems(SP.CamlQuery.CreateAllItemsQuery());
                clientContext.Load(usersList, _users => _users
                    .Include(_user => _user["Name"], _user => _user.Id));
                clientContext.ExecuteQuery();
                for (int i = 0, len = usersList.Count; i < len; i++)
                {
                    _userCollection.Add(usersList[i].Id, usersList[i]["Name"].ToString());
                }
                return _userCollection;
            }
        }

        [Obsolete("Use sharepoint_v2_person", true)]
        public string ListJS(Dictionary<int, string> userCollection)
        {
            StringBuilder result = new StringBuilder();
            var values = userCollection.Values.ToArray();
            for (int i = 0, len = userCollection.Count; i < len; i++)
            {
                string name = values[i].Replace("\\", "\\\\");
                if (i < len - 1)
                {
                    result.AppendFormat("'{0}',", name);
                }
                else
                {
                    result.AppendFormat("'{0}'", name);
                }
            }
            return result.ToString();
        }

        [Obsolete("Use sharepoint_v2_person", true)]
        public List<string> GetValues(SP.FieldUser field, SPListItem listItem, Dictionary<int, string> userCollection)
        {
            object value = listItem.Value(field.InternalName);

            if (value != null)
            {
                List<SP.FieldUserValue> valueList = null;
                if (field.AllowMultipleValues)
                {
                    valueList = ((SP.FieldUserValue[])value).ToList();
                }
                else
                {
                    valueList = new List<SP.FieldUserValue> { (SP.FieldUserValue)value };
                }
                return valueList.ConvertAll(item => userCollection[item.LookupId]);
            }
            else
            {
                return new List<string>();
            }
        }

        [Obsolete("Use sharepoint_v2_person", true)]
        public object FromUser(SP.FieldUser field, string users)
        {
            object value = null;
            if (!string.IsNullOrEmpty(users))
            {
                if (!field.AllowMultipleValues)
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
    }
}
