using System;
using System.Linq;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.UI.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.IdentityProvider.ScriptedExtension;

namespace Telligent.Evolution.Extensions.SharePoint.IdentityProvider
{
    public class IdentityProviderPlugin : IScriptedContentFragmentExtension, IInstallablePlugin, IConfigurablePlugin, IScriptedContentFragmentFactoryDefaultProvider, ICategorizedPlugin
    {
        private readonly Guid id = new Guid("79e51160b33e4479bb4ee73417c17770");

        public static class PropertyId
        {
            public const String IssuerName = "IssuerName";
            public const String SigningCertificateName = "SigningCertificateName";
            public const String EncryptingCertificateName = "EncryptingCertificateName";
            public const String DefaultIdentity = "DefaultIdentity";
        }

        #region IScriptedContentFragmentFactoryDefaultProvider Members

        public Guid ScriptedContentFragmentFactoryDefaultIdentifier
        {
            get { return id; }
        }

        #endregion

        #region IScriptedContentFragmentExtension Members

        public string ExtensionName
        {
            get { return "sharepoint_v1_saml"; }
        }

        public object Extension
        {
            get { return ServiceLocator.Get<ISAMLAuthentication>(); }
        }

        #endregion

        #region ICategorizedPlugin

        public string[] Categories { get { return new[] { "SharePoint" }; } }

        #endregion

        #region IPlugin Members

        public string Name
        {
            get { return "SharePoint SAML Authentication"; }
        }

        public string Description
        {
            get { return "Enables SAML Authentication for a SharePoint Integration."; }
        }

        public void Initialize() { }

        #endregion

        #region IInstallablePlugin Members

        public void Install(Version lastInstalledVersion)
        {
            RemoveWidgets(this);
            InstallWidgets();
        }

        public void Uninstall()
        {
            RemoveWidgets(this);
        }

        public Version Version
        {
            get { return GetType().Assembly.GetName().Version; }
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

                groups[0].Properties.Add(new Property(PropertyId.IssuerName, "Issuer Name", PropertyType.String, 0, String.Empty)
                {
                    DescriptionText = "Issuer from the certificate that will be presented to the token service. (TelligentSTS)"
                });

                groups[0].Properties.Add(new Property(PropertyId.SigningCertificateName, "Signing Certificate Name", PropertyType.String, 0, String.Empty)
                {
                    DescriptionText = "Name of the certificate that will be used for signing. (CN=TelligentSTS)"
                });

                groups[0].Properties.Add(new Property(PropertyId.EncryptingCertificateName, "Encrypting Certificate Name", PropertyType.String, 0, String.Empty)
                {
                    DescriptionText = "Name of the certificate that will be used for encrypting. (CN=TelligentEncryptionSTS)"
                });

                groups[0].Properties.Add(new Property(PropertyId.DefaultIdentity, "Default Identity", PropertyType.String, 0, String.Empty)
                {
                    DescriptionText = "Default service account user name for the Job Scheduler. (service_account)"
                });

                return groups;
            }
        }

        #endregion

        public static IdentityProviderPlugin Plugin
        {
            get
            {
                return PluginManager.Get<IdentityProviderPlugin>().FirstOrDefault();
            }
        }

        private void InstallWidgets()
        {
            var assembly = GetType().Assembly;
            var assemblyNameLength = assembly.GetName().Name.Length + 1;
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                string[] path = resourceName.Substring(assemblyNameLength).Split('.');

                // path: Resources.Widgets.[widget files]
                const int resourcesIndex = 0;
                const int widgetsIndex = 1;

                bool isAWidgetDefinitionFile = path.Length > 2
                    && string.Equals(path[resourcesIndex], "Resources", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path[widgetsIndex], "Widgets", StringComparison.OrdinalIgnoreCase);
                if (isAWidgetDefinitionFile)
                {
                    // path: Resources.Widgets.WidgetFolderId.[widget files]
                    const int widgetFolderIdIndex = 2;
                    Guid widgetFolderId = GetGuidFromResourceString(path[widgetFolderIdIndex]);
                    bool isAWidgetXmlFile = (string.Equals(path[path.Length - 1], "xml", StringComparison.OrdinalIgnoreCase));
                    bool isAWidgetFolder = path.Length > 3
                        && widgetFolderId != Guid.Empty
                        && !isAWidgetXmlFile;
                    if (isAWidgetFolder)
                    {
                        // path: Resources.Widgets.WidgetFolderId.ThemeId.[widget files]
                        const int themeIdIndex = 3;
                        Guid themeId = GetGuidFromResourceString(path[themeIdIndex]);
                        bool hasSubfolders = (themeId != Guid.Empty);
                        if (hasSubfolders)
                        {
                            string fileName = string.Join(".", path.ToList().GetRange(themeIdIndex + 1, path.Length - themeIdIndex - 1));
                            FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(
                                 this,
                                 widgetFolderId,
                                 themeId.ToString("N"),
                                 fileName,
                                 assembly.GetManifestResourceStream(resourceName)
                                 );
                        }
                        else
                        {
                            // path: Resources.Widgets.WidgetFolderId.FileName
                            const int fileNameIndex = 3;
                            string fileName = string.Join(".", path.ToList().GetRange(fileNameIndex, path.Length - fileNameIndex));
                            FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateSupplementaryFile(
                                 this,
                                 widgetFolderId,
                                 fileName,
                                 assembly.GetManifestResourceStream(resourceName));
                        }
                    }
                    else if (isAWidgetXmlFile)
                    {
                        // path: Resources.Widgets.WidgetName.xml
                        const int widgetNameIndex = 2;
                        FactoryDefaultScriptedContentFragmentProviderFiles.AddUpdateDefinitionFile(
                            this,
                            string.Join(".", path.ToList().GetRange(widgetNameIndex, path.Length - widgetNameIndex)),
                            assembly.GetManifestResourceStream(resourceName));
                    }
                }
            }
        }

        private static void RemoveWidgets(IScriptedContentFragmentFactoryDefaultProvider provider)
        {
            FactoryDefaultScriptedContentFragmentProviderFiles.DeleteAllFiles(provider);
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
    }
}
