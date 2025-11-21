# 💡 Inspiration for SecureCleaners

> **"In technology, stagnation is not an option—only evolution matters. Every line of code you write today shapes the future you build tomorrow. Stay curious, keep learning, and never stop pushing the boundaries of what's possible. The tech world rewards those who dare to innovate."**
>
> *— For the SecureCleaners*
>
> Remember: Your skills, dedication, and commitment to excellence don't just advance your career—they inspire your team and others like me, driving collective progress in the tech community. Embrace challenges, elevate your projects, and contribute to the broader developer community. Keep growing. Keep building. Keep changing the world, one commit at a time. 🚀

---

# SecureCleanApiWaf
**This isn't just a demo—it's a template for production development.** 🎯

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)](https://blazor.net/)
[![Azure](https://img.shields.io/badge/Azure-Ready-0078D4?logo=microsoft-azure&logoColor=white)](https://azure.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Containerization-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![GitHub Actions](https://img.shields.io/badge/GitHub%20Actions-CI%2FCD-2088FF?logo=github-actions&logoColor=white)](
[![JWT](https://img.shields.io/badge/JWT-JSON%20Web%20Tokens-000000?logo=json-web-tokens&logoColor=white)](https://jwt.io/)
[![Polly](https://img.shields.io/badge/Polly-7.2-blue?logo=polly&logoColor=white)](
[![Serilog](https://img.shields.io/badge/Serilog-2.12-blue?logo=serilog&logoColor=white)](
[![AutoMapper](https://img.shields.io/badge/AutoMapper-12.0-blue?logo=automapper&logoColor=white)](
[![MediatR](https://img.shields.io/badge/MediatR-9.0-blue?logo=mediatr&logoColor=white)](
[![CQRS](https://img.shields.io/badge/Pattern-CQRS%20%2B%20MediatR-blue)](docs/CleanArchitecture/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)](docs/CleanArchitecture/CLEAN_ARCHITECTURE_GUIDE.md)
[![Domain-Driven Design](https://img.shields.io/badge/DDD-Domain--Driven%20Design-brightgreen)](docs/CleanArchitecture/CLEAN-DDD_ARCH_README.md)
[![Documentation](https://img.shields.io/badge/Docs-Comprehensive-orange)](docs/)

---

## 📑 Table of Contents

### **Quick Navigation**
1. [📊 Executive Summary](#-executive-summary) 🆕 NEW
2. [Overview](#overview)
3. [Project Intent](#project-intent)
4. [⚡ Quick Reference](#-quick-reference) 🆕 NEW
5. [Getting Started](#-getting-started)
   - [Deployment Documentation](#-deployment-documentation)
     - [Master Deployment Guide](#-master-deployment-guide)
     - [Azure App Service Deployment](#-azure-app-service-deployment)
     - [Docker Deployment](#-docker-deployment)
     - [CI/CD Pipeline Deployment](#-cicd-pipeline-deployment)
     - [Quick Deployment Comparison](#-quick-deployment-comparison)
6. [Swagger/OpenAPI Support](#-swaggeropenapi-support)
7. [Key Backend Topics Demonstrated](#-key-backend-topics-demonstrated-in-this-project)
   - [Architecture & Design Patterns](#architecture--design-patterns)
   - [Enterprise Patterns](#enterprise-patterns)
   - [Performance & Scalability](#performance--scalability)
   - [Configuration & Security](#configuration--security)
   - [API Documentation & Testing](#api-documentation--testing)
   - [Cloud & DevOps](#cloud--devops)
   - [Code Quality & Maintainability](#code-quality--maintainability)
   - [Real-World Application](#real-world-application)
   - [Support & Contact](#support--contact)
8. [Clean Architecture](#-clean-architecture)
   - [Clean Architecture Documentation](#-clean-architecture-documentation)
9. [Service Alignment & Architecture Integration](#-service-alignment--architecture-integration)
   - [Service Responsibility](#service-responsibility)
   - [Result Pattern Implementation](#result-pattern-implementation)
   - [CQRS Integration](#cqrs-integration)
   - [Caching Integration](#caching-integration)
   - [Dependency Injection Configuration](#dependency-injection-configuration)
   - [HttpClient Management Best Practices](#httpclient-management-best-practices)
   - [Architecture Diagram: Request Flow](#architecture-diagram-request-flow)
10. [Security Implementation](#-security-implementation-development--demo)
    - [Complete Security Documentation](#-complete-security-documentation)
    - [Demo Implementation Notice](#-important-demo-implementation-notice)
    - [Security Features Implemented](#-security-features-implemented)
      - [JWT Bearer Authentication with CQRS](#1-jwt-bearer-authentication-with-cqrs-) 🔄 UPDATED
      - [External API Security](#2-external-api-security-apikeyhandler-) 🔄 UPDATED
11. [Support & Contact](#-support--contact)

---

## 📊 Executive Summary

**SecureCleanApiWaf** is a production-ready .NET 8 Blazor Server application demonstrating **enterprise-grade backend architecture** with comprehensive CQRS implementation, clean architecture principles, and Azure cloud integration.

### **🎯 Core Technical Achievements**

**1. CQRS Authentication System with MediatR**
   - ✅ **4 Commands, 4 Queries** - Complete authentication workflow
   - ✅ **MediatR Pipeline Integration** - Automatic caching, logging, and validation
   - ✅ **Dual-Cache Token Blacklisting** - Memory + Distributed cache for secure logout
   - ✅ **Custom Middleware** - `JwtBlacklistValidationMiddleware` for HTTP pipeline integration
   - ✅ **Admin Monitoring** - Health checks and statistics endpoints

**2. Clean Architecture Implementation**
   - ✅ **4-Layer Structure** - Core/Domain, Application, Infrastructure, Presentation
   - ✅ **15+ Interface Abstractions** - Dependency Inversion Principle throughout
   - ✅ **Feature-Based Organization** - CQRS components grouped by feature
   - ✅ **SOLID Principles** - Demonstrating maintainable, testable code
   - 🏗️ **Extensible Architecture** - New features follow the same CQRS + Clean Architecture patterns

**3. Production-Ready Security**
   - ✅ **JWT Bearer Authentication** - Role-based authorization (User, Admin)
   - ✅ **Token Blacklisting** - Secure logout with dual-cache strategy
   - ✅ **Rate Limiting** - 60 requests/min, 1000 requests/hr per IP
   - ✅ **CORS & Security Headers** - XSS, Clickjacking, MIME-sniffing protection
   - ✅ **API Key Management** - Centralized with Azure Key Vault integration
   - ✅ **Polly Resilience Patterns** - Retry with exponential backoff + Circuit Breaker

**4. Performance & Scalability**
   - ✅ **Dual-Layer Caching** - Cache-aside pattern + MediatR pipeline caching
   - ✅ **HttpClientFactory** - Prevents socket exhaustion, enables Polly integration
   - ✅ **Async/Await Throughout** - Non-blocking I/O operations
   - ✅ **Azure-Ready** - Key Vault, App Service, GitHub Actions CI/CD

### **📈 Implementation Metrics**

| Category | Details | Demonstration Value |
|----------|---------|---------------------|
| **Architecture Patterns** | CQRS, MediatR, Clean Architecture, Repository Pattern | ✅ Enterprise-grade design |
| **CQRS Components** | 8 (4 Commands + 4 Queries) | ✅ Command/Query separation |
| **Security Features** | 6 layers (Auth, Authz, Rate Limiting, CORS, Headers, Keys) | ✅ Defense in depth |
| **API Endpoints** | 15+ RESTful endpoints with versioning | ✅ Production API design |
| **Documentation** | 20+ markdown guides (3,000+ lines) | ✅ Professional documentation |
| **Code Quality** | XML comments, inline documentation, SOLID principles | ✅ Maintainable codebase |

### **🛠️ Technology Stack**

- **Framework:** .NET 8, C# 12, Blazor Server
- **Patterns:** CQRS, MediatR, Clean Architecture, Result Pattern, Repository Pattern
- **Security:** JWT Bearer, Token Blacklisting, Rate Limiting, CORS, Security Headers
- **Caching:** Memory Cache, Distributed Cache, MediatR Pipeline Caching
- **Resilience:** Polly (Retry with Exponential Backoff, Circuit Breaker)
- **Cloud:** Azure App Service, Azure Key Vault, GitHub Actions CI/CD
- **API:** RESTful v1, Swagger/OpenAPI, API Versioning
- **Testing:** Unit, Integration, Functional (strategies documented)

### **⏱️ Time to Value**

- **5 minutes** - Run locally with `dotnet run`
- **15 minutes** - Deploy to Azure App Service
- **30 minutes** - Complete authentication testing with Swagger

### **🎓 Key Takeaways for SecureClean Developers**

This project demonstrates:

1. **Enterprise Patterns** - Real-world CQRS + MediatR implementation, not just theory
2. **Clean Architecture** - Practical single-project implementation with clear layer boundaries
3. **Production Security** - JWT + blacklisting + rate limiting + middleware integration
4. **Azure Integration** - Key Vault, App Service, CI/CD - cloud-native from day one
5. **Code Quality** - Extensive documentation, SOLID principles, testable design
6. **Scalability** - Caching strategies, async patterns, resilience policies

**🚀 This is not a tutorial project - it's a production-ready implementation showcasing backend engineering expertise.**

---

## Overview

SecureCleanApiWaf is a Blazor Server application built for secure, scalable, and performant deployment on Azure. It demonstrates best practices in architecture, security, configuration management, API design, caching, error handling, and CI/CD automation. The solution integrates third-party APIs and leverages modern .NET 8 features.

---

## Project Intent

This project is designed to **demonstrate my hands-on experience in backend development** using modern .NET technologies. It showcases practical implementation of best practices in API design, CQRS, MediatR, caching, configuration management, error handling, security, and integration with Azure services. The codebase is structured to highlight real-world backend skills, including dependency injection, pipeline behaviors, and scalable architecture for cloud deployment.

---

## ⚡ Quick Reference

### **🎯 What Makes This Project Stand Out**

| Feature | Technology | Why It Matters |
|---------|-----------|----------------|
| **CQRS + MediatR** | Command/Query Pattern | ✅ Enterprise-grade architecture with clean separation of concerns |
| **JWT with Blacklisting** | Dual-Cache Strategy | ✅ Production-ready security with automatic token invalidation |
| **Custom Middleware** | Token Validation Pipeline | ✅ Clean, testable HTTP pipeline integration |
| **Clean Architecture** | Single-Project Design | ✅ Maintainable, testable, and scalable codebase |
| **Azure Integration** | Key Vault, App Service, CI/CD | ✅ Cloud-native deployment with secure secret management |
| **Polly Resilience** | Retry + Circuit Breaker | ✅ Production-grade fault tolerance for external APIs |

### **🚀 Quick Start (2 minutes)**

```bash
# 1. Clone & run locally
git clone https://github.com/dariemcarlosdev/SecureCleanApiWaf.git
cd SecureCleanApiWaf
dotnet run

# 2. Test authentication (PowerShell)
$response = Invoke-RestMethod -Uri "https://localhost:7178/api/v1/auth/login" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"username":"demo","password":"demo","role":"User"}'
$response.token

# 3. Explore APIs via Swagger
# Open: https://localhost:7178/swagger
```

### **🏗️ Architecture at a Glance**

```
Blazor UI → API Controllers → MediatR (CQRS) → Services → Azure/External APIs
              ↓                    ↓                ↓
        JWT Auth          Pipeline Behaviors   Dual-Cache
    (+ Blacklisting)      (Logging, Caching)  (Memory + Distributed)
```

### **📊 Key Metrics**

| Metric | Count | Description |
|--------|-------|-------------|
| **CQRS Components** | 8 | Commands + Queries + Handlers for authentication & data |
| **API Endpoints** | 15+ | RESTful v1 endpoints with JWT protection |
| **Security Layers** | 6 | Authentication, Authorization, Rate Limiting, CORS, Headers, API Keys |
| **Documentation Pages** | 20+ | Comprehensive implementation guides |
| **Test Coverage** | High | Unit, integration, and functional testing strategies |

### **🔍 Ready to Dive Deeper?**

**For SecureClean Developers reviewing this implementation:**

#### **📚 Documentation Hubs (START HERE)**
- 🏛️ **[Clean Architecture Hub](docs/CleanArchitecture/CLEAN-DDD_ARCH_README.md)** - Complete DDD & architecture documentation 🆕 NEW
- 🔐 **[Authentication & Authorization Hub](docs/AuthenticationAuthorization/AUTHENT-AUTHORIT_README.md)** - Complete security documentation 🆕 NEW
- 🌐 **[API Design Hub](docs/APIDesign/API_README.md)** - Complete API design documentation 🆕 NEW
- 🧪 **[Testing Documentation Hub](docs/Testing/TEST_README.md)** - Navigate all testing guides (API, Architecture, Unit Tests) 🆕 NEW
- 🚀 **[Deployment Hub](docs/Deployment/DEPLOYMENT_README.md)** - All deployment options and guides 🆕 NEW

#### **🔧 Specific Implementation Guides**
- 🔐 **[CQRS Authentication](#1-jwt-bearer-authentication-with-cqrs-)** - See the complete implementation
- 🧪 **[Clean Architecture Testing Strategy](docs/Testing/CLEAN_ARCHITECTURE_TESTING_STRATEGY.md)** - Unit, Integration & Architecture tests
- 🌐 **[API Testing Guide](docs/Testing/API_ENDPOINT_TESTING_GUIDE.md)** - How to test all 10 endpoints effectively
- 🔒 **[API Security Guide](docs/AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md)** - Understand security layers
- ✅ **[Authentication Testing Guide](docs/AuthenticationAuthorization/TEST_AUTHENTICATION_GUIDE.md)** - Try it yourself in 5 minutes
- 🏛️ **[Clean Architecture](docs/CleanArchitecture/CLEAN_ARCHITECTURE_GUIDE.md)** - Understand the design decisions
- 📖 **[Implementation Details](#-key-backend-topics-demonstrated-in-this-project)** - Full feature breakdown

---

## 🚀 Getting Started

Follow these steps to get the application running locally or deploy it to Azure:

1. **Clone the repository**
   ```bash
   git clone https://github.com/dariemcarlosdev/SecureCleanApiWaf.git
   cd SecureCleanApiWaf
   ```

2. **Set up Azure resources** (Key Vault, App Service)
   - Create an Azure Key Vault for secure secret management
   - Create an Azure App Service for hosting
   - Configure managed identity for secure access

3. **Configure secrets** in Azure and GitHub
   - Add application secrets to Azure Key Vault
   - Set up GitHub repository secrets for CI/CD
   - Configure environment-specific settings

4. **Run locally** with `dotnet run`
   ```bash
   dotnet run
   ```
   The application uses `appsettings.Development.json` for local development settings.

5. **Deploy via GitHub Actions**
   - Push to the `main` or `Dev` branch to trigger automated deployment
   - Monitor deployment progress in the GitHub Actions tab

---

### 🚀 Deployment Documentation

For comprehensive deployment guides across multiple platforms and environments, see the complete deployment documentation:

#### **📋 Master Deployment Guide**
- **[DEPLOYMENT_README.md](docs/Deployment/DEPLOYMENT_README.md)** - Complete deployment hub
  - Quick start guides (Azure App Service & Docker)
  - Platform comparison matrix
  - Prerequisites and configuration
  - Security best practices
  - Troubleshooting guide

#### **☁️ Azure App Service Deployment**
- **[Azure App Service Guide](docs/Deployment/AzureAppService/DEPLOYMENT_GUIDE.md)** - PaaS deployment (32 KB guide)
  - Azure resource setup (App Service, Key Vault)
  - Managed Identity configuration
  - GitHub Actions CI/CD pipeline
  - Application Insights integration
  - Complete deployment checklist
  - **Estimated time:** 10-15 minutes

#### **🐳 Docker Deployment**
- **[Complete Docker Guide](docs/Deployment/Docker/DOCKER_DEPLOYMENT.md)** - Full containerization guide (11 KB)
  - Building and running containers locally
  - Publishing to Docker Hub
  - Cloud deployment (Azure, AWS, GCP, Kubernetes)
  - Environment variables and configuration
  - **Estimated time:** 5-10 minutes (local)

- **[Docker Quick Reference](docs/Deployment/Docker/DOCKER_QUICK_REFERENCE.md)** - Quick commands and troubleshooting (4 KB)
- **[Docker Setup Summary](docs/Deployment/Docker/DOCKER_SETUP_SUMMARY.md)** - Overview and checklist (7 KB)

#### **🔄 CI/CD Pipeline Deployment**
- **[CI/CD Pipeline Guide](docs/CICD/CICD_PIPELINE_GUIDE.md)** - Automated deployment with GitHub Actions
  - GitHub Actions workflow configuration
  - Azure deployment automation
  - Docker Hub publishing pipeline
  - Environment variables and secrets management
  - Multi-environment deployment (Dev, Staging, Production)
  - Build, test, and deploy automation
  - **Best for:** Continuous integration and automated deployments

#### **📊 Quick Deployment Comparison**

| Deployment Option | Complexity | Time | Best For |
|-------------------|------------|------|----------|
| **Docker Compose (Local)** | ✅ Low | 5 min | Local development and testing |
| **Azure App Service** | ✅ Low | 15 min | Simple web apps, managed PaaS |
| **Azure Container Apps** | ⚙️ Medium | 10 min | Microservices, serverless containers |
| **Docker + Kubernetes** | 🔧 High | 30+ min | Enterprise, complex orchestration |
| **CI/CD (GitHub Actions)** | ⚙️ Medium | 20 min setup | Automated deployments, team collaboration |

**Choose your deployment path:**
- **New to cloud?** → [Azure App Service Guide](docs/Deployment/AzureAppService/DEPLOYMENT_GUIDE.md)
- **Want containers?** → [Docker Deployment Guide](docs/Deployment/Docker/DOCKER_DEPLOYMENT.md)
- **Need automation?** → [CI/CD Pipeline Guide](docs/CICD/CICD_PIPELINE_GUIDE.md)
- **Need overview?** → [Master Deployment Guide](docs/Deployment/DEPLOYMENT_README.md)

---

## 📖 Swagger/OpenAPI Support

Swagger (OpenAPI) support is enabled for API documentation and testing. The interactive Swagger UI provides a convenient way to explore and test all REST API endpoints exposed by the application.

### **Access Swagger UI:**

- **Development URL (HTTP):** [http://localhost:5006/swagger](http://localhost:5006/swagger)
- **Development URL (HTTPS):** [https://localhost:7178/swagger](https://localhost:7178/swagger)

### **Features:**
- ✅ Interactive API documentation
- ✅ Test endpoints directly from the browser
- ✅ View request/response models and schemas
- ✅ JWT authentication support (click "Authorize" button)
- ✅ API versioning support (v1, v2)

### **Using Swagger with Authentication:**
1. Obtain a JWT token from the `/api/v1/auth/token` endpoint
2. Click the "Authorize" button in Swagger UI
3. Enter `Bearer {your-token}` in the authorization dialog
4. Test protected endpoints with authentication

**Note:** Swagger is only available in Development mode for security reasons.

For more details on API testing and usage, see the [API Testing Guide](docs/Testing/API_ENDPOINT_TESTING_GUIDE.md).

---

## 🎯 Key Backend Topics Demonstrated in This Project

I have implemented a variety of backend development concepts and best practices throughout this project. The following points are covered and implemented:

### **Architecture & Design Patterns**
- ✅ **Blazor Server Architecture** - Real-time UI updates, server-side rendering, and SignalR integration (`App.razor`, `Home.razor`, `Routes.razor`)
  - 📖 [Web/Presentation Layer Guide](docs/CleanArchitecture/Projects/05-Web-Presentation-Layer.md)
- ✅ **RESTful API Design** - Controllers with versioning, model binding, and validation (`SampleController.cs`)
  - 🌐 **[API Design Hub](docs/APIDesign/API_README.md)** - Complete API design documentation 🆕 NEW
  - 📖 [API Design Guide](docs/APIDesign/API_DESIGN_GUIDE.md)
  - 📖 [API Contracts Examples](docs/APIDesign/API_CONTRACTS_EXAMPLES.md)
- ✅ **CQRS Pattern with MediatR** - Separation of commands and queries for authentication and data operations
  - **Authentication Commands**: `LoginUserCommand`, `BlacklistTokenCommand`
  - **Authentication Queries**: `IsTokenBlacklistedQuery`, `GetTokenBlacklistStatsQuery`
  - **Data Queries**: `GetApiDataQuery`, `GetApiDataQueryHandler`
  - **Feature-based organization** in `Core/Application/Features/`
  - 📖 [Application Layer Guide](docs/CleanArchitecture/Projects/02-Application-Layer.md)
  - 📖 [JWT Authentication CQRS Architecture](docs/AuthenticationAuthorization/JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md)
  - 📖 [CQRS Login Implementation](docs/AuthenticationAuthorization/CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md)
  - 📖 [CQRS Logout Implementation](docs/AuthenticationAuthorization/CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md)
- ✅ **Hybrid Mapping Strategy** - AutoMapper + Custom Mapper for flexible data transformation
  - Combines AutoMapper (known APIs) with Custom Mapper (dynamic APIs)
  - Decision matrix for choosing the right approach
  - Real-world examples and testing strategies
  - 📖 **[Hybrid Mapping Strategy](docs/CleanArchitecture/HYBRID-MAPPING-STRATEGY.md)** ⭐ RECOMMENDED
- ✅ **Custom Middleware** - `JwtBlacklistValidationMiddleware` for token validation in HTTP pipeline
  - 📖 [API Security Implementation Guide](docs/AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md)
  - 📖 [Web/Presentation Layer Guide](docs/CleanArchitecture/Projects/05-Web-Presentation-Layer.md)
- ✅ **Clean Architecture** - Evolving towards a multi-project Clean Architecture structure with clear layer boundaries and responsibilities
  - 📖 [Clean Architecture Guide](docs/CleanArchitecture/CLEAN_ARCHITECTURE_GUIDE.md)
  - 📖 [Clean Architecture Hub](docs/CleanArchitecture/CLEAN-DDD_ARCH_README.md)
  - 📖 [Migration Guide](docs/CleanArchitecture/MIGRATION_GUIDE.md)

### **Enterprise Patterns**
- ✅ **MediatR Pipeline Behaviors** - Custom pipeline behaviors for caching and cross-cutting concerns (`PipelineBehaviors/CachingBehavior.cs`, `PipelineBehaviors/ICacheable.cs`)
  - 📖 [Application Layer Guide](docs/CleanArchitecture/Projects/02-Application-Layer.md)
  - 📖 [API Design Guide - Performance Optimization](docs/APIDesign/API_DESIGN_GUIDE.md#performance-optimization)
- ✅ **CQRS Authentication** - Command/Query separation for authentication operations
  - Commands: `LoginUserCommand`, `BlacklistTokenCommand`
  - Queries: `IsTokenBlacklistedQuery`, `GetTokenBlacklistStatsQuery`
  - Handlers: Feature-based organization with MediatR integration
  - 📖 [JWT Authentication CQRS Architecture](docs/AuthenticationAuthorization/JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md)
  - 📖 [Authentication Index](docs/AuthenticationAuthorization/AUTHENTICATION_INDEX.md)
- ✅ **Dependency Injection** - Service registration and DI setup for controllers, services, and pipeline behaviors (`Program.cs`, `WebApplicationBuilderServicesExtensions.cs`)
  - 📖 [Infrastructure Layer Guide](docs/CleanArchitecture/Projects/03-Infrastructure-Layer.md)
  - 📖 [Interface Abstractions Summary](docs/CleanArchitecture/INTERFACE_ABSTRACTIONS_SUMMARY.md)
- ✅ **SOLID Principles** - Separation of concerns, IoC, and maintainable code structure throughout the solution
  - 📖 [Clean Architecture Guide](docs/CleanArchitecture/CLEAN_ARCHITECTURE_GUIDE.md)
  - 📖 [Domain Layer Guide](docs/CleanArchitecture/Projects/01-Domain-Layer.md)
- ✅ **Detailed Logging** - Structured logging with Serilog, request/response logging, and performance metrics (`Program.cs`, `WebApplicationExtensions.cs`)
  - 📖 [Web/Presentation Layer Guide](docs/CleanArchitecture/Projects/05-Web-Presentation-Layer.md)
  - 📖 [API Design Guide - Error Handling](docs/APIDesign/API_DESIGN_GUIDE.md#error-handling)
- ✅ **Resilience Patterns** - Retry and circuit breaker patterns using Polly for external API calls (`InfrastructureServiceExtensions.cs`)
  - 📖 [Infrastructure Layer Guide](docs/CleanArchitecture/Projects/03-Infrastructure-Layer.md)
  - 📖 [API Security Implementation Guide](docs/AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md)
- ✅ **Custom Middleware** - `JwtBlacklistValidationMiddleware` for token validation pipeline
  - 📖 [API Security Implementation Guide](docs/AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md)
  - 📖 [JWT Authentication CQRS Architecture](docs/AuthenticationAuthorization/JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md)
- ✅ **Asynchronous Programming** - Async/await patterns for non-blocking I/O operations in controllers and services
  - 📖 [API Design Guide - Performance Optimization](docs/APIDesign/API_DESIGN_GUIDE.md#performance-optimization)
  - 📖 [Infrastructure Layer Guide](docs/CleanArchitecture/Projects/03-Infrastructure-Layer.md)
- ✅ **HTTP Client Factory** - Typed HttpClient with DelegatingHandler for secure external API calls (`ApiKeyHandler.cs`, `InfrastructureServiceExtensions.cs`)
  - 📖 [Infrastructure Layer Guide](docs/CleanArchitecture/Projects/03-Infrastructure-Layer.md)
  - 📖 [API Design Guide - Performance Optimization](docs/APIDesign/API_DESIGN_GUIDE.md#performance-optimization)
- ✅ **Versioning** - API versioning strategy for backward compatibility (`SampleController.cs`, `Program.cs`)
  - 📖 [API Design Guide - API Versioning](docs/APIDesign/API_DESIGN_GUIDE.md#api-versioning)
- ✅ **Custom Error Details** - Standardized error responses thru Middleware (`WebApplicationExtensions.cs`)
  - 📖 [API Design Guide - Error Handling](docs/APIDesign/API_DESIGN_GUIDE.md#error-handling)
  - 📖 [Web/Presentation Layer Guide](docs/CleanArchitecture/Projects/05-Web-Presentation-Layer.md)

### **Performance & Scalability**
- ✅ **Caching Strategies** - In-memory and distributed caching implementations (`Caching/SampleCache.cs`, MediatR pipeline)
  - 📖 [API Design Guide - Caching Strategy](docs/APIDesign/API_DESIGN_GUIDE.md#caching-strategy)
  - 📖 [Application Layer Guide](docs/CleanArchitecture/Projects/02-Application-Layer.md)
  - 📖 [Infrastructure Layer Guide](docs/CleanArchitecture/Projects/03-Infrastructure-Layer.md)
- ✅ **Performance Optimization** - Caching strategies, latency logging, and efficient API calls
  - 📖 [API Design Guide - Performance Optimization](docs/APIDesign/API_DESIGN_GUIDE.md#performance-optimization)
- ✅ **Scalability** - Designed for cloud deployment with Azure services, scalable architecture, and CI/CD automation
  - 📖 [Deployment README](docs/Deployment/DEPLOYMENT_README.md)
  - 📖 [Azure App Service Deployment Guide](docs/Deployment/AzureAppService/DEPLOYMENT_GUIDE.md)
  - 📖 [Infrastructure.Azure Layer Guide](docs/CleanArchitecture/Projects/04-Infrastructure-Azure-Layer.md)

### **Configuration & Security**
- ✅ **Configuration Management** - Environment variables, appsettings files, and Azure Key Vault for secure configuration (`WebApplicationBuilderServicesExtensions.cs`)
  - 📖 [Infrastructure Layer Guide](docs/CleanArchitecture/Projects/03-Infrastructure-Layer.md)
  - 📖 [Infrastructure.Azure Layer Guide](docs/CleanArchitecture/Projects/04-Infrastructure-Azure-Layer.md)
  - 📖 [Azure App Service Deployment Guide](docs/Deployment/AzureAppService/DEPLOYMENT_GUIDE.md)
- ✅ **Security Best Practices** - Secure secret management, environment-based configuration, and production deployment practices
  - 📖 [API Security Implementation Guide](docs/AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md)
  - 📖 [Azure App Service Deployment Guide](docs/Deployment/AzureAppService/DEPLOYMENT_GUIDE.md)
- ✅ **JWT Authentication with CQRS** - Token-based authentication using Command/Query pattern
  - `LoginUserCommand` - CQRS command for user authentication
  - `BlacklistTokenCommand` - CQRS command for secure logout
  - `IsTokenBlacklistedQuery` - CQRS query for token validation
  - 📖 [JWT Authentication CQRS Architecture](docs/AuthenticationAuthorization/JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md)
  - 📖 [API Security Implementation Guide](docs/AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md)
  - 📖 [Test Authentication Guide](docs/AuthenticationAuthorization/TEST_AUTHENTICATION_GUIDE.md)
- ✅ **Token Blacklisting** - Dual-cache strategy for secure logout (Memory + Distributed cache)
  - 📖 [CQRS Logout Implementation](docs/AuthenticationAuthorization/CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md)
  - 📖 [JWT Authentication CQRS Architecture](docs/AuthenticationAuthorization/JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md)
- ✅ **JWT Middleware** - `JwtBlacklistValidationMiddleware` for automatic token validation
  - 📖 [API Security Implementation Guide](docs/AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md)
  - 📖 [Web/Presentation Layer Guide](docs/CleanArchitecture/Projects/05-Web-Presentation-Layer.md)
- ✅ **API Security** - Rate limiting, CORS, security headers, and external API key management
  - 📖 [API Security Implementation Guide](docs/AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md)
  - 📖 [API Design Guide - Security](docs/APIDesign/API_DESIGN_GUIDE.md#security)

### **API Documentation & Testing**
- ✅ **Swagger/OpenAPI Support** - API documentation and testing via Swagger UI (`Program.cs`, `WebApplicationBuilderServicesExtensions.cs`)
  - 📖 [API Testing Guide](docs/Testing/API_ENDPOINT_TESTING_GUIDE.md)
  - 📖 [Test Authentication Guide](docs/AuthenticationAuthorization/TEST_AUTHENTICATION_GUIDE.md)
  - 📖 **[Testing Documentation Hub](docs/Testing/TEST_README.md)** - Complete testing guide navigation 🆕 NEW
- ✅ **Error Handling** - Global try-catch in controllers and services, error pages (`SampleController.cs`, `Error.razor`)
  - 📖 [API Design Guide - Error Handling](docs/APIDesign/API_DESIGN_GUIDE.md#error-handling)
  - 📖 [Web/Presentation Layer Guide](docs/CleanArchitecture/Projects/05-Web-Presentation-Layer.md)
- ✅ **Testing Strategy** - Comprehensive unit, integration, and architecture testing strategies
  - 📖 **[Testing Documentation Hub](docs/Testing/TEST_README.md)** - Central hub for all testing guides 🆕 NEW
  - 📖 **[Clean Architecture Testing Strategy](docs/Testing/CLEAN_ARCHITECTURE_TESTING_STRATEGY.md)** - Complete testing strategy guide 🆕 NEW
  - 📖 [API Testing Guide](docs/Testing/API_ENDPOINT_TESTING_GUIDE.md)
  - 📖 [Test Authentication Guide](docs/AuthenticationAuthorization/TEST_AUTHENTICATION_GUIDE.md)
- ✅ **Comprehensive Documentation** - Full documentation of architecture, patterns, and implementation details in `docs/` folder
  - 📖 [Clean Architecture Hub](docs/CleanArchitecture/CLEAN-DDD_ARCH_README.md)
  - 📖 [Authentication Hub](docs/AuthenticationAuthorization/AUTHENT-AUTHORIT_README.md)
  - 📖 **[Testing Hub](docs/Testing/TEST_README.md)** - Navigate all testing documentation 🆕 NEW
  - 📖 [Deployment Hub](docs/Deployment/DEPLOYMENT_README.md)

---

## 🏛️ Clean Architecture

This project follows **Clean Architecture** principles to enhance maintainability, testability, and scalability. Clean Architecture provides:

- ✅ **Clear layer boundaries** with proper dependency flow
- ✅ **Interface abstractions** for Dependency Inversion Principle
- ✅ **Infrastructure independence** - swap frameworks without rewriting business logic
- ✅ **Enhanced testability** - test each layer in isolation
- ✅ **Team scalability** - multiple developers can work on different layers
- ✅ **Single-project structure** - maintaining fast development speed
- ✅ **Domain Layer** - 85% complete with rich entities and value objects 🎯

### 📚 Clean Architecture Documentation

For comprehensive Clean Architecture implementation details, see:

**📋 Main Guide:**
- **[CLEAN_ARCHITECTURE_GUIDE.md](docs/CleanArchitecture/CLEAN_ARCHITECTURE_GUIDE.md)** - Complete unified guide
  - Current project status with domain layer progress (85% complete)
  - Architecture principles and dependency flow
  - Single-project vs multi-project approaches
  - Layer responsibilities and implementation guidance
  - Domain entities: User, Token, ApiDataItem (NEW)
  - Value objects: Email, Role (NEW)
  - Domain enums: UserStatus, TokenStatus, TokenType, DataStatus (NEW)

**🔗 Layer Integration Guide:** 🆕
- **[LAYER_INTEGRATION_GUIDE.md](docs/CleanArchitecture/LAYER_INTEGRATION_GUIDE.md)** - How layers integrate
  - Visual integration architecture
  - Key integration points between all layers
  - Dependency Injection flow
  - Complete request flow examples (Authentication, Token Blacklisting, API Data)
  - Anti-patterns to avoid
  - Reference to specific implementation files

**📂 Layer-Specific Guides:**
- **[01-Domain-Layer.md](docs/CleanArchitecture/Projects/01-Domain-Layer.md)** - Domain implementation (85% complete)
  - Base classes, entities, value objects, enums
  - Business rules and domain logic
  - Implementation examples and patterns
  
- **[02-Application-Layer.md](docs/CleanArchitecture/Projects/02-Application-Layer.md)** - CQRS with MediatR
- **[03-Infrastructure-Layer.md](docs/CleanArchitecture/Projects/03-Infrastructure-Layer.md)** - External services
- **[04-Infrastructure-Azure-Layer.md](docs/CleanArchitecture/Projects/04-Infrastructure-Azure-Layer.md)** - Azure integration
- **[05-Web-Presentation-Layer.md](docs/CleanArchitecture/Projects/05-Web-Presentation-Layer.md)** - API and UI

**🚀 Implementation Guides:**
- **[MIGRATION_GUIDE.md](docs/CleanArchitecture/MIGRATION_GUIDE.md)** - Step-by-step migration guide

---

## 🔄 Service Alignment & Architecture Integration

This section explains how `ApiIntegrationService` aligns with CQRS, MediatR, and the caching pipeline, demonstrating proper separation of concerns and adherence to .NET best practices.

### **Service Responsibility**

`ApiIntegrationService` is laser-focused on its primary concern: **business logic and third-party API integration**.

**Core Responsibilities:**
- ✅ Handles HTTP communication with external APIs using `IHttpClientFactory`
- ✅ Implements business logic for API data retrieval and processing
- ✅ Returns `Result<T>` pattern for consistent error handling
- ✅ Remains stateless and thread-safe (suitable for singleton lifetime)

**What the service does NOT do:**
- ❌ Does not implement CQRS directly (that's the handler's responsibility)
- ❌ Does not implement `ICacheable` (caching is a cross-cutting concern)
- ❌ Does not know about MediatR pipeline (maintains reusability)
- ❌ Does not handle presentation concerns (stays in Infrastructure layer)

**Why this matters:**
- **Single Responsibility** - Service has one job: integrate with external APIs
- **Testability** - Easy to mock and test in isolation
- **Reusability** - Can be used outside of CQRS if needed
- **Maintainability** - Changes to API logic don't affect CQRS or caching

---

### **Result Pattern Implementation**

The service returns `Result<T>`, which encapsulates:
- ✅ **Success state** - Boolean indicating operation success
- ✅ **Data payload** - The actual data returned (if successful)
- ✅ **Error information** - Detailed error messages (if failed)

**Why Result<T> is ideal for MediatR handlers and CQRS queries:**

```csharp
// Clean error handling in handlers
var result = await _apiService.GetAllDataAsync<List<SampleDataDto>>(apiUrl);

if (!result.Success)
{
    _logger.LogError("API call failed: {Error}", result.Error);
    return Result<List<SampleDataDto>>.Fail(result.Error);
}

return Result<List<SampleDataDto>>.Ok(result.Data);
```

**Benefits:**
- No exception-based control flow
- Type-safe error handling
- Consistent response pattern across all handlers
- Easy to compose and chain operations

---

### **CQRS Integration**

**Key Principle:** The service itself does **not** implement CQRS or `ICacheable`. This is correct and intentional.

CQRS is achieved by **wrapping calls** to `ApiIntegrationService` in MediatR query handlers:

```csharp
// Query Handler (Application Layer)
public class GetApiDataQueryHandler : IRequestHandler<GetApiDataQuery, Result<List<SampleDataDto>>>
{
    private readonly IApiIntegrationService _apiService; // ✅ Service abstraction
    
    public GetApiDataQueryHandler(IApiIntegrationService apiService)
    {
        _apiService = apiService;
    }
    
    public async Task<Result<List<SampleDataDto>>> Handle(
        GetApiDataQuery request, 
        CancellationToken cancellationToken)
    {
        // Service call wrapped in CQRS handler
        return await _apiService.GetAllDataAsync<List<SampleDataDto>>("api/data");
    }
}
```

**Request Flow:**
```
Controller → MediatR → Query Handler → ApiIntegrationService → External API
```

**Separation Benefits:**
- **Handler** owns the CQRS pattern (implements `IRequestHandler`)
- **Service** is just a dependency (injected via DI)
- **Testing** - Can test handler logic and service logic independently

---

### **Caching Integration**

Caching is handled by the **MediatR pipeline** (`CachingBehavior`), not by the service itself.

**How it works:**

1. **Query implements `ICacheable`:**
```csharp
public record GetApiDataQuery : IRequest<Result<List<SampleDataDto>>>, ICacheable
{
    public string CacheKey => "api-data-all";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

2. **Pipeline behavior intercepts requests:**
```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(...)
    {
        if (request is ICacheable cacheable)
        {
            // Check cache first
            var cached = await _cache.GetAsync<TResponse>(cacheable.CacheKey);
            if (cached != null) return cached;
            
            // Execute handler (and therefore service) only on cache miss
            var response = await next();
            
            // Cache the response
            await _cache.SetAsync(cacheable.CacheKey, response, cacheable.Expiration);
            return response;
        }
        
        return await next();
    }
}
```

3. **Handler and service execute only on cache miss**

**Benefits:**
- ✅ **Transparent** - Handler doesn't know about caching
- ✅ **Reusable** - Any query can opt-in to caching
- ✅ **Testable** - Caching logic isolated in behavior
- ✅ **Configurable** - Each query defines its own cache settings

**Reference Files:**
- 📖 `PipelineBehaviors/CachingBehavior.cs` - Pipeline behavior implementation
- 📖 `PipelineBehaviors/ICacheable.cs` - Caching interface
- 📖 `Features/GetData/GetApiDataQuery.cs` - Query implementing `ICacheable`

---

### **Dependency Injection Configuration**

The service is registered as a **singleton** in DI, which is suitable for stateless services using `IHttpClientFactory`:

```csharp
// Infrastructure service registration
services.AddSingleton<IApiIntegrationService, ApiIntegrationService>();

// HttpClient with factory pattern
services.AddHttpClient<IApiIntegrationService, ApiIntegrationService>(client =>
{
    client.BaseAddress = new Uri(configuration["ThirdPartyApi:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddTransientHttpErrorPolicy(policy => 
    policy.WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
.AddHttpMessageHandler<ApiKeyHandler>();
```

**Why singleton lifetime?**
- ✅ Service is stateless (no instance state)
- ✅ Thread-safe for concurrent requests
- ✅ `IHttpClientFactory` manages HttpClient lifecycle internally
- ✅ Reduces memory allocation and GC pressure

**Reference Files:**
- 📖 `Presentation/Extensions/DependencyInjection/InfrastructureServiceExtensions.cs` - DI configuration

---

### **HttpClient Management Best Practices**

Using `IHttpClientFactory` in `ApiIntegrationService` aligns with .NET best practices and is **recommended by Microsoft**.

**Problems IHttpClientFactory solves:**
- ✅ **Socket exhaustion** - Reuses HTTP handlers to avoid running out of sockets
- ✅ **DNS changes** - Respects DNS TTL by periodically recreating handlers
- ✅ **Centralized configuration** - All HTTP settings in one place
- ✅ **Named/Typed clients** - Support for multiple external APIs
- ✅ **DI integration** - Easily mockable for testing
- ✅ **Resilience** - Built-in support for Polly policies

**Implementation in service:**
```csharp
public class ApiIntegrationService : IApiIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public ApiIntegrationService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<Result<T>> GetAllDataAsync<T>(string apiUrl)
    {
        // Factory creates properly configured client
        var client = _httpClientFactory.CreateClient("ThirdPartyApiClient");
        
        var response = await client.GetAsync(apiUrl);
        // ...handle response
    }
}
```

**Benefits:**
- ✅ **ApiKeyHandler** automatically adds API key to requests
- ✅ **Polly policies** provide retry and circuit breaker logic
- ✅ **Logging** via DelegatingHandler for full traceability
- ✅ **Configuration** externalized to appsettings.json

**Reference Files:**
- 📖 `Infrastructure/Services/ApiIntegrationService.cs` - Service implementation
- 📖 `Infrastructure/Handlers/ApiKeyHandler.cs` - API key injection handler

---

### **Architecture Diagram: Request Flow**

Here's how all the pieces work together:

```
+-------------------------------------------------------------+
│ 1. Controller/Component (Presentation Layer)               │
│    • Receives HTTP request                                  │
│    • Sends MediatR query                                    │
+-------------------------------------------------------------+
                         │
                         ↓
+-------------------------------------------------------------+
│ 2. MediatR Pipeline (Application Layer)                    │
│    • LoggingBehavior → logs request                        │
│    • ValidationBehavior → validates query                  │
│    • CachingBehavior → checks cache (ICacheable)           │
+-------------------------------------------------------------+
                         │
                         ↓ (cache miss)
+-------------------------------------------------------------+
│ 3. Query Handler (Application Layer)                       │
│    • GetApiDataQueryHandler                                │
│    • Calls IApiIntegrationService                          │
+-------------------------------------------------------------+
                         │
                         ↓
+-------------------------------------------------------------+
│ 4. ApiIntegrationService (Infrastructure Layer)            │
│    • Uses IHttpClientFactory                               │
│    • ApiKeyHandler adds headers                            │
│    • Polly handles retries/circuit breaker                 │
│    • Returns Result<T>                                     │
+-------------------------------------------------------------+
                         │
                         ↓
+--------------------+  +-------------------+
│   External API     │  │  Distributed      │
│   (Third-Party)    │  │  Cache (Redis)    │
+--------------------+  +-------------------+
          ☁️ AZURE CLOUD SERVICES ☁️
+-----------------------------------------------------------+
│  +--------------+  +--------------+  +-----------------+  │
│  │   Key Vault  │  │ App Service  │  │  App Insights   │  │
│  │   (Secrets)  │  │  (Hosting)   │  │  (Monitoring)   │  │
│  +--------------+  +--------------+  +-----------------+  │
+-----------------------------------------------------------+
```

---

## 🔒 Security Implementation (Development & Demo)

SecureCleanApiWaf includes a comprehensive security implementation designed to demonstrate my knowledge and skills in securing web applications and APIs. This implementation showcases **industry-standard security patterns** and best practices for **development and testing purposes**.

### **📚 Complete Security Documentation**

For comprehensive security implementation details, testing instructions, and production guidelines, refer to these dedicated guides:

#### **Security Architecture Guide**
- **[API-SECURITY-IMPLEMENTATION-GUIDE.md](docs/AuthenticationAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md)** - Complete security architecture documentation
  - JWT configuration and CQRS integration
  - Token blacklisting with dual-cache strategy
  - Custom middleware pipeline (`JwtBlacklistValidationMiddleware`)
  - External API security patterns  
  - Rate limiting setup
  - CORS configuration
  - Security headers reference
  - Resilience patterns (Polly)
  - Production deployment checklist

#### **CQRS Authentication Architecture**
- **[JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md](docs/AuthenticationAuthorization/JWT_AUTHENTICATION_CQRS_ARCHITECTURE.md)** - Complete CQRS implementation guide
  - Command pattern for login and logout
  - Query pattern for token validation
  - MediatR integration and pipeline behaviors
  - Automatic caching with `CachingBehavior`
  - Handler implementations and best practices

#### **Testing & Verification Guide**
- **[TEST_AUTHENTICATION_GUIDE.md](docs/AuthenticationAuthorization/TEST_AUTHENTICATION_GUIDE.md)** - Step-by-step testing guide
  - How to run the application
  - Getting JWT tokens via CQRS endpoints
  - Testing protected endpoints
  - Testing logout and token blacklisting
  - Testing authorization policies
  - Testing rate limiting
  - Admin monitoring endpoints
  - Troubleshooting guide

---

### **⚠️ Important: Demo Implementation Notice**

The security features in this project are implemented as a **demonstration of security knowledge and best practices**:

**Perfect for:**
- 📚 **Learning** - Shows industry-standard patterns and how I approach security
- 💼 **Portfolios** - Demonstrates security awareness and implementation skills
- 🎯 **Interviews** - Proves hands-on experience with authentication, authorization, and secure API design

**Important Notes:**
- ⚠️ **NOT production-ready as-is** - Requires real authentication provider and secure secret management for production use
- 🔧 **Development-focused** - Simplified for rapid testing and demonstration purposes only

---

### **🛡️ Security Features Implemented**

This section provides an overview of the six core security features implemented in SecureCleanApiWaf. Each feature follows industry best practices and includes complete reference documentation.

---

#### **1. JWT Bearer Authentication with CQRS** 🔐

Industry-standard token-based authentication using CQRS pattern with MediatR for clean architecture.

**Key Features:**
- ✅ **Stateless authentication** - No server-side sessions; tokens contain all user info
- ✅ **CQRS Integration** - Login and logout use Command pattern via MediatR
  - `LoginUserCommand` + Handler - User authentication with audit logging
  - `BlacklistTokenCommand` + Handler - Token invalidation on logout
  - `IsTokenBlacklistedQuery` + Handler - Token validation with automatic caching
- ✅ **Token Blacklisting** - Dual-cache strategy (Memory + Distributed cache)
- ✅ **Custom Middleware** - `JwtBlacklistValidationMiddleware` validates tokens before authorization
- ✅ **Role-based authorization** - User and Admin roles (defined in `AuthController.cs`)
- ✅ **Configurable token expiration** - Default 60 minutes (configurable in `appsettings.json`)
- ✅ **Admin Monitoring** - Statistics and health check endpoints via `TokenBlacklistController`
- ✅ **Token validation parameters** - Issuer, audience, and signing key validation
- ✅ **HTTPS enforcement** - Required in production environments
- ✅ **Swagger integration** - Built-in authentication testing support

**Implementation Files:**
- 📖 [`JwtTokenGenerator.cs`](Infrastructure/Security/JwtTokenGenerator.cs) - JWT token creation
- 📖 [`TokenBlacklistService.cs`](Infrastructure/Services/TokenBlacklistService.cs) - Token blacklist management
- 📖 [`JwtBlacklistValidationMiddleware.cs`](Infrastructure/Middleware/JwtBlacklistValidationMiddleware.cs) - Token validation pipeline
- 📖 [`LoginUserCommand.cs`](Core/Application/Features/Authentication/Commands/LoginUserCommand.cs) - CQRS login command
- 📖 [`BlacklistTokenCommand.cs`](Core/Application/Features/Authentication/Commands/BlacklistTokenCommand.cs) - CQRS logout command
- 📖 [`IsTokenBlacklistedQuery.cs`](Core/Application/Features/Authentication/Queries/IsTokenBlacklistedQuery.cs) - Token validation query
- 📖 [`AuthController.cs`](Presentation/Controllers/v1/AuthController.cs) - Authentication endpoints
- 📖 [`TokenBlacklistController.cs`](Presentation/Controllers/v1/TokenBlacklistController.cs) - Admin monitoring endpoints
- 📖 [`PresentationServiceExtensions.cs`](Presentation/Extensions/DependencyInjection/PresentationServiceExtensions.cs) - JWT configuration
- 📖 [`WebApplicationExtensions.cs`](Presentation/Extensions/HttpPipeline/WebApplicationExtensions.cs) - Middleware pipeline

**CQRS Flow:**
```
POST /api/v1/auth/login
  → LoginUserCommand
  → MediatR → LoginUserCommandHandler
  → JwtTokenGenerator
  → Returns token + metadata

POST /api/v1/auth/logout
  → BlacklistTokenCommand
  → MediatR → BlacklistTokenCommandHandler
  → TokenBlacklistService (dual cache)
  → Returns success + recommendations

Any Protected Endpoint
  → JwtBlacklistValidationMiddleware
  → IsTokenBlacklistedQuery (with caching)
  → MediatR → IsTokenBlacklistedQueryHandler
  → Validates token not blacklisted
```

---

#### **2. External API Security (ApiKeyHandler)** 🔑

Secure outgoing HTTP requests to third-party APIs using the DelegatingHandler pattern with centralized key management.

**Key Features:**
- ✅ **Centralized API key management** - Keys stored in secure configuration
- ✅ **Automatic header injection** - API keys and security headers added automatically
- ✅ **Retry policy with exponential backoff** - Polly integration for resilience
- ✅ **Circuit breaker pattern** - Prevents cascading failures
- ✅ **Request/response logging** - Full traceability for debugging
- ✅ **Azure Key Vault ready** - Production-ready secret storage integration

**Implementation Files:**
- 📖 [`ApiKeyHandler.cs`](Infrastructure/Handlers/ApiKeyHandler.cs) - DelegatingHandler implementation
- 📖 [`InfrastructureServiceExtensions.cs`](Presentation/Extensions/DependencyInjection/InfrastructureServiceExtensions.cs) - HttpClient configuration
- 📖 [`appsettings.json`](appsettings.json) - Third-party API configuration

**Code Example:**
```csharp
// API Key Handler Implementation
public class ApiKeyHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyHandler> _logger;
    
    public ApiKeyHandler(IConfiguration configuration, ILogger<ApiKeyHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // Add API key from secure configuration
        var apiKey = _configuration["ThirdPartyApi:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Add("X-API-Key", apiKey);
        }
        
        // Add security headers
        request.Headers.Add("User-Agent", "SecureCleanApiWaf/1.0");
        request.Headers.Add("Accept", "application/json");
        
        // Log outgoing request (optional)
        _logger.LogInformation("API Request: {Method} {Uri}", request.Method, request.RequestUri);
        
        // Send request with added headers
        var response = await base.SendAsync(request, cancellationToken);
        
        // Log response status (optional)
        _logger.LogInformation("API Response: {StatusCode}", response.StatusCode);
        
        return response;
    }
}

// HttpClient configuration with Polly policies
services.AddHttpClient("ThirdPartyApiClient", client =>
{
    client.BaseAddress = new Uri(configuration["ThirdPartyApi:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<ApiKeyHandler>() // Inject API key handler
.AddPolicyHandler(GetRetryPolicy())     // Add retry policy
.AddPolicyHandler(GetCircuitBreakerPolicy()); // Add circuit breaker
```

### **💡 Note on Architecture & Future Enhancements**

**For SecureClean Developers:**

This application demonstrates a **consistent, repeatable architectural pattern** that can scale with your needs. All features—current and future—follow the same proven structure:

#### **Pattern for Adding New Features:**

**1. Define Command/Query in Application Layer**
```
Core/Application/Features/{FeatureName}/
+-- Commands/
│   +-- {Feature}Command.cs
│   +-- {Feature}CommandHandler.cs
+-- Queries/
    +-- {Feature}Query.cs
    +-- {Feature}QueryHandler.cs
```

**2. Implement Business Logic in Infrastructure**
```
Infrastructure/
+-- Services/
    +-- {Feature}Service.cs (implementing I{Feature}Service)
```

**3. Expose via API in Presentation Layer**
```
Presentation/
+-- Controllers/v1/
    +-- {Feature}Controller.cs
```

**4. Apply Cross-Cutting Concerns Automatically**
- ✅ **Caching** - via `ICacheable` interface
- ✅ **Logging** - via MediatR `LoggingBehavior`
- ✅ **Validation** - via MediatR `ValidationBehavior`
- ✅ **Error Handling** - via `Result<T>` pattern

---

#### **Why This Matters:**

| Benefit | Impact |
|---------|--------|
| **Consistency** | Every feature looks and behaves the same way |
| **Predictability** | New developers can quickly understand any feature |
| **Maintainability** | Changes are localized and don't ripple through the codebase |
| **Testability** | Each layer can be tested independently |
| **Scalability** | Adding features doesn't increase complexity |

#### **Real-World Example:**

The **authentication system** demonstrates this pattern perfectly:

```
Authentication Feature:
+-- Commands: LoginUserCommand, BlacklistTokenCommand
+-- Queries: IsTokenBlacklistedQuery, GetTokenBlacklistStatsQuery
+-- Handlers: LoginUserCommandHandler, BlacklistTokenCommandHandler
+-- Services: TokenBlacklistService, JwtTokenGenerator
+-- Controllers: AuthController, TokenBlacklistController
+-- Cross-Cutting: Automatic caching, logging, validation
```

**Future Features** (user management, reporting, notifications, data processing) will follow this exact pattern.

---

## 📞 Support & Contact

**Project Maintainer:** Dariemcarlos  
**Email:** softevolutionsl@gmail.com  
**GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)  
**Repository:** [SecureCleanApiWaf](https://github.com/dariemcarlosdev/SecureCleanApiWaf)

**Looking for collaboration or have questions?** Feel free to reach out via email or open an issue on GitHub!

---

**Last Updated:** November 2025  
**Status:** ✅ Active Development & Maintained  
**License:** MIT

---

**Made with ❤️ by Dariemcarlos for the SecureClean Developer Community**
