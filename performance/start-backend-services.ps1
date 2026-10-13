# ==============================================================================
# Helper to Start all 4 RescuePlate Backend Microservices for Performance Testing
# ==============================================================================

$projectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "Starting RescuePlate Backend Microservices..." -ForegroundColor Cyan

$services = @(
    @{ Name = "UserService"; Port = 5000; Path = "backend\UserService" },
    @{ Name = "DonationService"; Port = 5001; Path = "backend\DonationService" },
    @{ Name = "RequestWorkflowService"; Port = 5002; Path = "backend\RequestWorkflowService" },
    @{ Name = "DeliveryWorkflowService"; Port = 5003; Path = "backend\DeliveryWorkflowService" }
)

$processes = @()

foreach ($svc in $services) {
    $fullPath = Join-Path $projectRoot $svc.Path
    $dllPath = Join-Path $fullPath "bin\Debug\net10.0\$($svc.Name).dll"
    $p = Start-Process dotnet -ArgumentList "`"$dllPath`" --urls http://localhost:$($svc.Port)" -WorkingDirectory $fullPath -PassThru -WindowStyle Hidden
    $processes += @{ Name = $svc.Name; Id = $p.Id; Port = $svc.Port }
}

$processes | Export-Clixml -Path (Join-Path $PSScriptRoot "results\running_services.xml")

Write-Host "Waiting 8 seconds for microservices to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

foreach ($svc in $services) {
    $conn = Get-NetTCPConnection -LocalPort $svc.Port -ErrorAction SilentlyContinue
    if ($conn) {
        Write-Host " [OK] $($svc.Name) listening on port $($svc.Port)" -ForegroundColor Green
    } else {
        Write-Host " [WAITING] $($svc.Name) initializing on port $($svc.Port)" -ForegroundColor Yellow
    }
}

Write-Host "All backend services launched." -ForegroundColor Green
