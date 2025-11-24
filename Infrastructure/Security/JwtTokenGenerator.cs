using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CleanArchitecture.ApiTemplate.Infrastructure.Security
{
    /// <summary>
    /// Provides functionality for generating JSON Web Tokens (JWT) for users with configurable claims and roles.
    /// </summary>
    /// <remarks>This class uses configuration settings to create signed JWT tokens suitable for
    /// authentication and authorization scenarios. The required settings, such as secret key, issuer, audience, and
    /// expiration, must be present in the application's configuration under the "JwtSettings" section. Tokens generated
    /// by this class can be used with standard JWT authentication middleware. Thread safety is ensured for token
    /// generation operations.</remarks>
    public class JwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance that uses the provided application configuration to read JWT settings for token generation.
        /// </summary>
        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Generates a JSON Web Token (JWT) containing user identity and role claims for authentication and
        /// authorization purposes.
        /// </summary>
        /// <remarks>The generated token includes standard claims such as subject ('sub'), username
        /// ('unique_name'), token identifier ('jti'), and issued-at time ('iat'). Role claims are added for each
        /// specified role, enabling role-based authorization. The token is signed using HMAC-SHA256 and is valid for
        /// the duration specified in the configuration. Ensure that the secret key is securely stored and meets
        /// recommended length requirements for security.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the token is being generated. This value is included in the
        /// token's 'sub' claim and must not be null or empty.</param>
        /// <param name="username">The username to associate with the token. This value is included in the token's 'unique_name' claim and must
        /// not be null or empty.</param>
        /// <param name="roles">An array of roles assigned to the user. Each role is added as a separate claim in the token. If null or
        /// empty, no role claims are included.</param>
        /// <returns>A string representing the serialized JWT. The token includes user identity and role claims, and is signed
        /// using the configured secret key.</returns>
        /// <summary>
        /// Generate a signed JSON Web Token containing user identity and optional role claims.
        /// </summary>
        /// <param name="userId">The unique identifier for the user, stored as the `sub` claim.</param>
        /// <param name="username">The user's username, stored as the `unique_name` claim.</param>
        /// <param name="roles">Optional array of role names to include as separate role claims; pass null or an empty array to omit role claims.</param>
        /// <returns>Compact serialized JWT string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the JWT secret key is not configured in the application settings.</exception>
        public string GenerateToken(string userId, string username, string[] roles = null)
        {
            // ===== STEP 1: Load JWT Configuration Settings =====
            // Read the JWT settings from appsettings.json under "JwtSettings" section
            var jwtSettings = _configuration.GetSection("JwtSettings");
            
            // SecretKey: Used to sign the token (must be kept secret and at least 32 characters)
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            
            // Issuer: Identifies who created and signed the token (e.g., "CleanArchitecture.ApiTemplate")
            var issuer = jwtSettings["Issuer"];
            
            // Audience: Identifies who the token is intended for (e.g., "CleanArchitecture.ApiTemplate.Api")
            var audience = jwtSettings["Audience"];
            
            // ExpirationMinutes: How long the token remains valid (default: 60 minutes)
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

            // ===== STEP 2: Create Security Key and Signing Credentials =====
            // Convert the secret key string into bytes for cryptographic operations
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            
            // Create signing credentials using HMAC-SHA256 algorithm
            // This ensures the token's integrity and authenticity
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // ===== STEP 3: Build Token Claims (Payload Data) =====
            // Claims are key-value pairs that contain information about the user
            // These will be encoded in the JWT token payload
            var claims = new List<Claim>
            {
                // Standard JWT Claims (registered claim names):
                
                // 'sub' (Subject): Unique identifier for the user
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                
                // 'unique_name': Username for display purposes
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                
                // 'jti' (JWT ID): Unique identifier for this specific token
                // Useful for token revocation and preventing replay attacks
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                
                // 'iat' (Issued At): Unix timestamp when token was created
                // Used for token age validation and security auditing
                new Claim(JwtRegisteredClaimNames.Iat, 
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), 
                    ClaimValueTypes.Integer64)
            };

            // ===== STEP 4: Add Role Claims (Authorization) =====
            // Roles determine what actions the user can perform in the application
            // Multiple roles can be assigned (e.g., "User", "Admin", "Manager")
            if (roles != null && roles.Length > 0)
            {
                foreach (var role in roles)
                {
                    // Add each role as a separate claim
                    // ClaimTypes.Role is a standard claim type recognized by ASP.NET Core authorization
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            // ===== STEP 5: Create the JWT Token =====
            // Combine all components into a complete JWT token structure
            var token = new JwtSecurityToken(
                // Token metadata (who issued it and who it's for)
                issuer: issuer,                    // Who created the token
                audience: audience,                // Who should accept this token
                
                // Token payload (user information and roles)
                claims: claims,                    // All the claims we built above
                
                // Token validity period
                notBefore: DateTime.UtcNow,       // Token is valid starting now
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes), // Token expires after specified minutes
                
                // Security signature (proves token hasn't been tampered with)
                signingCredentials: credentials   // HMAC-SHA256 signature using our secret key
            );

            // ===== STEP 6: Serialize Token to String =====
            // Convert the token object into a compact string format (3 parts separated by dots):
            // 1. Header (algorithm and token type): eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
            // 2. Payload (claims): eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ
            // 3. Signature (cryptographic hash): SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Generates a token for a user with the specified username and assigns the 'User' role.
        /// </summary>
        /// <param name="username">The username for which to generate the token. If not specified, defaults to "testuser".</param>
        /// <returns>A string containing the generated user token.</returns>
        /// <remarks>
        /// Convenience method for generating tokens for regular users.
        /// Creates a token with:
        /// - Random GUID as user ID
        /// - Specified username (or "testuser" if not provided)
        /// - Single role: "User"
        /// 
        /// Usage: var token = tokenGenerator.GenerateUserToken("john.doe");
        /// <summary>
        /// Generates a JWT for a standard user and includes the "User" role.
        /// </summary>
        /// <param name="username">The username to embed in the token; defaults to "testuser".</param>
        /// <returns>The serialized JWT string containing the user's identity and roles.</returns>
        public string GenerateUserToken(string username = "testuser")
        {
            // Generate a user token with basic "User" role
            // This provides standard access to non-administrative features
            return GenerateToken(Guid.NewGuid().ToString(), username, new[] { "User" });
        }

        /// <summary>
        /// Generates a token that grants administrative and user privileges for the specified username.
        /// </summary>
        /// <param name="username">The username for which to generate the admin token. Defaults to "admin" if not specified.</param>
        /// <returns>A string containing the generated token with both user and admin roles assigned.</returns>
        /// <remarks>
        /// Convenience method for generating tokens for administrators.
        /// Creates a token with:
        /// - Random GUID as user ID
        /// - Specified username (or "admin" if not provided)
        /// - Multiple roles: "User" and "Admin"
        /// 
        /// The "User" role is included to ensure admins can access all user features,
        /// while the "Admin" role grants access to administrative endpoints.
        /// 
        /// Usage: var token = tokenGenerator.GenerateAdminToken("admin.user");
        /// <summary>
        /// Generates a JWT for an administrative user.
        /// </summary>
        /// <param name="username">Username to include in the token; defaults to "admin".</param>
        /// <returns>The serialized JWT containing both "User" and "Admin" role claims.</returns>
        public string GenerateAdminToken(string username = "admin")
        {
            // Generate an admin token with both "User" and "Admin" roles
            // Admins inherit all user permissions plus additional administrative capabilities
            return GenerateToken(Guid.NewGuid().ToString(), username, new[] { "User", "Admin" });
        }
    }
}