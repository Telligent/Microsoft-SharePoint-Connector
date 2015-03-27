namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public static class PublicApi
    {
        #region List Items
        public static IListItems ListItems
        {
            get { return ServiceLocator.Get<IListItems>(); }
        }

        public static ILists Lists
        {
            get { return ServiceLocator.Get<ILists>(); }
        }
        #endregion

        #region Document Libraries
        public static IDocuments Documents
        {
            get { return ServiceLocator.Get<IDocuments>(); }
        }

        public static ILibraries Libraries
        {
            get { return ServiceLocator.Get<ILibraries>(); }
        }

        public static IFolders Folders
        {
            get { return ServiceLocator.Get<IFolders>(); }
        }
        #endregion

        public static IAttachments Attachments
        {
            get { return ServiceLocator.Get<IAttachments>(); }
        }

        #region Permissions
        public static IPermissions Permissions
        {
            get { return ServiceLocator.Get<IPermissions>(); }
        }
        #endregion

        #region URLs
        public static ISharePointUrls SharePointUrls
        {
            get { return ServiceLocator.Get<ISharePointUrls>(); }
        }
        #endregion

        public static ITaxonomies Taxonomies
        {
            get { return ServiceLocator.Get<ITaxonomies>(); }
        }

        public static IFields Fields
        {
            get { return ServiceLocator.Get<IFields>(); }
        }

        public static IUserProfiles UserProfiles
        {
            get { return ServiceLocator.Get<IUserProfiles>(); }
        }
    }
}
