# ==============================================================================
# Master Automated Performance Test Runner for Sprint 4
# ==============================================================================

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$jmxPath = Join-Path $scriptDir "scripts\RescuePlate_Sprint4_LoadTest.jmx"
$resultsDir = Join-Path $scriptDir "results"
$jtlPath = Join-Path $resultsDir "sprint4_load_results.jtl"
$htmlReportDir = Join-Path $resultsDir "dashboard_report"
$seedSql = Join-Path $scriptDir "seed-sprint4-data.sql"

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " [STEP 1/5] Seeding Database with Sprint 4 Test Data" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

[Environment]::SetEnvironmentVariable('PGPASSWORD', 'ArunS1573@DMS')
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -h localhost -U postgres -d RescuePlateDB -f $seedSql
if ($LASTEXITCODE -ne 0) {
    Write-Error "Database seeding failed!"
    exit 1
}
Write-Host "Database successfully seeded." -ForegroundColor Green

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " [STEP 2/5] Starting Background Microservices" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

# Kill any rogue processes on 5000, 5002, 5003 first
$roguePorts = @(5000, 5002, 5003)
foreach ($p in $roguePorts) {
    $conns = Get-NetTCPConnection -LocalPort $p -ErrorAction SilentlyContinue
    if ($conns) {
        foreach ($c in $conns) {
            Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue
        }
    }
}
Start-Sleep -Seconds 2

# Start UserService (5000)
$userProc = Start-Process dotnet `
    -ArgumentList "bin/Debug/net10.0/UserService.dll --urls http://localhost:5000" `
    -WorkingDirectory "$projectRoot\backend\UserService" `
    -PassThru

# Start RequestWorkflowService (5002)
$reqProc = Start-Process dotnet `
    -ArgumentList "bin/Debug/net10.0/RequestWorkflowService.dll --urls http://localhost:5002" `
    -WorkingDirectory "$projectRoot\backend\RequestWorkflowService" `
    -PassThru

# Start DeliveryWorkflowService (5003)
$delProc = Start-Process dotnet `
    -ArgumentList "bin/Debug/net10.0/DeliveryWorkflowService.dll --urls http://localhost:5003" `
    -WorkingDirectory "$projectRoot\backend\DeliveryWorkflowService" `
    -PassThru

Write-Host "Services started (PIDs: User=$($userProc.Id), Request=$($reqProc.Id), Delivery=$($delProc.Id))"

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " [STEP 3/5] Awaiting Microservice Health Readiness" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

$ready = $false
for ($i = 1; $i -le 15; $i++) {
    try {
        $delHealth = Invoke-RestMethod -Uri "http://localhost:5003/api/health" -TimeoutSec 2 -ErrorAction Stop
        $reqHealth = Invoke-RestMethod -Uri "http://localhost:5002/api/health" -TimeoutSec 2 -ErrorAction Stop
        if ($delHealth.status -eq "Online" -and $reqHealth.status -eq "Online") {
            $ready = $true
            break
        }
    } catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $ready) {
    Write-Error "Microservices failed to become ready in time."
    Stop-Process -Id $userProc.Id -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $reqProc.Id -Force -ErrorAction SilentlyContinue
    Stop-Process -Id $delProc.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

Write-Host "All microservices are ONLINE and responding to health probes." -ForegroundColor Green

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " [STEP 4/5] Running Apache JMeter Sprint 4 Concurrency Test" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

# Clean previous results
if (Test-Path $jtlPath) { Remove-Item -Force $jtlPath }
if (Test-Path $htmlReportDir) { Remove-Item -Recurse -Force $htmlReportDir }
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

$jmeterBat = "$HOME\.jmeter\apache-jmeter-5.6.3\bin\jmeter.bat"
if (-not (Test-Path $jmeterBat)) {
    $jmeterBat = "jmeter"
}

& $jmeterBat -n `
    -t "$jmxPath" `
    -JTHREAD_COUNT=50 `
    -JRAMP_UP_SECONDS=20 `
    -JLOOP_COUNT=5 `
    -l "$jtlPath" `
    -e -o "$htmlReportDir"

$jmeterExit = $LASTEXITCODE

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " [STEP 5/5] Teardown & Service Cleanup" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

Stop-Process -Id $userProc.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $reqProc.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $delProc.Id -Force -ErrorAction SilentlyContinue

Write-Host "Microservice test processes stopped." -ForegroundColor Green
Write-Host "Load test finished with exit code: $jmeterExit"
