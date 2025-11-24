namespace CleanArchitecture.ApiTemplate.Infrastructure.Data
{
    /// <summary>
    /// Configuration settings for database connection and Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// This class holds database configuration values read from appsettings.json.
    /// It's used with the Options pattern for type-safe configuration access.
    /// 
    /// Configuration Location:
    /// - appsettings.json: "DatabaseSettings" section
    /// - appsettings.Development.json: Local SQL Server Express
    /// - appsettings.Production.json: Azure SQL Database
    /// - Azure Key Vault: Production connection strings (secure)
    /// 
    /// Usage Example:
    /// ```json
    /// {
    ///   "DatabaseSettings": {
    ///     "ConnectionString": "Server=localhost;Database=CleanArchitecture.ApiTemplate;Trusted_Connection=True;",
    ///     "EnableSensitiveDataLogging": false,
    ///     "EnableDetailedErrors": false,
    ///     "CommandTimeout": 30,
    ///     "MaxRetryCount": 3,
    ///     "MaxRetryDelay": 30
    ///   }
    /// }
    /// ```
    /// 
    /// Security Best Practices:
    /// - Never commit connection strings to source control
    /// - Use User Secrets for local development
    /// - Use Azure Key Vault for production
    /// - Rotate credentials regularly
    /// - Use Managed Identity when possible
    /// </remarks>
    public class DatabaseSettings
    {
        /// <summary>
        /// Gets or sets the database connection string.
        /// </summary>
        /// <remarks>
        /// Connection String Format Examples:
        /// 
        /// SQL Server (Windows Authentication):
        /// Server=localhost;Database=CleanArchitecture.ApiTemplate;Trusted_Connection=True;TrustServerCertificate=True;
        /// 
        /// SQL Server (SQL Authentication):
        /// Server=localhost;Database=CleanArchitecture.ApiTemplate;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
        /// 
        /// Azure SQL Database:
        /// Server=tcp:yourserver.database.windows.net,1433;Database=CleanArchitecture.ApiTemplate;User ID=yourusername;Password=yourpassword;Encrypt=True;
        /// 
        /// SQL Server LocalDB (Development):
        /// Server=(localdb)\\mssqllocaldb;Database=CleanArchitecture.ApiTemplate;Trusted_Connection=True;
        /// 
        /// Important:
        /// - Always use encrypted connections in production (Encrypt=True)
        /// - Store passwords securely (Azure Key Vault, User Secrets)
        /// - Prefer Managed Identity over username/password in Azure
        /// </remarks>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to enable sensitive data logging.
        /// </summary>
        /// <remarks>
        /// When true, EF Core logs parameter values in SQL queries.
        /// 
        /// Development: Set to true for easier debugging
        /// Production: MUST be false for security (prevents password leakage in logs)
        /// 
        /// Example log output when enabled:
        /// "Executed DbCommand (5ms) [Parameters=[@p0='john@example.com'], CommandType='Text']
        ///  SELECT * FROM Users WHERE Email = @p0"
        /// 
        /// Security Risk:
        /// - Logs may contain passwords, credit cards, PII
        /// - Log aggregation tools may expose sensitive data
        /// - Compliance violations (GDPR, PCI-DSS)
        /// </remarks>
        public bool EnableSensitiveDataLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to enable detailed error messages.
        /// </summary>
        /// <remarks>
        /// When true, EF Core includes detailed error information in exceptions.
        /// 
        /// Development: Set to true for easier debugging
        /// Production: Set to false to avoid exposing internal details
        /// 
        /// Detailed errors include:
        /// - Full SQL query text
        /// - Database schema information
        /// - Internal EF Core state
        /// 
        /// Security Risk:
        /// - May expose database structure to attackers
        /// - Information disclosure vulnerability
        /// - Helps attackers understand your data model
        /// </remarks>
        public bool EnableDetailedErrors { get; set; } = false;

        /// <summary>
        /// Gets or sets the command timeout in seconds.
        /// </summary>
        /// <remarks>
        /// Maximum time to wait for a database command to complete.
        /// 
        /// Default: 30 seconds (EF Core default)
        /// 
        /// Adjust based on workload:
        /// - Simple queries: 5-15 seconds
        /// - Complex reports: 60-120 seconds
        /// - Batch operations: 300-600 seconds
        /// 
        /// Considerations:
        /// - Too low: Legitimate queries timeout
        /// - Too high: Poor user experience, resource waste
        /// - Use async operations to avoid blocking threads
        /// 
        /// For long-running operations, consider:
        /// - Background jobs (Hangfire, Azure Functions)
        /// - Read replicas for reporting
        /// - Query optimization (indexes, query tuning)
        /// </remarks>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for transient errors.
        /// </summary>
        /// <remarks>
        /// EF Core automatically retries failed operations for transient errors.
        /// 
        /// Default: 3 retries
        /// 
        /// Transient errors include:
        /// - Network timeouts
        /// - Deadlocks
        /// - Connection pool exhaustion
        /// - Temporary database unavailability
        /// 
        /// Not retried:
        /// - Constraint violations
        /// - Invalid SQL syntax
        /// - Permission denied
        /// - Authentication failures
        /// 
        /// Azure SQL Database specific:
        /// - Automatic failover during maintenance
        /// - Throttling during peak load
        /// - Region-level outages (rare)
        /// </remarks>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the maximum retry delay in seconds.
        /// </summary>
        /// <remarks>
        /// Maximum time to wait between retry attempts.
        /// 
        /// Default: 30 seconds
        /// 
        /// Retry delay strategy (exponential backoff):
        /// - Retry 1: ~1 second
        /// - Retry 2: ~3 seconds
        /// - Retry 3: ~9 seconds
        /// - Maximum: Capped at MaxRetryDelay
        /// 
        /// Benefits of exponential backoff:
        /// - Gives database time to recover
        /// - Reduces thundering herd problem
        /// - Better than fixed delay
        /// - Industry best practice
        /// 
        /// Tuning:
        /// - High traffic: Lower delay (faster failure detection)
        /// - Batch jobs: Higher delay (more patient)
        /// - Critical operations: More retries
        /// </remarks>
        public int MaxRetryDelay { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether to enable query splitting.
        /// </summary>
        /// <remarks>
        /// When true, EF Core splits complex queries with collections into multiple SQL queries.
        /// 
        /// Default: false (single query with JOINs)
        /// 
        /// When to enable:
        /// - Queries return large collections
        /// - Cartesian explosion issues (1 + N problem)
        /// - Timeouts on complex joins
        /// 
        /// Trade-offs:
        /// - Pros: Better performance for large result sets
        /// - Cons: Multiple database round-trips, potential consistency issues
        /// 
        /// Example:
        /// Without splitting: SELECT * FROM Orders JOIN OrderItems (1 query, large result)
        /// With splitting: SELECT * FROM Orders; SELECT * FROM OrderItems WHERE... (2 queries)
        /// </remarks>
        public bool EnableQuerySplitting { get; set; } = false;

        /// <summary>
        /// Validates the database settings.
        /// </summary>
        /// <returns>True if settings are valid, false otherwise.</returns>
        /// <remarks>
        /// Checks for:
        /// - Non-empty connection string
        /// - Valid timeout values
        /// - Valid retry configuration
        /// 
        /// Call this during startup to fail fast if misconfigured.
        /// <summary>
        /// Validates the database configuration values against required constraints.
        /// </summary>
        /// <returns>`true` if the ConnectionString is not empty, CommandTimeout is greater than 0, MaxRetryCount is between 0 and 10 inclusive, and MaxRetryDelay is greater than 0; `false` otherwise.</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                return false;

            if (CommandTimeout <= 0)
                return false;

            if (MaxRetryCount < 0 || MaxRetryCount > 10)
                return false;

            if (MaxRetryDelay <= 0)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a sanitized connection string for logging (without password).
        /// </summary>
        /// <returns>Connection string with password removed.</returns>
        /// <remarks>
        /// Safe to log for debugging without exposing credentials.
        /// 
        /// Example:
        /// Input: "Server=localhost;Database=MyDb;User=sa;Password=secret123;"
        /// Output: "Server=localhost;Database=MyDb;User=sa;Password=***;"
        /// <summary>
        /// Produces a connection string with embedded passwords redacted for safe logging.
        /// </summary>
        /// <returns>An empty string if <see cref="ConnectionString"/> is null or whitespace; otherwise the connection string with password values replaced with `***`.</returns>
        public string GetSanitizedConnectionString()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                return string.Empty;

            // Simple password removal (replace with *** for logging)
            var sanitized = System.Text.RegularExpressions.Regex.Replace(
                ConnectionString,
                @"Password\s*=\s*[^;]+",
                "Password=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Also handle PWD shorthand
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                @"PWD\s*=\s*[^;]+",
                "PWD=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return sanitized;
        }
    }
}