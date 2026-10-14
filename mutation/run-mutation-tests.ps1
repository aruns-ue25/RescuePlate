param(
    [ValidateSet("all", "delivery", "user", "donation", "request")]
    [string]$Service = "delivery"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootDir = (Get-Item "$scriptDir\..").FullName

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " RescuePlate - Mutation Testing Suite (Stryker.NET)" -ForegroundColor Cyan
Write-Host " Target Service: $Service" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Restore tools if needed
Push-Location $rootDir
try {
    Write-Host "[1/3] Restoring dotnet tools..." -ForegroundColor Yellow
    dotnet tool restore
}
finally {
    Pop-Location
}

function Run-StrykerForService {
    param (
        [string]$ServiceName,
        [string]$TestProjectPath
    )

    $fullTestPath = Join-Path $rootDir $TestProjectPath
    Write-Host ""
    Write-Host ">>> Running Mutation Tests for: $ServiceName" -ForegroundColor Green
    Write-Host ">>> Working Directory: $fullTestPath" -ForegroundColor DarkGray

    Push-Location $fullTestPath
    try {
        dotnet stryker
    }
    finally {
        Pop-Location
    }
}

switch ($Service) {
    "delivery" {
        Run-StrykerForService -ServiceName "DeliveryWorkflowService (Sprint 4)" -TestProjectPath "backend\DeliveryWorkflowService.UnitTests"
    }
    "user" {
        Run-StrykerForService -ServiceName "UserService (Sprint 1 & 2)" -TestProjectPath "backend\UserService.UnitTests"
    }
    "donation" {
        Run-StrykerForService -ServiceName "DonationService (Sprint 2)" -TestProjectPath "backend\DonationService.UnitTests"
    }
    "request" {
        Run-StrykerForService -ServiceName "RequestWorkflowService (Sprint 3)" -TestProjectPath "backend\RequestWorkflowService.UnitTests"
    }
    "all" {
        Run-StrykerForService -ServiceName "DeliveryWorkflowService (Sprint 4)" -TestProjectPath "backend\DeliveryWorkflowService.UnitTests"
        Run-StrykerForService -ServiceName "DonationService (Sprint 2)" -TestProjectPath "backend\DonationService.UnitTests"
        Run-StrykerForService -ServiceName "RequestWorkflowService (Sprint 3)" -TestProjectPath "backend\RequestWorkflowService.UnitTests"
        Run-StrykerForService -ServiceName "UserService (Sprint 1 & 2)" -TestProjectPath "backend\UserService.UnitTests"
    }
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Mutation Testing Complete! Reports saved to mutation/reports/" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
