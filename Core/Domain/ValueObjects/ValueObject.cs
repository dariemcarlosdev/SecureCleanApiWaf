namespace SecureCleanApiWaf.Core.Domain.ValueObjects
{
    /// <summary>
    /// Base class for all value objects in the domain layer.
    /// Value objects are immutable and defined by their attributes rather than identity.
    /// </summary>
    /// <remarks>
    /// Value Objects vs Entities:
    /// - **Entities**: Have identity, mutable, compared by ID
    /// - **Value Objects**: No identity, immutable, compared by value
    /// 
    /// Characteristics of Value Objects:
    /// - Immutable: Once created, cannot be changed
    /// - Equality by value: Two value objects with same values are equal
    /// - No identity: Don't have an ID property
    /// - Side-effect free: Methods return new instances instead of modifying
    /// 
    /// Examples of Value Objects:
    /// - Email address
    /// - Money (amount + currency)
    /// - Date range
    /// - Address
    /// - Phone number
    /// - Color
    /// 
    /// Benefits:
    /// - Encapsulates validation logic
    /// - Prevents primitive obsession
    /// - Makes domain model more expressive
    /// - Thread-safe due to immutability
    /// - Can be shared across entities safely
    /// 
    /// Implementation Guide:
    /// ```csharp
    /// public class Email : ValueObject
    /// {
    ///     public string Value { get; private set; }
    ///     
    ///     private Email(string value)
    ///     {
    ///         Value = value;
    ///     }
    ///     
    ///     public static Email Create(string email)
    ///     {
    ///         if (string.IsNullOrWhiteSpace(email))
    ///             throw new DomainException("Email cannot be empty");
    ///             
    ///         if (!IsValidEmail(email))
    ///             throw new DomainException("Invalid email format");
    ///             
    ///         return new Email(email.ToLowerInvariant());
    ///     }
    ///     
    ///     protected override IEnumerable<object> GetEqualityComponents()
    ///     {
    ///         yield return Value;
    ///     }
    /// }
    /// ```
    /// 
    /// Design Pattern Reference:
    /// This implements the Value Object pattern from Domain-Driven Design (DDD).
    /// See: Eric Evans - "Domain-Driven Design" (2003)
    /// </remarks>
    public abstract class ValueObject
    {
        /// <summary>
        /// Gets the atomic values that define this value object.
        /// Used for equality comparison.
        /// </summary>
        /// <returns>An enumerable of components that define the value object's equality.</returns>
        /// <remarks>
        /// Subclasses must implement this method to return all properties
        /// that should be used for equality comparison.
        /// 
        /// Example:
        /// ```csharp
        /// protected override IEnumerable<object> GetEqualityComponents()
        /// {
        ///     yield return Amount;
        ///     yield return Currency;
        /// }
        /// ```
        /// 
        /// Order matters! Components should be yielded in a consistent order
        /// for proper equality comparison.
        /// <summary>
/// Provides the sequence of values that define this value object's identity for equality comparison.
/// </summary>
/// <returns>
/// An ordered sequence of component values used to determine equality; elements may be null and the order of items is significant.
/// </returns>
        protected abstract IEnumerable<object> GetEqualityComponents();

        /// <summary>
        /// Determines whether the specified value object is equal to the current value object.
        /// </summary>
        /// <param name="obj">The object to compare with the current value object.</param>
        /// <returns>true if the specified value object is equal to the current value object; otherwise, false.</returns>
        /// <remarks>
        /// Equality is determined by comparing all equality components.
        /// Two value objects are equal if all their components are equal.
        /// <summary>
        /// Determines whether this value object is equal to another object of the same runtime type by comparing their equality components.
        /// </summary>
        /// <param name="obj">The object to compare with the current value object.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="ValueObject"/> of the same runtime type and all equality components are equal; <c>false</c> otherwise.</returns>
        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ValueObject)obj;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        /// <summary>
        /// Serves as the default hash function for value objects.
        /// </summary>
        /// <returns>A hash code for the current value object.</returns>
        /// <remarks>
        /// Hash code is computed from all equality components.
        /// This ensures that equal value objects have the same hash code.
        /// 
        /// Important for:
        /// - Dictionary/HashSet usage
        /// - Entity Framework change tracking
        /// - Caching mechanisms
        /// <summary>
        /// Produces a hash code that represents the value object's equality-defining components.
        /// </summary>
        /// <returns>An integer hash code derived from the value object's equality components.</returns>
        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// Determines whether two value object instances are equal using the == operator.
        /// </summary>
        public static bool operator ==(ValueObject? left, ValueObject? right)
        {
            if (left is null && right is null)
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two value object instances are not equal using the != operator.
        /// </summary>
        public static bool operator !=(ValueObject? left, ValueObject? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Creates a copy of this value object.
        /// </summary>
        /// <returns>A new instance with the same component values.</returns>
        /// <remarks>
        /// Since value objects are immutable, this performs a memberwise clone.
        /// Useful when you need a new instance for modifications in derived classes.
        /// <summary>
        /// Creates a shallow copy of the current value object instance.
        /// </summary>
        /// <remarks>
        /// The copy is a new instance of the same runtime type containing the same component values; reference-type components are copied by reference.
        /// </remarks>
        /// <returns>A new ValueObject instance with the same component values as the original.</returns>
        protected ValueObject GetCopy()
        {
            return (ValueObject)MemberwiseClone();
        }
    }
}