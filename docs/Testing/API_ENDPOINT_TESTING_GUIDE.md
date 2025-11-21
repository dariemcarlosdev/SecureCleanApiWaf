# API Testing Guide - SecureCleanApiWaf

> "Comprehensive testing is the cornerstone of reliable APIs—master local development, containerized deployment, and testing tools like Swagger and Postman to ensure your .NET 8 endpoints are production-ready."

## Overview

This guide provides complete instructions for testing all SecureCleanApiWaf API endpoints using multiple approaches. Whether you're running the application locally, in a Docker container, or testing with Swagger UI or Postman, this guide covers everything you need to know.

---

## Table of Contents

1. [Getting Started](#getting-started)
   - [Prerequisites](#prerequisites)
   - [Quick Start Checklist](#quick-start-checklist)
2. [Running the Application](#running-the-application)
   - [Option 1: Run Locally (Development)](#option-1-run-locally-development)
   - [Option 2: Run with Docker](#option-2-run-with-docker)
   - [Verifying Application is Running](#verifying-application-is-running)
3. [Testing with Swagger UI](#testing-with-swagger-ui)
   - [Accessing Swagger UI](#accessing-swagger-ui)
   - [Testing Public Endpoints](#testing-public-endpoints)
   - [Testing Protected Endpoints](#testing-protected-endpoints)
   - [Testing Admin Endpoints](#testing-admin-endpoints)
   - [Testing Logout and Token Blacklisting](#testing-logout-and-token-blacklisting)
4. [Testing with Postman](#testing-with-postman)
   - [Setting Up Postman](#setting-up-postman)
   - [Creating a Collection](#creating-a-collection)
   - [Testing Endpoints](#testing-endpoints)
   - [Environment Variables](#environment-variables)
   - [Automated Testing Scripts](#automated-testing-scripts)
5. [Complete Endpoint Reference](#complete-endpoint-reference)
   - [Authentication Endpoints](#authentication-endpoints)
   - [Sample Data Endpoints](#sample-data-endpoints)
   - [Token Blacklist Management](#token-blacklist-management)
6. [Advanced Testing Scenarios](#advanced-testing-scenarios)
   - [Rate Limiting](#rate-limiting)
   - [CORS Testing](#cors-testing)
   - [Error Handling](#error-handling)
   - [Performance Testing](#performance-testing)
7. [Troubleshooting](#troubleshooting)
8. [Best Practices](#best-practices)
9. [Reference Files](#reference-files)
10. [Contact](#contact)

---

## Getting Started

### **Prerequisites**

**For Local Development:**
- ? **.NET 8 SDK** installed ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- ? **Visual Studio 2022** (optional, recommended) or **VS Code**
- ? **Git** (for cloning repository)

**For Docker:**
- ? **Docker Desktop** installed ([Download](https://www.docker.com/products/docker-desktop))
- ? Docker engine running (check with `docker --version`)

**For Testing:**
- ? **Modern web browser** (Chrome, Edge, Firefox)
- ? **Postman** installed ([Download](https://www.postman.com/downloads/)) (optional)
- ? **cURL** (optional, included in Windows 10+, macOS, Linux)

### **Quick Start Checklist**

Before testing, ensure:
- [ ] Application source code cloned/downloaded
- [ ] .NET 8 SDK installed (for local run) OR Docker installed (for container run)
- [ ] Configuration files present (`appsettings.json`, `appsettings.Development.json`)
- [ ] Port 8080 (Docker) or 7178 (local HTTPS) is available
- [ ] Internet connection (for external API integration tests)

---

## Running the Application

### **Option 1: Run Locally (Development)**

**Advantages:**
- ? Fast iteration (immediate code changes)
- ? Full debugging support with breakpoints
- ? Detailed logging and error messages
- ? Hot reload (automatic restart on file changes)

**Steps:**

1. **Navigate to Project Directory**
   ```bash
   cd "C:\DATA\MYSTUFFS\PROFESSIONAL STUFF\TECH CHALLENGE\SecureCleanApiWaf"
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the Application**
   
   **Standard Run:**
   ```bash
   dotnet run
   ```
   
   **With Hot Reload (recommended for development):**
   ```bash
   dotnet watch run
   ```
   
   **From Visual Studio:**
   - Open `SecureCleanApiWaf.sln`
   - Press `F5` (Debug) or `Ctrl+F5` (Run without debugging)

4. **Application URLs**
   
   The application will start on:
   - **HTTPS:** `https://localhost:7178` (primary)
   - **HTTP:** `http://localhost:5000` (redirects to HTTPS)
   
   You'll see output like:
   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: https://localhost:7178
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://localhost:5000
   info: Microsoft.Hosting.Lifetime[0]
         Application started. Press Ctrl+C to shut down.
   ```

5. **Verify Application is Running**
   ```bash
   curl https://localhost:7178/api/v1/sample/status
   ```
   
   Expected response:
   ```json
   {
     "status": "SampleController is running.",
     "timestamp": "2024-01-15T10:30:00Z"
   }
   ```

**Stopping the Application:**
- Press `Ctrl+C` in terminal
- Or stop debugging in Visual Studio

---

### **Option 2: Run with Docker**

**Advantages:**
- ? Consistent environment (same as production)
- ? No .NET SDK required
- ? Easy to share with team
- ? Isolates dependencies
- ? Simplified deployment testing

**Prerequisites:**
- Docker Desktop running
- Application pre-published (see step 1)

**Steps:**

1. **Publish the Application**
   
   Docker uses pre-built binaries. First, publish the app:
   ```bash
   cd "C:\DATA\MYSTUFFS\PROFESSIONAL STUFF\TECH CHALLENGE\SecureCleanApiWaf"
   dotnet restore
   dotnet build -c Release
   dotnet publish -c Release -o ./publish
   ```
   
   This creates compiled files in `./publish` folder.

2. **Build Docker Image**
   ```bash
   docker build -t SecureCleanApiWaf:latest .
   ```
   
   Expected output:
   ```
   [+] Building 15.2s (8/8) FINISHED
    => [internal] load build definition from Dockerfile
    => => transferring dockerfile: 1.23kB
    => [internal] load .dockerignore
    => [internal] load metadata for mcr.microsoft.com/dotnet/aspnet:8.0
    => CACHED [1/3] FROM mcr.microsoft.com/dotnet/aspnet:8.0
    => [internal] load build context
    => [2/3] WORKDIR /app
    => [3/3] COPY ./publish .
    => exporting to image
    => => naming to docker.io/library/SecureCleanApiWaf:latest
   ```

3. **Start Container with Docker Compose (Recommended)**
   ```bash
   docker-compose up -d
   ```
   
   Expected output:
   ```
   Creating network "SecureCleanApiWaf_default" with the default driver
   Creating SecureCleanApiWaf ... done
   ```

   **OR Start Container Manually:**
   ```bash
   docker run -d \
     --name SecureCleanApiWaf \
     -p 8080:8080 \
     -e ASPNETCORE_ENVIRONMENT=Development \
     -e ThirdPartyApi__BaseUrl=https://jsonplaceholder.typicode.com/ \
     --restart unless-stopped \
     SecureCleanApiWaf:latest
   ```

4. **Application URLs**
   
   The application will be available at:
   - **HTTP:** `http://localhost:8080`
   
   **Note:** Docker uses HTTP (port 8080), not HTTPS. In production, configure HTTPS at the reverse proxy level (e.g., nginx, Azure App Service).

5. **Verify Container is Running**
   
   **Check Container Status:**
   ```bash
   docker ps
   ```
   
   Expected output:
   ```
   CONTAINER ID   IMAGE                  COMMAND                  STATUS         PORTS                    NAMES
   abc123def456   SecureCleanApiWaf:latest   "dotnet SecureCleanApiWaf…"   Up 2 minutes   0.0.0.0:8080->8080/tcp   SecureCleanApiWaf
   ```
   
   **Test Health Endpoint:**
   ```bash
   curl http://localhost:8080/api/v1/sample/status
   ```
   
   Expected response:
   ```json
   {
     "status": "SampleController is running.",
     "timestamp": "2024-01-15T10:30:00Z"
   }
   ```

6. **View Container Logs**
   
   **Docker Compose:**
   ```bash
   docker-compose logs -f SecureCleanApiWaf
   ```
   
   **Docker:**
   ```bash
   docker logs -f SecureCleanApiWaf
   ```
   
   Press `Ctrl+C` to stop viewing logs (container keeps running).

**Stopping the Application:**

**Docker Compose:**
```bash
docker-compose stop     # Stop (can restart later)
docker-compose down     # Stop and remove containers
```

**Docker:**
```bash
docker stop SecureCleanApiWaf      # Stop container
docker rm SecureCleanApiWaf        # Remove container
```

**Rebuilding After Code Changes:**
```bash
# Publish changes
dotnet publish -c Release -o ./publish

# Rebuild and restart
docker-compose up -d --build
```

---

### **Verifying Application is Running**

**Quick Health Check:**

**Local (HTTPS):**
```bash
curl https://localhost:7178/api/v1/sample/status
```

**Docker (HTTP):**
```bash
curl http://localhost:8080/api/v1/sample/status
```

**Expected Response:**
```json
{
  "status": "SampleController is running.",
  "timestamp": "2024-01-15T10:30:00.1234567Z"
}
```

**Browser Test:**
- Open browser
- Navigate to `https://localhost:7178/api/v1/sample/status` (local) or `http://localhost:8080/api/v1/sample/status` (Docker)
- You should see the JSON response

---

## Testing with Swagger UI

Swagger UI provides an interactive API documentation and testing interface.

### **Accessing Swagger UI**

**Local Development:**
```
https://localhost:7178/swagger
```

**Docker:**
```
http://localhost:8080/swagger
```

**What You'll See:**
- ?? **API Groups**: Controllers organized by feature (Auth, Sample, Token Blacklist)
- ?? **Endpoint List**: All available API endpoints
- ?? **Authorization Button**: Authenticate with JWT token
- ?? **Documentation**: Endpoint descriptions, parameters, responses

### **Testing Public Endpoints**

Public endpoints don't require authentication. Perfect for initial testing.

**Example: Health Check**

1. **Find Endpoint**: Scroll to `GET /api/v1/sample/status`
2. **Click "Try it out"**: Enables testing mode
3. **Click "Execute"**: Sends request to API
4. **View Response**:
   ```json
   {
     "status": "SampleController is running.",
     "timestamp": "2024-01-15T10:30:00Z"
   }
   ```
5. **Response Details**:
   - **Code:** `200` (success)
   - **Headers:** `content-type: application/json`
   - **Body:** JSON response

**Other Public Endpoints:**
- `GET /api/v1/auth/token?type=user` - Quick user token generation
- `GET /api/v1/auth/token?type=admin` - Quick admin token generation
- `GET /api/v1/token-blacklist/health` - Blacklist system health

### **Testing Protected Endpoints**

Protected endpoints require JWT authentication.

**Step 1: Get JWT Token**

**Option A: Quick Token (Recommended for Testing)**

1. Find `GET /api/v1/auth/token`
2. Click **"Try it out"**
3. Set **type** parameter: `user` or `admin`
4. Click **"Execute"**
5. **Copy token** from response:
   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
     "tokenType": "Bearer",
     "type": "user",
     "roles": ["User"],
     "usage": "Add to headers: Authorization: Bearer {token}"
   }
   ```

**Option B: Full Login (Production-like)**

1. Find `POST /api/v1/auth/login`
2. Click **"Try it out"**
3. Enter request body:
   ```json
   {
     "username": "john.doe",
     "password": "any_password",
     "role": "User"
   }
   ```
4. Click **"Execute"**
5. **Copy token** from response:
   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
     "tokenType": "Bearer",
     "expiresIn": 1800,
     "username": "john.doe",
     "roles": ["User"],
     "tokenId": "abc12345",
     "processingMethod": "CQRS_Command_Pattern",
     "message": "Use in Authorization header: 'Bearer {token}'"
   }
   ```

**Step 2: Authorize in Swagger**

1. Click **"Authorize"** button (?? icon at top right)
2. **Enter token**: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
   - ?? **Include "Bearer " prefix** (note the space!)
3. Click **"Authorize"**
4. Click **"Close"**

? You'll see ?? icon on all protected endpoints (indicates authorized)

**Step 3: Test Protected Endpoint**

1. Find `GET /api/v1/sample`
2. Click **"Try it out"**
3. Click **"Execute"**
4. **Expected Response** (200 OK):
   ```json
   {
     "id": 1,
     "name": "Sample Data",
     "description": "External API data"
   }
   ```

**Without Token:**
- Response Code: `401 Unauthorized`
- Response Body:
  ```json
  {
    "error": "Unauthorized",
    "message": "No JWT token provided or invalid token"
  }
  ```

### **Testing Admin Endpoints**

Admin endpoints require both authentication AND the "Admin" role.

**Step 1: Get Admin Token**

1. Find `GET /api/v1/auth/token`
2. Set **type** parameter: `admin`
3. Click **"Execute"**
4. Copy admin token (includes both User and Admin roles)

**Step 2: Authorize with Admin Token**

1. Click **"Authorize"** button
2. Enter: `Bearer {admin-token}`
3. Click **"Authorize"** then **"Close"**

**Step 3: Test Admin Endpoint**

1. Find `GET /api/v1/sample/admin`
2. Click **"Try it out"**
3. Click **"Execute"**
4. **Expected Response** (200 OK):
   ```json
   {
     "message": "This is admin-only data",
     "user": "admin"
   }
   ```

**With User Token (Non-Admin):**
- Response Code: `403 Forbidden`
- Response Body:
  ```json
  {
    "error": "Forbidden",
    "message": "User does not have Admin role"
  }
  ```

### **Testing Logout and Token Blacklisting**

**Step 1: Get and Test Token**

1. Get user token: `GET /api/v1/auth/token?type=user`
2. Authorize in Swagger with token
3. Test protected endpoint: `GET /api/v1/sample` ? ? 200 OK

**Step 2: Logout (Blacklist Token)**

1. Find `POST /api/v1/auth/logout`
2. Click **"Try it out"**
3. Click **"Execute"**
4. **Expected Response** (200 OK):
   ```json
   {
     "message": "Logout successful via CQRS pattern",
     "status": "blacklisted",
     "details": {
       "token_id": "abc12345",
       "username": "testuser",
       "blacklisted_at": "2024-01-15T10:35:00Z",
       "expires_at": "2024-01-15T11:05:00Z",
       "processing_method": "CQRS_Command_Pattern",
       "client_actions": [
         "Remove token from storage",
         "Redirect to login page",
         "Clear user session"
       ]
     }
   }
   ```

**Step 3: Verify Token is Blacklisted**

1. Try using same token: `GET /api/v1/sample`
2. **Expected Response** (401 Unauthorized):
   ```json
   {
     "error": "Token has been revoked",
     "message": "Please log in again"
   }
   ```

**Step 4: Admin Verification (Optional)**

1. Get admin token
2. Find `GET /api/v1/token-blacklist/status`
3. Set **token** parameter to blacklisted token
4. Click **"Execute"**
5. **Response:**
   ```json
   {
     "is_blacklisted": true,
     "token_id": "abc12345",
     "status": "blacklisted",
     "details": "Token was blacklisted via logout",
     "blacklisted_at": "2024-01-15T10:35:00Z",
     "token_expires_at": "2024-01-15T11:05:00Z",
     "checked_at": "2024-01-15T10:36:00Z",
     "from_cache": true,
     "processing_method": "CQRS_Query_Pattern"
   }
   ```

---

## Testing with Postman

Postman provides advanced API testing capabilities with collections, environments, and automated scripts.

### **Setting Up Postman**

1. **Download and Install**
   - Visit: https://www.postman.com/downloads/
   - Install Postman for your OS

2. **Create Workspace**
   - Open Postman
   - Click **"Workspaces"** ? **"Create Workspace"**
   - Name: `SecureCleanApiWaf Testing`
   - Visibility: `Personal`

3. **Import SSL Certificate (Local HTTPS Only)**
   
   **For Local Development (https://localhost:7178):**
   - Settings ? Certificates ? Add Certificate
   - Host: `localhost:7178`
   - Enable: "Disable SSL certificate verification" (for development only)
   
   **For Docker (http://localhost:8080):**
   - No certificate needed (HTTP)

### **Creating a Collection**

1. **Create New Collection**
   - Click **"Collections"** ? **"+"** (New Collection)
   - Name: `SecureCleanApiWaf API`
   - Description: `Complete API testing suite for SecureCleanApiWaf`

2. **Collection-Level Authorization**
   - Select collection ? **"Authorization"** tab
   - Type: `Bearer Token`
   - Token: `{{jwt_token}}` (we'll set this variable later)

3. **Collection Variables**
   - Click **"Variables"** tab
   - Add variables:
     ```
     base_url_local    https://localhost:7178
     base_url_docker   http://localhost:8080
     jwt_token         (leave empty, will be set dynamically)
     admin_token       (leave empty, will be set dynamically)
     ```

### **Testing Endpoints**

#### **1. Authentication Endpoints**

**Request: Quick User Token (GET)**

```
Method: GET
URL: {{base_url_local}}/api/v1/auth/token
Params:
  - type: user
```

**Tests Tab (JavaScript):**
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains token", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.token).to.be.a('string');
    pm.environment.set("jwt_token", jsonData.token);
});
```

**Request: Full Login (POST)**

```
Method: POST
URL: {{base_url_local}}/api/v1/auth/login
Headers:
  - Content-Type: application/json
Body (raw JSON):
{
  "username": "john.doe",
  "password": "demo123",
  "role": "User"
}
```

**Tests Tab:**
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Token is returned", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.token).to.be.a('string');
    pm.expect(jsonData.username).to.eql("john.doe");
    pm.expect(jsonData.roles).to.include("User");
    
    // Save token for subsequent requests
    pm.environment.set("jwt_token", jsonData.token);
});

pm.test("CQRS pattern used", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.processingMethod).to.eql("CQRS_Command_Pattern");
});
```

**Request: Admin Token (GET)**

```
Method: GET
URL: {{base_url_local}}/api/v1/auth/token
Params:
  - type: admin
```

**Tests Tab:**
```javascript
pm.test("Admin token contains Admin role", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.roles).to.include("Admin");
    pm.environment.set("admin_token", jsonData.token);
});
```

#### **2. Protected Endpoints**

**Request: Get All Data (User)**

```
Method: GET
URL: {{base_url_local}}/api/v1/sample
Headers:
  - Authorization: Bearer {{jwt_token}}
```

**Pre-request Script:**
```javascript
// Ensure we have a valid token
if (!pm.environment.get("jwt_token")) {
    throw new Error("JWT token not set. Run login request first.");
}
```

**Tests Tab:**
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is JSON", function () {
    pm.response.to.be.json;
});

pm.test("Response contains data", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.be.an('object');
});
```

**Request: Get Data by ID**

```
Method: GET
URL: {{base_url_local}}/api/v1/sample/123
Headers:
  - Authorization: Bearer {{jwt_token}}
```

**Tests Tab:**
```javascript
pm.test("Status code is 200 or 404", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 404]);
});
```

#### **3. Admin Endpoints**

**Request: Admin Data**

```
Method: GET
URL: {{base_url_local}}/api/v1/sample/admin
Headers:
  - Authorization: Bearer {{admin_token}}
```

**Tests Tab:**
```javascript
pm.test("Status code is 200 with admin token", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains admin data", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.message).to.include("admin-only");
});

// Test with user token (should fail)
pm.sendRequest({
    url: pm.environment.get("base_url_local") + "/api/v1/sample/admin",
    method: 'GET',
    header: {
        'Authorization': 'Bearer ' + pm.environment.get("jwt_token")
    }
}, function (err, response) {
    pm.test("User token gets 403 Forbidden", function () {
        pm.expect(response.code).to.eql(403);
    });
});
```

#### **4. Logout and Blacklisting**

**Request: Logout**

```
Method: POST
URL: {{base_url_local}}/api/v1/auth/logout
Headers:
  - Authorization: Bearer {{jwt_token}}
```

**Tests Tab:**
```javascript
pm.test("Logout successful", function () {
    pm.response.to.have.status(200);
});

pm.test("Token blacklisted", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.status).to.eql("blacklisted");
});

// Test token is now invalid
setTimeout(function() {
    pm.sendRequest({
        url: pm.environment.get("base_url_local") + "/api/v1/sample",
        method: 'GET',
        header: {
            'Authorization': 'Bearer ' + pm.environment.get("jwt_token")
        }
    }, function (err, response) {
        pm.test("Blacklisted token returns 401", function () {
            pm.expect(response.code).to.eql(401);
        });
    });
}, 500);
```

**Request: Check Token Status (Admin)**

```
Method: GET
URL: {{base_url_local}}/api/v1/token-blacklist/status
Headers:
  - Authorization: Bearer {{admin_token}}
Params:
  - token: {{jwt_token}}
  - bypassCache: true
```

**Tests Tab:**
```javascript
pm.test("Token status retrieved", function () {
    pm.response.to.have.status(200);
});

pm.test("Shows blacklisted status", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.is_blacklisted).to.be.true;
});
```

### **Environment Variables**

**Create Environment:**
1. Click **"Environments"** ? **"+"** (New Environment)
2. Name: `Local Development`
3. Add variables:
   ```
   base_url          https://localhost:7178
   jwt_token         (dynamic)
   admin_token       (dynamic)
   user_id           123
   test_username     john.doe
   ```

**Create Docker Environment:**
1. Name: `Docker`
2. Add variables:
   ```
   base_url          http://localhost:8080
   jwt_token         (dynamic)
   admin_token       (dynamic)
   ```

**Switch Environments:**
- Click environment dropdown (top right)
- Select `Local Development` or `Docker`

### **Automated Testing Scripts**

**Collection Pre-request Script:**
```javascript
// Automatically refresh token if expired (optional advanced feature)
const tokenExpiry = pm.environment.get("token_expiry");
if (tokenExpiry && Date.now() > tokenExpiry) {
    pm.sendRequest({
        url: pm.environment.get("base_url") + "/api/v1/auth/token?type=user",
        method: 'GET'
    }, function (err, response) {
        if (!err && response.code === 200) {
            const data = response.json();
            pm.environment.set("jwt_token", data.token);
            pm.environment.set("token_expiry", Date.now() + 1800000); // 30 min
        }
    });
}
```

**Collection Tests (Global Assertions):**
```javascript
// Check all responses have correct content type
pm.test("Content-Type is application/json", function () {
    pm.response.to.have.header("Content-Type", /application\/json/);
});

// Check response time is acceptable
pm.test("Response time is less than 2000ms", function () {
    pm.expect(pm.response.responseTime).to.be.below(2000);
});
```

**Run Collection:**
1. Click collection ? **"Run"**
2. Select requests to run
3. Click **"Run SecureCleanApiWaf API"**
4. View results

---

## Complete Endpoint Reference

### **Quick Reference Table - All Endpoints**

| # | Endpoint | Method | Auth | Role | Description | Status Codes |
|---|----------|--------|------|------|-------------|--------------|
| 1 | `/api/v1/auth/token` | GET | ? | None | Quick token generation | 200 |
| 2 | `/api/v1/auth/login` | POST | ? | None | Full login (CQRS) | 200, 400, 500 |
| 3 | `/api/v1/auth/logout` | POST | ? | User | Logout & blacklist token | 200, 400, 401, 500 |
| 4 | `/api/v1/sample/status` | GET | ? | None | Health check | 200 |
| 5 | `/api/v1/sample` | GET | ? | User | Get all data | 200, 400, 401, 500 |
| 6 | `/api/v1/sample/{id}` | GET | ? | User | Get data by ID | 200, 400, 401, 404, 500 |
| 7 | `/api/v1/sample/admin` | GET | ? | Admin | Admin-only data | 200, 401, 403 |
| 8 | `/api/v1/token-blacklist/status` | GET | ? | User | Check token status | 200, 400, 401 |
| 9 | `/api/v1/token-blacklist/stats` | GET | ? | Admin | System statistics | 200, 401, 403, 500 |
| 10 | `/api/v1/token-blacklist/health` | GET | ? | None | Blacklist health | 200, 503 |

**Legend:**
- ? **Auth Required** | ? **Public (No Auth)**
- **Roles:** None (public), User, Admin

---

### **Authentication Endpoints**

| Endpoint | Method | Auth | Description | Request Body | Response |
|----------|--------|------|-------------|--------------|----------|
| `/api/v1/auth/token` | GET | ? No | Quick token generation | Query: `type=user\|admin` | `{ token, tokenType, roles }` |
| `/api/v1/auth/login` | POST | ? No | Full login (CQRS) | `{ username, password, role }` | `{ token, tokenType, expiresIn, ... }` |
| `/api/v1/auth/logout` | POST | ? Yes | Logout and blacklist token (CQRS) | None | `{ message, status, details }` |

#### **Endpoint-Specific Troubleshooting**

**1. `GET /api/v1/auth/token`**

Common Issues:
- **Issue:** Token not generated
  - **Cause:** Application not running or incorrect URL
  - **Solution:** Verify app is running: `curl https://localhost:7178/api/v1/sample/status`

- **Issue:** Invalid type parameter
  - **Cause:** Typo in `type` parameter (must be "user" or "admin")
  - **Solution:** Use exact values: `?type=user` or `?type=admin` (case-insensitive)

- **Issue:** Token too long for some tools
  - **Cause:** JWT tokens can be 500+ characters
  - **Solution:** Copy entire token string, ensure no truncation

**Quick Test:**
```bash
# User token
curl "https://localhost:7178/api/v1/auth/token?type=user"

# Admin token
curl "https://localhost:7178/api/v1/auth/token?type=admin"
```

---

**2. `POST /api/v1/auth/login`**

Common Issues:
- **Issue:** 400 Bad Request - Username required
  - **Cause:** Empty or missing username field
  - **Solution:** Provide any non-empty username (demo accepts all)
  ```json
  { "username": "testuser", "password": "any", "role": "User" }
  ```

- **Issue:** 500 Internal Server Error
  - **Cause:** Malformed JSON body
  - **Solution:** Validate JSON syntax, ensure Content-Type header is set
  ```bash
  curl -X POST https://localhost:7178/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"john","password":"test","role":"User"}'
  ```

- **Issue:** Token not saved in Postman
  - **Cause:** Missing test script to extract token
  - **Solution:** Add to Tests tab:
  ```javascript
  pm.environment.set("jwt_token", pm.response.json().token);
  ```

**Quick Test:**
```bash
curl -X POST https://localhost:7178/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"john.doe","password":"demo123","role":"User"}' | jq
```

---

**3. `POST /api/v1/auth/logout`**

Common Issues:
- **Issue:** 400 Bad Request - Missing token
  - **Cause:** No Authorization header provided
  - **Solution:** Add Authorization header with Bearer token
  ```bash
  curl -X POST https://localhost:7178/api/v1/auth/logout \
    -H "Authorization: Bearer YOUR_TOKEN_HERE"
  ```

- **Issue:** 401 Unauthorized
  - **Cause:** Token expired or already blacklisted
  - **Solution:** Generate new token and try again

- **Issue:** Logout succeeds but can still use token
  - **Cause:** Caching delay (rare)
  - **Solution:** Wait 1-2 seconds, try protected endpoint again

- **Issue:** "Token already blacklisted" error
  - **Cause:** Attempting to logout same token twice
  - **Solution:** This is expected behavior; token is already invalid

**Quick Test:**
```bash
# Get token
TOKEN=$(curl -s "https://localhost:7178/api/v1/auth/token?type=user" | jq -r '.token')

# Logout
curl -X POST https://localhost:7178/api/v1/auth/logout \
  -H "Authorization: Bearer $TOKEN"

# Verify token is blacklisted (should fail with 401)
curl https://localhost:7178/api/v1/sample \
  -H "Authorization: Bearer $TOKEN"
