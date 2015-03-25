using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class MultiChoiceEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_multichoice"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IMultiChoiceEditor>(); }
        }

        public string Name
        {
            get { return "Multi Choice Editor (sharepoint_v1_multichoice)"; }
        }

        public string Description
        {
            get { return "Multi Choice Editor functionality."; }
        }

        public void Initialize() { }
    }

    public interface IMultiChoiceEditor
    {
        Dictionary<string, string> GetChoices(SP.Field field);
        bool IsSelected(string itemValue, string valueAsText);
        string GetOwnValue(SP.Field field, string valueAsText);
        string[] GetValueToSave(string values, string ownCBValue, string ownValue);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class MultiChoiceEditor : IMultiChoiceEditor
    {
        public Dictionary<string, string> GetChoices(SP.Field field)
        {
            var fieldChoice = field as SP.FieldMultiChoice;
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string item in fieldChoice.Choices)
            {
                result.Add(item, item);
            }
            return result;
        }

        public bool IsSelected(string choiceValue, string valueAsText)
        {
            if (string.IsNullOrEmpty(valueAsText))
                return false;
            string[] values = GetValues(valueAsText); 
            return values.Contains(choiceValue.Trim());
        }

        public string GetOwnValue(SP.Field field, string valueAsText)
        {
            if (string.IsNullOrEmpty(valueAsText))
                return string.Empty;
            string[] values = GetValues(valueAsText);
            foreach (string value in values)
            {
                bool isOwnValue = true;
                foreach (string choiceValue in GetChoices(field).Values)
                {
                    if (choiceValue == value)
                        isOwnValue = false;
                }
                if (isOwnValue)
                    return value;
            }
            return string.Empty;
        }

        string[] GetValues(string valueAsText)
        {
            string[] values = valueAsText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            int i = values.Length;
            while (i-- > 0)
                values[i] = values[i].Trim();
            return values;
        }

        public string[] GetValueToSave(string values, string ownCBValue, string ownValue)
        {
            if (string.IsNullOrEmpty(values))
                return null;
            List<string> valueList = new List<string>(values.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            bool isOwnValue = valueList.Contains(ownCBValue);
            if (isOwnValue)
            {
                valueList.Remove(ownCBValue);
                if (ownValue != null && !string.IsNullOrEmpty(ownValue.Trim()))
                    valueList.Add(ownValue);
            }
            return valueList.ToArray();
        }
    }
}
