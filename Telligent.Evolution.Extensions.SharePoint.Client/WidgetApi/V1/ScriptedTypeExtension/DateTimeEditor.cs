using System;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    class DateTimeEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_datetimeeditor"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IDateTimeEditor>(); }
        }

        public string Name
        {
            get { return "Date and Time Editor Control (sharepoint_v1_datetimeeditor)"; }
        }

        public string Description
        {
            get { return "Date and Time Editor Control functionality"; }
        }

        public void Initialize() { }
    }

    public interface IDateTimeEditor
    {
        DateTime? GetValueToSave(string value);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class DateTimeEditor : IDateTimeEditor
    {
        public DateTime? GetValueToSave(string value)
        {
            DateTime dt;
            if (DateTime.TryParseExact(value, new string[] { "MM/dd/yyyy", "MM/dd/yyyy hh:mm tt" }, System.Globalization.DateTimeFormatInfo.CurrentInfo, System.Globalization.DateTimeStyles.None, out dt))
                return dt;
            return null;
        }
    }
}
