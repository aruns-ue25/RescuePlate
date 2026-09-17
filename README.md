# RescuePlate

RescuePlate is a food waste redistribution platform that connects businesses with surplus food to organizations that can redistribute it to people in need.

The platform supports food donors, recipient organizations, and administrators through a distributed web-based system.

## Project Overview

RescuePlate provides a platform for:

- Food donors to create and manage surplus-food donations
- Organizations to discover available donations
- Users to manage their profiles and account information
- Organizations and donors to interact through donation request and fulfilment workflows
- Distributed event processing using Apache Kafka
- Automated testing through unit, integration, and end-to-end tests
- Continuous integration and continuous deployment

## Technology Stack

### Frontend

- React
- Vite
- JavaScript
- Axios
- Cypress

### Backend

- ASP.NET Core
- .NET 10
- Entity Framework Core
- PostgreSQL
- Npgsql
- JWT Authentication
- BCrypt password hashing
- Apache Kafka

### DevOps and Deployment

- Git
- GitHub
- GitHub Actions
- Docker
- Azure Container Registry
- Azure Container Apps
- Azure PostgreSQL

### Testing

- xUnit
- ASP.NET Core Integration Testing
- Cypress E2E Testing

## Repository Structure

```text
RescuePlate/
│
├── backend/
│   ├── DonationService/
│   ├── DonationService.UnitTests/
│   ├── DonationService.IntegrationTests/
│   ├── UserService/
│   ├── UserService.UnitTests/
│   ├── UserService.IntegrationTests/
│   └── RescuePlate.slnx
│
├── frontend/
│   ├── src/
│   ├── cypress/
│   ├── cypress.config.js
│   ├── package.json
│   └── package-lock.json
│
├── .github/
│   └── workflows/
│       ├── ci.yml
│       ├── sprint2-ci.yml
│       └── cd.yml
│
└── README.md