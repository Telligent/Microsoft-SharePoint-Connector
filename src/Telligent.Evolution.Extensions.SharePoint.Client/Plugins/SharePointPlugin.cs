using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Api;
using Telligent.Evolution.Extensions.SharePoint.Client.InternalApi;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Entities;
using Telligent.Evolution.Extensions.SharePoint.Client.Rest;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins
{
    public class SharePointPlugin : IPluginGroup, IRequiredConfigurationPlugin, IInstallablePlugin, IScriptedContentFragmentFactoryDefaultProvider, ICategorizedPlugin
    {
        private readonly Guid id = new Guid("00380cd3627a4de991e4fcb12716e735");

        #region IPlugin Members

        public string Name
        {
            get { return "SharePoint Core Functionality"; }
        }

        public string Description
        {
            get { return "This feature contains all plugins defining SharePoint core functionality."; }
        }

        public void Initialize()
        {
        }

        #endregion

        #region IConfigurablePlugin Members

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                var group = new PropertyGroup("setup", "Setup", 1);
                group.Properties.Add(new Property("cachetimeout", "API Cache", PropertyType.Int, 0, "120")
                {
                    DescriptionText = "The default cache timeout in seconds."
                });

                var connection = new PropertyGroup("connection", "Connection", 1);
                var connectionType = new Property("ConnectionType", "Connection Type", PropertyType.String, 0, "connectionStringName")
                {
                    ControlType = typeof(PropertyVisibilityValueSelectionControl)
                };

                var connectionStringName = new PropertyValue("connectionStringName", "Connection String Name", 1);
                var connectionStringOption = new PropertyValue("connectionString", "Connection String", 2);

                connectionStringOption.Attributes["propertiesToHide"] = "ConnectionStringName";
                connectionStringOption.Attributes["propertiesToShow"] = "ConnectionString";

                connectionStringName.Attributes["propertiesToHide"] = "ConnectionString";
                connectionStringName.Attributes["propertiesToShow"] = "ConnectionStringName";

                connectionType.SelectableValues.Add(connectionStringOption);
                connectionType.SelectableValues.Add(connectionStringName);

                connection.Properties.Add(connectionType);
                connection.Properties.Add(new Property("ConnectionString", "Database Connection String", PropertyType.String, 1, ""));

                var connectionStringProperty = new Property("ConnectionStringName", "Connection String Name", PropertyType.String, 2, "");
                var availableStrings = PublicApi.DatabaseConnections.GetAvailableConnectionNames().ToList();
                for (int i = 0; i < availableStrings.Count; i++)
                {
                    connectionStringProperty.SelectableValues.Add(new PropertyValue(availableStrings[i], availableStrings[i], i));
                }

                connection.Properties.Add(connectionStringProperty);

                return new[] { group, connection };
            }
        }

        public void Update(IPluginConfiguration configuration)
        {
            SharePointDataService.ConnectionString = GetDatabaseConnection(configuration).ConnectionString;

            var cacheTimeOut = configuration.GetInt("cachetimeout");
            if (cacheTimeOut >= 0)
            {
                var cacheTimeSpan = TimeSpan.FromSeconds(cacheTimeOut);
                foreach (var p in typeof(Api.Version1.PublicApi).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetProperty))
                {
                    var obj = p.GetValue(null, null) as ICacheable;
                    if (obj != null)
                    {
                        obj.CacheTimeOut = cacheTimeSpan;
                    }
                }
            }
        }

        #endregion

        #region IRequiredConfigurationPlugin Members

        public bool IsConfigured
        {
            get
            {
                return SharePointDataService.IsConnectionStringValid();
            }
        }

        #endregion

        #region IScriptedContentFragmentFactoryDefaultProvider Members

        public Guid ScriptedContentFragmentFactoryDefaultIdentifier
        {
            get { return id; }
        }

        #endregion

        #region ICategorizedPlugin

        public string[] Categories { get { return new[] { "SharePoint" }; } }

        #endregion

        #region IInstallablePlugin Members

        public void Install(Version lastInstalledVersion)
        {
            if (Version > lastInstalledVersion)
            {
                // Install SQL tables and stored procedures
                SharePointDataService.Install();

                var assembly = GetType().Assembly;
                var assemblyName = assembly.GetName().Name;
                var resources = AssemblyResources(assembly).ToList();
                InstallWidgets(assemblyName, resources);
                InstallPages(assemblyName, resources);
                InstallCss(assemblyName, resources);
                InstallSupplementaryFiles(assemblyName, resources);
                InstallJavaScriptFiles(assemblyName, resources);

                // Clear Cache for Theme Configuration
                foreach (var theme in Themes.List(ThemeTypes.Site))
                {
                    Evolution.Components.ThemeConfigurationDatas.Expire(theme.Type);
                }
            }
        }

        public void Uninstall()
        {
            // Uninstall SQL tables and stored procedures
            SharePointDataService.UnInstall();
            RemoveWidgets(this);

            var assembly = GetType().Assembly;
            var assemblyName = assembly.GetName().Name;
            var resources = AssemblyResources(assembly).ToList();
            RemovePages(assemblyName, resources);
            RemoveCss(assemblyName, resources);
            RemoveSupplementaryFiles(assemblyName, resources);
            RemoveJavaScriptFiles(assemblyName, resources);
        }

        public Version Version
        {
            get { return GetType().Assembly.GetName().Version; }
        }

        #endregion

        #region IPluginGroup Members

        public IEnumerable<Type> Plugins
        {
            get
            {
                return new[] {
                    // Endpoints
                    typeof(SharePointEndpoints),

                    // Scripted List Extensions
                    typeof(version1.SharePointCalendarExtension),
                    typeof(version1.SharePointFileExtension),
                    typeof(version1.SharePointFolderExtension),
                    typeof(version1.SharePointLibraryExtension),
                    typeof(version1.SharePointListExtension),
                    typeof(version1.SharePointListItemExtension),
                    typeof(version1.SharePointPermissionsExtension),
                    typeof(version1.SharePointUIExtension),
                    typeof(version1.SharePointUrlsExtension),
                    typeof(version1.SharePointViewExtension),
                    typeof(version1.SharePointFieldsExtension),
                    typeof(version2.SharePointFileExtension),
                    typeof(version2.SharePointLibraryExtension),
                    typeof(version2.SharePointListExtension),
                    typeof(version2.SharePointListItemExtension),
                    typeof(version2.SharePointPermissionsExtension),
                    typeof(version2.SharePointLibraryUrlsExtension),
                    typeof(version2.SharePointFileUrlsExtension),
                    typeof(version2.SharePointListUrlsExtension),
                    typeof(version2.SharePointListItemUrlsExtension),
                    typeof(version2.TaxonomiesExtension),

                    // Scripted Type Extensions
                    typeof(version1.AttachmentsEditorExtension),
                    typeof(version1.ChoiceEditorExtension),
                    typeof(version1.DateTimeEditorExtension),
                    typeof(version1.FieldEditorExtension),
                    typeof(version1.HyperlinkOrPictureEditorExtension),
                    typeof(version1.LookupEditorExtension),
                    typeof(version1.ManagedMetadataEditorExtension),
                    typeof(version1.MultiChoiceEditorExtension),
                    typeof(version1.NumberEditorExtension),
                    typeof(version1.PersonOrGroupEditorExtension),
                    typeof(version1.RecurrenceEditorExtension),
                    typeof(version2.AttachmentsExtension),
                    typeof(version2.PersonOrGroupEditorExtension),

                    // Content types
                    typeof(DocumentContentType),
                    typeof(LibraryApplicationType),
                    typeof(ItemContentType),
                    typeof(ListApplicationType),

                    // Permissions
                    typeof(SharePointPermissionsPlugin)
                };
            }
        }

        #endregion

        public static SharePointPlugin Plugin
        {
            get
            {
                return PluginManager.Get<SharePointPlugin>().FirstOrDefault();
            }
        }

        private SqlConnection GetDatabaseConnection(IPluginConfiguration configuration)
        {
            if (configuration == null) throw new ConfigurationErrorsException("Configuration for this plugin is not available or incorrect");

            if (configuration.GetString("ConnectionType") == "connectionString")
            {
                if (string.IsNullOrEmpty(configuration.GetString("ConnectionString")))
                    throw new ConfigurationErrorsException("A connection string must be supplied when the connection type is connection string");

                return new SqlConnection(configuration.GetString("ConnectionString"));
            }

            if (string.IsNullOrEmpty(configuration.GetString("ConnectionStringName")))
                throw new ConfigurationErrorsException("A connection string name must be supplied when the connection type is connection string name");

            return PublicApi.DatabaseConnections.GetConnection(configuration.GetString("ConnectionStringName"));
        }

        private static Guid GetGuidFromResourceString(string resourceName)
        {
            Guid outGuid;
            if (Guid.TryParse(resourceName.StartsWith("_") ? resourceName.Substring(1) : resourceName, out outGuid))
            {
                return outGuid;
            }
            return Guid.Empty;
        }

        private void InstallWidgets(string assemblyName, IEnumerable<string> resourceNames)
        {
            foreach (string resourceName in resourceNames)
            {
                string[] path = resourceName.Split('.');

                // path: Resources.Widgets.[name]
                const int resourcesIndex = 0;
                const int widgetsIndex = 1;
                const int widgetNameIndex = 2;

                // path: Resources.Widgets.[name].[file].xml
                const int xmlFileIndex = 3;
                int xmlExtensionIndex = path.Length - 1;

                // path: Resources.Widgets.[name].[guid].[theme].[file]
                const int widgetFolderIdIndex = 3;
                const int themeIndex = 4;
                const int fileIndex = 5;

                bool isAWidgetDefinitionFile = path.Length > 4
                    && string.Equals(path[resourcesIndex], "Resources", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path[widgetsIndex], "Widgets", StringComparison.OrdinalIgnoreCase);
                if (!isAWidgetDefinitionFile) continue;

                var resourceFullName = string.Concat(assemblyName, ".", resourceName);
                bool isAWidgetXmlFile = path.Length == 5
                    && string.Equals(path[xmlExtensionIndex], "xml", StringComparison.OrdinalIgnoreCase);
                if (isAWidgetXmlFile)
                {
                    var fileName = string.Join(".", path.ToList().GetRange(xmlFileIndex, path.Length - xmlFileIndex));
                    FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateDefinitionFile(
                        this,
                        fileName,
                        EmbeddedResources.GetStream(resourceFullName));
                    continue;
                }

                var widgetFolderId = GetGuidFromResourceString(path[widgetFolderIdIndex]);
                bool isAWidgetResourceFile = path.Length > 5
                    && widgetFolderId != Guid.Empty;
                if (isAWidgetResourceFile)
                {
                    var fileName = string.Join(".", path.ToList().GetRange(fileIndex, path.Length - fileIndex));
                    var theme = SupportedThemes.Get(path[themeIndex]);
                    if (theme != null)
                    {
                        FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(
                            this,
                            widgetFolderId,
                            theme.Id.ToString("N"),
                            fileName,
                            EmbeddedResources.GetStream(resourceFullName));
                    }
                    else
                    {
                        FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(
                            this,
                            widgetFolderId,
                            fileName,
                            EmbeddedResources.GetStream(resourceFullName));
                    }
                }
            }
        }

        private static void RemoveWidgets(IScriptedContentFragmentFactoryDefaultProvider provider)
        {
            FactoryDefaultScriptedContentFragmentProviderFiles.DeleteAllFiles(provider);
        }

        private void InstallPages(string assemblyName, IEnumerable<string> resourceNames)
        {
            var xml = new XmlDocument();
            var contentPages = Pages(assemblyName, resourceNames);
            foreach (var theme in Themes.List(ThemeTypes.Group))
            {
                var supportedTheme = SupportedThemes.Get(theme.Id);
                if (supportedTheme == null) continue;

                var themeName = supportedTheme.Name;
                foreach (var contentPage in contentPages.Where(p => p.Theme == themeName).ToList())
                {
                    xml.LoadXml(EmbeddedResources.GetString(contentPage.Path));
                    foreach (XmlNode xmlPage in xml.SelectNodes("//contentFragmentPage"))
                    {
                        var pageName = xmlPage.Attributes["pageName"].Value;
#if DEBUG
                        Telligent.Evolution.Extensibility.Api.Version1.PublicApi.Eventlogs.Write(string.Format("Adding Page \"{0}\"", pageName), new Telligent.Evolution.Extensibility.Api.Version1.EventLogEntryWriteOptions { Category = "SharePoint", EventId = 1000, EventType = "Information" });
#endif
                        if (theme.IsConfigurationBased)
                        {
                            ThemePages.AddUpdateFactoryDefault(theme, xmlPage);
                        }
                        if (!ThemePages.DefaultExists(theme, pageName, false))
                        {
                            ThemePages.AddUpdateDefault(theme, xmlPage);
                        }
                        ThemePages.AddUpdate(theme, ThemeTypes.Site, xmlPage);
                    }
                }
            }
        }

        private void RemovePages(string assemblyName, IEnumerable<string> resourceNames)
        {
            var xml = new XmlDocument();
            var contentPages = Pages(assemblyName, resourceNames);
            foreach (var theme in Themes.List(ThemeTypes.Group))
            {
                var supportedTheme = SupportedThemes.Get(theme.Id);
                if (supportedTheme == null) continue;

                var themeName = supportedTheme.Name;
                foreach (var contentPage in contentPages.Where(p => p.Theme == themeName).ToList())
                {
                    xml.LoadXml(EmbeddedResources.GetString(contentPage.Path));
                    foreach (XmlNode xmlPage in xml.SelectNodes("//contentFragmentPage"))
                    {
                        var pageName = xmlPage.Attributes["pageName"].Value;
                        if (theme.IsConfigurationBased)
                        {
                            ThemePages.DeleteFactoryDefault(theme, pageName, false);
                        }
                        ThemePages.DeleteDefault(theme, pageName, false);
                        ThemePages.Delete(theme, pageName, false);
                    }
                }
            }
        }

        private void InstallCss(string assemblyName, IEnumerable<string> resourceNames)
        {
            var cssResources = CssResources(assemblyName, resourceNames);
            foreach (var theme in Themes.List(ThemeTypes.Site))
            {
                var supportedTheme = SupportedThemes.Get(theme.Id);
                if (supportedTheme == null) continue;

                var themeName = supportedTheme.Name;
                foreach (var cssResource in cssResources.Where(res => res.Theme == themeName).ToList())
                {
                    using (var stream = EmbeddedResources.GetStream(cssResource.Path))
                    {
                        if (theme.IsConfigurationBased)
                        {
                            ThemeFiles.AddUpdateFactoryDefault(theme, ThemeProperties.CssFiles, cssResource.Name, stream, (int)stream.Length);
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                        ThemeFiles.AddUpdate(theme, ThemeTypes.Site, ThemeProperties.CssFiles, cssResource.Name, stream, (int)stream.Length);
                    }
                }
            }
        }

        private void RemoveCss(string assemblyName, IEnumerable<string> resourceNames)
        {
            List<string> cssFileNames = CssResources(assemblyName, resourceNames)
                                        .GroupBy(res => res.Name)
                                        .Select(res => res.Key).ToList();
            foreach (var theme in Themes.List(ThemeTypes.Site))
            {
                foreach (var fileName in cssFileNames)
                {
                    ThemeFiles.Remove(theme, ThemeTypes.Site, ThemeProperties.CssFiles, fileName);
                    if (theme.IsConfigurationBased)
                    {
                        ThemeFiles.RemoveFactoryDefault(theme, ThemeProperties.CssFiles, fileName);
                    }
                }
            }
        }

        private void InstallSupplementaryFiles(string assemblyName, IEnumerable<string> resourceNames)
        {
            var supplementaryFiles = GetSupplementaryFiles(assemblyName, resourceNames);
            foreach (var theme in Themes.List(ThemeTypes.Site))
            {
                var supportedTheme = SupportedThemes.Get(theme.Id);
                if (supportedTheme == null) continue;

                var themeName = supportedTheme.Name;
                foreach (var image in supplementaryFiles.Where(res => res.Theme == themeName).ToList())
                {
                    using (var stream = EmbeddedResources.GetStream(image.Path))
                    {
                        if (theme.IsConfigurationBased)
                        {
                            ThemeFiles.AddUpdateFactoryDefault(theme, ThemeProperties.SupplementaryFiles, image.Name, stream, (int)stream.Length);
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                        ThemeFiles.AddUpdate(theme, ThemeTypes.Site, ThemeProperties.SupplementaryFiles, image.Name, stream, (int)stream.Length);
                    }
                }
            }
        }

        private void RemoveSupplementaryFiles(string assemblyName, IEnumerable<string> resourceNames)
        {
            List<string> SupplementaryFilesNames = GetSupplementaryFiles(assemblyName, resourceNames)
                                          .GroupBy(res => res.Name)
                                          .Select(res => res.Key).ToList();
            foreach (var theme in Themes.List(ThemeTypes.Site))
            {
                foreach (var fileName in SupplementaryFilesNames)
                {
                    ThemeFiles.Remove(theme, ThemeTypes.Site, ThemeProperties.SupplementaryFiles, fileName);
                    if (theme.IsConfigurationBased)
                    {
                        ThemeFiles.RemoveFactoryDefault(theme, ThemeProperties.SupplementaryFiles, fileName);
                    }
                }
            }
        }

        private void InstallJavaScriptFiles(string assemblyName, IEnumerable<string> resourceNames)
        {
            var jsFiles = GetJavaScriptFiles(assemblyName, resourceNames);
            foreach (var theme in Themes.List(ThemeTypes.Site))
            {
                var supportedTheme = SupportedThemes.Get(theme.Id);
                if (supportedTheme == null) continue;

                var themeName = supportedTheme.Name;
                foreach (var jsFile in jsFiles.ToList())
                {
                    using (var stream = EmbeddedResources.GetStream(jsFile.Path))
                    {
                        if (theme.IsConfigurationBased)
                        {
                            ThemeFiles.AddUpdateFactoryDefault(theme, ThemeProperties.JavascriptFiles, jsFile.Name, stream, (int)stream.Length);
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                        ThemeFiles.AddUpdate(theme, ThemeTypes.Site, ThemeProperties.JavascriptFiles, jsFile.Name, stream, (int)stream.Length);
                    }
                }
            }
        }

        private void RemoveJavaScriptFiles(string assemblyName, IEnumerable<string> resourceNames)
        {
            List<string> jsFiles = GetJavaScriptFiles(assemblyName, resourceNames)
                                          .GroupBy(res => res.Name)
                                          .Select(res => res.Key).ToList();
            foreach (var theme in Themes.List(ThemeTypes.Site))
            {
                foreach (var jsFileName in jsFiles)
                {
                    ThemeFiles.Remove(theme, ThemeTypes.Site, ThemeProperties.JavascriptFiles, jsFileName);
                    if (theme.IsConfigurationBased)
                    {
                        ThemeFiles.RemoveFactoryDefault(theme, ThemeProperties.JavascriptFiles, jsFileName);
                    }
                }
            }
        }

        internal static IEnumerable<string> AssemblyResources(System.Reflection.Assembly assembly)
        {
            var startIndex = assembly.GetName().Name.Length + 1;
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                // path: Resources.[Resource]
                string[] path = resourceName.Substring(startIndex).Split('.');
                if (path.Length > 2 && string.Equals(path[0], "Resources", StringComparison.OrdinalIgnoreCase))
                {
                    yield return resourceName.Substring(startIndex);
                }
            }
        }

        internal static IEnumerable<ContentPageFile> Pages(string assemblyName, IEnumerable<string> resourceNames)
        {
            foreach (string resourceName in resourceNames)
            {
                string[] path = resourceName.Split('.');

                // path: Resources.Pages.[theme].([file].xml)
                const int resourcesIndex = 0;
                const int pagesIndex = 1;
                const int themeIndex = 2;
                const int fileIndex = 3;
                const int xmlExtensionIndex = 4;

                bool isAPageDefinitionFile = path.Length == 5
                    && string.Equals(path[resourcesIndex], "Resources", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path[pagesIndex], "Pages", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path[xmlExtensionIndex], "xml", StringComparison.OrdinalIgnoreCase);
                if (isAPageDefinitionFile)
                {
                    yield return new ContentPageFile(assemblyName, resourceName);
                }
            }
        }

        internal static IEnumerable<CssResource> CssResources(string assemblyName, IEnumerable<string> resourceNames)
        {
            foreach (string resourceName in resourceNames)
            {
                string[] path = resourceName.Split('.');

                // path: Resources.Css.theme.([file].css)
                const int resourcesIndex = 0;
                const int cssIndex = 1;
                const int themeIndex = 2;
                const int fileIndex = 3;
                const int cssExtensionIndex = 4;

                bool isACssDefinitionFile = path.Length == 5
                    && string.Equals(path[resourcesIndex], "Resources", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path[cssIndex], "Css", StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(path[cssExtensionIndex], "css", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(path[cssExtensionIndex], "less", StringComparison.OrdinalIgnoreCase));
                if (isACssDefinitionFile)
                {
                    yield return new CssResource(assemblyName, resourceName);
                }
            }
        }

        internal static IEnumerable<SupplementaryFile> GetSupplementaryFiles(string assemblyName, IEnumerable<string> resourceNames)
        {
            foreach (string resourceName in resourceNames)
            {
                string[] path = resourceName.Split('.');

                // path: Resources.SupplementaryFiles.[theme].([fileName].[extension])
                const int resourcesIndex = 0;
                const int imageIndex = 1;
                const int themeIndex = 2;
                const int fileIndex = 3;
                const int fileExtensionIndex = 4;

                bool isAnImageDefinitionFile = path.Length == 5
                    && string.Equals(path[resourcesIndex], "Resources", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path[imageIndex], "SupplementaryFiles", StringComparison.OrdinalIgnoreCase);

                if (isAnImageDefinitionFile)
                {
                    yield return new SupplementaryFile(assemblyName, resourceName);
                }
            }
        }

        internal static IEnumerable<JavaScriptFile> GetJavaScriptFiles(string assemblyName, IEnumerable<string> resourceNames)
        {
            foreach (string resourceName in resourceNames)
            {
                string[] path = resourceName.Split('.');

                // path: Resources.Javascript.([fileName].js)
                const int resourcesIndex = 0;
                const int javascriptIndex = 1;
                const int fileIndex = 2;

                bool isAJavaScriptDefinitionFile = path.Length > 3
                    && string.Equals(path[resourcesIndex], "Resources", StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(path[javascriptIndex], "Javascript", StringComparison.InvariantCultureIgnoreCase);

                if (isAJavaScriptDefinitionFile)
                {
                    yield return new JavaScriptFile(assemblyName, resourceName);
                }
            }
        }
    }
}
