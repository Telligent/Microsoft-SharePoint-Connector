using System;
using Telligent.Evolution.Extensibility.Security.Version1;
using Telligent.Evolution.Extensibility.Version1;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.DocumentLibrary;
using Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content.List;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Plugins.Content
{
    public static class SharePointPermissionIds
    {
        public static readonly Guid ViewLibrary = new Guid("D7F94F1F-5022-4456-AB51-A925EDCFBB6C");
        public static readonly Guid ViewList = new Guid("5DD73E19-8775-4778-94F3-8BEFC93A9876");
    }

    public class SharePointPermissionsPlugin : IPlugin, ITranslatablePlugin, IPermissionRegistrar
    {
        internal static class Permissions
        {
            public const string ViewLibraryName = "Permission_SharePoint_ViewLibrary_Name";
            public const string ViewLibraryDescription = "Permission_SharePoint_ViewLibrary_Description";
            public const string ViewListName = "Permission_SharePoint_ViewList_Name";
            public const string ViewListDescription = "Permission_SharePoint_ViewList_Description";
        }

        #region IPlugin

        private ITranslatablePluginController translatableController;

        public string Name
        {
            get { return "SharePoint Permissions"; }
        }
        public string Description
        {
            get { return "Registers permissions for SharePoint."; }
        }

        public void Initialize() { }

        #endregion

        #region IPermissionRegistrar Members

        public void RegisterPermissions(IPermissionRegistrarController permissionController)
        {
            permissionController.Register(new Permission(
                SharePointPermissionIds.ViewLibrary,
                Permissions.ViewLibraryName,
                Permissions.ViewLibraryDescription,
                translatableController,
                LibraryApplicationType.Id,
                new PermissionConfiguration
                {
                    Joinless = new JoinlessGroupPermissionConfiguration { Administrators = true, Moderators = true, RegisteredUsers = true, Everyone = true },
                    PublicOpen = new MembershipGroupPermissionConfiguration { Owners = true, Managers = true, Everyone = true, Members = true },
                    PublicClosed = new MembershipGroupPermissionConfiguration { Owners = true, Managers = true, Everyone = true, Members = true },
                    PrivateListed = new MembershipGroupPermissionConfiguration { Owners = true, Managers = true, Members = true },
                    PrivateUnlisted = new MembershipGroupPermissionConfiguration { Owners = true, Managers = true, Members = true }
                }));

            permissionController.Register(new Permission(
                SharePointPermissionIds.ViewList,
                Permissions.ViewListName,
                Permissions.ViewListDescription,
                translatableController,
                ListApplicationType.Id,
                new PermissionConfiguration
                {
                    Joinless = new JoinlessGroupPermissionConfiguration { Administrators = true, Moderators = true, RegisteredUsers = true, Everyone = true, Owners = true },
                    PublicOpen = new MembershipGroupPermissionConfiguration { Owners = true, Managers = true, Everyone = true, Members = true },
                    PublicClosed = new MembershipGroupPermissionConfiguration { Owners = true, Managers = true, Everyone = true, Members = true },
                    PrivateListed = new MembershipGroupPermissionConfiguration { Owners = true, Managers = true, Members = true },
                    PrivateUnlisted = new MembershipGroupPermissionConfiguration { Owners = true, Managers = true, Members = true }
                }));
        }

        #endregion

        #region ITranslatablePlugin Members

        public void SetController(ITranslatablePluginController controller)
        {
            translatableController = controller;
        }

        public Translation[] DefaultTranslations
        {
            get
            {
                var enUS = new Translation("en-us");

                enUS.Set(Permissions.ViewLibraryName, "SharePoint - View Library");
                enUS.Set(Permissions.ViewLibraryDescription, "Allows a user to view Libraries.");

                enUS.Set(Permissions.ViewListName, "SharePoint - View List");
                enUS.Set(Permissions.ViewListDescription, "Allows a user to view Lists.");

                return new[] { enUS };
            }
        }

        #endregion
    }
}
