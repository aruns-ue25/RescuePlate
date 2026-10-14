# ==============================================================================
# Helper to Stop RescuePlate Backend Microservices
# ==============================================================================

$runningServicesXml = Join-Path $PSScriptRoot "results\running_services.xml"

if (Test-Path $runningServicesXml) {
    $processes = Import-Clixml -Path $runningServicesXml
    foreach ($p in $processes) {
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        Write-Host "Stopped $($p.Name) (PID: $($p.Id))" -ForegroundColor Yellow
    }
    Remove-Item -Force $runningServicesXml -ErrorAction SilentlyContinue
}

$ports = @(5000, 5001, 5002, 5003)
foreach ($port in $ports) {
    $conn = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($conn) {
        $pids = $conn | Select-Object -ExpandProperty OwningProcess -Unique
        foreach ($pidToKill in $pids) {
            Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "All performance testing backend services stopped." -ForegroundColor Green
