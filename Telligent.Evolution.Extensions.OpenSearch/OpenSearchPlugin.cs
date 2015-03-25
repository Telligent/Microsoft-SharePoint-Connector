using System;
using System.Linq;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    public class OpenSearchPlugin : IScriptedContentFragmentExtension, IConfigurablePlugin, IScriptedContentFragmentFactoryDefaultProvider, ITranslatablePlugin, ICategorizedPlugin
    {
        public static class PropertyId
        {
            public const String OpenSearch = "OpenSearchProviderList";
        }

        private readonly Guid ID = new Guid("c493822f2af94dfaba83d2ecee54ee75");

        private ITranslatablePluginController translationController;

        #region IExtension Members

        public string ExtensionName
        {
            get { return "telligent_v1_opensearch"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ScriptedExtension.IOpenSearch>(); }
        }

        #endregion

        #region ICategorizedPlugin

        public string[] Categories { get { return new[] { "SharePoint" }; } }

        #endregion

        #region IPlugin Members

        public string Name
        {
            get { return "OpenSearch Providers"; }
        }

        public string Description
        {
            get { return "Enables scripted content fragments to access the OpenSearch Providers."; }
        }

        public void Initialize()
        {
            Func<string, Guid> getGuidFromResourceString = delegate(string inGuid)
            {
                Guid outGuid;
                if (Guid.TryParse(inGuid.StartsWith("_") ? inGuid.Substring(1) : inGuid, out outGuid))
                    return outGuid;
                return Guid.Empty;
            };

            var assembly = GetType().Assembly;
            var assemblyNameLength = assembly.GetName().Name.Length + 1;
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                string[] path = resourceName.Substring(assemblyNameLength).Split('.');
                if (path.Length > 3
                    && string.Compare(path[0], "filestorage", true) == 0
                    && string.Compare(path[1], "defaultwidgets", true) == 0
                    && getGuidFromResourceString(path[2]) == ScriptedContentFragmentFactoryDefaultIdentifier)
                {
                    Guid instanceId = getGuidFromResourceString(path[3]);
                    if (path.Length > 4
                        && instanceId != Guid.Empty
                        && (string.Compare(path[4], "xml", true) != 0 || path.Length > 5))
                        FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(
                             this,
                             instanceId,
                             string.Join(".", path.ToList().GetRange(4, path.Length - 4)),
                             assembly.GetManifestResourceStream(resourceName)
                             );
                    else if (string.Compare(path[path.Length - 1], "xml", true) == 0)
                        FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateDefinitionFile(
                            this,
                            string.Join(".", path.ToList().GetRange(3, path.Length - 3)),
                            assembly.GetManifestResourceStream(resourceName)
                            );
                }
            }
        }

        #endregion

        #region IConfigurablePlugin Members

        public IPluginConfiguration Configuration { get; private set; }

        public void Update(IPluginConfiguration configuration)
        {
            Configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var groups = new[] { new PropertyGroup("options", "Options", 0) };
                var providers = new Property(PropertyId.OpenSearch, String.Empty, PropertyType.Custom, 0, String.Empty)
                {
                    ControlType = typeof(SearchProvidersListControl)
                };
                groups[0].Properties.Add(providers);
                return groups;
            }
        }

        #endregion

        #region ITranslatablePlugin

        public Translation[] DefaultTranslations
        {
            get
            {
                var t = new Translation("en-us");

                t.Set("configuration_widgettitle", "Widget Title");
                t.Set("configuration_providername", "Search Provider");
                t.Set("configuration_resultsperpage", "Results Per Page");
                t.Set("configuration_textonly", "Text only results");
                t.Set("configuration_showmore", "Show More Results Link");

                return new[] { t };
            }
        }

        public void SetController(ITranslatablePluginController controller)
        {
            translationController = controller;
        }

        #endregion

        #region IScriptedContentFragmentFactoryDefaultProvider Members

        public Guid ScriptedContentFragmentFactoryDefaultIdentifier
        {
            get { return ID; }
        }

        #endregion

        public string GetResourceString(string resourceName)
        {
            return translationController.GetLanguageResourceValue(resourceName);
        }

        public static OpenSearchPlugin Plugin
        {
            get
            {
                return PluginManager.Get<OpenSearchPlugin>().FirstOrDefault();
            }
        }

        public static SearchProvidersList GetSearchProvidersList
        {
            get
            {
                SearchProvidersList searchProvidersList = null;
                var openSearchPlugin = Plugin;
                if (openSearchPlugin != null)
                {
                    string value = openSearchPlugin.Configuration.GetCustom(PropertyId.OpenSearch);
                    searchProvidersList = new SearchProvidersList(value);
                }
                return searchProvidersList;
            }
        }
    }
}
