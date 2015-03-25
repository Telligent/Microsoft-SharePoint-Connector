using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    class ChoiceEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_choice"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IChoiceEditor>(); }
        }

        public string Name
        {
            get { return "Choice Editor (sharepoint_v1_choice)"; }
        }

        public string Description
        {
            get { return "Choice Editor functionality."; }
        }

        public void Initialize() { }
    }

    public interface IChoiceEditor
    {
        Dictionary<string, string> GetChoices(SP.Field field);

        bool IsOwnValue(SP.Field field, string valueAsText);

        bool CheckMultiKey(string key, string valueAsText);

        string GetMultiOwnValue(SP.Field field, string valueAsText);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class ChoiceEditor : IChoiceEditor
    {
        public Dictionary<string, string> GetChoices(SP.Field field)
        {
            var fieldChoice = field as SP.FieldChoice;
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!fieldChoice.Required)
            {
                if (fieldChoice.EditFormat == SP.ChoiceFormatType.Dropdown)
                    result.Add(string.Empty, string.Empty);
                else
                    result.Add(string.Empty, "{Not selected}");
            }
            foreach (string item in fieldChoice.Choices)
            {
                result.Add(item, item);
            }
            return result;
        }

        public bool IsOwnValue(SP.Field field, string valueAsText)
        {
            var fieldChoice = field as SP.FieldChoice;
            if (!fieldChoice.FillInChoice || string.IsNullOrEmpty(valueAsText))
                return false;
            foreach (string key in fieldChoice.Choices)
                if (key == valueAsText)
                    return false;
            return true;
        }

        public bool CheckMultiKey(string key, string valueAsText)
        {
            if (string.IsNullOrEmpty(valueAsText))
                return false;
            List<string> values = new List<string>(valueAsText.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries));
            return values.Contains(key);
        }

        public string GetMultiOwnValue(SP.Field field, string valueAsText)
        {
            if (string.IsNullOrEmpty(valueAsText))
                return string.Empty;
            string[] values = valueAsText.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            var fieldChoice = field as SP.FieldChoice;
            foreach (string value in values)
            {
                bool isOwnValue = true;
                foreach (string key in fieldChoice.Choices)
                {
                    if (key == value)
                        isOwnValue = false;
                }
                if (isOwnValue)
                    return value;
            }
            return string.Empty;
        }
    }
}
