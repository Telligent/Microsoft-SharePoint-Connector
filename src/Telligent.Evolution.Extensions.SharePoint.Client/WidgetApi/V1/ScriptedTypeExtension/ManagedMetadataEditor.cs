using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Components.Extensions;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;
using SP = Microsoft.SharePoint.Client;
using TaxonomyClientService = Telligent.Evolution.Extensions.SharePoint.WebServices.TaxonomyClientService;

namespace Telligent.Evolution.Extensions.SharePoint.Client.version1
{
    public class ManagedMetadataEditorExtension : IScriptedContentFragmentExtension
    {
        public string ExtensionName
        {
            get { return "sharepoint_v1_managedmetadata"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<IManagedMetadataEditor>(); }
        }

        public string Name
        {
            get { return "Managed Metadata Editor (sharepoint_v1_managedmetadata)"; }
        }

        public string Description
        {
            get { return "Managed Metadata Editor functionality."; }
        }

        public void Initialize() { }
    }

    public interface IManagedMetadataEditor
    {
        ManagedMetadataEntity Get(SP.Field field, object value, SPList list);
    }

    [Documentation(Category = Documentation.Categories.SharePoint)]
    public class ManagedMetadataEditor : IManagedMetadataEditor
    {
        private readonly ICredentialsManager credentials;

        internal ManagedMetadataEditor()
            : this(ServiceLocator.Get<ICredentialsManager>())
        {
        }

        internal ManagedMetadataEditor(ICredentialsManager credentials)
        {
            this.credentials = credentials;
        }

        public ManagedMetadataEntity Get(SP.Field field, object value, SPList list)
        {
            return new ManagedMetadataEntity(credentials, field, value, list.SPWebUrl);
        }
    }

    public class ManagedMetadataEntity
    {
        private ICredentialsManager credentials;

        private string sspId;
        private string termSetId;

        public Dictionary<string, string> Terms { get; set; }
        private List<string> currentKeys;

        private bool allowMultipleValues = false;
        public bool AllowMultipleValues
        {
            get
            {
                return allowMultipleValues;
            }
        }

        private List<String> _keys;
        private List<String> Keys
        {
            get
            {
                if (_keys != null)
                    return _keys;
                return Terms.Keys.ToList();
            }
        }

        internal ManagedMetadataEntity(SP.Field Field, object Value, string baseurl) :
            this(ServiceLocator.Get<ICredentialsManager>(), Field, Value, baseurl)
        {
        }

        internal ManagedMetadataEntity(ICredentialsManager credentials, SP.Field Field, object Value, string baseurl)
        {
            this.credentials = credentials;
            Init(Field, baseurl);
            SetCurrentKeys(Value);
        }

        private void SetCurrentKeys(object value)
        {
            currentKeys = new List<string>();
            if (value != null)
            {
                if (allowMultipleValues)
                {
                    var values = (object[])value;
                    foreach (var _value in values)
                    {
                        currentKeys.Add(_value.ToString().Split('|')[1]);
                    }
                }
                else
                {
                    var _value = value.ToString();
                    currentKeys.Add(_value.Split('|')[1]);
                }
            }
        }

        private void Init(SP.Field Field, string baseurl)
        {
            var xml = Field.SchemaXml;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
            XmlReader xmlReader = XmlReader.Create(new StringReader(xml), readerSettings);
            var xpathDocument = new XPathDocument(xmlReader);
            XPathNavigator nav = xpathDocument.CreateNavigator();

            var allowMultipleValuesNode = nav.SelectSingleNode(@"/Field");
            bool.TryParse(allowMultipleValuesNode.GetAttribute("Mult", ""), out allowMultipleValues);

            var sspIdPropertyNode = nav.SelectSingleNode(@"/Field/Customization/ArrayOfProperty/Property[Name='SspId']");
            sspId = sspIdPropertyNode.SelectSingleNode(@"Value").Value;

            var termSetIdPropertyNode = nav.SelectSingleNode(@"/Field/Customization/ArrayOfProperty/Property[Name='TermSetId']");
            termSetId = termSetIdPropertyNode.SelectSingleNode(@"Value").Value;

            using (var tax = new TaxonomyClientService.Taxonomywebservice())
            {

                tax.Url = baseurl.TrimEnd('/') + "/_vti_bin/TaxonomyClientService.asmx";
                tax.Credentials = credentials.Get(baseurl).Credentials();
                string termsXml = tax.GetChildTermsInTermSet(new Guid(sspId), CultureInfo.CurrentCulture.LCID, new Guid(termSetId));
                Terms = ParseResult(termsXml);
            }
        }

        private Dictionary<string, string> ParseResult(string xml)
        {
            Dictionary<string, string> termCollection = new Dictionary<string, string>();
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(xml);
            XmlNodeList terms = xdoc.GetElementsByTagName("T");
            foreach (XmlNode term in terms)
            {
                string termName = term.FirstChild.FirstChild.Attributes["a32"].Value;
                string termId = term.Attributes["a9"].Value;
                termCollection.Add(termId, termName);
            }
            return termCollection;
        }

        public bool IsSelected(string key)
        {
            return currentKeys.Contains(key);
        }
    }
}
