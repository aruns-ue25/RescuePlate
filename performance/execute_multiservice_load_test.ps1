# ==============================================================================
# RescuePlate Multi-Service Performance & Load Testing Runner
# Microservices: UserService (5000), DonationService (5001), RequestWorkflowService (5002)
# ==============================================================================

param(
    [int]$Threads = 30,
    [int]$RampUp = 10,
    [int]$Loops = 5,
    [switch]$AutoStartServices
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$jmxPath = Join-Path $scriptDir "scripts\RescuePlate_MultiService_LoadTest.jmx"
$resultsDir = Join-Path $scriptDir "results"
$jtlPath = Join-Path $resultsDir "multiservice_load_results.jtl"
$htmlReportDir = Join-Path $resultsDir "multiservice_dashboard_report"
$seedSql = Join-Path $scriptDir "seed-services-perf-data.sql"

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " RescuePlate Multi-Service JMeter Performance Test Runner" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "Services Under Test:     UserService (5000), DonationService (5001), RequestService (5002)"
Write-Host "Virtual Users (Threads): $Threads"
Write-Host "Ramp-up (Seconds):       $RampUp"
Write-Host "Iterations per User:     $Loops"
Write-Host "JMX Script:              $jmxPath"
Write-Host "Report Directory:        $htmlReportDir"
Write-Host "------------------------------------------------------------------"

# 1. Database Seeding
Write-Host "[STEP 1/4] Seeding Database with Performance Test Data..." -ForegroundColor Cyan
$psqlPaths = @(
    "C:\Program Files\PostgreSQL\18\bin\psql.exe",
    "C:\Program Files\PostgreSQL\17\bin\psql.exe",
    "C:\Program Files\PostgreSQL\16\bin\psql.exe",
    "psql"
)
$psqlCmd = $null
foreach ($p in $psqlPaths) {
    if (Test-Path $p -ErrorAction SilentlyContinue) {
        $psqlCmd = $p
        break
    } elseif (Get-Command $p -ErrorAction SilentlyContinue) {
        $psqlCmd = $p
        break
    }
}

if ($psqlCmd) {
    if (-not $env:PGPASSWORD) {
        $env:PGPASSWORD = "ArunS1573@DMS"
    }
    $env:ConnectionStrings__PostgreSQLConnection = "Host=localhost;Port=5432;Database=RescuePlateDB;Username=postgres;Password=ArunS1573@DMS"
    & $psqlCmd -h localhost -U postgres -d RescuePlateDB -f $seedSql
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database successfully seeded with performance test dataset." -ForegroundColor Green
    } else {
        Write-Warning "Database seeding exited with code $LASTEXITCODE. Proceeding with existing dataset."
    }
} else {
    Write-Warning "psql command not found on PATH. Ensure PostgreSQL database is seeded."
}

# 2. Check Service Ports (5000, 5001, 5002)
Write-Host "[STEP 2/4] Verifying Microservice Health Readiness..." -ForegroundColor Cyan
$ports = @(5000, 5001, 5002)
$missingPorts = @()

foreach ($port in $ports) {
    $conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if (-not $conn) {
        $missingPorts += $port
    }
}

$launchedProcs = @()
$connStr = "Host=localhost;Port=5432;Database=RescuePlateDB;Username=postgres;Password=ArunS1573@DMS"
if ($missingPorts.Count -gt 0 -and $AutoStartServices) {
    Write-Host "Starting missing services on ports: $($missingPorts -join ', ')..." -ForegroundColor Yellow
    if ($missingPorts -contains 5000) {
        $uDir = Join-Path $projectRoot "backend\UserService"
        $uDll = Join-Path $uDir "bin\Debug\net10.0\UserService.dll"
        $userProc = Start-Process dotnet -ArgumentList "`"$uDll`" --urls http://localhost:5000 --ConnectionStrings:PostgreSQLConnection `"$connStr`"" -WorkingDirectory $uDir -PassThru
        $launchedProcs += $userProc
    }
    if ($missingPorts -contains 5001) {
        $dDir = Join-Path $projectRoot "backend\DonationService"
        $dDll = Join-Path $dDir "bin\Debug\net10.0\DonationService.dll"
        $donProc = Start-Process dotnet -ArgumentList "`"$dDll`" --urls http://localhost:5001 --ConnectionStrings:PostgreSQLConnection `"$connStr`"" -WorkingDirectory $dDir -PassThru
        $launchedProcs += $donProc
    }
    if ($missingPorts -contains 5002) {
        $rDir = Join-Path $projectRoot "backend\RequestWorkflowService"
        $rDll = Join-Path $rDir "bin\Debug\net10.0\RequestWorkflowService.dll"
        $reqProc = Start-Process dotnet -ArgumentList "`"$rDll`" --urls http://localhost:5002 --ConnectionStrings:PostgreSQLConnection `"$connStr`"" -WorkingDirectory $rDir -PassThru
        $launchedProcs += $reqProc
    }
    Start-Sleep -Seconds 5
}

# Verify Health Endpoints
$allOnline = $false
Write-Host "Verifying microservice health status..." -ForegroundColor Yellow
for ($i = 1; $i -le 30; $i++) {
    $uOk = $false
    $dOk = $false
    $rOk = $false
    try {
        $u = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($u.StatusCode -eq 200) { $uOk = $true }
    } catch { }

    try {
        $d = Invoke-WebRequest -Uri "http://localhost:5001/api/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($d.StatusCode -eq 200) { $dOk = $true }
    } catch { }

    try {
        $r = Invoke-WebRequest -Uri "http://localhost:5002/api/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($r.StatusCode -eq 200) { $rOk = $true }
    } catch { }

    if ($uOk -and $dOk -and $rOk) {
        $allOnline = $true
        break
    }
    Write-Host " [Probe $i/30] UserService: $(if ($uOk) {'OK'} else {'waiting'}), DonationService: $(if ($dOk) {'OK'} else {'waiting'}), RequestService: $(if ($rOk) {'OK'} else {'waiting'})"
    Start-Sleep -Seconds 1
}

if (-not $allOnline) {
    Write-Error "Failed to bring all 3 microservices online within 30 seconds. Aborting performance test."
    exit 1
}
Write-Host "All 3 target microservices are ONLINE and healthy." -ForegroundColor Green

# 3. Clean Previous Results
Write-Host "[STEP 3/4] Preparing Output Directories..." -ForegroundColor Cyan
if (Test-Path $jtlPath) { Remove-Item -Force $jtlPath }
if (Test-Path $htmlReportDir) { Remove-Item -Recurse -Force $htmlReportDir }
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

# 4. Locate & Run JMeter
Write-Host "[STEP 4/4] Executing Apache JMeter Performance Test Suite..." -ForegroundColor Cyan
$jmeterExe = Get-Command jmeter -ErrorAction SilentlyContinue
if (-not $jmeterExe) {
    $fallbackJmeter = "$HOME\.jmeter\apache-jmeter-5.6.3\bin\jmeter.bat"
    if (Test-Path $fallbackJmeter) {
        $jmeterCmd = $fallbackJmeter
    } else {
        Write-Error "Apache JMeter executable not found. Please ensure JMeter is installed and added to PATH."
        exit 1
    }
} else {
    $jmeterCmd = "jmeter"
}

& $jmeterCmd -n `
    -t "$jmxPath" `
    -JTHREAD_COUNT=$Threads `
    -JRAMP_UP_SECONDS=$RampUp `
    -JLOOP_COUNT=$Loops `
    -l "$jtlPath" `
    -e -o "$htmlReportDir"

$jmeterExit = $LASTEXITCODE

# Cleanup auto-launched processes if any
foreach ($proc in $launchedProcs) {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "------------------------------------------------------------------"
Write-Host "Execution Finished. Exit Code: $jmeterExit"
if (Test-Path (Join-Path $htmlReportDir "index.html")) {
    Write-Host "Interactive HTML Dashboard Report generated at:" -ForegroundColor Green
    Write-Host (Join-Path $htmlReportDir "index.html") -ForegroundColor Green
}
