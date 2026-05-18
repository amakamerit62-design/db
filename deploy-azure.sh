#!/bin/bash
# Azure App Service Deployment Script for VB.NET RAT Server
# This script automates the deployment to Azure App Service

# Configuration (EDIT THESE)
RESOURCE_GROUP="rat-deployment"
APP_SERVICE_PLAN="rat-plan"
APP_NAME="rat-server-$(date +%s)"  # Unique name
LOCATION="eastus"
SKU="F1"  # F1 = Free, B1 = Basic ($12/mo)

echo "=== VB.NET RAT Server - Azure Deployment ==="
echo "This script will create an Azure App Service and deploy your server"
echo ""
echo "Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  App Service Plan: $APP_SERVICE_PLAN"
echo "  App Name: $APP_NAME"
echo "  Location: $LOCATION"
echo "  SKU: $SKU (Free tier)"
echo ""
echo "Prerequisites:"
echo "  1. Azure CLI installed (https://docs.microsoft.com/cli/azure)"
echo "  2. Azure account created (https://azure.microsoft.com/free)"
echo "  3. Logged in: az login"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cancelled"
    exit 1
fi

echo ""
echo "Step 1: Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION

echo ""
echo "Step 2: Creating App Service plan..."
az appservice plan create \
    --name $APP_SERVICE_PLAN \
    --resource-group $RESOURCE_GROUP \
    --is-linux false \
    --sku $SKU

echo ""
echo "Step 3: Creating web app (.NET Framework 4.8)..."
az webapp create \
    --resource-group $RESOURCE_GROUP \
    --plan $APP_SERVICE_PLAN \
    --name $APP_NAME \
    --runtime "DOTNETCORE|4.8"

echo ""
echo "Step 4: Enabling WebSocket..."
az webapp config set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --web-sockets-enabled true

echo ""
echo "Step 5: Setting up GitHub deployment..."
# Note: This requires interactive authentication for GitHub
az webapp deployment source config-zip \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --src . 2>/dev/null || {
    echo "Note: GitHub integration requires manual setup in Azure Portal"
    echo "  1. Go to https://portal.azure.com"
    echo "  2. Find your app: $APP_NAME"
    echo "  3. Deployment Center -> GitHub -> Authorize"
}

echo ""
echo "=== Deployment Complete ==="
echo ""
echo "Your app is ready at:"
echo "  https://$APP_NAME.azurewebsites.net"
echo ""
echo "WebSocket URL for Client:"
echo "  ws://$APP_NAME.azurewebsites.net:9823"
echo "  wss://$APP_NAME.azurewebsites.net:443 (with TLS)"
echo ""
echo "Next steps:"
echo "  1. Update Client/Program.vb lines 50-52 with your server URL"
echo "  2. Rebuild client: dotnet build Client/Client.vbproj -c Release"
echo "  3. Test connection and monitor logs in Azure Portal"
echo ""
echo "Monitor logs:"
echo "  az webapp log tail --resource-group $RESOURCE_GROUP --name $APP_NAME"
