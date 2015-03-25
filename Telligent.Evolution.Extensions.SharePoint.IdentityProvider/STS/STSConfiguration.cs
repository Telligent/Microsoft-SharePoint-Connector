using Microsoft.IdentityModel.Configuration;
using Microsoft.IdentityModel.SecurityTokenService;

namespace Telligent.Evolution.Extensions.SharePoint.IdentityProvider.STS
{
    public class STSConfiguration : SecurityTokenServiceConfiguration
    {
        public STSConfiguration(string issuerName, X509SigningCredentials cred)
            : base(issuerName, cred)
        {
            SecurityTokenService = typeof(TelligentSTS);
        }
    }
}
