<#
.SYNOPSIS
    Master Automated Testing Pipeline Runner for SE3112 Assignment (RescuePlate).

.DESCRIPTION
    Executes all 4 individual testing paradigms systematically:
    1. Unit Testing & Mocking (xUnit)
    2. Mutation Testing (Stryker.NET)
    3. Load & Performance Testing (Apache JMeter)
    4. UI / E2E Testing (Cypress)

.PARAMETER Stage
    The specific stage to execute: 'all', 'unit', 'mutation', 'load', or 'e2e'. Defaults to 'all'.
#>
param(
    [ValidateSet("all", "unit", "mutation", "load", "e2e")]
    [string]$Stage = "all"
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host " RescuePlate - SE3112 Microservice Testing Pipeline Master Runner" -ForegroundColor Cyan
Write-Host " Selected Stage: $Stage" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

# -----------------------------------------------------------------------------
# STAGE 1: Unit Testing (xUnit)
# -----------------------------------------------------------------------------
function Run-UnitTests {
    Write-Host ""
    Write-Host ">>> [1/4] Running Unit Tests for DeliveryWorkflowService (Sprint 4)..." -ForegroundColor Green
    Push-Location "$rootDir\backend\DeliveryWorkflowService.UnitTests"
    try {
        dotnet test --logger "console;verbosity=normal"
    }
    finally {
        Pop-Location
    }
}

# -----------------------------------------------------------------------------
# STAGE 2: Mutation Testing (Stryker.NET)
# -----------------------------------------------------------------------------
function Run-MutationTests {
    Write-Host ""
    Write-Host ">>> [2/4] Running Stryker.NET Mutation Testing (Sprint 4)..." -ForegroundColor Green
    Push-Location $rootDir
    try {
        dotnet tool restore
        & "$rootDir\mutation\run-mutation-tests.ps1" -Service delivery
    }
    finally {
        Pop-Location
    }
}

# -----------------------------------------------------------------------------
# STAGE 3: Load & Performance Testing (JMeter)
# -----------------------------------------------------------------------------
function Run-LoadTests {
    Write-Host ""
    Write-Host ">>> [3/4] Running Apache JMeter Load & Concurrency Test (Sprint 4)..." -ForegroundColor Green
    Push-Location "$rootDir\performance"
    try {
        & "$rootDir\performance\execute_sprint4_load_test.ps1"
    }
    finally {
        Pop-Location
    }
}

# -----------------------------------------------------------------------------
# STAGE 4: UI / E2E Testing (Cypress)
# -----------------------------------------------------------------------------
function Run-E2ETests {
    Write-Host ""
    Write-Host ">>> [4/4] Running Cypress UI & End-to-End Tests (Sprint 4)..." -ForegroundColor Green
    Push-Location "$rootDir\frontend"
    try {
        npx cypress run --spec "cypress/e2e/sprint4*.cy.js"
    }
    finally {
        Pop-Location
    }
}

# -----------------------------------------------------------------------------
# Execution Router
# -----------------------------------------------------------------------------
switch ($Stage) {
    "unit" {
        Run-UnitTests
    }
    "mutation" {
        Run-MutationTests
    }
    "load" {
        Run-LoadTests
    }
    "e2e" {
        Run-E2ETests
    }
    "all" {
        Run-UnitTests
        Run-MutationTests
        Run-LoadTests
        Run-E2ETests
    }
}

Write-Host ""
Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host " Testing Pipeline Stage '$Stage' Finished Successfully!" -ForegroundColor Green
Write-Host "==========================================================================" -ForegroundColor Cyan
