# RescuePlate Mutation Testing Suite (Stryker.NET)

This directory contains the automated mutation testing configuration and execution scripts for the **RescuePlate** microservice architecture.

## Overview

Mutation testing evaluates the fault tolerance and quality of the existing unit test suites by deliberately injecting artificial defects (mutants) into the production source code and checking if the test suite catches (kills) them.

- **Tool:** [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) (`dotnet-stryker` v5.0.0)
- **Target Microservices:**
  - `DeliveryWorkflowService` (Sprint 4 - Deliveries & Tracking)
  - `UserService` (Sprint 1 & 2 - Auth, Admin, Profile)
  - `DonationService` (Sprint 2 - Donations & Expiry)
  - `RequestWorkflowService` (Sprint 3 - Food Requests & Matching)

---

## Prerequisites

1. .NET 10 SDK
2. Dotnet tools restored:
   ```bash
   dotnet tool restore
   ```

---

## How to Run

A unified PowerShell script (`run-mutation-tests.ps1`) is provided to run mutation testing for an individual service or across all services.

### 1. Run for Sprint 4 (Recommended for Live Demo - fast execution):
```powershell
.\mutation\run-mutation-tests.ps1 -Service delivery
```

### 2. Run for Other Services:
```powershell
# UserService (Sprint 1 & 2)
.\mutation\run-mutation-tests.ps1 -Service user

# DonationService (Sprint 2)
.\mutation\run-mutation-tests.ps1 -Service donation

# RequestWorkflowService (Sprint 3)
.\mutation\run-mutation-tests.ps1 -Service request
```

### 3. Run for All Microservices Sequentially:
```powershell
.\mutation\run-mutation-tests.ps1 -Service all
```

---

## Output Reports

Interactive HTML reports are automatically generated and saved into:
```
mutation/reports/<service-name>/
```
Opening `mutation-report.html` in any web browser presents:
- **Mutation Score %**
- **Killed Mutants** (Tests detected the artificial bug)
- **Survived Mutants** (Tests passed despite the bug - highlights gaps)
- **Timeout / Compile Errors**
- Interactive code viewer highlighting exact mutant lines with diffs.
