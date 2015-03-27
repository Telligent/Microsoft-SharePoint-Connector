#--------------------------------------------#
# Install Incremental and Full profile sync
#--------------------------------------------#

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Tasks,
    [Parameter(Mandatory = $false)]
    [string]$Incremental = "0 */5 * * * ? *",
    [Parameter(Mandatory = $false)]
    [string]$Full        = "0 0 23 1/6 * ? *"
)

#------------  tasks.config  ----------------#
[string]$configPath = $Tasks.TrimEnd('\') + "\tasks.config";
$configPathExists = Test-Path $configPath;
if(!$configPathExists)
{
    Write-Host "The path '$configPath' doesn't exist."
    Break;
}

[xml]$doc = Get-Content($configPath);
$jobs = $doc.jobs.cron.jobs;
$fullProfileSyncJobType = "Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs.FullProfileSyncJob, Telligent.Evolution.Extensions.SharePoint.ProfileSync";
$fullProfileSyncJob = $jobs.SelectSingleNode("job[@type='" + $fullProfileSyncJobType + "']");
if (!$fullProfileSyncJob) 
{
    $fullProfileSyncJob = $doc.CreateElement("job");
    $fullProfileSyncJob.SetAttribute("schedule", $Full);
    $fullProfileSyncJob.SetAttribute("type", $fullProfileSyncJobType);
    $null = $jobs.AppendChild($fullProfileSyncJob);
    Write-Host 'Added full profile sync job';
}
else
{
    $fullProfileSyncJob.SetAttribute("schedule", $Full);
    Write-Host 'Updated full profile sync job';
}

$incrementalProfileSyncJobType = "Telligent.Evolution.Extensions.SharePoint.ProfileSync.Jobs.IncrementalProfileSyncJob, Telligent.Evolution.Extensions.SharePoint.ProfileSync";
$incrementalProfileSyncJob = $jobs.SelectSingleNode("job[@type='" + $incrementalProfileSyncJobType + "']");
if (!$incrementalProfileSyncJob)
{
    $incrementalProfileSyncJob = $doc.CreateElement("job");
    $incrementalProfileSyncJob.SetAttribute("schedule", $Incremental);
    $incrementalProfileSyncJob.SetAttribute("type", $incrementalProfileSyncJobType);
    $null = $jobs.AppendChild($incrementalProfileSyncJob);
    Write-Host 'Added incremental profile sync job';
}
else
{
    $incrementalProfileSyncJob.SetAttribute("schedule", $Incremental);
    Write-Host 'Updated incremental profile sync job';
}
$doc.Save($configPath);
#--------------------------------------------#

Write-Host "Press any key to exit ..."
$x = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")