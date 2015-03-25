#--------------------------------------------#
# Uninstall Incremental and Full profile sync
#--------------------------------------------#

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$Tasks
)

#------------  tasks.config  ----------------#
[string]$configPath = $Tasks.TrimEnd('\') + "\tasks.config";
[bool]$configPathExists = Test-Path $configPath;
if(!$configPathExists)
{
    Write-Host "The path '$configPath' doesn't exist."
    Break;
}

[xml]$doc = Get-Content($configPath);
$jobs = $doc.jobs.cron.jobs;
$fullProfileSyncJobType = "Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs.FullProfileSyncJob, Telligent.Evolution.Extensions.SharePoint.ProfileSync";
$fullProfileSyncJob = $jobs.SelectSingleNode("job[@type='" + $fullProfileSyncJobType + "']");
if($fullProfileSyncJob)
{
    $null = $jobs.RemoveChild($fullProfileSyncJob);
    Write-Host "Removed full profile sync job";
}

$incrementalProfileSyncJobType = "Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs.IncrementalProfileSyncJob, Telligent.Evolution.Extensions.SharePoint.ProfileSync";
$incrementalProfileSyncJob = $jobs.SelectSingleNode("job[@type='" + $incrementalProfileSyncJobType + "']");
if($incrementalProfileSyncJob)
{
    $null = $jobs.RemoveChild($incrementalProfileSyncJob);
    Write-Host "Removed incremental profile sync job";
}

$doc.Save($configPath);
#--------------------------------------------#

Write-Host "Press any key to exit ..."
$x = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")