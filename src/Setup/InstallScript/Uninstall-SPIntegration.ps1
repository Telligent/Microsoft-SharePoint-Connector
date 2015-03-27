#--------------------------------------------#
# Uninstall SyncModule and Web.config settings
#--------------------------------------------#

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$Web,
    [Parameter(Mandatory=$true)]
    [string]$Tasks
)

#----------------  Utility  -----------------#
function TestConfigPath($path)
{
    $pathExists = Test-Path $path;
    if(!$pathExists)
    {
        Write-Host "The path `"$configPath`" doesn't exist.";
    }
    return $pathExists;
}

#--------------- Web.config -----------------#
$configPath = $Web.TrimEnd('\') + "\Web.config";
if(TestConfigPath $configPath)
{
    [xml] $doc = Get-Content($configPath);
    [bool]$updated = $false;
    $configuration = $doc.configuration;
    $configSections = $configuration.configSections;
    $sharePointConnectionSection = $configSections.SelectSingleNode("section[@name='SharePointConnection']");
    if($sharePointConnectionSection)
    {
        $null = $configSections.RemoveChild($sharePointConnectionSection);
        $updated = $true;
        Write-Host "Web.config - SharePointConnection section has been removed";
    }

    $sharePointConnection = $configuration.SelectSingleNode("SharePointConnection");
    if($sharePointConnection)
    {
        $null = $configuration.RemoveChild($sharePointConnection);
        $updated = $true;
        Write-Host "Web.config - SharePointConnection node has been removed";
    }
    
    if($updated)
    {
        $doc.Save($configPath)
    }
}

#------ communityserver_override.config -----#
$configPath = $Web.TrimEnd('\') + "\communityserver_override.config";
if(TestConfigPath $configPath) 
{
    [xml] $doc = Get-Content($configPath);
    $syncModuleChild = $doc.Overrides.SelectSingleNode("Override/add[@type='CommunityServer.Sync.SyncModule, CommunityServer.Sync']")
    if($syncModuleChild)
    {
        $null = $doc.Overrides.RemoveChild($syncModuleChild.ParentNode);
        Write-Host "communityserver_override.config - SyncModule has been removed";
        $doc.Save($configPath)
    }
}
#-------------- Messages.xml ----------------#
$configPath = $Web.TrimEnd('\') + "\Languages\en-US\Messages.xml";
if(TestConfigPath $configPath)
{
    [xml] $doc = Get-Content($configPath);
    [bool]$updated = $false;
    foreach ($messageId in @(451, 452, 453)) 
    {
        $message = $doc.root.SelectSingleNode("message[@id=$messageId]");
        if($message)
        { 
            $null = $doc.root.RemoveChild($message);
            $updated = $true;
            Write-Host "Messages.config - The message with id '$messageId' has been removed";
        }
    }
    if($updated)
    {
        $doc.Save($configPath)
    }
}
#------------  tasks.config  ----------------#
$configPath = $Tasks.TrimEnd('\') + "\tasks.config";
if(TestConfigPath $configPath)
{
    [xml]$doc = Get-Content($configPath);
    [bool]$updated = $false;

    $jobs = $doc.jobs.cron.jobs;
    $fullProfileSyncJobType = "Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs.FullProfileSyncJob, Telligent.Evolution.Extensions.SharePoint.ProfileSync";
    $fullProfileSyncJob = $jobs.SelectSingleNode("job[@type='" + $fullProfileSyncJobType + "']");
    if($fullProfileSyncJob)
    {
        $null = $jobs.RemoveChild($fullProfileSyncJob);
        $updated = $true;
        Write-Host "tasks.config - The full profile sync job definition has been removed";
    }

    $incrementalProfileSyncJobType = "Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs.IncrementalProfileSyncJob, Telligent.Evolution.Extensions.SharePoint.ProfileSync";
    $incrementalProfileSyncJob = $jobs.SelectSingleNode("job[@type='" + $incrementalProfileSyncJobType + "']");
    if($incrementalProfileSyncJob)
    {
        $null = $jobs.RemoveChild($incrementalProfileSyncJob);
        $updated = $true;
        Write-Host "tasks.config - The incremental profile sync job definition has been removed";
    }

    if($updated)
    {
        $doc.Save($configPath);
    }
}
#--------------------------------------------#

Write-Host "Press any key to exit ..."
$x = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")