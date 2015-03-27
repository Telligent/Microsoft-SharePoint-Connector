using System;
using System.Collections.Generic;
using System.Linq;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility.Rest.Version2;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil.Methods;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Model;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Controllers;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager.Rest.Resources;

namespace Telligent.Evolution.Extensions.SharePoint.IntegrationManager
{
    public class IntegrationManagerPlugin : IConfigurablePlugin, IRestEndpoints, ICategorizedPlugin
    {
        public static class PropertyId
        {
            public const string SPObjectManager = "SPSiteCollectionId";
            public const string PartnershipManager = "PartnershipCollectionId";
        }

        #region ICategorizedPlugin

        public string[] Categories { get { return new[] { "SharePoint" }; } }

        #endregion

        #region IPlugin Members

        public string Name
        {
            get { return "SharePoint Integration Manager"; }
        }

        public string Description
        {
            get { return "Enables SharePoint Site Collection configuration."; }
        }

        public void Initialize() { }

        #endregion

        #region IConfigurablePlugin Members

        public IPluginConfiguration Configuration { get; private set; }

        public void Update(IPluginConfiguration configuration)
        {
            IntegrationProviders.ExpireTags();
            Configuration = configuration;
        }

        public PropertyGroup[] ConfigurationOptions
        {
            get
            {
                const string spObjectManagerTab = "Site Collections";
                var groups = new[] { new PropertyGroup("sitecollections", spObjectManagerTab, 0) };
                var spSites = new Property(PropertyId.SPObjectManager, String.Empty, PropertyType.Custom, 0, String.Empty)
                {
                    ControlType = typeof(IntegrationManagerControl)
                };
                groups[0].Properties.Add(spSites);
                return groups;
            }
        }

        #endregion

        #region IRestEndpoints

        public void Register(IRestEndpointController controller)
        {
            controller.Add(2, "sharepoint/integration/managers", new { }, new { }, HttpMethod.Get, request => IntegrationManagerControllerHelper.List(new IntegrationManagerListRequest(request)));
            controller.Add(2, "sharepoint/integration/managers/{managerId}", new { }, new { }, HttpMethod.Get, request => IntegrationManagerControllerHelper.Get(new IntegrationManagerRequest(request)));
            controller.Add(2, "sharepoint/integration/{groupId}/manager", new { }, new { groupid = @"\d+" }, HttpMethod.Get, request => IntegrationManagerControllerHelper.Get(new IntegrationManagerRequest(request)));
        }

        #endregion

        public static IntegrationManagerPlugin Plugin
        {
            get
            {
                return PluginManager.Get<IntegrationManagerPlugin>().FirstOrDefault();
            }
        }

        public static Authentication CurrentAuth(string url)
        {
            var integrationManagerList = GetProviders();
            var result = integrationManagerList.FindByUrl(url);
            return result != null ? result.Authentication : new Anonymous();
        }

        public static List<IntegrationProvider> GetAllProviders()
        {
            var integrationManagerList = GetProviders();
            return integrationManagerList.GetAllProviders();
        }

        public static IntegrationProviders GetProviders()
        {
            var integrationManagerPlugin = Plugin;
            if (integrationManagerPlugin != null)
            {
                return new IntegrationProviders(integrationManagerPlugin.Configuration.GetString(PropertyId.SPObjectManager));
            }
            return new IntegrationProviders();
        }
    }
}
