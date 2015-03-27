using Telligent.Evolution.Extensions.SharePoint.Client.Rest.Api.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client.Api.Version1
{
    public static class RestApi
    {
        public static ISPListController List
        {
            get { return ServiceLocator.Get<ISPListController>(); }
        }

        public static ISPViewController View
        {
            get { return ServiceLocator.Get<ISPViewController>(); }
        }

        public static ISPUserOrGroupController UserOrGroup
        {
            get { return ServiceLocator.Get<ISPUserOrGroupController>(); }
        }
    }
}
