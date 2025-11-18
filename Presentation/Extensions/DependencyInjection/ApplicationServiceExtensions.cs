using System.Reflection;
using AutoMapper;
using MediatR;
using SecureCleanApiWaf.Core.Application.Common.Behaviors;
using SecureCleanApiWaf.Core.Application.Common.Mapping;
using SecureCleanApiWaf.Core.Application.Features.SampleData.Queries;
using SecureCleanApiWaf.Core.Application.Features.Authentication.Commands;
using SecureCleanApiWaf.Core.Application.Features.Authentication.Queries;
using SecureCleanApiWaf.Core.Application.Common.Models;
using SecureCleanApiWaf.Core.Application.Common.DTOs;
using SecureCleanApiWaf.Models;

namespace SecureCleanApiWaf.Presentation.Extensions.DependencyInjection
{
    /// <summary>
    /// Dependency injection setup for Application layer services
    /// Registers MediatR, pipeline behaviors, and application-specific services
    /// </summary>
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Get the assembly containing Application layer code
            var applicationAssembly = typeof(GetApiDataQuery<>).Assembly;
            
            // Register MediatR - this will automatically find and register all non-generic handlers
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(applicationAssembly);
            });

            // ===== Register AutoMapper =====
            // Scans assembly for Profile classes and registers them automatically
            // Profiles found: ApiDataMappingProfile
            services.AddAutoMapper(applicationAssembly);

            // ===== Register Application layer mappers and utilities =====
            // Custom mapper for dynamic/unknown API responses (complements AutoMapper)
            services.AddSingleton<ApiDataMapper>();

            // Explicitly register concrete generic handlers for each DTO type you use
            // This avoids the arity mismatch by registering closed generic types
            RegisterConcreteGenericHandlers(services);
            
            // Explicitly register authentication CQRS handlers
            RegisterAuthenticationHandlers(services);
            
            // Register MediatR Pipeline Behaviors (order matters!)
            // CachingBehavior must be registered for queries that implement ICacheable
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
            
            return services;
        }

        private static void RegisterConcreteGenericHandlers(IServiceCollection services)
        {
            // Register closed generic handlers for each concrete type you need
            // Add more registrations here as you add more DTO/Model types
            
            // For SampleDtoModel
            services.AddTransient<IRequestHandler<GetApiDataQuery<SampleDtoModel>, Result<SampleDtoModel>>, 
                                   GetApiDataQueryHandler<SampleDtoModel>>();
            
            services.AddTransient<IRequestHandler<GetApiDataByIdQuery<SampleDtoModel>, Result<SampleDtoModel>>, 
                                   GetApiDataByIdQueryHandler<SampleDtoModel>>();
            
            // Example: If you add another model like ProductDto, register it like this:
            // services.AddTransient<IRequestHandler<GetApiDataQuery<ProductDto>, Result<ProductDto>>, 
            //                        GetApiDataQueryHandler<ProductDto>>();
            // services.AddTransient<IRequestHandler<GetApiDataByIdQuery<ProductDto>, Result<ProductDto>>, 
            //                        GetApiDataByIdQueryHandler<ProductDto>>();
        }

        /// <summary>
        /// Registers CQRS handlers for authentication-related operations.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <remarks>
        /// This method explicitly registers all authentication-related CQRS handlers:
        /// 
        /// Commands:
        /// - LoginUserCommand ? LoginUserCommandHandler
        ///   Handles JWT token generation for user authentication
        /// - BlacklistTokenCommand ? BlacklistTokenCommandHandler
        ///   Handles JWT token blacklisting during logout operations
        /// 
        /// Queries:
        /// - IsTokenBlacklistedQuery ? IsTokenBlacklistedQueryHandler
        ///   Checks if a JWT token is currently blacklisted (with caching)
        /// - GetTokenBlacklistStatsQuery ? GetTokenBlacklistStatsQueryHandler
        ///   Retrieves comprehensive blacklist statistics (with caching)
        /// 
        /// All handlers follow the established patterns:
        /// - Use Result<T> for consistent error handling
        /// - Implement proper logging and error handling
        /// - Support caching through ICacheable interface where appropriate
        /// - Integrate with existing infrastructure services
        /// </remarks>
        private static void RegisterAuthenticationHandlers(IServiceCollection services)
        {
            // ===== COMMAND HANDLERS =====

            // LoginUserCommand: Handles JWT token generation for user authentication
            // Used by: AuthController.Login() for secure login functionality
            // Features: Credential validation, token generation, comprehensive logging
            services.AddTransient<IRequestHandler<LoginUserCommand, Result<LoginResponseDto>>,
                                   LoginUserCommandHandler>();
            
            // BlacklistTokenCommand: Handles JWT token blacklisting operations
            // Used by: AuthController.Logout() for secure logout functionality
            // Features: Token validation, blacklisting, comprehensive logging
            services.AddTransient<IRequestHandler<BlacklistTokenCommand, Result<BlacklistTokenResponse>>, 
                                   BlacklistTokenCommandHandler>();

            // ===== QUERY HANDLERS =====
            
            // IsTokenBlacklistedQuery: Checks token blacklist status
            // Used by: JwtBlacklistValidationMiddleware for authentication pipeline
            // Features: Fast lookups, caching support, detailed status information
            services.AddTransient<IRequestHandler<IsTokenBlacklistedQuery, Result<TokenBlacklistStatusDto>>, 
                                   IsTokenBlacklistedQueryHandler>();

            // GetTokenBlacklistStatsQuery: Retrieves comprehensive statistics
            // Used by: Administrative endpoints, health checks, monitoring dashboards
            // Features: Performance metrics, security insights, health indicators
            services.AddTransient<IRequestHandler<GetTokenBlacklistStatsQuery, Result<TokenBlacklistStatisticsDto>>, 
                                   GetTokenBlacklistStatsQueryHandler>();
        }
    }
}
