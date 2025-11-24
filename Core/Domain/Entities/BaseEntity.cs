namespace CleanArchitecture.ApiTemplate.Core.Domain.Entities
{
    /// <summary>
    /// Base entity class that provides common properties for all domain entities.
    /// Implements audit fields and soft delete functionality.
    /// </summary>
    /// <remarks>
    /// All domain entities should inherit from this class to ensure consistent:
    /// - Identity management (Guid-based IDs)
    /// - Audit tracking (Created/Updated timestamps)
    /// - Soft delete support (for data retention compliance)
    /// 
    /// Design Principles:
    /// - Immutable ID: Set once during creation, never changed
    /// - Protected setters: Ensures encapsulation and controlled mutations
    /// - UTC timestamps: For consistent global time handling
    /// - Soft delete pattern: Preserves data for auditing and recovery
    /// 
    /// Usage Example:
    /// ```csharp
    /// public class User : BaseEntity
    /// {
    ///     public string Username { get; private set; }
    ///     
    ///     public static User Create(string username)
    ///     {
    ///         return new User 
    ///         { 
    ///             Id = Guid.NewGuid(),
    ///             Username = username,
    ///             CreatedAt = DateTime.UtcNow
    ///         };
    ///     }
    /// }
    /// ```
    /// </remarks>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Gets the unique identifier for this entity.
        /// </summary>
        /// <remarks>
        /// Uses Guid for several advantages:
        /// - Globally unique across distributed systems
        /// - Generated client-side (no DB round-trip)
        /// - Non-sequential (security benefit)
        /// - Suitable for distributed architectures
        /// </remarks>
        public Guid Id { get; protected set; }

        /// <summary>
        /// Gets the timestamp when this entity was created.
        /// </summary>
        /// <remarks>
        /// Always stored in UTC to prevent timezone confusion.
        /// Set once during entity creation and never modified.
        /// Useful for:
        /// - Auditing and compliance
        /// - Sorting by creation order
        /// - Data retention policies
        /// - Debugging and troubleshooting
        /// </remarks>
        public DateTime CreatedAt { get; protected set; }

        /// <summary>
        /// Gets the timestamp when this entity was last updated.
        /// </summary>
        /// <remarks>
        /// Null indicates entity has never been modified since creation.
        /// Updated automatically by domain methods or infrastructure interceptors.
        /// Used for:
        /// - Change tracking
        /// - Optimistic concurrency control
        /// - Cache invalidation
        /// - Sync conflict resolution
        /// </remarks>
        public DateTime? UpdatedAt { get; protected set; }

        /// <summary>
        /// Gets the timestamp when this entity was soft-deleted.
        /// </summary>
        /// <remarks>
        /// Null indicates entity is active (not deleted).
        /// Soft delete preserves data for:
        /// - Regulatory compliance (GDPR, SOX, HIPAA)
        /// - Audit trails
        /// - Data recovery
        /// - Historical reporting
        /// </remarks>
        public DateTime? DeletedAt { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this entity is soft-deleted.
        /// </summary>
        /// <remarks>
        /// Quick boolean check for deleted status.
        /// Should be used in all queries to filter out deleted entities:
        /// ```csharp
        /// var activeUsers = context.Users.Where(u => !u.IsDeleted);
        /// ```
        /// </remarks>
        public bool IsDeleted { get; protected set; }

        /// <summary>
        /// Marks this entity as deleted without physically removing it from the database.
        /// </summary>
        /// <remarks>
        /// Soft delete pattern provides several benefits:
        /// - Maintains referential integrity
        /// - Preserves audit history
        /// - Allows data recovery
        /// - Complies with data retention regulations
        /// 
        /// After soft delete:
        /// - IsDeleted becomes true
        /// - DeletedAt is set to current UTC time
        /// - UpdatedAt is also updated
        /// - Entity can be restored using Restore() method
        /// 
        /// Implementation Note:
        /// Infrastructure layer should configure global query filters
        /// to automatically exclude soft-deleted entities from queries.
        /// <summary>
        /// Marks the entity as soft-deleted.
        /// </summary>
        /// <remarks>
        /// Sets <see cref="IsDeleted"/> to true and records <see cref="DeletedAt"/> and <see cref="UpdatedAt"/> with the current UTC time.
        /// </remarks>
        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Restores a previously soft-deleted entity to active status.
        /// </summary>
        /// <remarks>
        /// Restores entity by:
        /// - Setting IsDeleted to false
        /// - Clearing DeletedAt timestamp
        /// - Updating UpdatedAt timestamp
        /// 
        /// Use cases:
        /// - User accidentally deleted data
        /// - Undelete/undo functionality
        /// - Regulatory restoration requirements
        /// - Testing and development scenarios
        /// 
        /// Note: Only works for soft-deleted entities.
        /// Hard-deleted (physically removed) entities cannot be restored.
        /// <summary>
        /// Restores a soft-deleted entity by clearing its deletion state and updating its last-modified time.
        /// </summary>
        /// <remarks>
        /// Sets <see cref="IsDeleted"/> to false, clears <see cref="DeletedAt"/>, and sets <see cref="UpdatedAt"/> to the current UTC time.
        /// </remarks>
        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Determines whether two entities are equal based on their IDs.
        /// </summary>
        /// <param name="obj">The entity to compare with the current entity.</param>
        /// <returns>true if the specified entity is equal to the current entity; otherwise, false.</returns>
        /// <remarks>
        /// Entity equality is determined by ID comparison only.
        /// Two entities with the same ID are considered the same entity,
        /// regardless of their other property values.
        /// 
        /// This follows Domain-Driven Design (DDD) principles where
        /// entities are defined by their identity, not their attributes.
        /// <summary>
        /// Determines whether the specified object represents the same domain entity by comparing concrete types and non-empty identifiers.
        /// </summary>
        /// <returns>`true` if <paramref name="obj"/> is a BaseEntity of the same concrete type and both entities have the same non-empty Id; `false` otherwise.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is not BaseEntity other)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            if (Id == Guid.Empty || other.Id == Guid.Empty)
                return false;

            return Id == other.Id;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current entity.</returns>
        /// <remarks>
        /// Hash code is based on the entity's ID.
        /// This ensures consistent hashing for entities with the same ID.
        /// <summary>
        /// Gets the hash code for the entity derived from its Id.
        /// </summary>
        /// <returns>The hash code computed from the entity's Id.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determines whether two entity instances are equal using the == operator.
        /// </summary>
        public static bool operator ==(BaseEntity? left, BaseEntity? right)
        {
            if (left is null && right is null)
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two entity instances are not equal using the != operator.
        /// </summary>
        public static bool operator !=(BaseEntity? left, BaseEntity? right)
        {
            return !(left == right);
        }
    }
}