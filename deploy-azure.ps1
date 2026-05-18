# Azure App Service Deployment Script for VB.NET RAT Server (Windows PowerShell)
# This script automates the deployment to Azure App Service

# Configuration (EDIT THESE)
$resourceGroup = "rat-deployment"
$appServicePlan = "rat-plan"
$appName = "rat-server-$(Get-Date -Format 'yyyyMMddHHmmss')"
$location = "eastus"
$sku = "F1"  # F1 = Free, B1 = Basic ($12/mo)

Write-Host "=== VB.NET RAT Server - Azure Deployment ===" -ForegroundColor Cyan
Write-Host "This script will create an Azure App Service and deploy your server"
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Resource Group: $resourceGroup"
Write-Host "  App Service Plan: $appServicePlan"
Write-Host "  App Name: $appName"
Write-Host "  Location: $location"
Write-Host "  SKU: $sku (Free tier)"
Write-Host ""
Write-Host "Prerequisites:" -ForegroundColor Yellow
Write-Host "  1. Azure CLI installed (https://docs.microsoft.com/cli/azure)"
Write-Host "  2. Azure account created (https://azure.microsoft.com/free)"
Write-Host "  3. Logged in: az login"
Write-Host ""

$continue = Read-Host "Continue? (y/n)"
if ($continue -ne "y" -and $continue -ne "Y") {
    Write-Host "Cancelled"
    exit 1
}

Write-Host ""
Write-Host "Step 1: Creating resource group..." -ForegroundColor Cyan
az group create --name $resourceGroup --location $location

Write-Host ""
Write-Host "Step 2: Creating App Service plan..." -ForegroundColor Cyan
az appservice plan create `
    --name $appServicePlan `
    --resource-group $resourceGroup `
    --is-linux false `
    --sku $sku

Write-Host ""
Write-Host "Step 3: Creating web app (.NET Framework 4.8)..." -ForegroundColor Cyan
az webapp create `
    --resource-group $resourceGroup `
    --plan $appServicePlan `
    --name $appName `
    --runtime "DOTNETCORE|4.8"

Write-Host ""
Write-Host "Step 4: Enabling WebSocket..." -ForegroundColor Cyan
az webapp config set `
    --resource-group $resourceGroup `
    --name $appName `
    --web-sockets-enabled true

Write-Host ""
Write-Host "Step 5: GitHub Integration (Manual Setup)" -ForegroundColor Cyan
Write-Host "Note: GitHub integration requires manual setup in Azure Portal"
Write-Host "  1. Go to https://portal.azure.com"
Write-Host "  2. Search for your app: $appName"
Write-Host "  3. Deployment Center -> GitHub"
Write-Host "  4. Authorize and select repository: amakamerit62-design/db"
Write-Host ""

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Your app is ready at:" -ForegroundColor Yellow
Write-Host "  https://$appName.azurewebsites.net"
Write-Host ""
Write-Host "WebSocket URL for Client:" -ForegroundColor Yellow
Write-Host "  ws://$appName.azurewebsites.net:9823"
Write-Host "  wss://$appName.azurewebsites.net:443 (with TLS)"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Update Client/Program.vb lines 50-52 with your server URL"
Write-Host "  2. Rebuild client: dotnet build Client/Client.vbproj -c Release"
Write-Host "  3. Set up GitHub deployment in Azure Portal"
Write-Host "  4. Test connection and monitor logs"
Write-Host ""
Write-Host "Monitor logs:" -ForegroundColor Cyan
Write-Host "  az webapp log tail --resource-group $resourceGroup --name $appName"
