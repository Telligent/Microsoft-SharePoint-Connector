using System;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    class HyperlinkOrPictureEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_hyperlink"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IHyperlinkOrPictureEditor>(); }
        }

        public string Name
        {
            get { return "Hyperlink Or Picture Editor Control (sharepoint_v1_hyperlink)"; }
        }

        public string Description
        {
            get { return "Hyperlink Or Picture Editor functionality"; }
        }

        public void Initialize() { }
    }

    public interface IHyperlinkOrPictureEditor
    {
        SP.FieldUrlValue GetValue(object value);

        string Render(string text);

        SP.FieldUrlValue SetValue(string url, string description, string displayFormat);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class HyperlinkOrPictureEditor : IHyperlinkOrPictureEditor
    {
        public SP.FieldUrlValue GetValue(object value)
        {
            SP.FieldUrlValue val = value as SP.FieldUrlValue;
            if (val == null || !(value is SP.FieldUrlValue))
                val = new SP.FieldUrlValue();
            return val;
        }

        public string Render(string text)
        {
            return String.Format(text, "<a href='#' onclick=\"TestUrl(jQuery(this).next().val()); return false;\">", "</a>");
        }

        public SP.FieldUrlValue SetValue(string url, string description, string displayFormat)
        {
            object value;
            if (url.Trim().ToLower() == "http://" || url.Trim().ToLower() == "https://")
                value = null;
            else
            {
                if (String.IsNullOrEmpty(description) && displayFormat == "Hyperlink")
                    description = url;
                SP.FieldUrlValue v = new SP.FieldUrlValue();
                v.Url = url;
                v.Description = description;
                value = v;
            }
            return value as SP.FieldUrlValue;
        }
    }
}
