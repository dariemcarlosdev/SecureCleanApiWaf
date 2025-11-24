# JWT Token Blacklist Integration with CQRS + MediatR

## 📖 **Overview**

This implementation demonstrates how to integrate JWT token blacklisting with your existing CQRS pattern and MediatR implementation. The solution provides clean separation of concerns, automatic caching, comprehensive error handling, and follows your established architectural patterns.

## 🏗️ **Architecture Overview**

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
┌─────────────────────────────────────────────────────────────┐
│  AuthController           │  TokenBlacklistController        │
│  - Login (Direct)         │  - GetTokenStatus (CQRS Query)  │
│  - Logout (CQRS Command)  │  - GetStats (CQRS Query)        │
│  - GetToken (Direct)      │  - GetHealth (CQRS Query)       │
└─────────────────────────────────────────────────────────────┘
                                    ↓
                                    ↓
┌─────────────────────────────────────────────────────────────┐
│                 INFRASTRUCTURE LAYER                         │
┌─────────────────────────────────────────────────────────────┐
│  JwtBlacklistValidationMiddleware                            │
│  - Uses IsTokenBlacklistedQuery (CQRS)                      │
│  - Automatic caching via CachingBehavior                     │
│  - Integrates with authentication pipeline                   │
└─────────────────────────────────────────────────────────────┘
                                    ↓
                                    ↓
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                         │
┌─────────────────────────────────────────────────────────────┐
│  COMMANDS                    │  QUERIES                      │
│  - BlacklistTokenCommand     │  - IsTokenBlacklistedQuery    │
│  - BlacklistTokenHandler     │  - IsTokenBlacklistedHandler  │
│                              │  - GetTokenBlacklistStatsQuery│
│                              │  - GetTokenBlacklistStatsHandler
└─────────────────────────────────────────────────────────────┘
                                    ↓
                                    ↓
┌─────────────────────────────────────────────────────────────┐
│                 INFRASTRUCTURE SERVICES                      │
┌─────────────────────────────────────────────────────────────┐
│  ITokenBlacklistService → TokenBlacklistService              │
│  - Dual cache strategy (Memory + Distributed)                │
│  - Automatic TTL based on token expiration                   │
│  - Comprehensive logging and error handling                  │
└─────────────────────────────────────────────────────────────┘
```

## 🧩 **CQRS Components Created**

### **📋 Commands**
- **`LoginUserCommand`** - Command to authenticate user and generate JWT token
- **`LoginUserCommandHandler`** - Handles user authentication and token generation
- **`BlacklistTokenCommand`** - Command to blacklist JWT tokens during logout
- **`BlacklistTokenCommandHandler`** - Handles token blacklisting business logic

### **🔍 Queries**
- **`IsTokenBlacklistedQuery`** - Query to check if token is blacklisted (with caching)
- **`IsTokenBlacklistedQueryHandler`** - Handles token validation checks
- **`GetTokenBlacklistStatsQuery`** - Query for comprehensive system statistics
- **`GetTokenBlacklistStatsQueryHandler`** - Handles statistics aggregation

### **Response Models**
- **`LoginResponse`** - Rich response with token and metadata
- **`BlacklistTokenResponse`** - Rich response with security recommendations
- **`TokenBlacklistStatus`** - Detailed token status with metadata
- **`TokenBlacklistStatistics`** - Enhanced statistics with health indicators

## 🔗 **Integration Points**

### **1. AuthController (Updated)**
```csharp
// BEFORE: Direct service injection
public AuthController(JwtTokenGenerator tokenGenerator, ITokenBlacklistService tokenBlacklistService)

// AFTER: CQRS with MediatR
public AuthController(JwtTokenGenerator tokenGenerator, IMediator mediator)

[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var command = new LoginUserCommand(request.Username, request.Password, request.Role, clientIp, userAgent);
    var result = await _mediator.Send(command);
    // Handle result with rich response data
}

[HttpPost("logout")]
public async Task<IActionResult> Logout()
{
    var command = new BlacklistTokenCommand(token, "user_logout", clientIp, userAgent);
    var result = await _mediator.Send(command);
    // Handle result with rich response data
}
```

### **2. JWT Middleware (Updated)**
```csharp
// BEFORE: Direct service injection
public JwtBlacklistValidationMiddleware(RequestDelegate next, ITokenBlacklistService tokenBlacklistService)

// AFTER: CQRS with MediatR
public JwtBlacklistValidationMiddleware(RequestDelegate next, IMediator mediator)

public async Task InvokeAsync(HttpContext context)
{
    var query = new IsTokenBlacklistedQuery(token, bypassCache: false);
    var result = await _mediator.Send(query);
    // Automatic caching, comprehensive error handling
}
```

### **3. New Administrative Controller**
```csharp
[ApiController]
[Route("api/v1/token-blacklist")]
public class TokenBlacklistController : ControllerBase
{
    // GET /api/v1/token-blacklist/status?token=xxx
    // GET /api/v1/token-blacklist/stats (Admin only)
    // GET /api/v1/token-blacklist/health (Anonymous)
}
```

## ✅ **CQRS Benefits Achieved**

### **1. Separation of Concerns**
- **Commands**: Handle write operations (blacklisting tokens)
- **Queries**: Handle read operations (checking status, getting stats)
- **Controllers**: Focus on HTTP concerns only
- **Handlers**: Contain pure business logic

### **2. Automatic Caching**
```csharp
public class IsTokenBlacklistedQuery : IRequest<Result<TokenBlacklistStatus>>, ICacheable
{
    public int SlidingExpirationInMinutes { get; set; } = 1;  // Fast cache for security
    public int AbsoluteExpirationInMinutes { get; set; } = 2;
}
```

### **3. Consistent Error Handling**
```csharp
// All operations return Result<T> for consistent error handling
public async Task<Result<BlacklistTokenResponse>> Handle(BlacklistTokenCommand request)
{
    try 
    {
        // Business logic
        return Result<BlacklistTokenResponse>.Ok(response);
    }
    catch (Exception ex)
    {
        return Result<BlacklistTokenResponse>.Fail("Error message");
    }
}
```

### **4. Pipeline Behaviors**
- **CachingBehavior**: Automatic caching for queries implementing `ICacheable`
- **Future extensibility**: Easy to add validation, logging, or other behaviors

## 🔄 **Request Flow Examples**

### **Login Flow (Command)**
```
1. POST /api/v1/auth/login
2. AuthController extracts credentials
3. Creates LoginUserCommand with audit info
4. MediatR.Send() → LoginUserCommandHandler
5. Handler validates credentials and generates JWT token
6. Returns rich LoginResponse with token metadata
7. Controller formats final HTTP response
```

### **Logout Flow (Command)**
```
1. POST /api/v1/auth/logout
2. AuthController extracts token from header
3. Creates BlacklistTokenCommand with audit info
4. MediatR.Send() → BlacklistTokenCommandHandler
5. Handler validates token and calls ITokenBlacklistService
6. Returns rich BlacklistTokenResponse with recommendations
7. Controller formats final HTTP response
```

### **Token Validation Flow (Query with Caching)**
```
1. Middleware extracts token from request
2. Creates IsTokenBlacklistedQuery
3. MediatR.Send() → CachingBehavior checks cache first
4. If cache miss: IsTokenBlacklistedQueryHandler calls service
5. Result cached automatically for future requests
6. Middleware acts on blacklist status (allow/deny)
```

### **Statistics Flow (Cached Query)**
```
1. GET /api/v1/token-blacklist/stats
2. TokenBlacklistController creates GetTokenBlacklistStatsQuery
3. MediatR.Send() → CachingBehavior (5-minute cache)
4. Handler aggregates comprehensive statistics
5. Returns enhanced statistics with health indicators
6. Controller formats administrative response
```

## 🧪 **Usage Examples**

### **Testing the Implementation**

1. **Generate Token**:
```bash
GET /api/v1/auth/token?type=user
```

2. **Use Token**:
```bash
GET /api/v1/sample
Authorization: Bearer <token>
```

3. **Logout (CQRS Command)**:
```bash
POST /api/v1/auth/logout
Authorization: Bearer <token>
```

4. **Check Token Status (CQRS Query)**:
```bash
GET /api/v1/token-blacklist/status?token=<token>
Authorization: Bearer <admin-token>
```

5. **Get System Statistics (CQRS Query)**:
```bash
GET /api/v1/token-blacklist/stats
Authorization: Bearer <admin-token>
```

## 🔒 **Security Features**

### **Enhanced Security with CQRS**
- **Audit Trail**: All operations logged through handlers
- **Rich Context**: Commands include IP, User-Agent for security analysis  
- **Detailed Responses**: Comprehensive status information for monitoring
- **Health Monitoring**: Real-time system health through queries
- **Administrative Controls**: Role-based access to sensitive endpoints

### **Performance Optimizations**
- **Intelligent Caching**: Different cache strategies for different operations
- **Efficient Lookups**: O(1) cache lookups with automatic invalidation
- **Dual Cache Strategy**: Memory + distributed cache for best performance
- **Health Indicators**: Proactive monitoring of system performance

## ✅ **Integration Complete**

Your JWT token blacklist is now fully integrated with your CQRS + MediatR pattern! The implementation provides:

✅ **Clean Architecture**: Follows your established patterns  
✅ **Automatic Caching**: Leverages your existing CachingBehavior  
✅ **Consistent Error Handling**: Uses your Result<T> pattern  
✅ **Rich Monitoring**: Administrative endpoints with comprehensive data  
✅ **Security First**: Enhanced audit trails and security features  
✅ **Performance Optimized**: Intelligent caching strategies  
✅ **Testable**: Clean separation enables easy unit testing  
✅ **Extensible**: Easy to add more authentication features  

The solution seamlessly integrates with your existing architecture while providing enterprise-grade JWT logout functionality!

---

## 🆘 **Contact & Support**

### **Project Information**
- **Project Name:** CleanArchitecture.ApiTemplate - JWT Authentication with CQRS Architecture
- **Component:** Authentication & Authorization - CQRS Integration
- **Version:** 1.3.0 (Complete CQRS Architecture)
- **Framework:** .NET 8 with MediatR
- **Patterns:** CQRS, Mediator, Repository, Result<T>
- **Repository:** [https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate)

### **Author & Maintainer**
- **Name:** Dariem Carlos
- **GitHub:** [@dariemcarlosdev](https://github.com/dariemcarlosdev)
- **Email:** softevolutionsl@gmail.com
- **Branch:** Dev
- **Location:** Professional Tech Challenge Submission

### **Getting Help**

#### 🏗️ **Architecture Questions**
For questions about the CQRS architecture:
1. Review this document for complete architecture overview
2. Check [CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md](CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md) for login details
3. Check [CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md](CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md) for logout details
4. Review MediatR pipeline behaviors in `Common/Behaviors/`
5. Check [existing issues](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/issues?q=label%3Acqrs+label%3Aarchitecture)

#### 💻 **CQRS Implementation Questions**
For CQRS pattern implementation questions:
1. Review command and query handlers in `Core/Application/Features/Authentication/`
2. Check `ICacheable` interface for caching strategy
3. Review `CachingBehavior` for automatic caching logic
4. Check `Result<T>` pattern implementation
5. Test with provided usage examples

#### 🔗 **Integration Questions**
For questions about integrating with existing code:
1. Review the Integration Points section above
2. Check updated `AuthController` implementation
3. Review `JwtBlacklistValidationMiddleware` changes
4. Check `TokenBlacklistController` for admin endpoints
5. Review dependency injection configuration

#### 🐛 **Bug Reports**
If you find an architecture or integration issue:
1. Verify the issue with complete testing flow
2. Check MediatR pipeline execution
3. Review handler logs
4. Check cache behavior
5. Create an issue with:
   - Architecture component affected
   - Steps to reproduce
   - Expected vs actual behavior
   - Handler logs and cache status

#### 📖 **Documentation Improvements**
To improve this architecture guide:
1. Open a [pull request](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/pulls) with improvements
2. Add diagrams or flow charts if helpful
3. Include code examples for clarity
4. Update related implementation summaries

### **Support Channels**

#### 📧 **Direct Contact**
For private inquiries:
- **Email:** softevolutionsl@gmail.com
- **Subject:** `[CleanArchitecture.ApiTemplate CQRS Architecture] Your Question`
- **Response Time:** 24-48 hours

#### 💬 **GitHub Discussions**
For architecture and pattern discussions:
- Use [GitHub Discussions](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate/discussions)
- Tag with: `cqrs`, `mediatr`, `architecture`, `authentication`, `patterns`
- Share your architectural insights and experiences

#### 🐙 **GitHub Issues**
For bugs and enhancements:
- **Bug Reports:** Include architecture component and flow details
- **Feature Requests:** Describe architectural enhancement needs
- **Labels:** `cqrs`, `architecture`, `mediatr`, `enhancement`

### **Quick Reference**

#### 🏗️ **Architecture Layers**
```
Presentation Layer
├── AuthController (Login, Logout)
└── TokenBlacklistController (Status, Stats, Health)

Infrastructure Layer
└── JwtBlacklistValidationMiddleware (Token validation)

Application Layer
├── Commands (LoginUserCommand, BlacklistTokenCommand)
├── Queries (IsTokenBlacklistedQuery, GetTokenBlacklistStatsQuery)
└── Handlers (Command/Query handlers with business logic)

Infrastructure Services
└── TokenBlacklistService (Dual cache implementation)
```

#### 📋 **Key Components**
```
CQRS Commands:
- LoginUserCommand + LoginUserCommandHandler
- BlacklistTokenCommand + BlacklistTokenCommandHandler

CQRS Queries:
- IsTokenBlacklistedQuery + Handler (1-2 min cache)
- GetTokenBlacklistStatsQuery + Handler (5-10 min cache)

Response Models:
- LoginResponse, BlacklistTokenResponse
- TokenBlacklistStatus, TokenBlacklistStatistics

Behaviors:
- CachingBehavior (ICacheable interface)
- Result<T> pattern for error handling
```

#### 🧪 **Testing the Architecture**
```bash
# Complete CQRS flow test
# 1. Login (Command)
curl -X POST "https://localhost:7178/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"user","password":"pass","role":"User"}'

# 2. Use token
curl -X GET "https://localhost:7178/api/v1/sample" \
  -H "Authorization: Bearer $TOKEN"

# 3. Logout (Command)
curl -X POST "https://localhost:7178/api/v1/auth/logout" \
  -H "Authorization: Bearer $TOKEN"

# 4. Check status (Cached Query)
curl -X GET "https://localhost:7178/api/v1/token-blacklist/status?token=$TOKEN" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# 5. Get stats (Cached Query)
curl -X GET "https://localhost:7178/api/v1/token-blacklist/stats" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

### **Related Resources**

#### 📚 **Documentation**
- 📖 [AUTHENT-AUTHORIT_README.md](AUTHENT-AUTHORIT_README.md) - Main authentication hub
- 📖 [CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md](CQRS_LOGIN_IMPLEMENTATION_SUMMARY.md) - Login implementation
- 📖 [CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md](CQRS_LOGOUT_IMPLEMENTATION_SUMMARY.md) - Logout implementation
- 📖 [TEST_AUTHENTICATION_GUIDE.md](TEST_AUTHENTICATION_GUIDE.md) - Complete testing guide
- 📖 [API-SECURITY-IMPLEMENTATION-GUIDE.md](API-SECURITY-IMPLEMENTATION-GUIDE.md) - Security guide

#### 🔗 **External Resources**
- [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)
- [CQRS Pattern by Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Result Pattern in C#](https://enterprisecraftsmanship.com/posts/error-handling-exception-or-result/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)

### **Architecture Benefits**

#### ✅ **Achieved with CQRS**
- **Separation of Concerns** - Commands vs Queries clearly separated
- **Testability** - Each handler can be unit tested independently
- **Scalability** - Queries can be cached differently than commands
- **Maintainability** - Clear patterns make code easy to understand
- **Extensibility** - Easy to add new commands/queries without affecting existing code

#### 🚀 **Performance Optimizations**
- **Intelligent Caching** - Different strategies for different operations
- **Automatic Cache Management** - via `ICacheable` and `CachingBehavior`
- **Dual Cache Strategy** - Memory + Distributed for best performance
- **Efficient Lookups** - O(1) cache operations
- **Reduced Database Load** - Caching reduces repeated queries

#### 🔒 **Security Enhancements**
- **Audit Trail** - All operations logged through handlers
- **Rich Context** - Commands include IP, User-Agent for analysis
- **Comprehensive Monitoring** - Health checks and statistics
- **Role-Based Access** - Admin endpoints properly secured
- **Token Lifecycle Management** - Complete blacklisting workflow

### **Implementation Checklist**

#### ✅ **Completed**
- ✅ CQRS Commands for Login and Logout
- ✅ CQRS Queries for Token Validation and Statistics
- ✅ Automatic caching via `ICacheable` interface
- ✅ `CachingBehavior` for pipeline caching
- ✅ `Result<T>` pattern for error handling
- ✅ Rich response models with metadata
- ✅ Middleware integration with CQRS
- ✅ Admin endpoints with role-based access
- ✅ Comprehensive logging and audit trails
- ✅ Health monitoring and statistics

#### 🔧 **Optional Enhancements**
- 🔧 `ValidationBehavior` for input validation
- 🔧 `LoggingBehavior` for request/response logging
- 🔧 `PerformanceBehavior` for slow query detection
- 🔧 Real-time notifications on suspicious activity
- 🔧 Advanced analytics dashboard
- 🔧 Multi-tenancy support
- 🔧 Event sourcing integration

### **Contributing**

#### 🤝 **How to Contribute**
Contributions to improve the CQRS architecture are welcome!

1. **Fork the repository**
2. **Create a feature branch** from `Dev`
3. **Make your architectural changes**
4. **Test thoroughly** with all flows
5. **Submit a pull request** with:
   - Clear description of architectural changes
   - Justification for pattern improvements
   - Test results for all flows
   - Updated architecture documentation

#### ✅ **Contribution Guidelines**
- Follow CQRS pattern principles strictly
- Maintain separation between Commands and Queries
- Use `Result<T>` for all handler responses
- Implement `ICacheable` for appropriate queries
- Add comprehensive logging in handlers
- Update architecture diagrams if needed
- Include unit tests for new handlers
- Update all related documentation

---

**Last Updated:** January 2025  
**Document Status:** ✅ Complete  
**Architecture Status:** ✅ Production-Ready  
**CQRS Pattern:** ✅ Fully Implemented  
**Integration Status:** ✅ Complete

---

*This architecture guide is part of the CleanArchitecture.ApiTemplate authentication system.*  
*For the latest updates, visit the [GitHub repository](https://github.com/dariemcarlosdev/CleanArchitecture.ApiTemplate).*
