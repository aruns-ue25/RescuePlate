# RescuePlate — Food Waste Redistribution Platform

[![RescuePlate CI](https://github.com/aruns-ue25/RescuePlate/actions/workflows/ci.yml/badge.svg)](https://github.com/aruns-ue25/RescuePlate/actions/workflows/ci.yml)
[![Testing Pipeline](https://github.com/aruns-ue25/RescuePlate/actions/workflows/testing-pipeline.yml/badge.svg)](https://github.com/aruns-ue25/RescuePlate/actions/workflows/testing-pipeline.yml)
[![RescuePlate CD](https://github.com/aruns-ue25/RescuePlate/actions/workflows/cd.yml/badge.svg)](https://github.com/aruns-ue25/RescuePlate/actions/workflows/cd.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Target Framework](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Frontend](https://img.shields.io/badge/React-18-blue.svg)](https://react.dev/)

RescuePlate is an **event-driven, decoupled microservices platform** designed to eliminate food waste by connecting commercial food donors (restaurants, hotels, bakeries, grocers) with registered charitable organizations. Built on **ASP.NET Core 10**, **React 18 (Vite)**, **Apache Kafka (KRaft mode)**, and **PostgreSQL 16**, the platform automates surplus food listings, portion claims, delivery tracking, and administrative governance.

---

## 1. Project Overview & Architecture

### Microservice Topology
The platform consists of four single-responsibility microservices and a single-page frontend application:

* **User & Authentication Service (`UserService` — Port 5000)**: Identity lifecycle, BCrypt password hashing, JWT minting, Role-Based Access Control (RBAC), user profiles, and administrative 2FA challenge flow.
* **Donation Management Service (`DonationService` — Port 5001)**: Surplus food listings CRUD, categorization, quantity tracking, and background expiry worker (`DonationExpiryBackgroundService`).
* **Request Workflow Service (`RequestWorkflowService` — Port 5002)**: Organization portion claims, donor claim review/acceptance, community food need requests, and Kafka event pub/sub.
* **Delivery Workflow Service (`DeliveryWorkflowService` — Port 5003)**: Pickup scheduling, driver collection updates, verification code confirmation, and audit logs.
* **Frontend SPA (`frontend` — Port 3000 / 5173)**: React 18 Single Page Application with Axios, TailwindCSS, and role-tailored dashboards.
* **Event Streaming**: **Apache Kafka 7.8 (KRaft Mode)** coordinating cross-service state synchronization (`donation-events`, `request-events`) without ZooKeeper container overhead.
* **Database Layer**: **PostgreSQL 16** with Entity Framework Core (EF Core 10) maintaining separate `DbContext` models per service on a shared, cost-effective database cluster.

---

## 2. Live Cloud Deployment & Evaluator Swagger UI Links

RescuePlate is deployed on **Microsoft Azure Container Apps (Southeast Asia Region)**. Evaluators can access interactive Swagger UI interfaces for all microservices directly from a web browser:

| Microservice | Base API URL | Live Interactive Swagger UI | Health Check |
| :--- | :--- | :--- | :--- |
| **UserService** | `https://rescueplate-userservice.wittyrock-ff20867a.southeastasia.azurecontainerapps.io` | [Open UserService Swagger](https://rescueplate-userservice.wittyrock-ff20867a.southeastasia.azurecontainerapps.io/swagger) | `/health` |
| **DonationService** | `https://rescueplate-donationservice.wittyrock-ff20867a.southeastasia.azurecontainerapps.io` | [Open DonationService Swagger](https://rescueplate-donationservice.wittyrock-ff20867a.southeastasia.azurecontainerapps.io/swagger) | `/health` |
| **RequestService** | `https://rescueplate-requestservice.wittyrock-ff20867a.southeastasia.azurecontainerapps.io` | [Open RequestService Swagger](https://rescueplate-requestservice.wittyrock-ff20867a.southeastasia.azurecontainerapps.io/swagger) | `/health` |
| **DeliveryService** | `https://rescueplate-deliveryservice.wittyrock-ff20867a.southeastasia.azurecontainerapps.io` | [Open DeliveryService Swagger](https://rescueplate-deliveryservice.wittyrock-ff20867a.southeastasia.azurecontainerapps.io/swagger) | `/health` |

### Evaluator Seeded Test Credentials

| Role | Email | Password | Permissions & Capabilities |
| :--- | :--- | :--- | :--- |
| **Donor** | `aruns@gmail.com` | `ArunS@123` | Post surplus food listings, manage active posts, accept/reject portion claims. |
| **Organization** | `kathisan@gmail.com` | `Kathi@123` | Browse surplus listings, submit food portion claims, confirm physical receipts. |
| **Admin** | `admin@rescueplate.org` | `Admin@123` | Platform oversight, audit trails, user account deactivation/activation. |

* **Admin Access Key**: `ADMIN-SECURE-KEY-2026`

---

## 3. Repository Structure

```
RescuePlate/
├── .github/
│   └── workflows/
│       ├── ci.yml                     # Continuous integration build & test pipeline
│       ├── cd.yml                     # Continuous deployment to Azure Container Apps (OIDC)
│       ├── testing-pipeline.yml       # Multi-role testing battery (Unit, Stryker, JMeter, Cypress)
│       ├── requestworkflow-ci.yml     # Dedicated CI for RequestWorkflowService
│       └── deliveryworkflow-ci.yml    # Dedicated CI for DeliveryWorkflowService
├── backend/
│   ├── RescuePlate.slnx               # Master .NET 10 solution file
│   ├── UserService/                   # User & Authentication Service (Port 5000)
│   ├── UserService.UnitTests/
│   ├── UserService.IntegrationTests/
│   ├── DonationService/               # Donation Management Service (Port 5001)
│   ├── DonationService.UnitTests/
│   ├── DonationService.IntegrationTests/
│   ├── RequestWorkflowService/        # Request & Claim Workflow Service (Port 5002)
│   ├── RequestWorkflowService.UnitTests/
│   ├── RequestWorkflowService.IntegrationTests/
│   ├── DeliveryWorkflowService/       # Delivery Logistics & Handover Service (Port 5003)
│   ├── DeliveryWorkflowService.UnitTests/
│   └── DeliveryWorkflowService.IntegrationTests/
├── frontend/                          # React 18 + Vite SPA (Port 3000 / 5173)
│   ├── cypress/                       # Automated Cypress E2E browser test suites
│   └── src/                           # Components, pages, contexts, Axios client
├── mutation/                          # Stryker.NET mutation testing HTML/JSON reports
│   └── reports/                       # Per-service mutation coverage artifacts
├── performance/                       # Apache JMeter load and performance test suite
│   ├── scripts/                       # JMeter test plan (*.jmx)
│   └── results/                       # HTML dashboard reports & JTL samples
├── docker-compose.yml                 # Local multi-container development environment
├── API_DOCUMENTATION.md               # Complete OpenAPI/Swagger evaluation catalog
└── README.md                          # Project documentation and build instructions
```

---

## 4. Prerequisites

Ensure the following tools are installed on your machine before running locally:

* **.NET SDK 10.0** ([Download](https://dotnet.microsoft.com/download))
* **Node.js 20+** or **24.x** & **npm 10+** ([Download](https://nodejs.org/))
* **Docker Desktop** (v25+) & **Docker Compose** ([Download](https://www.docker.com/products/docker-desktop/))
* **PostgreSQL 16** (Only required if running services outside Docker)
* **Git** (v2.40+)

---

## 5. Environment Configuration

The application uses environment variables for configuration. Create a `.env` file in the root directory:

```ini
# PostgreSQL Database Settings
POSTGRES_DB=RescuePlateDB
POSTGRES_USER=postgres
POSTGRES_PASSWORD=YourSecurePassword123!

# JWT Secret Key (min 32 characters)
JWT_SECRET_KEY=RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood

# Service Origins & Environment
FRONTEND_URL=http://localhost:3000
ASPNETCORE_ENVIRONMENT=Development
```

---

## 6. Build and Run Instructions

### Option A: Complete System via Docker Compose (Recommended)

To build and run all 4 microservices, the React frontend, PostgreSQL, and Apache Kafka with a single command:

```bash
# 1. Clone repository
git clone https://github.com/aruns-ue25/RescuePlate.git
cd RescuePlate

# 2. Build and launch all containers in detached mode
docker compose up -d --build

# 3. Verify running containers
docker compose ps
```

#### Local Port Mapping:
* **Frontend SPA**: `http://localhost:3000`
* **UserService Swagger**: `http://localhost:5000/swagger`
* **DonationService Swagger**: `http://localhost:5001/swagger`
* **RequestService Swagger**: `http://localhost:5002/swagger`
* **DeliveryService Swagger**: `http://localhost:5003/swagger`
* **PostgreSQL**: `localhost:5432`
* **Apache Kafka**: `localhost:9092`

To shut down all containers:
```bash
docker compose down
```

---

### Option B: Running Individual Services for Development

#### 1. Start Infrastructure Dependencies
```bash
docker compose up -d postgres kafka
```

#### 2. Restore and Build .NET Microservices
```bash
# Restore all dependencies across the solution
dotnet restore backend/RescuePlate.slnx

# Build all microservices
dotnet build backend/RescuePlate.slnx --configuration Release
```

#### 3. Run Microservices (Separate Terminals)
```bash
# Terminal 1: User & Authentication Service (Port 5000)
dotnet run --project backend/UserService/UserService.csproj --urls "http://localhost:5000"

# Terminal 2: Donation Management Service (Port 5001)
dotnet run --project backend/DonationService/DonationService.csproj --urls "http://localhost:5001"

# Terminal 3: Request Workflow Service (Port 5002)
dotnet run --project backend/RequestWorkflowService/RequestWorkflowService.csproj --urls "http://localhost:5002"

# Terminal 4: Delivery Workflow Service (Port 5003)
dotnet run --project backend/DeliveryWorkflowService/DeliveryWorkflowService.csproj --urls "http://localhost:5003"
```

#### 4. Run the React Frontend
```bash
# Terminal 5: Frontend Development Server
cd frontend
npm install
npm run dev
```
The frontend will start at `http://localhost:5173` (or `http://localhost:3000` via Docker).

---

## 7. Testing Instructions

RescuePlate includes automated testing across all four layers of the test pyramid:

### 1. Run Unit Tests (xUnit)
```bash
# Run all unit tests across the entire platform
dotnet test backend/RescuePlate.slnx --filter "FullyQualifiedName!~IntegrationTests"

# Run unit tests for a specific microservice
dotnet test backend/UserService.UnitTests
dotnet test backend/DonationService.UnitTests
dotnet test backend/RequestWorkflowService.UnitTests
dotnet test backend/DeliveryWorkflowService.UnitTests
```

### 2. Run Integration Tests (xUnit + WebApplicationFactory)
```bash
# Run all integration tests across all microservices
dotnet test backend/RescuePlate.slnx --filter "FullyQualifiedName~IntegrationTests"
```

### 3. Run Mutation Testing (Stryker.NET)
```bash
# Restore dotnet tools
dotnet tool restore

# Run mutation testing on a service
cd backend/DeliveryWorkflowService.UnitTests
dotnet stryker
```
Mutation test HTML reports are generated under: `mutation/reports/<ServiceName>/mutation-report.html`.

### 4. Run End-to-End Browser Tests (Cypress)
```bash
cd frontend
# Run Cypress headlessly
npm run test

# Or open interactive Cypress runner
npx cypress open
```

### 5. Run Concurrency & Load Testing (Apache JMeter)
Execute the non-GUI JMeter load test against the backend services:
```bash
jmeter -n \
  -t performance/scripts/RescuePlate_Sprint4_LoadTest.jmx \
  -l performance/results/sprint4_load_results.jtl \
  -e -o performance/results/dashboard_report
```
View the interactive HTML dashboard report at: `performance/results/dashboard_report/index.html`.

---

## 8. Continuous Integration & Deployment (CI/CD)

The repository uses automated GitHub Actions workflows:

1. **Continuous Integration (`ci.yml` & `testing-pipeline.yml`)**:
   * Triggers on pull requests and pushes to `main`.
   * Restores, builds, and executes all unit tests, Stryker mutation testing, Cypress E2E flows, and JMeter load tests in parallel.
   * Collects XPlat code coverage and publishes unified TRX/HTML step summaries.
2. **Continuous Deployment (`cd.yml`)**:
   * Triggers automatically upon successful CI runs on `main`.
   * Authenticates to Microsoft Azure using passwordless **GitHub OpenID Connect (OIDC)**.
   * Builds and pushes versioned multi-stage Docker images to **Azure Container Registry** (`rescueplateacr26`).
   * Updates container revisions in **Azure Container Apps** with zero-downtime rolling deployments and verifies health probes (`/health`).

---

## 9. Project Contributors & Agile Roles

| Student Name | Student ID | Primary Roles Across Sprints |
| :--- | :--- | :--- |
| **Sugarthan Arun** | IT24200310 | Business Analyst (S1) / DevOps (S2) / Developer (S3) / QA Engineer (S4) |
| **Kathisan A.M.** | IT24103470 | Developer (S1) / Business Analyst (S2) / QA Engineer (S3) / DevOps (S4) |
| **Srikarsan K.** | IT24103459 | QA Engineer (S1) / Developer (S2) / DevOps (S3) / Business Analyst (S4) |
| **Sathushan M.** | IT24103551 | DevOps (S1) / QA Engineer (S2) / Business Analyst (S3) / Developer (S4) |

---

## 10. License

This project is developed for the **SE3022 Case Study Project (Year 3 Semester 1, 2026)** at SLIIT and is licensed under the [MIT License](LICENSE).
