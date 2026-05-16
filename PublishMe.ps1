$rg = 'ResourceGroup01'
$location = 'West Europe'
$appServicePlanName = 'KissGymApiPlan'
$webAppName = 'KissGymApi'

$apiProjectPath = '.\GymApi.Api'
$publishOutputFolder = Join-Path $apiProjectPath 'bin\Release\net8.0\publish'
$archiveFileName = 'deploy.zip'
$archivePath = Join-Path $publishOutputFolder $archiveFileName

Write-Host "Step 0: Building, Publishing, and Zipping the API..."

dotnet publish $apiProjectPath -c Release -o $publishOutputFolder

if (Test-Path $archivePath) {
    Remove-Item $archivePath
}
Compress-Archive -Path (Join-Path $publishOutputFolder '*') -DestinationPath $archivePath -Force

Write-Host "API published and zipped to $archivePath"

Write-Host "Step 1: Creating or updating Azure Resource Group '$rg'..."
New-AzResourceGroup -Name $rg -Location $location -Force

Write-Host "Step 2: Creating or updating Azure App Service Plan '$appServicePlanName'..."
$existingAppServicePlan = Get-AzAppServicePlan -ResourceGroupName $rg -Name $appServicePlanName -ErrorAction SilentlyContinue
if (-not $existingAppServicePlan) {
    New-AzAppServicePlan -ResourceGroupName $rg -Name $appServicePlanName -Location $location -Tier "Free"
} else {
    Write-Host "App Service Plan '$appServicePlanName' already exists. Skipping creation."
}

Write-Host "Step 3: Creating or updating Azure Web App '$webAppName'..."
New-AzWebApp -ResourceGroupName $rg -Name $webAppName -Location $location -AppServicePlan $appServicePlanName

Write-Host "Step 4: Deploying the .NET Core API to web app '$webAppName'..."
Publish-AzWebApp -ResourceGroupName $rg -Name $webAppName -ArchivePath $archivePath -Force

Write-Host "Deployment complete!"
