using System.Reflection;
using AutoMapper;
using MediatR;
using CleanArchitecture.ApiTemplate.Core.Application.Common.Behaviors;
using CleanArchitecture.ApiTemplate.Core.Application.Common.Mapping;
using CleanArchitecture.ApiTemplate.Core.Application.Features.SampleData.Queries;
using CleanArchitecture.ApiTemplate.Core.Application.Features.Authentication.Commands;
using CleanArchitecture.ApiTemplate.Core.Application.Features.Authentication.Queries;
using CleanArchitecture.ApiTemplate.Core.Application.Common.Models;
using CleanArchitecture.ApiTemplate.Core.Application.Common.DTOs;
using CleanArchitecture.ApiTemplate.Models;

namespace CleanArchitecture.ApiTemplate.Presentation.Extensions.DependencyInjection
{
    /// <summary>
    /// Dependency injection setup for Application layer services
    /// Registers MediatR, pipeline behaviors, and application-specific services
    /// </summary>
    public static class ApplicationServiceExtensions
    {
        /// <summary>
        /// Registers application-layer services into the dependency injection container, including MediatR handlers, AutoMapper profiles, the ApiDataMapper, concrete closed generic request handlers for DTOs, authentication CQRS handlers, and MediatR pipeline behaviors (e.g., caching).
        /// </summary>
        /// <returns>The same <see cref="IServiceCollection"/> instance with application services registered.</returns>
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

        /// <summary>
        /// Registers closed generic MediatR handlers for concrete DTO/model types required by the application.
        /// </summary>
        /// <remarks>
        /// Adds transient registrations that map specific request types (for example, <c>GetApiDataQuery&lt;T&gt;</c> and <c>GetApiDataByIdQuery&lt;T&gt;</c>) to their concrete handler implementations.
        /// Extend this method with additional registrations when new DTO/model types are introduced (see the <c>SampleDtoModel</c> example).
        /// </remarks>
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
        /// <summary>
        /// Registers authentication-related MediatR request handlers into the DI container.
        /// </summary>
        /// <remarks>
        /// Adds transient registrations for command and query handlers used by the authentication flow:
        /// - LoginUserCommand -> LoginUserCommandHandler (IRequestHandler&lt;LoginUserCommand, Result&lt;LoginResponseDto&gt;&gt;)
        /// - BlacklistTokenCommand -> BlacklistTokenCommandHandler (IRequestHandler&lt;BlacklistTokenCommand, Result&lt;BlacklistTokenResponse&gt;&gt;)
        /// - IsTokenBlacklistedQuery -> IsTokenBlacklistedQueryHandler (IRequestHandler&lt;IsTokenBlacklistedQuery, Result&lt;TokenBlacklistStatusDto&gt;&gt;)
        /// - GetTokenBlacklistStatsQuery -> GetTokenBlacklistStatsQueryHandler (IRequestHandler&lt;GetTokenBlacklistStatsQuery, Result&lt;TokenBlacklistStatisticsDto&gt;&gt;)
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