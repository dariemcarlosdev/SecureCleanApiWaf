using System.Net.Http.Headers;

namespace SecureCleanApiWaf.Infrastructure.Handlers
{
    /// <summary>
    /// Provides an HTTP message handler that automatically adds an API key and standard headers to outgoing requests.
    /// Implement DelegatingHandler to create a custom handler that can be used in an HttpClient request pipeline.
    /// This handler is registered with an HttpClient configured specifically.
    /// for external APIs. This handler is designed to facilitate secure communication with third-party services.
    /// </summary>
    /// <remarks>This handler retrieves the API key from application configuration and injects it into the
    /// request headers, following common patterns such as "X-API-Key". It also adds security-related headers and logs
    /// request and response details for monitoring purposes. Use this handler in an HttpClient pipeline when
    /// communicating with third-party APIs that require API key authentication. The API key must be configured in the
    /// application's settings under the expected key name.
    /// for production scenarios, consider retrieving the API key from a secure vault or secret management service.
    /// Please adjust the header name and authentication scheme based on the specific requirements of the external API.
    /// As a reference, chechk: Docs/AuthenticationAndAuthorization/API-SECURITY-IMPLEMENTATION-GUIDE.md
    /// </remarks>
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
            // Retrieve API key from secure configuration (Azure Key Vault in production)
            var apiKey = _configuration["ThirdPartyApi:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("API Key is not configured for external API calls");
            }
            else
            {
                // Add API key to request headers (adjust header name based on external API requirements)
                // Common patterns: X-API-Key, Api-Key, Authorization: ApiKey {key}
                request.Headers.Add("X-API-Key", apiKey);
                
                // Alternative: Bearer token pattern
                // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                
                _logger.LogDebug("API Key added to request for {RequestUri}", request.RequestUri);
            }

            // Add additional security headers
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); // Expect JSON responses
            request.Headers.UserAgent.ParseAdd("BlueTreadApp/1.0"); // Identify the client application

            // Log request for monitoring
            _logger.LogInformation("Sending request to external API: {Method} {Uri}", 
                request.Method, request.RequestUri);

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Send the request
                var response = await base.SendAsync(request, cancellationToken);
                
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                // Log response
                _logger.LogInformation(
                    "Received response from external API: {StatusCode} in {Duration}ms", 
                    (int)response.StatusCode, duration);

                // Optional: Retry logic for specific status codes
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "External API returned error: {StatusCode} - {ReasonPhrase}", 
                        (int)response.StatusCode, response.ReasonPhrase);
                }

                return response;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request to external API timed out after {Duration}ms", 
                    (DateTime.UtcNow - startTime).TotalMilliseconds);
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to external API failed");
                throw;
            }
        }
    }
}
