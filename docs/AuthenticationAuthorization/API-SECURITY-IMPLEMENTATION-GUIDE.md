# 🔒 SecureClean API Security Implementation

> "Effective API security is not just about authentication—it's about layered protection, resilient design, and safeguarding every endpoint against evolving threats."

## 📑 Table of Contents

### **Quick Navigation**
1. [Overview](#overview)
2. [Security Features Implemented](#-security-features-implemented)
   - [1. JWT Bearer Authentication with CQRS](#1-jwt-bearer-authentication-with-cqrs)
   - [2. Authorization Policies](#2-authorization-policies)
3. [Authentication & Authorization Workflow](#-authentication--authorization-workflow)
   - [Workflow Overview](#-workflow-overview)
   - [Token Generation Flow](#-token-generation-flow)
   - [Complete CQRS Authentication Flow](#-complete-cqrs-authentication-flow)
   - [Implementation Files](#-implementation-files)
   - [Configuration Details](#-configuration-details)
4. [Key Concepts](#-key-concepts)
5. [Security Considerations](#-security-considerations)
6. [Additional Security Features](#-additional-security-features)
7. [Production Deployment Checklist](#-production-deployment-checklist)
8. [Architecture](#-architecture)
9. [Benefits of This Implementation](#-benefits-of-this-implementation)
10. [Additional Resources](#-additional-resources)
11. [Contact & Support](#-contact--support)

---

## Overview

This document outlines the **comprehensive security architecture** implemented in the SecureClean API. It focuses on the implementation details, configuration, and architectural patterns for JWT authentication with CQRS.

**For testing instructions**, see [TEST_AUTHENTICATION_GUIDE.md](TEST_AUTHENTICATION_GUIDE.md).

---

## ✨ Security Features Implemented

### 1. **JWT Bearer Authentication with CQRS**
Protects all API endpoints with industry-standard JSON Web Token authentication using CQRS pattern for login and logout operations.

#### Configuration (`appsettings.json`)
```json
"JwtSettings": {
  "SecretKey": "YourSuperSecretKeyForJWT_MustBeAtLeast32CharactersLong!",
  "Issuer": "CleanArchitecture.ApiTemplate",
  "Audience": "CleanArchitecture.ApiTemplate.Api",
  "ExpirationMinutes": 60
}
```

#### Key Features
- ✅ Token-based stateless authentication
- ✅ **CQRS Integration**: Login uses `LoginUserCommand`, Logout uses `BlacklistTokenCommand`
- ✅ **Token Blacklisting**: Secure logout with dual-cache strategy
- ✅ Configurable expiration times
- ✅ Issuer and audience validation
- ✅ Zero clock skew tolerance
- ✅ HTTPS enforcement in production
- ✅ **Admin Monitoring**: Token status and statistics endpoints

#### CQRS Architecture
- **Login**: `LoginUserCommand` → `LoginUserCommandHandler` → JWT generation
- **Logout**: `BlacklistTokenCommand` → `BlacklistTokenCommandHandler` → Token blacklisting
- **Validation**: `IsTokenBlacklistedQuery` → Automatic caching (1-2 min)
- **Statistics**: `GetTokenBlacklistStatsQuery` → Extended caching (5-10 min)

---

### 2. **Authorization Policies**
Role-based access control (RBAC) for granular permissions.

#### Policies
- **ApiUser**: Requires authenticated user
- **AdminOnly**: Requires Admin role

#### Controller Example
```csharp
[Authorize] // Requires authentication
public class SampleController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllData() { } // Requires auth
    
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")] // Requires Admin role
    public IActionResult GetAdminData() { }
    
    [HttpGet("status")]
    [AllowAnonymous] // Public endpoint
    public IActionResult GetStatus() { }
}
```

---

## 🔄 Authentication & Authorization Workflow

This section explains the complete authentication and authorization flow implemented in the SecureClean API, including all components, middleware order, and request processing.

---

### 📖 Workflow Overview

This flow describes how an incoming API request is authenticated and authorized using JWT Bearer tokens.

**Step-by-Step Request Flow:**

```
1. CLIENT REQUEST
   What it does: Client sends HTTP request with JWT token in Authorization header
   Format: "Bearer {token}"
   ↓
   GET /api/v1/sample
   Header: Authorization: Bearer eyJhbGc...
   
    
2. HTTP PIPELINE (Middleware Chain - Program.cs)
   What it does: Request passes through middleware chain in order
   ↓
   ➜ Exception Handler (catch errors)
   ➜ HTTPS Redirection (secure traffic)
   ➜ Static Files (serve CSS/JS)
   ➜ Routing (match endpoint)
   ➜ CORS (validate origin)
   ➜ Rate Limiting (throttle requests)
   ➜ Authentication (JWT validation) → [TOKEN VALIDATED HERE]
   ➜ JwtBlacklistValidation (CQRS Query) → [BLACKLIST CHECK]
   ➜ Authorization (policy check) → [PERMISSIONS CHECKED]
   ➜ Security Headers (add protection)
   
   
3. AUTHENTICATION (UseAuthentication)
   What it does: Validates JWT token signature, expiration, issuer/audience
   ↓
   ➜ Extract JWT from Authorization header
   ➜ Validate signature (HMAC-SHA256)
   ➜ Validate issuer (CleanArchitecture.ApiTemplate)
   ➜ Validate audience (CleanArchitecture.ApiTemplate.Api)
   ➜ Check expiration (ClockSkew = 0)
   ➜ Extract claims (sub, unique_name, roles, jti)
   ➜ Set User.Identity (ClaimsPrincipal)
   ↓
   Result: User.Identity.IsAuthenticated = true/false
   

4. BLACKLIST VALIDATION (JwtBlacklistValidationMiddleware - CQRS)
   What it does: Checks if token was logged out (blacklisted)
   ↓
   ➜ Extract token from Authorization header
   ➜ Create IsTokenBlacklistedQuery (CQRS)
   ➜ MediatR.Send(query)
      +- CachingBehavior checks cache first (1-2 min)
      +- If cache miss: Query handler checks service
   ➜ If blacklisted: Return 401 Unauthorized
   ➜ If valid: Continue to next middleware
   ↓
   Result: Token is valid and not blacklisted
   
   
5. AUTHORIZATION (UseAuthorization)
   What it does: Checks if authenticated user has required permissions
   ↓
   ➜ Check [Authorize] attribute on endpoint
   ➜ Verify User.Identity.IsAuthenticated = true
   ➜ Check [Authorize(Policy = "AdminOnly")] if present
   ➜ Verify User has required role claim
   ↓
   Decision: Allow (200) or Deny (401/403)
   
   
6. CONTROLLER EXECUTION
   What it does: Executes endpoint business logic via CQRS handlers
   ↓
   ➜ Log request with User.Identity.Name
   ➜ Validate input parameters
   ➜ Send command/query via MediatR
   ➜ Process business logic (CQRS handlers)
   ➜ Return HTTP response (200, 400, 500)
   
   
7. HTTP RESPONSE
   What it does: Response travels back through middleware chain
   ↓
   ➜ Security Headers added
   ➜ Response logged
   ➜ Sent back to client
```

---

### 📖 Token Generation Flow

This flow describes how JWT tokens are generated using CQRS pattern for login.

**Production-Ready Authentication with CQRS**

**Step-by-Step Token Generation (Login):**

```
1. CLIENT REQUESTS TOKEN
   What it does: Client sends login request with credentials
   ↓
   POST /api/v1/auth/login (CQRS - Recommended)
   Body: { "username": "john", "password": "demo", "role": "User" }
   
   OR
   
   GET /api/v1/auth/token?type=user (Direct - Quick testing)
   

2. AUTH CONTROLLER (AuthController.cs - Presentation Layer)
   What it does: Receives login request, creates CQRS command
   ↓
   Endpoint: POST /api/v1/auth/login
   ➜ [AllowAnonymous] - No authentication required
   ➜ Extracts credentials and audit info (ClientIP, UserAgent)
   ➜ Creates LoginUserCommand (CQRS)
      +- username, password, role
      +- clientIpAddress (audit)
      +- userAgent (audit)
   ➜ Calls: await _mediator.Send(command)
   

3. LOGIN COMMAND HANDLER (LoginUserCommandHandler.cs - Application Layer)
   What it does: Handles authentication business logic via CQRS
   ↓
   Handler: LoginUserCommandHandler
   
   ➜ Step 1: Validate username is provided
   ➜ Step 2: Authenticate user (simplified for demo)
      ➜ Production: Verify password hash against database
   ➜ Step 3: Determine roles (User/Admin)
   ➜ Step 4: Generate JWT token
      Calls: JwtTokenGenerator.GenerateToken(userId, username, roles)
   ➜ Step 5: Extract token metadata
      +- JTI (JWT ID for blacklisting)
      +- IssuedAt timestamp
      +- ExpiresAt timestamp
   ➜ Step 6: Build LoginResponse with rich metadata
   ➜ Step 7: Log security event (IP, UserAgent, username)
   

4. JWT TOKEN GENERATOR (JwtTokenGenerator.cs - Infrastructure Layer)
   What it does: Creates signed JWT token with user claims
   ↓
   Method: GenerateToken(userId, username, roles[])
   
   ➜ Load JWT settings from appsettings.json
      - SecretKey, Issuer, Audience, ExpirationMinutes
   ➜ Create security key and signing credentials
      - SymmetricSecurityKey (HMAC-SHA256)
   ➜ Build token claims (payload)
      - sub (User ID), unique_name, role, jti, iat, exp
   ➜ Create JWT token object
      - Set issuer, audience, claims, expiration
   ➜ Sign and serialize to string
      - Format: header.payload.signature
   

5. RESPONSE TO CLIENT
   What it does: Returns rich LoginResponse with token and metadata
   ↓
   JSON Response (CQRS):
   {
     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
     "tokenType": "Bearer",
     "expiresIn": 1800,
     "username": "john",
     "roles": ["User"],
     "tokenId": "abc123",
     "issuedAt": "2024-01-15T10:00:00Z",
     "expiresAt": "2024-01-15T10:30:00Z",
     "processingMethod": "CQRS_Command_Pattern",
     "message": "Token generated successfully"
   }
   
   Client uses: Authorization: Bearer {token}


⚠️ IMPORTANT: For production, replace with Azure AD, IdentityServer, or Auth0
```

---

## 🔄 Complete CQRS Authentication Flow

### **Login Flow (CQRS Command)**
```
POST /api/v1/auth/login
  ↓
AuthController receives LoginRequest
  ↓
Creates LoginUserCommand(username, password, role, clientIP, userAgent)
  ↓
MediatR.Send(command) → LoginUserCommandHandler
  ↓
Handler:
  +- Validates username
  +- Determines roles (User/Admin)
  +- Calls JwtTokenGenerator.GenerateToken()
  +- Extracts token metadata (JTI, IssuedAt, ExpiresAt)
  +- Returns LoginResponse with rich metadata
  ↓
Controller formats HTTP response
  ↓
Returns: Token + TokenId + Timestamps + ProcessingMethod
```

### **Logout Flow (CQRS Command)**
```
POST /api/v1/auth/logout
  ↓
AuthController extracts token from Authorization header
  ↓
Creates BlacklistTokenCommand(token, reason, clientIP, userAgent)
  ↓
MediatR.Send(command) → BlacklistTokenCommandHandler
  ↓
Handler:
  +- Validates token format and extracts JTI
  +- Calls ITokenBlacklistService.BlacklistTokenAsync()
  │   +- Stores in Memory Cache
  │   +- Stores in Distributed Cache
  +- Logs security event
  +- Returns BlacklistTokenResponse with recommendations
  ↓
Controller formats HTTP response
  ↓
Returns: Status + TokenId + Client Actions + ProcessingMethod
```

### **Token Validation Flow (CQRS Query with Caching)**
```
Any protected endpoint request
  ↓
JwtBlacklistValidationMiddleware intercepts
  ↓
Extracts token from Authorization header
  ↓
Creates IsTokenBlacklistedQuery(token, bypassCache: false)
  ↓
MediatR.Send(query) → CachingBehavior
  +- Check cache first (1-2 min expiration)
  +- If HIT: Return cached result
  +- If MISS: Continue to handler
  ↓
IsTokenBlacklistedQueryHandler
  +- Validates token format
  +- Calls ITokenBlacklistService.IsTokenBlacklistedAsync()
  │   +- Check Memory Cache (sub-millisecond)
  │   +- Check Distributed Cache (fallback)
  +- Returns TokenBlacklistStatus
  ↓
CachingBehavior caches result
  ↓
Middleware processes result:
  +- If blacklisted: Returns 401 Unauthorized
  +- If valid: Continues to endpoint
```

### **Admin Statistics Flow (CQRS Query with Extended Caching)**
```
GET /api/v1/token-blacklist/stats
  ↓
TokenBlacklistController
  ↓
Creates GetTokenBlacklistStatsQuery(bypassCache: false)
  ↓
MediatR.Send(query) → CachingBehavior
  +- Check cache first (5-10 min expiration)
  +- If MISS: Continue to handler
  ↓
GetTokenBlacklistStatsQueryHandler
  +- Retrieves base stats from service
  +- Calculates performance metrics
  +- Calculates security metrics
  +- Determines health indicators
  +- Returns TokenBlacklistStatistics
  ↓
CachingBehavior caches result
  ↓
Controller formats administrative response
```

---

## 🏗️ Architecture

![SecureClean API Architecture](./media/architecture-diagram.png)

---

## ✅ Benefits of This Implementation

- **Robust Security**: Follows industry best practices for API security.
- **Scalability**: Designed to scale with your application and user base.
- **Flexibility**: Easily configurable to meet the specific needs of your organization.
- **Comprehensive Logging**: Detailed logging and monitoring for enhanced visibility.
- **Resilience**: Built-in resilience patterns to handle transient faults.
- **CQRS Integration**: Clean separation of authentication commands and queries
- **Token Blacklisting**: Secure logout with dual-cache strategy
- **Admin Monitoring**: Comprehensive token management and statistics

---

## 📚 Additional Resources

- [OWASP API Security Top 10](https://owasp.org/www-project-api-security-top-10)
- [JWT.io - JSON Web Tokens](https://jwt.io/)
- [Microsoft Identity Platform documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [Stripe API Security Guidelines](https://stripe.com/docs/security)
- [Throttling and Rate Limiting in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-6.0)
- [CQRS Pattern by Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)

---

## 🆘 Contact & Support

### **Project Information**
- **Project:** CleanArchitecture.ApiTemplate - API Security Implementation
- **Version:** 1.3.0 (Complete CQRS Authentication)
- **Framework:** .NET 8 with JWT Bearer Authentication
- **Repository:** [https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate)

### **Author**
- **Name:** Dariem Carlos
- **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)
- **Email:** softevolutionsl@gmail.com

### **Getting Help**

For security implementation questions:
1. Review [TEST_AUTHENTICATION_GUIDE.md](TEST_AUTHENTICATION_GUIDE.md) for testing procedures
2. Check [JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md](JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md) for architecture details
3. Review [CQRS Implementation Summaries](CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md) for detailed guidance
4. Open an [issue](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues) with label `security`

### **Support Channels**
- 📧 **Email:** softevolutionsl@gmail.com
- 💬 **GitHub Discussions:** [CleanArchitecture.ApiTemplate Discussions](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/discussions)
- 🐙 **GitHub Issues:** [Report Issues](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues)

### **Related Documentation**
- 📖 [AUTHENT-AUTHORIT_README.md](AUTHENT-AUTHORIT_README.md) - Main authentication hub
- 📖 [CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md](CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md) - Login details
- 📖 [CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md](CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md) - Logout details
- 📖 [JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md](JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md) - Complete architecture

---

**Last Updated:** January 2025  
**Status:** ✅ Production-Ready  
**Security Implementation:** Complete with CQRS Pattern

---

*For the latest security updates and best practices, visit the [GitHub repository](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate).*
