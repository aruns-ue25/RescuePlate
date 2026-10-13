# ==============================================================================
# RescuePlate Sprint 4 Performance & Load Testing Runner
# ==============================================================================

param(
    [int]$Threads = 50,
    [int]$RampUp = 20,
    [int]$Loops = 5
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$jmxPath = Join-Path $scriptDir "scripts\RescuePlate_Sprint4_LoadTest.jmx"
$resultsDir = Join-Path $scriptDir "results"
$jtlPath = Join-Path $resultsDir "sprint4_load_results.jtl"
$htmlReportDir = Join-Path $resultsDir "dashboard_report"

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " RescuePlate Sprint 4 JMeter Performance & Load Test Suite" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "Virtual Users (Threads): $Threads"
Write-Host "Ramp-up (Seconds):       $RampUp"
Write-Host "Iterations per User:     $Loops"
Write-Host "JMX Script:              $jmxPath"
Write-Host "Report Output:           $htmlReportDir"
Write-Host "------------------------------------------------------------------"

# 1. Verify JMeter Executable
$jmeterExe = Get-Command jmeter -ErrorAction SilentlyContinue
if (-not $jmeterExe) {
    $fallbackJmeter = "$HOME\.jmeter\apache-jmeter-5.6.3\bin\jmeter.bat"
    if (Test-Path $fallbackJmeter) {
        $jmeterCmd = $fallbackJmeter
    } else {
        Write-Error "Apache JMeter executable not found. Please verify installation."
        exit 1
    }
} else {
    $jmeterCmd = "jmeter"
}

# 2. Check Service Ports (5000, 5002, 5003)
$ports = @(5000, 5002, 5003)
$inactivePorts = @()
foreach ($port in $ports) {
    $conn = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if (-not $conn) {
        $inactivePorts += $port
    }
}

if ($inactivePorts.Count -gt 0) {
    Write-Warning "The following backend service ports are not actively listening: $($inactivePorts -join ', ')"
    Write-Warning "Please start the backend microservices before running the load test."
}

# 3. Clean Previous Results
if (Test-Path $jtlPath) {
    Remove-Item -Force $jtlPath
}
if (Test-Path $htmlReportDir) {
    Remove-Item -Recurse -Force $htmlReportDir
}
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

Write-Host "Executing JMeter test plan in Non-GUI CLI mode..." -ForegroundColor Yellow

# 4. Execute JMeter in Non-GUI mode with HTML report generation
& $jmeterCmd -n `
    -t "$jmxPath" `
    -JTHREAD_COUNT=$Threads `
    -JRAMP_UP_SECONDS=$RampUp `
    -JLOOP_COUNT=$Loops `
    -l "$jtlPath" `
    -e -o "$htmlReportDir"

Write-Host "==================================================================" -ForegroundColor Green
Write-Host " Load Test Execution Finished Successfully!" -ForegroundColor Green
Write-Host " Raw JTL Metrics: $jtlPath" -ForegroundColor Green
Write-Host " HTML Dashboard:  $htmlReportDir\index.html" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Green
