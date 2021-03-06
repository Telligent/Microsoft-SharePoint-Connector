#--------------------------------------------------#
# Install Trusted Identity Provider in SharePoint
#
#	$Name = "STSCert" 
#	$Path = "STSCert.cer"
#	$Realm = "http://<SharePoint Site Url>/_trust/"
#	$Login = "http://<Telligent Site Url>/login"
#	$Identity = "Telligent"
#	$Description = "Telligent Community Identity Provider"
#
#--------------------------------------------------#

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Name,
	[Parameter(Mandatory = $true)]
    [string]$Path,
	[Parameter(Mandatory = $true)]
    [string]$Realm,
	[Parameter(Mandatory = $true)]
    [string]$Login,
    [Parameter(Mandatory = $false)]
    [string]$Identity = "Telligent",
    [Parameter(Mandatory = $false)]
    [string]$Description = "Telligent Community Identity Provider"
)

#---------------------------------------------------------------------#
Add-PSSnapin Microsoft.SharePoint.PowerShell –erroraction SilentlyContinue
#---------------------------------------------------------------------#
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($Path)
$rootAuthority = Get-SPTrustedRootAuthority $Name  –erroraction SilentlyContinue
if($rootAuthority -eq $null)
{
    $rootAuthority = New-SPTrustedRootAuthority -Name $Name -Certificate $cert
}

$trusted = Get-SPTrustedIdentityTokenIssuer -Identity $Identity –erroraction SilentlyContinue
if($trusted -eq $null)
{
    $emailClaim = New-SPClaimTypeMapping -IncomingClaimType "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" -IncomingClaimTypeDisplayName "EMail" –SameAsIncoming
    $nameClaim  = New-SPClaimTypeMapping -IncomingClaimType "http://schemas.telligent.com/sharepoint/claims/title" -IncomingClaimTypeDisplayName "Title" –SameAsIncoming
    $roleClaim  = New-SPClaimTypeMapping -IncomingClaimType "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" -IncomingClaimTypeDisplayName "Role" -SameAsIncoming
    $trusted    = New-SPTrustedIdentityTokenIssuer -Name $Identity -Description $Description -realm $Realm -ImportTrustCertificate $cert -ClaimsMappings $emailClaim,$nameClaim,$roleClaim -SignInUrl $Login -IdentifierClaim $emailClaim.InputClaimType
}
#---------------------------------------------------------------------#
Write-Host "Press any key to exit ..."
$x = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")