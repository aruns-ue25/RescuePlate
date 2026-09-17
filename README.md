# RescuePlate

RescuePlate is a microservice-based food waste redistribution platform designed to connect food donors such as restaurants and hotels with organizations such as charities. The platform supports user management, donation management, and food redistribution workflows.

## Repository Structure

src/backend/services/UserService/: User registration, authentication, profile management, account management, and RBAC logic.

src/backend/services/DonationService/: Donation creation, management, availability, expiry handling, and donation event publishing.

src/frontend/: React + Vite single-page application.

docker-compose.yml: Local development environment containing PostgreSQL, backend services, frontend, and Kafka.

.github/workflows/ci.yml: Continuous Integration workflow for building and testing the backend and frontend.

.github/workflows/cd.yml: Continuous Deployment workflow for building Docker images and deploying the application to Azure.

## Prerequisites

.NET SDK 10.0

Node.js 24.x

Docker & Docker Compose

PostgreSQL 16

Git

## Local Development Setup

Clone the repository:

git clone https://github.com/aruns-ue25/RescuePlate.git

Navigate to the project directory:

cd RescuePlate

## Running the Application with Docker Compose

Start the required services:

docker compose up -d

This starts the local development environment including:

- PostgreSQL
- UserService
- DonationService
- React frontend
- Apache Kafka

Check the running containers:

docker compose ps

## Running Backend Services

The backend services are located under:

src/backend/services/

UserService handles:

- User registration and login
- JWT authentication
- Role-based access control
- User profile management
- Account management

DonationService handles:

- Donation creation
- Donation management
- Available donation browsing
- Donation expiry
- Donation events through Kafka

To run a service individually, navigate to its project directory and use:

dotnet run

## Running Frontend

Navigate to the frontend directory:

cd src/frontend

Install dependencies:

npm install

Run the development server:

npm run dev

The frontend will be available through the Vite development server.

## Running Tests

Run all backend tests from the backend directory:

dotnet test

To run UserService unit tests:

dotnet test UserService.UnitTests

To run UserService integration tests:

dotnet test UserService.IntegrationTests

To run DonationService unit tests:

dotnet test DonationService.UnitTests

To run DonationService integration tests:

dotnet test DonationService.IntegrationTests

## CI/CD

The project uses GitHub Actions for Continuous Integration and Continuous Deployment.

### Continuous Integration

The CI pipeline:

- Restores backend dependencies
- Builds the backend
- Runs unit and integration tests
- Installs frontend dependencies
- Builds the React frontend

The CI workflow is triggered by pushes and pull requests to the main branch.

### Continuous Deployment

After a successful CI workflow and deployment trigger, the CD pipeline:

- Builds Docker images
- Pushes images to Azure Container Registry
- Deploys the services to Azure Container Apps

The production environment uses Azure PostgreSQL for persistent database storage.

## Docker

Docker is used to package the application services into portable containers.

Docker images are built for the backend services and frontend and stored in Azure Container Registry (ACR) for deployment.

## Kafka

Apache Kafka is used for event-driven communication between services.

DonationService publishes donation-related events such as:

- DonationCreated
- DonationUpdated
- DonationCancelled

## Production Deployment

The application is deployed using Azure Container Apps.

The production architecture includes:

- React Frontend → Azure Container App
- UserService → Azure Container App
- DonationService → Azure Container App
- PostgreSQL → Azure Database for PostgreSQL
- Kafka → Event-driven communication
- Azure Container Registry → Docker image storage
- GitHub Actions → CI/CD automation
