# ?? Quick Start: Testing API Security

> "Robust authentication and authorization are the foundation of secure APIs—test thoroughly to ensure your .NET 8 endpoints are protected against real-world threats."

## ?? Table of Contents

### **Quick Navigation**
1. [Step 1: Run the Application](#step-1-run-the-application)
2. [Step 2: Open Swagger UI](#step-2-open-swagger-ui)
3. [Step 3: Understanding JWT Authentication](#step-3-understanding-jwt-authentication)
4. [Step 4: Test Secure Login](#step-4-test-secure-login)
5. [Step 5: Authorize in Swagger](#step-5-authorize-in-swagger)
6. [Step 6: Test Protected Endpoints](#step-6-test-protected-endpoints)
7. [Step 7: Test Secure Logout with Token Blacklisting](#step-7-test-secure-logout-with-token-blacklisting)
8. [Step 8: Test Rate Limiting](#step-8-test-rate-limiting)
9. [What Each Security Feature Does](#-what-each-security-feature-does)
10. [Troubleshooting](#-troubleshooting)
11. [New Implementation Files](#-new-implementation-files)
12. [Next Steps](#-next-steps)
13. [Interview Talking Points](#-interview-talking-points)
14. [Contact](#contact)

---

## Step 1: Run the Application
```bash
dotnet run
```

The API will be available at: `https://localhost:7178`

---

## Step 2: Open Swagger UI
Navigate to: `https://localhost:7178/swagger`

---

## ?? Quick Configuration Reference

**JWT Settings** (`appsettings.json`):
```json
"JwtSettings": {
  "SecretKey": "YourSuperSecretKeyForJWT_MustBeAtLeast32CharactersLong!",
  "Issuer": "CleanArchitecture.ApiTemplate",
  "Audience": "CleanArchitecture.ApiTemplate.Api",
  "ExpirationMinutes": 60
}
```

**Rate Limiting** (`appsettings.json`):
```json
"IpRateLimiting": {
  "GeneralRules": [
    { "Endpoint": "*", "Period": "1m", "Limit": 60 }
  ]
}
```

**Endpoints Summary**:
- `POST /api/v1/auth/login` - CQRS login (recommended)
- `GET /api/v1/auth/token` - Quick token (testing)
- `POST /api/v1/auth/logout` - CQRS logout
- `GET /api/v1/token-blacklist/status` - Check token status (admin)
- `GET /api/v1/token-blacklist/stats` - System statistics (admin)

For complete configuration details, see [API-SECURITY-IMPLEMENTATION-GUIDE.md](API-SECURITY-IMPLEMENTATION-GUIDE.md).

---

## Step 3: Understanding JWT Authentication

### ?? Overview
The application implements **secure JWT authentication** for API access. JWT (JSON Web Token) bearer tokens contain user identity and role information, are stateless, cryptographically signed, and can be validated without database lookups.

**Key Features:**
- ? **Stateless Authentication**: No server-side session storage
- ? **Role-Based Access**: User and Admin roles with different permissions
- ? **Token Expiration**: Configurable lifetime (default: 30 minutes)
- ? **Cryptographic Signing**: HS256 algorithm with secret key
- ? **JWT Claims**: Contains user ID, username, roles, JTI (for blacklisting), and expiration

### JWT Token Structure

A JWT token has 3 parts separated by dots:

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI3Mzk4Zi4uLiJ9.dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
¦----------- HEADER -----------¦---------- PAYLOAD (Claims) ---------¦------- SIGNATURE ------¦
```

**Header:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload (Claims):**
```json
{
  "sub": "7398f4a4-4e1c-4a63-8ab9-f0f1f06bc4f0",
  "unique_name": "john.doe",
  "role": "User",
  "jti": "abc12345",
  "nbf": 1705341600,
  "exp": 1705343400,
  "iat": 1705341600,
  "iss": "CleanArchitecture.ApiTemplate",
  "aud": "CleanArchitecture.ApiTemplateUsers"
}
```

**Signature:**
```
HMACSHA256(
  base64UrlEncode(header) + "." + base64UrlEncode(payload),
  secret_key
)
```

### How Token Validation Works

```
Client sends request with token
  ?
UseAuthentication() middleware intercepts
  ?
JWT Bearer Handler validates:
  +- Signature (cryptographic verification)
  +- Issuer (iss claim)
  +- Audience (aud claim)
  +- Expiration (exp > now)
  +- Not Before (nbf <= now)
  ?
If valid:
  +- Extracts claims
  +- Creates ClaimsPrincipal
  +- Continues to blacklist validation
  ?
If invalid:
  +- Returns 401 Unauthorized
```

**?? Development Notice:**
This implementation is for **development/demonstration**. In production, use:
- Azure Active Directory (Microsoft Entra ID)
- IdentityServer / Duende IdentityServer
- Auth0, Okta, or other OAuth 2.0 providers

---

## Step 4: Test Secure Login

### Available Login Endpoints

The application provides **two methods** for obtaining JWT tokens using **CQRS pattern with MediatR**:

| Endpoint | Method | Purpose | Use Case | CQRS Implementation |
|----------|--------|---------|----------|---------------------|
| `/api/v1/auth/login` | POST | Full login with JSON body | Production-like login | ? Uses `LoginUserCommand` |
| `/api/v1/auth/token` | GET | Quick token generation | Testing & development | Direct generation |

### CQRS Login Architecture

The **POST /api/v1/auth/login** endpoint uses CQRS pattern for authentication:

```
POST /api/v1/auth/login
  ?
AuthController receives LoginRequest
  ?
Creates LoginUserCommand(username, password, role, clientIP, userAgent)
  ?
MediatR ? LoginUserCommandHandler called. 
  ?
Handler validates credentials
  ?
JwtTokenGenerator creates token with claims
  ?
Handler extracts token metadata (JTI, expiration)
  ?
Returns LoginResponse with token + metadata
  ?
Controller formats HTTP response
```

**CQRS Benefits:**
- ? Clean separation of concerns
- ? Testable business logic
- ? Consistent error handling via Result<T>
- ? Comprehensive audit logging
- ? Easy to extend with behaviors

### Method 1: Using Swagger UI

#### **Quick Token (GET /api/v1/auth/token)**
Best for quick testing:

1. Find `GET /api/v1/auth/token` in Swagger
2. Click "Try it out"
3. Select **type**: `user` or `admin`
4. Click "Execute"
5. Copy the token from response

#### **Full Login (POST /api/v1/auth/login)**
More realistic login flow:

1. Find `POST /api/v1/auth/login` in Swagger
2. Click "Try it out"
3. Enter credentials:
   ```json
   {
     "username": "john.doe",
     "password": "any_value",
     "role": "User"
   }
   ```
4. Click "Execute"
5. Copy the token

**Response includes:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "tokenType": "Bearer",
  "expiresIn": 1800,
  "username": "john.doe",
  "roles": ["User"],
  "tokenId": "abc12345",
  "issuedAt": "2024-01-15T10:00:00Z",
  "expiresAt": "2024-01-15T10:30:00Z",
  "processingMethod": "CQRS_Command_Pattern",
  "message": "Use in Authorization header: 'Bearer {token}'"
}
```

**CQRS Metadata:**
- `tokenId`: JWT ID (JTI) for tracking and blacklisting
- `issuedAt`: When token was created
- `expiresAt`: When token expires
- `processingMethod`: Confirms CQRS pattern usage

### Method 2: Using cURL

```bash
# Quick token (GET) - Fastest method
TOKEN=$(curl -s -X GET "https://localhost:7178/api/v1/auth/token?type=user" | jq -r '.token')

# Full login (POST) - Realistic flow
TOKEN=$(curl -s -X POST "https://localhost:7178/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"john.doe","password":"demo","role":"User"}' \
  | jq -r '.token')

echo "Token: ${TOKEN:0:50}..."
```

### Role Differences

**User Token:**
```bash
curl -X GET "https://localhost:7178/api/v1/auth/token?type=user"
```
- Role: `["User"]`
- Access: Regular protected endpoints
- Cannot access: Admin-only endpoints

**Admin Token:**
```bash
curl -X GET "https://localhost:7178/api/v1/auth/token?type=admin"
```
- Roles: `["User", "Admin"]`
- Access: All endpoints (user + admin)
- Full system access

### Login Test Script

Save as `test-login.sh`:

```bash
#!/bin/bash
echo "=== JWT Login Testing ==="

# Test 1: User login
echo -e "\n1. User Login..."
USER_TOKEN=$(curl -s -X GET "https://localhost:7178/api/v1/auth/token?type=user" | jq -r '.token')
echo "User Token: ${USER_TOKEN:0:50}..."

# Test 2: Test user access
echo -e "\n2. Test Protected Endpoint (User)..."
curl -s -X GET "https://localhost:7178/api/v1/sample" \
  -H "Authorization: Bearer $USER_TOKEN" | jq

# Test 3: Test admin endpoint (should fail)
echo -e "\n3. Test Admin Endpoint (User - Should Fail)..."
curl -s -w "\n%{http_code}" -X GET "https://localhost:7178/api/v1/sample/admin" \
  -H "Authorization: Bearer $USER_TOKEN" | tail -n 1

# Test 4: Admin login
echo -e "\n4. Admin Login..."
ADMIN_TOKEN=$(curl -s -X GET "https://localhost:7178/api/v1/auth/token?type=admin" | jq -r '.token')
echo "Admin Token: ${ADMIN_TOKEN:0:50}..."

# Test 5: Test admin access
echo -e "\n5. Test Admin Endpoint (Admin - Should Succeed)..."
curl -s -X GET "https://localhost:7178/api/v1/sample/admin" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

echo -e "\n=== Complete ==="
```

---

## Step 5: Authorize in Swagger

1. Click **"Authorize"** button (?? icon) at top of Swagger
2. Enter: `Bearer {your-token}`
3. Click **"Authorize"**
4. Click **"Close"**

? You'll see ?? on protected endpoints

---

## Step 6: Test Protected Endpoints

### Endpoint Security Levels

| Endpoint | Auth Required | Role Required | Expected Behavior |
|----------|---------------|---------------|-------------------|
| `GET /api/v1/sample/status` | ? No | None | Always returns 200 OK |
| `GET /api/v1/sample` | ? Yes | User | 401 without token, 200 with token |
| `GET /api/v1/sample/admin` | ? Yes | Admin | 403 with User token, 200 with Admin token |

### Quick Test Commands

```bash
# Public endpoint (no auth needed)
curl -X GET "https://localhost:7178/api/v1/sample/status"

# Protected endpoint (needs token)
TOKEN=$(curl -s -X GET "https://localhost:7178/api/v1/auth/token?type=user" | jq -r '.token')
curl -X GET "https://localhost:7178/api/v1/sample" \
  -H "Authorization: Bearer $TOKEN"

# Admin endpoint (needs admin role)
ADMIN_TOKEN=$(curl -s -X GET "https://localhost:7178/api/v1/auth/token?type=admin" | jq -r '.token')
curl -X GET "https://localhost:7178/api/v1/sample/admin" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

---

## Step 7: Test Secure Logout with Token Blacklisting

### ?? Overview
The application supports **secure logout** using **JWT token blacklisting** with **CQRS pattern**. When users log out, their token is blacklisted and cannot be reused.

**Key Features:**
- ? **CQRS Integration**: Uses `BlacklistTokenCommand` for logout
- ? **Dual Cache Strategy**: Memory + Distributed cache
- ? **Automatic Expiration**: Tokens auto-removed based on TTL
- ? **Admin Monitoring**: Token status and statistics endpoints
- ? **Health Checks**: Integrated monitoring

### Complete Logout Flow

```
POST /api/v1/auth/logout
  ?
AuthController extracts token ? Creates BlacklistTokenCommand
  ?
MediatR ? BlacklistTokenCommandHandler
  ?
Validates token ? Extracts JTI + expiration
  ?
ITokenBlacklistService ? Stores in dual cache
  ?
Returns BlacklistTokenResponse
  ?
Subsequent requests: JwtBlacklistValidationMiddleware
  ?
IsTokenBlacklistedQuery ? Checks cache
  ?
If blacklisted: 401 Unauthorized
If valid: Continue to endpoint
```

### Test Logout - Quick Method

```bash
# 1. Get token and test it works
TOKEN=$(curl -s -X GET "https://localhost:7178/api/v1/auth/token?type=user" | jq -r '.token')
curl -X GET "https://localhost:7178/api/v1/sample" -H "Authorization: Bearer $TOKEN"

# 2. Logout (blacklist token)
curl -X POST "https://localhost:7178/api/v1/auth/logout" \
  -H "Authorization: Bearer $TOKEN"

# 3. Try using same token (should fail with 401)
curl -X GET "https://localhost:7178/api/v1/sample" -H "Authorization: Bearer $TOKEN"
```

### Test Logout - Complete Script

Save as `test-logout.sh`:

```bash
#!/bin/bash
echo "=== JWT Logout Testing ==="

# 1. Get and test token
echo -e "\n1. Getting token..."
TOKEN=$(curl -s -X GET "https://localhost:7178/api/v1/auth/token?type=user" | jq -r '.token')
echo "Token: ${TOKEN:0:50}..."

echo -e "\n2. Testing token works..."
curl -s -X GET "https://localhost:7178/api/v1/sample" \
  -H "Authorization: Bearer $TOKEN" | jq

# 3. Logout
echo -e "\n3. Logging out..."
curl -s -X POST "https://localhost:7178/api/v1/auth/logout" \
  -H "Authorization: Bearer $TOKEN" | jq

# 4. Verify blacklisted
echo -e "\n4. Verifying token is blacklisted..."
RESPONSE=$(curl -s -w "\n%{http_code}" -X GET "https://localhost:7178/api/v1/sample" \
  -H "Authorization: Bearer $TOKEN")
echo "HTTP Status: $(echo "$RESPONSE" | tail -n 1) (Expected: 401)"

# 5. Admin verification
echo -e "\n5. Admin check (optional)..."
ADMIN_TOKEN=$(curl -s -X GET "https://localhost:7178/api/v1/auth/token?type=admin" | jq -r '.token')
curl -s -X GET "https://localhost:7178/api/v1/token-blacklist/status?token=$TOKEN" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

echo -e "\n=== Complete ==="
```

### Admin Management Endpoints

```bash
# Get admin token
ADMIN_TOKEN=$(curl -s -X GET "https://localhost:7178/api/v1/auth/token?type=admin" | jq -r '.token')

# Check specific token status
curl -X GET "https://localhost:7178/api/v1/token-blacklist/status?token=$USER_TOKEN" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Get system statistics
curl -X GET "https://localhost:7178/api/v1/token-blacklist/stats" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Health check (no auth required)
curl -X GET "https://localhost:7178/api/v1/token-blacklist/health"
```

### Performance Optimization

- **Memory Cache**: <1ms lookups (fastest layer)
- **Distributed Cache**: Shared across instances (consistency)
- **Query Caching**: 1-2 min cache via CQRS
- **Auto-Expiration**: Tokens removed when naturally expired

---

## Step 8: Test Rate Limiting

```bash
# Send 70 requests to trigger rate limit
for i in {1..70}; do
  echo "Request $i"
  curl -X GET "https://localhost:7178/api/v1/sample/status"
  sleep 0.5
done
```

**Expected:** After ~60 requests/minute: `429 Too Many Requests`

---

## ?? What Each Security Feature Does

| Feature | Purpose | Test Result |
|---------|---------|-------------|
| **JWT Authentication** | Verifies user identity | 401 without token ? |
| **JWT Logout/Blacklist** | Secure token invalidation | 401 after logout ? |
| **Authorization** | Controls what users can do | 403 for non-admin ? |
| **Rate Limiting** | Prevents API abuse | 429 after limit ? |
| **CORS** | Controls who can call API | Browser enforced ? |
| **Security Headers** | Prevents web attacks | Check headers ? |
| **CQRS Pattern** | Clean architecture | Testable & maintainable ? |

---

## ?? Troubleshooting

### Issue: "401 Unauthorized" even with token
**Solution**: 
- Check token hasn't expired (default: 30 min)
- Verify format: `Bearer {token}` (note the space)
- Check if token was logged out (blacklisted)
- Generate new token

### Issue: "403 Forbidden" on admin endpoint
**Solution**: 
- Use admin token: `?type=admin`
- Verify token contains Admin role

### Issue: Logout Not Working
**Solution**:
- Verify token in Authorization header
- Check `ITokenBlacklistService` is registered
- Verify `JwtBlacklistValidationMiddleware` in pipeline
- Check logs for errors
- Ensure cache services configured

### Issue: Rate limiting too strict
**Solution**: 
- Edit `appsettings.Development.json`
- Increase `IpRateLimiting.GeneralRules.Limit`
- Restart application

---

## ?? Implementation Files Reference

For detailed architecture and implementation details, see [API-SECURITY-IMPLEMENTATION-GUIDE.md](API-SECURITY-IMPLEMENTATION-GUIDE.md).

### **Quick Reference: Key Components**

#### **Application Layer (CQRS)**
```
Core/Application/Features/Authentication/
+-- Commands/
¦   +-- LoginUserCommand.cs + Handler
¦   +-- BlacklistTokenCommand.cs + Handler
+-- Queries/
    +-- IsTokenBlacklistedQuery.cs + Handler
    +-- GetTokenBlacklistStatsQuery.cs + Handler
```

#### **Infrastructure Layer**
```
Infrastructure/
+-- Security/
¦   +-- JwtTokenGenerator.cs
+-- Services/
¦   +-- TokenBlacklistService.cs
+-- Middleware/
    +-- JwtBlacklistValidationMiddleware.cs
```

#### **Presentation Layer**
```
Presentation/Controllers/v1/
+-- AuthController.cs (Login + Logout)
+-- TokenBlacklistController.cs (Admin endpoints)
```

### **Configuration Files**
- `appsettings.json` - JWT settings, CORS, Rate limiting
- `Program.cs` - Middleware pipeline configuration
- `ApplicationServiceExtensions.cs` - Handler registrations

---

## ?? Key Concepts for Testing

### **Authentication vs Authorization**
When testing, remember:
- **Authentication** (401): "Who are you?" - Token validation
- **Authorization** (403): "What can you do?" - Role/policy check

### **Middleware Order**
The request flows through middleware in this order:
1. Authentication (validates token)
2. Blacklist Check (CQRS query with caching)
3. Authorization (checks roles/policies)

### **JWT Claims Used**
- `sub`: User ID (GUID)
- `unique_name`: Username
- `role`: User roles (User, Admin)
- `jti`: JWT ID (for blacklisting)
- `iat`: Issued at timestamp
- `exp`: Expiration timestamp

### **Cache Strategy**
- **Query Cache**: 1-2 min for blacklist checks
- **Statistics Cache**: 5-10 min for admin stats
- **Memory Cache**: Sub-millisecond lookups
- **Distributed Cache**: Multi-instance consistency

---

## ?? Additional Security Features

For complete details, see [API-SECURITY-IMPLEMENTATION-GUIDE.md](API-SECURITY-IMPLEMENTATION-GUIDE.md#additional-security-features).

### **Quick Overview**

**Rate Limiting**
- 60 requests per minute per IP
- 1000 requests per hour per IP
- Returns 429 when limit exceeded

**CORS**
- Whitelist-based origin validation
- Configured in `appsettings.json`
- Development: localhost allowed

**Security Headers**
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Configured in `Program.cs`

**External API Security**
- ApiKeyHandler for third-party APIs
- Retry policy with exponential backoff
- Circuit breaker pattern

---

## ?? Next Steps

1. ? Test all endpoints with Swagger
2. ? Test login/logout functionality
3. ? Test admin management endpoints
4. ? Verify rate limiting
5. ? Review `JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md` for complete architecture
6. ? Review `API-SECURITY-IMPLEMENTATION-GUIDE.md` for implementation details
7. ? Configure external API credentials (if needed)
8. ? Plan production deployment with Azure Key Vault

---

## ?? Interview Talking Points

1. **JWT Authentication**: "Implemented stateless JWT authentication with HS256 signing and role-based access control"

2. **Secure Logout with Blacklisting**: "Added JWT blacklisting using dual-cache strategy (memory + distributed) for performance and multi-instance consistency"

3. **CQRS Integration**: "Integrated blacklist with CQRS pattern via MediatR, separating commands (logout) from queries (validation) for maintainability"

4. **Performance Optimization**: "Dual-cache with sub-millisecond memory lookups and distributed cache for scaled instances"

5. **Clean Architecture**: "Security concerns properly layered: Application defines CQRS, Infrastructure implements services, Presentation handles HTTP"

6. **Production Ready**: "Would use Azure AD, move secrets to Key Vault, and implement Redis for distributed caching in production"

---

## ?? Contact

**Need Help?**

- ?? **Documentation:** Start with deployment guides
- ?? **Issues:** [GitHub Issues](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues)
- ?? **Email:** softevolutionsl@gmail.com
- ?? **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)

---

**Last Updated:** January 2025  
**Maintainer:** Dariemcarlos  
**GitHub:** [CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate)  
**Version:** 1.2.0 - Unified and Simplified

---

**Happy Testing! ??**
