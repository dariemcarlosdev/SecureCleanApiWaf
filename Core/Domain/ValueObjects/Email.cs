using CleanArchitecture.ApiTemplate.Core.Domain.Exceptions;
using System.Net.Mail;

namespace CleanArchitecture.ApiTemplate.Core.Domain.ValueObjects
{
    /// <summary>
    /// Represents an email address value object with built-in validation.
    /// </summary>
    /// <remarks>
    /// This value object encapsulates email address validation logic and ensures
    /// that only valid email addresses can exist in the domain model.
    /// 
    /// Why Value Object for Email?
    /// - **Type Safety**: Prevents primitive obsession (using string everywhere)
    /// - **Validation**: Centralized email validation logic
    /// - **Immutability**: Email cannot be changed after creation
    /// - **Domain Language**: Makes code more expressive and readable
    /// - **Reusability**: Validation logic in one place
    /// 
    /// Benefits:
    /// ```csharp
    /// // Before (Primitive Obsession)
    /// public class User
    /// {
    ///     public string Email { get; set; } // Any string accepted!
    /// }
    /// 
    /// // After (Value Object)
    /// public class User
    /// {
    ///     public Email Email { get; private set; } // Only valid emails!
    /// }
    /// ```
    /// 
    /// Usage Examples:
    /// ```csharp
    /// // Creating valid email
    /// var email = Email.Create("user@example.com");
    /// 
    /// // Will throw DomainException
    /// try
    /// {
    ///     var invalidEmail = Email.Create("not-an-email");
    /// }
    /// catch (DomainException ex)
    /// {
    ///     Console.WriteLine(ex.Message); // "Invalid email format"
    /// }
    /// 
    /// // Equality comparison
    /// var email1 = Email.Create("USER@EXAMPLE.COM");
    /// var email2 = Email.Create("user@example.com");
    /// Console.WriteLine(email1 == email2); // True (case-insensitive)
    /// 
    /// // Using in collections
    /// var uniqueEmails = new HashSet<Email>
    /// {
    ///     Email.Create("user1@example.com"),
    ///     Email.Create("user2@example.com")
    /// };
    /// ```
    /// 
    /// Entity Framework Configuration:
    /// ```csharp
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.Entity<User>()
    ///         .Property(u => u.Email)
    ///         .HasConversion(
    ///             email => email.Value, // To database
    ///             value => Email.Create(value) // From database
    ///         )
    ///         .HasMaxLength(320) // RFC 5321 max length
    ///         .IsRequired();
    ///         
    ///     // Add unique index
    ///     modelBuilder.Entity<User>()
    ///         .HasIndex(u => u.Email)
    ///         .IsUnique();
    /// }
    /// ```
    /// </remarks>
    public class Email : ValueObject
    {
        /// <summary>
        /// Gets the email address value.
        /// </summary>
        /// <remarks>
        /// Always stored in lowercase for consistent comparison and querying.
        /// Maximum length: 320 characters (per RFC 5321):
        /// - Local part: 64 characters max
        /// - @ symbol: 1 character
        /// - Domain: 255 characters max
        /// </remarks>
        public string Value { get; private set; }

        /// <summary>
        /// Private constructor to enforce factory method usage.
        /// </summary>
        /// <param name="value">The validated email address.</param>
        /// <remarks>
        /// Private constructor ensures that emails can only be created
        /// through the Create factory method, which performs validation.
        /// This is a key pattern in Domain-Driven Design.
        /// <summary>
        /// Initializes a new <see cref="Email"/> instance with a validated, normalized email value.
        /// </summary>
        /// <param name="value">The validated, lowercase email address to store as the value object.</param>
        private Email(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new Email value object with validation.
        /// </summary>
        /// <param name="email">The email address string to validate and create.</param>
        /// <returns>A validated Email value object.</returns>
        /// <exception cref="DomainException">Thrown when the email is invalid.</exception>
        /// <remarks>
        /// Validation Rules:
        /// 1. Must not be null, empty, or whitespace
        /// 2. Must be valid email format (uses System.Net.Mail.MailAddress)
        /// 3. Must not exceed 320 characters
        /// 4. Normalized to lowercase for consistency
        /// 
        /// Examples:
        /// ```csharp
        /// // Valid emails
        /// Email.Create("user@example.com")         ?
        /// Email.Create("user.name@example.com")    ?
        /// Email.Create("user+tag@example.co.uk")   ?
        /// Email.Create("user123@sub.example.com")  ?
        /// 
        /// // Invalid emails (throw DomainException)
        /// Email.Create("")                         ? Empty
        /// Email.Create("notanemail")               ? No @ symbol
        /// Email.Create("@example.com")             ? No local part
        /// Email.Create("user@")                    ? No domain
        /// Email.Create("user name@example.com")    ? Space in local part
        /// ```
        /// 
        /// Performance Note:
        /// Uses System.Net.Mail.MailAddress for validation, which is
        /// comprehensive but not the fastest option. For high-throughput
        /// scenarios, consider regex-based validation.
        /// <summary>
        /// Creates an Email value object from the provided string after validating and normalizing it.
        /// </summary>
        /// <param name="email">The email address to validate and convert; leading and trailing whitespace are ignored.</param>
        /// <returns>An Email instance containing the validated email stored in lowercase.</returns>
        /// <exception cref="DomainException">Thrown if the input is null, empty, or whitespace; if the trimmed address exceeds 320 characters; or if the address has an invalid format.</exception>
        public static Email Create(string email)
        {
            // Validation 1: Check for null or empty
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new DomainException("Email address cannot be empty");
            }

            // Trim whitespace
            email = email.Trim();

            // Validation 2: Check length (RFC 5321)
            if (email.Length > 320)
            {
                throw new DomainException(
                    "Email address cannot exceed 320 characters");
            }

            // Validation 3: Format validation using MailAddress
            if (!IsValidEmail(email))
            {
                throw new DomainException(
                    $"Invalid email format: {email}");
            }

            // Normalize to lowercase for consistent storage and comparison
            return new Email(email.ToLowerInvariant());
        }

        /// <summary>
        /// Validates whether a string is a valid email address format.
        /// </summary>
        /// <param name="email">The email string to validate.</param>
        /// <returns>True if valid email format, false otherwise.</returns>
        /// <remarks>
        /// Uses System.Net.Mail.MailAddress for comprehensive validation.
        /// This validates according to RFC 5322 (Internet Message Format).
        /// 
        /// Validation includes:
        /// - Valid characters in local part
        /// - Valid characters in domain
        /// - Proper @ symbol placement
        /// - Valid domain structure
        /// - Quoted strings support
        /// - IP address domains (e.g., user@[192.168.1.1])
        /// 
        /// Alternative Validation (Regex):
        /// ```csharp
        /// private static readonly Regex EmailRegex = new Regex(
        ///     @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        ///     RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// 
        /// private static bool IsValidEmail(string email)
        /// {
        ///     return EmailRegex.IsMatch(email);
        /// }
        /// ```
        /// 
        /// Note: Regex is faster but less comprehensive than MailAddress.
        /// <summary>
        /// Determines whether the provided string is a syntactically valid email address and contains only the address portion (no display name).
        /// </summary>
        /// <param name="email">The candidate email string to validate.</param>
        /// <returns>`true` if the string is a valid email address and matches the parsed address exactly, `false` otherwise.</returns>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                
                // MailAddress parses "display name <email@example.com>"
                // We only want the email part
                return mailAddress.Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the local part of the email address (before @).
        /// </summary>
        /// <returns>The local part of the email.</returns>
        /// <example>
        /// For "user.name@example.com", returns "user.name"
        /// <summary>
        /// Gets the local part (the substring before the '@') of the email value.
        /// </summary>
        /// <returns>The substring before the '@', or the entire email value if the address has no local part (missing or starting with '@').</returns>
        public string GetLocalPart()
        {
            var atIndex = Value.IndexOf('@');
            return atIndex > 0 ? Value[..atIndex] : Value;
        }

        /// <summary>
        /// Gets the domain part of the email address (after @).
        /// </summary>
        /// <returns>The domain part of the email.</returns>
        /// <example>
        /// For "user@example.com", returns "example.com"
        /// <summary>
        /// Gets the domain portion of the email (the substring after the '@').
        /// </summary>
        /// <returns>The domain part of the email, or an empty string if the email has no '@' or no domain portion.</returns>
        public string GetDomain()
        {
            var atIndex = Value.IndexOf('@');
            return atIndex > 0 && atIndex < Value.Length - 1
                ? Value[(atIndex + 1)..]
                : string.Empty;
        }

        /// <summary>
        /// Checks if the email belongs to a specific domain.
        /// </summary>
        /// <param name="domain">The domain to check (case-insensitive).</param>
        /// <returns>True if email domain matches, false otherwise.</returns>
        /// <example>
        /// ```csharp
        /// var email = Email.Create("user@example.com");
        /// email.IsFromDomain("example.com");     // True
        /// email.IsFromDomain("EXAMPLE.COM");     // True (case-insensitive)
        /// email.IsFromDomain("subdomain.example.com"); // False
        /// ```
        /// <summary>
        /// Determines whether the email's domain equals the specified domain (case-insensitive).
        /// </summary>
        /// <param name="domain">The domain to compare against (leading/trailing whitespace is ignored).</param>
        /// <returns>`true` if the email's domain matches `domain` (case-insensitive), `false` otherwise.</returns>
        public bool IsFromDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            return GetDomain().Equals(
                domain.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a masked version of the email for display purposes.
        /// </summary>
        /// <returns>A masked email address.</returns>
        /// <remarks>
        /// Useful for privacy-conscious displays, confirmations, and logs.
        /// 
        /// Masking Strategy:
        /// - Show first 2 characters of local part
        /// - Mask middle with asterisks
        /// - Show last 2 characters of local part
        /// - Keep full domain visible
        /// 
        /// Examples:
        /// ```csharp
        /// "user@example.com"           ? "us**@example.com"
        /// "john.doe@example.com"       ? "jo****oe@example.com"
        /// "a@example.com"              ? "a@example.com"
        /// "ab@example.com"             ? "ab@example.com"
        /// "longusername@example.com"   ? "lo********me@example.com"
        /// ```
        /// <summary>
        /// Produces a privacy-preserving display form of the email by masking part of the local part.
        /// </summary>
        /// <remarks>
        /// If the local part has 3 or fewer characters, the original email value is returned unchanged.
        /// For longer local parts, the method keeps the first two and last two characters, replaces the middle with asterisks (at least two), and preserves the domain.
        /// Examples: "ab@example.com" -> "ab@example.com", "username@example.com" -> "us****me@example.com".
        /// </remarks>
        /// <returns>The masked email string suitable for display.</returns>
        public string ToMaskedString()
        {
            var localPart = GetLocalPart();
            var domain = GetDomain();

            if (localPart.Length <= 3)
            {
                // Don't mask very short local parts
                return Value;
            }

            var firstTwo = localPart[..2];
            var lastTwo = localPart.Length >= 4
                ? localPart[^2..]
                : string.Empty;

            var maskLength = Math.Max(2, localPart.Length - 4);
            var mask = new string('*', maskLength);

            return $"{firstTwo}{mask}{lastTwo}@{domain}";
        }

        /// <summary>
        /// Returns the email address string.
        /// </summary>
        /// <summary>
        /// Gets the underlying normalized email string.
        /// </summary>
        /// <returns>The normalized email address stored by this instance.</returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Gets the equality components for value object comparison.
        /// </summary>
        /// <returns>An enumerable of components (email value).</returns>
        /// <remarks>
        /// Emails are compared by their normalized value (lowercase).
        /// "USER@EXAMPLE.COM" == "user@example.com" returns true.
        /// <summary>
        /// Provides the sequence of components that define this value object's equality.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{object}"/> that yields the email's normalized value.</returns>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        /// <summary>
        /// Implicit conversion from Email to string.
        /// </summary>
        /// <param name="email">The email value object.</param>
        public static implicit operator string(Email email)
        {
            return email.Value;
        }
    }
}