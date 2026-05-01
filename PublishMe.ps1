# Define variables
$rg = 'ResourceGroup01'
$location = 'West Europe'
$appServicePlanName = 'ASP-ResourceGroup01-bd1b (F1: 1)'
$webAppName = 'KissGymApi'
$archivePath = 'E:\\DWP\\github\\kiss-gym\\gym-api\\GymApi.Api\\bin\\Release\\net10.0\\publish\\deploy.zip'

# Step 1: Create a new resource group for the App Service
New-AzResourceGroup -Name $rg -Location $location

# Step 2: Create an App Service plan in the resource group
New-AzAppServicePlan -ResourceGroupName $rg -Name $appServicePlanName -Location $location -Tier "Standard" -NumberofWorkers 1 -WorkerSize "Small"

# Step 3: Create a new web app in the App Service plan
New-AzWebApp -ResourceGroupName $rg -Name $webAppName -Location $location -AppServicePlan $appServicePlanName

# Step 4: Deploy the .NET Core API to the web app
Publish-AzWebApp -ResourceGroupName $rg -Name $webAppName -ArchivePath $archivePath
