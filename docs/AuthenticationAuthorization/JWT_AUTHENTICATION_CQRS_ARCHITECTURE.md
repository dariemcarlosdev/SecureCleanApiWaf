# JWT Token Blacklist Integration with CQRS + MediatR

## ?? **Overview**

This implementation demonstrates how to integrate JWT token blacklisting with your existing CQRS pattern and MediatR implementation. The solution provides clean separation of concerns, automatic caching, comprehensive error handling, and follows your established architectural patterns.

## ??? **Architecture Overview**

```
???????????????????????????????????????????????????????????????????
?                    PRESENTATION LAYER                           ?
???????????????????????????????????????????????????????????????????
?  AuthController           ?  TokenBlacklistController           ?
?  - Login (Direct)         ?  - GetTokenStatus (CQRS Query)     ?
?  - Logout (CQRS Command)  ?  - GetStats (CQRS Query)           ?
?  - GetToken (Direct)      ?  - GetHealth (CQRS Query)          ?
???????????????????????????????????????????????????????????????????
                                    ?
                                    ?
???????????????????????????????????????????????????????????????????
?                 INFRASTRUCTURE LAYER                            ?
???????????????????????????????????????????????????????????????????
?  JwtBlacklistValidationMiddleware                               ?
?  - Uses IsTokenBlacklistedQuery (CQRS)                        ?
?  - Automatic caching via CachingBehavior                       ?
?  - Integrates with authentication pipeline                      ?
???????????????????????????????????????????????????????????????????
                                    ?
                                    ?
???????????????????????????????????????????????????????????????????
?                    APPLICATION LAYER                            ?
???????????????????????????????????????????????????????????????????
?  COMMANDS                    ?  QUERIES                         ?
?  - BlacklistTokenCommand     ?  - IsTokenBlacklistedQuery       ?
?  - BlacklistTokenHandler     ?  - IsTokenBlacklistedHandler     ?
?                              ?  - GetTokenBlacklistStatsQuery   ?
?                              ?  - GetTokenBlacklistStatsHandler ?
???????????????????????????????????????????????????????????????????
                                    ?
                                    ?
???????????????????????????????????????????????????????????????????
?                 INFRASTRUCTURE SERVICES                         ?
???????????????????????????????????????????????????????????????????
?  ITokenBlacklistService ? TokenBlacklistService                ?
?  - Dual cache strategy (Memory + Distributed)                  ?
?  - Automatic TTL based on token expiration                     ?
?  - Comprehensive logging and error handling                    ?
???????????????????????????????????????????????????????????????????
```

## ?? **CQRS Components Created**

### **Commands**
- **`LoginUserCommand`** - Command to authenticate user and generate JWT token
- **`LoginUserCommandHandler`** - Handles user authentication and token generation
- **`BlacklistTokenCommand`** - Command to blacklist JWT tokens during logout
- **`BlacklistTokenCommandHandler`** - Handles token blacklisting business logic

### **Queries**
- **`IsTokenBlacklistedQuery`** - Query to check if token is blacklisted (with caching)
- **`IsTokenBlacklistedQueryHandler`** - Handles token validation checks
- **`GetTokenBlacklistStatsQuery`** - Query for comprehensive system statistics
- **`GetTokenBlacklistStatsQueryHandler`** - Handles statistics aggregation

### **Response Models**
- **`LoginResponse`** - Rich response with token and metadata
- **`BlacklistTokenResponse`** - Rich response with security recommendations
- **`TokenBlacklistStatus`** - Detailed token status with metadata
- **`TokenBlacklistStatistics`** - Enhanced statistics with health indicators

## ?? **Integration Points**

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

## ?? **CQRS Benefits Achieved**

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

## ?? **Request Flow Examples**

### **Login Flow (Command)**
```
1. POST /api/v1/auth/login
2. AuthController extracts credentials
3. Creates LoginUserCommand with audit info
4. MediatR.Send() ? LoginUserCommandHandler
5. Handler validates credentials and generates JWT token
6. Returns rich LoginResponse with token metadata
7. Controller formats final HTTP response
```

### **Logout Flow (Command)**
```
1. POST /api/v1/auth/logout
2. AuthController extracts token from header
3. Creates BlacklistTokenCommand with audit info
4. MediatR.Send() ? BlacklistTokenCommandHandler
5. Handler validates token and calls ITokenBlacklistService
6. Returns rich BlacklistTokenResponse with recommendations
7. Controller formats final HTTP response
```

### **Token Validation Flow (Query with Caching)**
```
1. Middleware extracts token from request
2. Creates IsTokenBlacklistedQuery
3. MediatR.Send() ? CachingBehavior checks cache first
4. If cache miss: IsTokenBlacklistedQueryHandler calls service
5. Result cached automatically for future requests
6. Middleware acts on blacklist status (allow/deny)
```

### **Statistics Flow (Cached Query)**
```
1. GET /api/v1/token-blacklist/stats
2. TokenBlacklistController creates GetTokenBlacklistStatsQuery
3. MediatR.Send() ? CachingBehavior (5-minute cache)
4. Handler aggregates comprehensive statistics
5. Returns enhanced statistics with health indicators
6. Controller formats administrative response
```

## ??? **Usage Examples**

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

## ?? **Security Features**

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

## ?? **Integration Complete**

Your JWT token blacklist is now fully integrated with your CQRS + MediatR pattern! The implementation provides:

? **Clean Architecture**: Follows your established patterns  
? **Automatic Caching**: Leverages your existing CachingBehavior  
? **Consistent Error Handling**: Uses your Result<T> pattern  
? **Rich Monitoring**: Administrative endpoints with comprehensive data  
? **Security First**: Enhanced audit trails and security features  
? **Performance Optimized**: Intelligent caching strategies  
? **Testable**: Clean separation enables easy unit testing  
? **Extensible**: Easy to add more authentication features  

The solution seamlessly integrates with your existing architecture while providing enterprise-grade JWT logout functionality!
