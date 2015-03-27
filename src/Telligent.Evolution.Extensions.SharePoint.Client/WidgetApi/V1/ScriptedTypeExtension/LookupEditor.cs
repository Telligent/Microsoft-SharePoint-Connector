using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    class LookupEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_lookup"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ILookupEditor>(); }
        }

        public string Name
        {
            get { return "Lookup Editor Control (sharepoint_v1_lookup)"; }
        }

        public string Description
        {
            get { return "Lookup Editor functionality."; }
        }

        public void Initialize() { }
    }

    public interface ILookupEditor
    {
        SP.FieldLookupValue[] GetSelectedValues(SPListItem listItem, SP.Field field);

        Dictionary<string, string> GetValues(SPList currentList, object listItem, SP.Field field, bool removeSelected);

        SP.FieldLookupValue[] GetValueToSave(string sValue);

        bool IsSelected(SPListItem listItem, SP.Field field, string value);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class LookupEditor : ILookupEditor
    {
        private readonly ICredentialsManager credentials;

        internal LookupEditor()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        internal LookupEditor(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public SP.FieldLookupValue[] GetSelectedValues(SPListItem listItem, SP.Field field)
        {
            if (listItem == null)
                return new SP.FieldLookupValue[0];
            return (SP.FieldLookupValue[])listItem.Value(field.InternalName);
        }

        public Dictionary<string, string> GetValues(SPList currentList, object splistItem, SP.Field field, bool removeSelected)
        {
            SPListItem listItem = splistItem as SPListItem;
            SP.FieldLookup lookupField = field as SP.FieldLookup;
            if (lookupField == null)
                return new Dictionary<string, string>();
            using (var clientContext = new SPContext(currentList.SPWebUrl, credentials.Get(currentList.SPWebUrl)))
            {
                SP.Web lookupWeb = clientContext.Site.OpenWebById(lookupField.LookupWebId);
                clientContext.Load(lookupWeb);
                SP.List list = lookupWeb.Lists.GetById(new Guid(lookupField.LookupList));
                clientContext.Load(list);
                SP.ListItemCollection items = list.GetItems(SP.CamlQuery.CreateAllItemsQuery());
                clientContext.Load(items);
                clientContext.ExecuteQuery();
                SP.FieldLookupValue[] selectedValues = null;
                if (removeSelected)
                {
                    selectedValues = GetSelectedValues(listItem, field);
                }
                Dictionary<string, string> values = new Dictionary<string, string>();
                foreach (SP.ListItem item in items)
                {
                    if (removeSelected && selectedValues.Any(v => v.LookupId == item.Id))
                        continue;
                    object lookupVal = item[lookupField.LookupField];
                    if (lookupVal != null)
                        values.Add(item.Id.ToString(), (string)lookupVal);
                }
                return values;
            }
        }

        public SP.FieldLookupValue[] GetValueToSave(string sValue)
        {
            string[] sVal = sValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<SP.FieldLookupValue> values = new List<SP.FieldLookupValue>();
            foreach (string v in sVal)
            {
                values.Add(new SP.FieldLookupValue() { LookupId = int.Parse(v) });
            }
            return values.ToArray();
        }

        public bool IsSelected(SPListItem listItem, SP.Field field, string value)
        {
            if (listItem == null)
                return false;
            object obj = listItem.Value(field.InternalName);
            if (obj == null)
                return false;
            SP.FieldLookupValue lookupVal = obj as SP.FieldLookupValue;
            if (lookupVal == null)
                return false;
            return lookupVal.LookupId.ToString() == value;
        }
    }
}
