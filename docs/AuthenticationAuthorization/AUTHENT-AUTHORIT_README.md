# Authentication & Authorization Documentation - SecureCleanApiWaf

> **"Security is not a feature—it's the foundation of trust in every API call."**

## ?? Overview

Welcome to the **Authentication & Authorization** documentation hub for SecureCleanApiWaf. This guide serves as your starting point to understand how this project implements industry-standard security patterns with JWT authentication, CQRS integration, and token blacklisting.

**?? What You'll Find Here:**
- Complete CQRS-based JWT authentication implementation
- Token blacklisting with dual-cache strategy
- Step-by-step testing guides
- Security architecture and implementation details
- API security best practices

---

## ?? Table of Contents

### **Quick Navigation**
1. [What is JWT Authentication with CQRS?](#-what-is-jwt-authentication-with-cqrs)
2. [Why This Implementation Matters](#-why-this-implementation-matters)
3. [System Status](#-system-status)
4. [Architecture Overview](#-architecture-overview)
5. [Documentation Structure](#-documentation-structure)
6. [Getting Started](#-getting-started)
7. [Quick Reference](#-quick-reference)
8. [Security Features](#-security-features)
9. [CQRS Components](#-cqrs-components)
10. [Testing](#-testing)
11. [Related Documentation](#-related-documentation)
12. [Contact & Support](#-contact--support)

---

## ?? What is JWT Authentication with CQRS?

**JWT (JSON Web Token) Authentication** provides:
- ? **Stateless authentication** - No server-side session storage
- ? **Token-based security** - Cryptographically signed tokens
- ? **Role-based access control** - User and Admin roles
- ? **Self-contained claims** - User identity and permissions in token

**CQRS (Command Query Responsibility Segregation)** integration provides:
- ? **Clean separation** - Commands (login/logout) vs Queries (validation)
- ? **Testable handlers** - Unit test each command/query independently
- ? **Automatic caching** - Query results cached via MediatR pipeline
- ? **Consistent patterns** - Result<T> for all operations

**Token Blacklisting** adds:
- ? **Secure logout** - Invalidate tokens before natural expiration
- ? **Dual-cache strategy** - Memory + Distributed cache for performance
- ? **Admin monitoring** - Statistics and health check endpoints

**This project demonstrates all three working together in production-ready architecture.**

---

## ?? Why This Implementation Matters

### **For SecureClean Developers**

This implementation demonstrates:

| Challenge | Solution in This Project |
|-----------|--------------------------|
| **"How do I implement JWT authentication?"** | Complete CQRS-based JWT with login/logout/validation |
| **"How do I handle logout with stateless tokens?"** | Token blacklisting with dual-cache strategy |
| **"How do I integrate auth with CQRS?"** | Commands for login/logout, Queries for validation |
| **"How do I test authentication?"** | Complete testing guide with Swagger and cURL examples |
| **"How do I secure APIs in production?"** | Rate limiting, CORS, security headers, token blacklisting |

### **Real-World Application**

- ?? **Production-Ready** - JWT + blacklisting + CQRS integration
- ??? **Clean Architecture** - Proper layer separation (Domain, Application, Infrastructure)
- ?? **Testable** - Mock-friendly with MediatR and interface abstractions
- ?? **Maintainable** - Clear patterns make onboarding fast
- ?? **Extensible** - Easy to add OAuth, 2FA, or other auth providers

---

## ?? System Status

### **Current Implementation: Complete CQRS Authentication**

```
Authentication System (100% Complete)
+-- Commands/                 [? Login, Logout]
+-- Queries/                  [? Token validation, Statistics]
+-- Handlers/                 [? 4 handlers with caching]
+-- Services/                 [? JWT generation, Token blacklisting]
+-- Middleware/               [? Custom validation pipeline]
+-- Controllers/              [? Auth & Admin endpoints]
```

### **Implementation Maturity**

| Component | Status | Key Features |
|-----------|--------|--------------|
| **JWT Generation** | ? 100% | HS256 signing, configurable expiration, role claims |
| **CQRS Commands** | ? 100% | LoginUserCommand, BlacklistTokenCommand with handlers |
| **CQRS Queries** | ? 100% | IsTokenBlacklistedQuery, GetTokenBlacklistStatsQuery |
| **Token Blacklisting** | ? 100% | Dual-cache (Memory + Distributed), automatic expiration |
| **Middleware** | ? 100% | JwtBlacklistValidationMiddleware in HTTP pipeline |
| **Admin Endpoints** | ? 100% | Token status, statistics, health checks |
| **Testing** | ? 100% | Complete guide with Swagger and cURL examples |

---

## ?? Architecture Overview

### **Authentication Flow**

```
+-----------------------------------------------------+
¦  1. Client ? POST /api/v1/auth/login                ¦
¦     Sends: { username, password, role }             ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  2. AuthController ? LoginUserCommand                ¦
¦     Creates CQRS command with credentials           ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  3. MediatR ? LoginUserCommandHandler                ¦
¦     Validates credentials, generates JWT token       ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  4. JwtTokenGenerator ? Creates Token                ¦
¦     Signs token with claims (sub, role, jti, exp)   ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  5. Returns JWT Token + Metadata                     ¦
¦     { token, tokenType, expiresIn, roles, ... }     ¦
+-----------------------------------------------------+
```

### **Logout & Blacklisting Flow**

```
+-----------------------------------------------------+
¦  1. Client ? POST /api/v1/auth/logout               ¦
¦     Sends: Authorization: Bearer {token}            ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  2. AuthController ? BlacklistTokenCommand           ¦
¦     Extracts token, creates CQRS command            ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  3. MediatR ? BlacklistTokenCommandHandler           ¦
¦     Validates token, extracts JTI and expiration    ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  4. TokenBlacklistService ? Dual Cache               ¦
¦     Stores in Memory Cache + Distributed Cache      ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  5. Returns Success + Client Actions                 ¦
¦     { status: "blacklisted", recommendations }      ¦
+-----------------------------------------------------+
```

### **Token Validation Flow**

```
+-----------------------------------------------------+
¦  1. Client ? GET /api/v1/sample (Protected)         ¦
¦     Sends: Authorization: Bearer {token}            ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  2. JwtBlacklistValidationMiddleware                 ¦
¦     Intercepts request before authorization          ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  3. MediatR ? IsTokenBlacklistedQuery (Cached)       ¦
¦     Checks cache first (1-2 min cache)              ¦
+-----------------------------------------------------+
                     ¦
                     ?
+-----------------------------------------------------+
¦  4. IsTokenBlacklistedQueryHandler                   ¦
¦     Queries TokenBlacklistService                   ¦
+-----------------------------------------------------+
                     ¦
        +-------------------------+
        ¦                         ¦
        ?                         ?
+--------------+         +--------------+
¦ If Blacklisted¦         ¦  If Valid    ¦
¦ Return 401    ¦         ¦  Continue    ¦
+--------------+         +--------------+
```

---

## ?? Documentation Structure

### **?? Main Guides**

#### **1. [TEST_AUTHENTICATION_GUIDE.md](TEST_AUTHENTICATION_GUIDE.md)** - ?? START HERE for Testing
**Your hands-on guide to testing all authentication features.**

**What's Inside:**
- ? Step-by-step testing workflow (8 steps)
- ? JWT authentication testing procedures
- ? Swagger UI testing with screenshots
- ? cURL command examples for all endpoints
- ? Login/logout testing workflows
- ? Token blacklisting verification
- ? Role-based access control testing
- ? Rate limiting verification
- ? Complete troubleshooting guide
- ? Interview talking points

**When to Read:** Start here if you want to test the authentication system.

---

#### **2. [JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md](JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md)** - Architecture Deep Dive
**Complete CQRS integration and architectural decisions.**

**What's Inside:**
- ? CQRS authentication architecture
- ? Visual integration diagrams
- ? Complete request/response flows
- ? MediatR pipeline integration
- ? Automatic caching with CachingBehavior
- ? Performance optimization strategies
- ? Why CQRS for authentication?

**When to Read:** After testing, read this to understand the architecture.

---

#### **3. [API-SECURITY-IMPLEMENTATION-GUIDE.md](API-SECURITY-IMPLEMENTATION-GUIDE.md)** - Complete Security Guide
**Comprehensive security implementation details.**

**What's Inside:**
- ? JWT configuration and setup
- ? Token blacklisting implementation
- ? Custom middleware pipeline
- ? Rate limiting configuration
- ? CORS setup
- ? Security headers
- ? External API security (ApiKeyHandler)
- ? Polly resilience patterns
- ? Production deployment checklist

**When to Read:** For complete security implementation details and production guidance.

---

### **?? Implementation Guides**

#### **[CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md](CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md)** - Login Details
**Detailed login CQRS implementation reference.**

**What's Inside:**
- ? LoginUserCommand structure
- ? LoginUserCommandHandler implementation
- ? JWT token generation process
- ? Response metadata and audit logging
- ? Testing examples

**When to Use:** Reference for implementing similar CQRS commands.

---

#### **[CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md](CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md)** - Logout Details
**Detailed logout and blacklisting implementation reference.**

**What's Inside:**
- ? BlacklistTokenCommand structure
- ? IsTokenBlacklistedQuery with caching
- ? GetTokenBlacklistStatsQuery implementation
- ? Dual-cache strategy details
- ? Admin monitoring endpoints
- ? Middleware integration

**When to Use:** Reference for implementing token management features.

---

## ?? Getting Started

### **For New Developers (Start Here!)**

**Day 1: Test the System**
1. ?? Read [TEST_AUTHENTICATION_GUIDE.md](TEST_AUTHENTICATION_GUIDE.md)
2. ?? Run the application: `dotnet run`
3. ?? Open Swagger: `https://localhost:7178/swagger`
4. ? Follow Steps 1-8 to test all authentication features

**Day 2: Understand Architecture**
1. ?? Read [JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md](JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md)
2. ?? Review CQRS command and query patterns
3. ?? Understand why CQRS for authentication
4. ?? See how layers integrate

**Day 3: Security Deep Dive**
1. ??? Read [API-SECURITY-IMPLEMENTATION-GUIDE.md](API-SECURITY-IMPLEMENTATION-GUIDE.md)
2. ?? Review JWT configuration
3. ?? Understand token blacklisting
4. ?? Study security middleware pipeline

---

### **For Security Engineers**

**Quick Assessment Path:**
1. ? [API-SECURITY-IMPLEMENTATION-GUIDE.md](API-SECURITY-IMPLEMENTATION-GUIDE.md) - Security overview
2. ? [JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md](JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md) - Architecture review
3. ? [TEST_AUTHENTICATION_GUIDE.md](TEST_AUTHENTICATION_GUIDE.md) - Verify implementation

**Focus Areas:**
- JWT signing and validation (HS256)
- Token blacklisting strategy (dual-cache)
- Rate limiting configuration
- CORS and security headers
- Production security checklist

---

### **For Team Leads**

**Evaluation Checklist:**
1. ?? [JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md](JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md) - Architecture maturity
2. ?? [TEST_AUTHENTICATION_GUIDE.md](TEST_AUTHENTICATION_GUIDE.md) - Testing coverage
3. ??? [API-SECURITY-IMPLEMENTATION-GUIDE.md](API-SECURITY-IMPLEMENTATION-GUIDE.md) - Security posture
4. ?? Implementation files - Code quality review

**Team Onboarding:**
- Use TEST_AUTHENTICATION_GUIDE for hands-on training
- Assign CQRS implementation guides based on features
- Review security checklist for production readiness

---

## ? Quick Reference

### **CQRS Components Summary**

| Component Type | Count | Files |
|----------------|-------|-------|
| **Commands** | 2 | LoginUserCommand, BlacklistTokenCommand |
| **Command Handlers** | 2 | LoginUserCommandHandler, BlacklistTokenCommandHandler |
| **Queries** | 2 | IsTokenBlacklistedQuery, GetTokenBlacklistStatsQuery |
| **Query Handlers** | 2 | IsTokenBlacklistedQueryHandler, GetTokenBlacklistStatsQueryHandler |
| **Services** | 2 | JwtTokenGenerator, TokenBlacklistService |
| **Middleware** | 1 | JwtBlacklistValidationMiddleware |
| **Controllers** | 2 | AuthController, TokenBlacklistController |

**Total:** 13 components implementing complete authentication system

---

### **API Endpoints**

| Endpoint | Method | Auth | Role | CQRS Type |
|----------|--------|------|------|-----------|
| `/api/v1/auth/login` | POST | ? No | None | Command |
| `/api/v1/auth/token` | GET | ? No | None | Direct |
| `/api/v1/auth/logout` | POST | ? Yes | User | Command |
| `/api/v1/token-blacklist/status` | GET | ? Yes | User | Query |
| `/api/v1/token-blacklist/stats` | GET | ? Yes | Admin | Query |
| `/api/v1/token-blacklist/health` | GET | ? No | None | Direct |

---

### **Folder Structure**

```
Core/Application/Features/Authentication/
+-- Commands/
¦   +-- LoginUserCommand.cs                    [CQRS Command]
¦   +-- LoginUserCommandHandler.cs             [Handler + Validation]
¦   +-- BlacklistTokenCommand.cs               [CQRS Command]
¦   +-- BlacklistTokenCommandHandler.cs        [Handler + Cache]
+-- Queries/
    +-- IsTokenBlacklistedQuery.cs             [CQRS Query + ICacheable]
    +-- IsTokenBlacklistedQueryHandler.cs      [Handler + Cache]
    +-- GetTokenBlacklistStatsQuery.cs         [CQRS Query + ICacheable]
    +-- GetTokenBlacklistStatsQueryHandler.cs  [Handler + Cache]

Infrastructure/
+-- Security/
¦   +-- JwtTokenGenerator.cs                   [JWT Creation]
+-- Services/
¦   +-- TokenBlacklistService.cs               [Dual-Cache Service]
+-- Middleware/
    +-- JwtBlacklistValidationMiddleware.cs    [HTTP Pipeline]

Presentation/Controllers/v1/
+-- AuthController.cs                          [Login + Logout]
+-- TokenBlacklistController.cs                [Admin Endpoints]
```

---

## ?? Security Features

### **1. JWT Bearer Authentication**
- ? HS256 signing algorithm
- ? Configurable expiration (default: 30 min)
- ? Role-based claims (User, Admin)
- ? Issuer and audience validation
- ? JTI (JWT ID) for tracking

### **2. Token Blacklisting**
- ? Dual-cache strategy (Memory + Distributed)
- ? Automatic expiration cleanup
- ? <1ms memory cache lookups
- ? Multi-instance consistency with distributed cache

### **3. CQRS Integration**
- ? Commands for write operations (Login, Logout)
- ? Queries for read operations (Validation, Statistics)
- ? Automatic caching via MediatR pipeline
- ? Consistent error handling with Result<T>

### **4. Rate Limiting**
- ? 60 requests per minute per IP
- ? 1000 requests per hour per IP
- ? Configurable in appsettings.json

### **5. CORS & Headers**
- ? Whitelist-based origin validation
- ? Security headers (XSS, Clickjacking, MIME-sniffing protection)
- ? HTTPS enforcement in production
