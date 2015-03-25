using Telligent.Evolution.Extensions.SharePoint.IdentityProvider.STS;

namespace Telligent.Evolution.Extensions.SharePoint.IdentityProvider.ScriptedExtension
{
    public interface ISAMLAuthentication
    {
        STSConfiguration Configuration { get; }

        bool SignIn();

        void SignOut();

        bool LoginAsAnotherUser(string returnUrl);

        string Url();
    }
}
