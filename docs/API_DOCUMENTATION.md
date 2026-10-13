# RescuePlate API Documentation & Interactive Swagger UI Guide

This document provides complete instructions for accessing, authenticating, and testing the RescuePlate microservices platform using **Swagger UI (OpenAPI 3.0)**, fulfilling the ASE API Documentation requirements.

---

## 4.1 Swagger UI Links (Evaluator Access)

RescuePlate is composed of four modular microservices built with ASP.NET Core 10. Swagger UI is **permanently enabled across all environments** (Development, Docker, Staging, and Production) and is accessible via web browser without requiring external tooling.

### Microservice Endpoints Table

| Microservice | Base URL | Interactive Swagger UI Link | OpenAPI JSON Specification | Health Check |
| :--- | :--- | :--- | :--- | :--- |
| **User & Authentication Service** | `http://localhost:5000` | [http://localhost:5000/swagger](http://localhost:5000/swagger) | [http://localhost:5000/swagger/v1/swagger.json](http://localhost:5000/swagger/v1/swagger.json) | `http://localhost:5000/health` |
| **Donation Management Service** | `http://localhost:5001` | [http://localhost:5001/swagger](http://localhost:5001/swagger) | [http://localhost:5001/swagger/v1/swagger.json](http://localhost:5001/swagger/v1/swagger.json) | `http://localhost:5001/health` |
| **Request Workflow Service** | `http://localhost:5002` | [http://localhost:5002/swagger](http://localhost:5002/swagger) | [http://localhost:5002/swagger/v1/swagger.json](http://localhost:5002/swagger/v1/swagger.json) | `http://localhost:5002/health` |
| **Delivery Workflow Service** | `http://localhost:5003` | [http://localhost:5003/swagger](http://localhost:5003/swagger) | [http://localhost:5003/swagger/v1/swagger.json](http://localhost:5003/swagger/v1/swagger.json) | `http://localhost:5003/health` |

> **Note on Deployed / Docker Execution**:  
> In Docker deployments (`docker-compose up -d`), all four microservices expose the exact same host ports (`5000`, `5001`, `5002`, `5003`). If deployed to a remote staging host, simply replace `localhost` with the target host IP or domain name (e.g., `http://<server-ip>:5000/swagger`).

---

## 4.2 Authentication Guide (JWT Bearer Scheme)

All protected microservice endpoints enforce Role-Based Access Control (RBAC) via **JWT (JSON Web Tokens)**. 

### How to Authenticate in Swagger UI (Step-by-Step)

```
┌─────────────────────────────────┐
│ 1. Open UserService Swagger UI  │ ──► Call POST /api/auth/login with test credentials
└─────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│ 2. Copy the JWT Bearer Token    │ ──► Extract the "token" value from response JSON
└─────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│ 3. Click "Authorize 🔓" Button  │ ──► Located at the top right of any Swagger UI page
└─────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│ 4. Enter: Bearer <your_token>   │ ──► Click "Authorize", then "Close" (Padlock locks 🔒)
└─────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│ 5. Execute Protected Endpoints   │ ──► Click "Try it out" and "Execute" on any endpoint!
└─────────────────────────────────┘
```

### Seeded Test Credentials

The database auto-initializes the following pre-configured test users for each supported role:

| Role | Email | Password | Allowed Capabilities |
| :--- | :--- | :--- | :--- |
| **DONOR** | `donor@rescueplate.com` | `Donor@123` | Post surplus food listings, manage listings, accept/reject food requests, submit food offers. |
| **ORGANIZATION** | `org@rescueplate.com` | `Org@123` | Browse surplus listings, submit portion claims, post organization food needs, confirm receipts. |
| **ADMIN** | `admin@rescueplate.com` | `Admin@123` | Platform oversight, view all users/organizations, inspect cross-service analytics dashboards. |

---

## 4.3 Sample Requests and Responses Catalog

Below are ready-to-use sample request payloads and corresponding JSON responses for each microservice.

---

### Sprint 1: User & Authentication Service (`Port 5000`)

#### 1. Register a Food Donor
- **Endpoint**: `POST /api/auth/register`
- **Access**: Public

**Sample Request Body:**
```json
{
  "email": "freshbakery@example.com",
  "password": "Password@123",
  "role": "DONOR",
  "businessOrOrgName": "Fresh Crust Bakery",
  "contactNumber": "+1555123456",
  "address": "124 Market Street, Downtown",
  "contactPersonName": "Sarah Jenkins"
}
```

**Sample Response (`201 Created`):**
```json
{
  "success": true,
  "message": "User registered successfully.",
  "data": {
    "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
    "email": "freshbakery@example.com",
    "role": "DONOR",
    "businessOrOrgName": "Fresh Crust Bakery",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2026-10-15T00:00:00Z"
  },
  "errors": []
}
```

#### 2. User Login
- **Endpoint**: `POST /api/auth/login`
- **Access**: Public

**Sample Request Body:**
```json
{
  "email": "donor@rescueplate.com",
  "password": "Donor@123"
}
```

**Sample Response (`200 OK`):**
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "id": "e4f3a2b1-1234-5678-9abc-def012345678",
    "email": "donor@rescueplate.com",
    "role": "DONOR",
    "businessOrOrgName": "Downtown Bistro",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJlNGYzYTJiMS0xMjM0...",
    "expiresAt": "2026-10-15T00:00:00Z"
  },
  "errors": []
}
```

#### 3. View Current Profile
- **Endpoint**: `GET /api/users/profile`
- **Access**: Authorized (`DONOR`, `ORGANIZATION`, `ADMIN`)

**Sample Response (`200 OK`):**
```json
{
  "success": true,
  "message": "Profile retrieved successfully.",
  "data": {
    "id": "e4f3a2b1-1234-5678-9abc-def012345678",
    "email": "donor@rescueplate.com",
    "role": "DONOR",
    "businessOrOrgName": "Downtown Bistro",
    "contactNumber": "+1555987654",
    "address": "456 Central Ave",
    "contactPersonName": "Chef Mario",
    "isActive": true,
    "createdAt": "2026-10-01T10:00:00Z"
  },
  "errors": []
}
```

---

### Sprint 2: Donation Management Service (`Port 5001`)

#### 1. Create a Surplus Food Listing
- **Endpoint**: `POST /api/donations`
- **Access**: Authorized (`DONOR`)

**Sample Request Body:**
```json
{
  "foodTitle": "Vegetable Curry and Rice Packs",
  "category": "Cooked Meals",
  "totalQuantity": 40,
  "unit": "portions",
  "expiryHours": 6,
  "collectionMode": "Pickup",
  "location": "Downtown Bistro, 456 Central Ave",
  "notes": "Freshly cooked lunch surplus. Packed in hygiene-sealed boxes.",
  "dietaryTags": "Vegetarian, Gluten-Free"
}
```

**Sample Response (`201 Created`):**
```json
{
  "success": true,
  "message": "Donation created successfully.",
  "data": {
    "id": 105,
    "donorId": "e4f3a2b1-1234-5678-9abc-def012345678",
    "donorName": "Downtown Bistro",
    "donorEmail": "donor@rescueplate.com",
    "foodTitle": "Vegetable Curry and Rice Packs",
    "category": "Cooked Meals",
    "totalQuantity": 40,
    "claimedQuantity": 0,
    "remainingQuantity": 40,
    "unit": "portions",
    "status": "Available",
    "collectionMode": "Pickup",
    "location": "Downtown Bistro, 456 Central Ave",
    "notes": "Freshly cooked lunch surplus. Packed in hygiene-sealed boxes.",
    "dietaryTags": "Vegetarian, Gluten-Free",
    "expiryTime": "2026-10-14T06:30:00Z",
    "createdAt": "2026-10-14T00:30:00Z"
  },
  "errors": []
}
```

#### 2. Query Available Surplus Food
- **Endpoint**: `GET /api/donations/available?category=Cooked%20Meals`
- **Access**: Public / Authorized

**Sample Response (`200 OK`):**
```json
{
  "success": true,
  "message": "Available donations retrieved successfully.",
  "data": [
    {
      "id": 105,
      "donorName": "Downtown Bistro",
      "foodTitle": "Vegetable Curry and Rice Packs",
      "category": "Cooked Meals",
      "remainingQuantity": 40,
      "unit": "portions",
      "status": "Available",
      "location": "Downtown Bistro, 456 Central Ave",
      "expiryTime": "2026-10-14T06:30:00Z"
    }
  ],
  "errors": []
}
```

#### 3. Administrative Donation Summary
- **Endpoint**: `GET /api/donations/admin/summary`
- **Access**: Authorized (`ADMIN`)

**Sample Response (`200 OK`):**
```json
{
  "success": true,
  "message": "Admin summary metrics retrieved.",
  "data": {
    "totalDonations": 84,
    "activeDonations": 32,
    "fullyClaimedDonations": 41,
    "expiredDonations": 7,
    "cancelledDonations": 4,
    "totalQuantityRescued": 2450,
    "recentDonations": []
  },
  "errors": []
}
```

---

### Sprint 3: Request Workflow Service (`Port 5002`)

#### 1. Submit a Food Claim Request
- **Endpoint**: `POST /api/requests`
- **Access**: Authorized (`ORGANIZATION`)

**Sample Request Body:**
```json
{
  "donationId": 105,
  "requestedQuantity": 15,
  "notes": "Urgent request for tonight's shelter distribution program."
}
```

**Sample Response (`201 Created`):**
```json
{
  "success": true,
  "message": "Donation request submitted successfully.",
  "data": {
    "id": 301,
    "donationId": 105,
    "donationTitle": "Vegetable Curry and Rice Packs",
    "organizationId": "c9d8e7f6-5432-10fe-dcba-9876543210ab",
    "organizationName": "Hope Community Shelter",
    "donorId": "e4f3a2b1-1234-5678-9abc-def012345678",
    "requestedQuantity": 15,
    "acceptedQuantity": null,
    "unit": "portions",
    "status": "PENDING",
    "notes": "Urgent request for tonight's shelter distribution program.",
    "createdAt": "2026-10-14T00:35:00Z"
  },
  "errors": []
}
```

#### 2. Donor Accepts Food Request
- **Endpoint**: `POST /api/requests/301/accept`
- **Access**: Authorized (`DONOR`)

**Sample Response (`200 OK`):**
```json
{
  "success": true,
  "message": "Donation request accepted successfully.",
  "data": {
    "id": 301,
    "donationId": 105,
    "donationTitle": "Vegetable Curry and Rice Packs",
    "organizationName": "Hope Community Shelter",
    "requestedQuantity": 15,
    "acceptedQuantity": 15,
    "status": "ACCEPTED",
    "updatedAt": "2026-10-14T00:36:00Z"
  },
  "errors": []
}
```

#### 3. Organization Posts a Food Need Request
- **Endpoint**: `POST /api/need-requests`
- **Access**: Authorized (`ORGANIZATION`)

**Sample Request Body:**
```json
{
  "title": "Need 50 Packaged Loaves of Bread",
  "category": "Bakery",
  "quantityNeeded": 50,
  "unit": "loaves",
  "description": "Required for daily breakfast relief across 3 local shelter centers.",
  "location": "North District Distribution Point",
  "neededByDate": "2026-10-16T18:00:00Z"
}
```

**Sample Response (`201 Created`):**
```json
{
  "success": true,
  "message": "Food need request created successfully.",
  "data": {
    "id": 401,
    "organizationId": "c9d8e7f6-5432-10fe-dcba-9876543210ab",
    "organizationName": "Hope Community Shelter",
    "title": "Need 50 Packaged Loaves of Bread",
    "category": "Bakery",
    "quantityNeeded": 50,
    "fulfilledQuantity": 0,
    "remainingNeeded": 50,
    "unit": "loaves",
    "status": "OPEN",
    "neededByDate": "2026-10-16T18:00:00Z",
    "createdAt": "2026-10-14T00:37:00Z"
  },
  "errors": []
}
```

#### 4. Donor Submits an Offer for an Org Need
- **Endpoint**: `POST /api/need-requests/401/offers`
- **Access**: Authorized (`DONOR`)

**Sample Request Body:**
```json
{
  "foodType": "Artisan Whole Wheat Loaves",
  "offeredQuantity": 50,
  "notes": "Freshly baked today, packaged and ready for collection."
}
```

**Sample Response (`201 Created`):**
```json
{
  "success": true,
  "message": "Food offer submitted successfully. Pending Organization review.",
  "data": {
    "id": 501,
    "orgFoodNeedRequestId": 401,
    "donorId": "e4f3a2b1-1234-5678-9abc-def012345678",
    "donorName": "Fresh Crust Bakery",
    "foodType": "Artisan Whole Wheat Loaves",
    "offeredQuantity": 50,
    "acceptedQuantity": null,
    "unit": "loaves",
    "status": "PENDING",
    "createdAt": "2026-10-14T00:38:00Z"
  },
  "errors": []
}
```

---

### Sprint 4: Delivery Workflow Service (`Port 5003`)

#### 1. Arrange Food Collection & Logistics
- **Endpoint**: `POST /api/deliveries/arrange`
- **Access**: Authorized (`DONOR` or `ORGANIZATION`)

**Sample Request Body:**
```json
{
  "requestId": 301,
  "pickupAddress": "Downtown Bistro, 456 Central Ave",
  "deliveryAddress": "Hope Community Shelter, 789 Elm St",
  "contactName": "Sarah Driver",
  "contactPhone": "+1555333444",
  "scheduledCollectionTime": "2026-10-14T14:00:00Z",
  "notes": "Driver will arrive with thermal insulated food transport bins."
}
```

**Sample Response (`201 Created`):**
```json
{
  "success": true,
  "message": "Collection arrangement created successfully.",
  "data": {
    "id": 601,
    "requestId": 301,
    "donationId": 105,
    "donationTitle": "Vegetable Curry and Rice Packs",
    "donorName": "Downtown Bistro",
    "organizationName": "Hope Community Shelter",
    "pickupAddress": "Downtown Bistro, 456 Central Ave",
    "deliveryAddress": "Hope Community Shelter, 789 Elm St",
    "contactName": "Sarah Driver",
    "contactPhone": "+1555333444",
    "scheduledCollectionTime": "2026-10-14T14:00:00Z",
    "status": "CollectionArranged",
    "arrangedAt": "2026-10-14T00:40:00Z",
    "statusHistory": [
      {
        "previousStatus": "NONE",
        "newStatus": "CollectionArranged",
        "changedByRole": "DONOR",
        "timestamp": "2026-10-14T00:40:00Z"
      }
    ]
  },
  "errors": []
}
```

#### 2. Record Food Pickup / Collection
- **Endpoint**: `POST /api/deliveries/601/collect`
- **Access**: Authorized (`DONOR`)

**Sample Request Body:**
```json
{
  "notes": "Food handoff completed. Temperature logged at 65°C."
}
```

**Sample Response (`200 OK`):**
```json
{
  "success": true,
  "message": "Donation collection recorded successfully.",
  "data": {
    "id": 601,
    "status": "Collected",
    "collectedAt": "2026-10-14T14:05:00Z"
  },
  "errors": []
}
```

#### 3. Organization Confirms Receipt
- **Endpoint**: `POST /api/deliveries/601/confirm-receipt`
- **Access**: Authorized (`ORGANIZATION`)

**Sample Request Body:**
```json
{
  "notes": "Items arrived intact. All 15 portions accounted for."
}
```

**Sample Response (`200 OK`):**
```json
{
  "success": true,
  "message": "Donation receipt confirmed successfully.",
  "data": {
    "id": 601,
    "status": "Received",
    "receivedAt": "2026-10-14T14:45:00Z"
  },
  "errors": []
}
```

#### 4. Complete Donation Lifecycle
- **Endpoint**: `POST /api/deliveries/601/complete`
- **Access**: Authorized (`DONOR` or `ORGANIZATION`)

**Sample Response (`200 OK`):**
```json
{
  "success": true,
  "message": "Donation lifecycle successfully marked as completed.",
  "data": {
    "id": 601,
    "status": "Completed",
    "completedAt": "2026-10-14T15:00:00Z"
  },
  "errors": []
}
```

#### 5. Real-Time Tracking & Full Timestamped History
- **Endpoint**: `GET /api/deliveries/601`
- **Access**: Authorized (`DONOR`, `ORGANIZATION`, `ADMIN`)

**Sample Response (`200 OK`):**
```json
{
  "success": true,
  "message": "Delivery tracking details retrieved.",
  "data": {
    "id": 601,
    "requestId": 301,
    "donationId": 105,
    "donationTitle": "Vegetable Curry and Rice Packs",
    "donorName": "Downtown Bistro",
    "organizationName": "Hope Community Shelter",
    "pickupAddress": "Downtown Bistro, 456 Central Ave",
    "deliveryAddress": "Hope Community Shelter, 789 Elm St",
    "status": "Completed",
    "arrangedAt": "2026-10-14T00:40:00Z",
    "collectedAt": "2026-10-14T14:05:00Z",
    "receivedAt": "2026-10-14T14:45:00Z",
    "completedAt": "2026-10-14T15:00:00Z",
    "statusHistory": [
      {
        "previousStatus": "NONE",
        "newStatus": "CollectionArranged",
        "changedByRole": "DONOR",
        "notes": "Arrangement created",
        "timestamp": "2026-10-14T00:40:00Z"
      },
      {
        "previousStatus": "CollectionArranged",
        "newStatus": "Collected",
        "changedByRole": "DONOR",
        "notes": "Food handoff completed",
        "timestamp": "2026-10-14T14:05:00Z"
      },
      {
        "previousStatus": "Collected",
        "newStatus": "Received",
        "changedByRole": "ORGANIZATION",
        "notes": "Items arrived intact",
        "timestamp": "2026-10-14T14:45:00Z"
      },
      {
        "previousStatus": "Received",
        "newStatus": "Completed",
        "changedByRole": "DONOR",
        "notes": "Lifecycle completed",
        "timestamp": "2026-10-14T15:00:00Z"
      }
    ]
  },
  "errors": []
}
```

---

## 4.4 Evaluator Verification Checklist

To verify the complete platform lifecycle in Swagger UI:

- [x] **1. Explore Public Endpoints**: Open `http://localhost:5000/swagger` and verify all endpoints are documented with XML descriptions.
- [x] **2. Authenticate as Donor**: Execute `POST /api/auth/login` with `donor@rescueplate.com` / `Donor@123`.
- [x] **3. Authorize in Swagger UI**: Click the **Authorize 🔓** button and paste `Bearer <token>`.
- [x] **4. Post Donation**: Open `http://localhost:5001/swagger`, authorize, and call `POST /api/donations`.
- [x] **5. Claim Food as Organization**: Authenticate as `org@rescueplate.com`, open `http://localhost:5002/swagger`, and call `POST /api/requests`.
- [x] **6. Accept Request as Donor**: Call `POST /api/requests/{id}/accept`.
- [x] **7. Complete Delivery Lifecycle**: Open `http://localhost:5003/swagger` and progress through `arrange` ➔ `collect` ➔ `confirm-receipt` ➔ `complete`.
- [x] **8. Verify Audit History**: Call `GET /api/deliveries/{id}` to view the complete timestamped audit trail.
