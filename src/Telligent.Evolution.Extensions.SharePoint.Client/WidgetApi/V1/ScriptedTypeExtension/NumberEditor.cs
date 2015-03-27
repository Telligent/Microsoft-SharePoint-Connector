using System.Xml;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class NumberEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_fieldnumber"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<INumberEditor>(); }
        }

        public string Name
        {
            get { return "Number Editor (sharepoint_v1_fieldnumber)"; }
        }

        public string Description
        {
            get { return "Number Editor functionality."; }
        }

        public void Initialize() { }
    }

    public interface INumberEditor
    {
        bool ShowAsPercentage(SP.Field field);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class NumberEditor : INumberEditor
    {
        public bool ShowAsPercentage(SP.Field field)
        {
            bool? _showAsPercentage = null;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(field.SchemaXml);
                string sPercentage = doc.FirstChild.Attributes["Percentage"].Value;
                bool p = false;
                if (bool.TryParse(sPercentage, out p))
                    _showAsPercentage = p;
                else
                    _showAsPercentage = false;
            }
            catch
            {
                _showAsPercentage = false;
            }
            return _showAsPercentage.Value;
        }
    }
}
