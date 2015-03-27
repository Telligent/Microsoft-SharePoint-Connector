using Telligent.Evolution.Extensions.SharePoint.Components.AuthenticationUtil;
using Telligent.Evolution.Extensions.SharePoint.IntegrationManager;

namespace Telligent.Evolution.Extensions.SharePoint.Client.InternalApi
{
    internal interface ICredentialsManager
    {
        Authentication Get(string url);
    }

    internal class SPCredentialsManager : ICredentialsManager
    {
        public Authentication Get(string url)
        {
            return IntegrationManagerPlugin.CurrentAuth(url);
        }
    }
}
