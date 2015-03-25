using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Claims;
using Microsoft.IdentityModel.Configuration;
using Microsoft.IdentityModel.Protocols.WSTrust;
using Microsoft.IdentityModel.SecurityTokenService;

namespace Telligent.Evolution.Extensions.SharePoint.IdentityProvider.STS
{
    public class TelligentSTS : SecurityTokenService
    {
        public TelligentSTS(SecurityTokenServiceConfiguration configuration) : base(configuration) { }

        /// <summary>
        /// This method returns the configuration for the token issuance request. The configuration
        /// is represented by the Scope class.
        /// </summary>
        /// <param name="principal">The caller's principal.</param>
        /// <param name="request">The incoming RST.</param>
        /// <returns>The scope information to be used for the token issuance.</returns>
        protected override Scope GetScope(IClaimsPrincipal principal, RequestSecurityToken request)
        {
            var plugin = IdentityProviderPlugin.Plugin;
            if (plugin == null) return null;

            var scope = new Scope(request.AppliesTo.Uri.OriginalString, SecurityTokenServiceConfiguration.SigningCredentials);
            var encryptingCertificateName = plugin.Configuration.GetString(IdentityProviderPlugin.PropertyId.EncryptingCertificateName);
            
            if (!string.IsNullOrEmpty(encryptingCertificateName))
            {
                scope.EncryptingCredentials = new X509EncryptingCredentials(CertificateUtil.GetCertificate(StoreName.My, StoreLocation.LocalMachine, encryptingCertificateName));
            }
            else
            {
                scope.TokenEncryptionRequired = false;
            }
            
            // Set the ReplyTo address for the WS-Federation passive protocol (wreply). This is the address to which responses will be directed.
            scope.ReplyToAddress = scope.AppliesToAddress;
            return scope;
        }

        /// <summary>
        /// This method returns the claims to be issued in the token.
        /// </summary>
        /// <param name="principal">The caller's principal.</param>
        /// <param name="request">The incoming RST, can be used to obtain additional information.</param>
        /// <param name="scope">The scope information corresponding to this request.</param> 
        /// <exception cref="ArgumentNullException">If 'principal' parameter is null.</exception>
        /// <returns>The outgoing claimsIdentity to be included in the issued token.</returns>
        protected override IClaimsIdentity GetOutputClaimsIdentity(IClaimsPrincipal principal, RequestSecurityToken request, Scope scope)
        {
            if (null == principal)
            {
                throw new ArgumentNullException("principal");
            }

            var outputIdentity = new ClaimsIdentity();
            var claims = UserInfo.GetClaimsForUser(principal.Identity.Name);

            foreach (Claim claim in claims)
            {
                outputIdentity.Claims.Add(claim);
            }

            return outputIdentity;
        }
    }
}
