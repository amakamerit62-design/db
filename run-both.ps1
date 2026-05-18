# Run both Server and Client in separate processes with visible output
$serverPath = "C:\Users\USER\Desktop\project\project synced\Server\bin\Debug\AsyncRAT.exe"
$clientPath = "C:\Users\USER\Desktop\project\project synced\Client\bin\Debug\Client.exe"

Write-Host "Starting Server..." -ForegroundColor Cyan
Start-Process -FilePath $serverPath -WindowStyle Normal

# Wait for server to start
Start-Sleep -Seconds 2

Write-Host "Starting Client..." -ForegroundColor Yellow
Start-Process -FilePath $clientPath -WindowStyle Normal

Write-Host "Both processes started. Check their console windows for output." -ForegroundColor Green
Write-Host "Close console windows to stop." -ForegroundColor Green
