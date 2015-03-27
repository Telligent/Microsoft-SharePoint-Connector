using System;
using System.IO;
using System.Xml;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using SP = Microsoft.SharePoint.Client;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class FieldEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_fieldeditor"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IFieldEditor>(); }
        }

        public string Name
        {
            get { return "Field Editor base functionality (sharepoint_v1_fieldeditor)"; }
        }

        public string Description
        {
            get { return "Field Editor base functionality."; }
        }

        public void Initialize() { }
    }

    public interface IFieldEditor
    {
        string TypeOf(SP.Field field);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class FieldEditor : IFieldEditor
    {
        public string TypeOf(SP.Field field)
        {
            //"TaxonomyFieldTypeMulti"
            //"TaxonomyFieldType"
            string fieldType = GetType(field.SchemaXml);
            return fieldType;
        }

        private string GetType(string xml)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            XmlReader xmlReader = XmlReader.Create(new StringReader(xml), readerSettings);
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Field")
                {
                    while (xmlReader.MoveToNextAttribute())
                    {
                        if (xmlReader.Name == "Type")
                        {
                            return xmlReader.Value;
                        }
                    }
                }
            }
            return String.Empty;
        }
    }
}
