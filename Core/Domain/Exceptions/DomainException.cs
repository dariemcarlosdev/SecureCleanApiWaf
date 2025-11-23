namespace SecureCleanApiWaf.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a domain rule is violated or invalid business operation is attempted.
    /// </summary>
    /// <remarks>
    /// Domain exceptions represent violations of business rules and invariants.
    /// They are part of the domain model and express business concepts.
    /// 
    /// When to use DomainException:
    /// - Business rule violations (e.g., "Cannot place order with zero items")
    /// - Invalid state transitions (e.g., "Cannot cancel a completed order")
    /// - Invariant violations (e.g., "Email must be unique")
    /// - Value object validation failures (e.g., "Invalid email format")
    /// 
    /// When NOT to use DomainException:
    /// - Infrastructure failures (use specific exceptions like DbException)
    /// - Validation failures at API layer (use validation attributes)
    /// - Authentication/Authorization failures (use UnauthorizedException)
    /// - External service failures (use specific HTTP exceptions)
    /// 
    /// Best Practices:
    /// 1. Use descriptive, business-oriented messages
    ///    ? "Order cannot be shipped without a delivery address"
    ///    ? "DeliveryAddress is null"
    /// 
    /// 2. Include relevant context in the message
    ///    ? "Cannot refund order {OrderId} because it's still pending"
    ///    ? "Invalid operation"
    /// 
    /// 3. Throw early in domain methods
    ///    ```csharp
    ///    public void Ship()
    ///    {
    ///        if (Status != OrderStatus.Confirmed)
    ///            throw new DomainException($"Cannot ship order in {Status} status");
    ///            
    ///        // shipping logic...
    ///    }
    ///    ```
    /// 
    /// 4. Catch and handle appropriately in application layer
    ///    ```csharp
    ///    try
    ///    {
    ///        order.Ship();
    ///    }
    ///    catch (DomainException ex)
    ///    {
    ///        return Result<T>.Fail(ex.Message);
    ///    }
    ///    ```
    /// 
    /// Architecture Layer Placement:
    /// ```
    /// ???????????????????????????????????????
    /// ?     Presentation Layer              ?
    /// ?  (Catches & maps to HTTP responses) ?
    /// ???????????????????????????????????????
    ///                  ?
    /// ???????????????????????????????????????
    /// ?     Application Layer               ?
    /// ?  (Catches & wraps in Result<T>)     ?
    /// ???????????????????????????????????????
    ///                  ?
    /// ???????????????????????????????????????
    /// ?     Domain Layer                    ?
    /// ?  (Throws DomainException) ? HERE    ?
    /// ???????????????????????????????????????
    /// ```
    /// 
    /// Error Handling Example:
    /// ```csharp
    /// // Domain Layer
    /// public class User : BaseEntity
    /// {
    ///     public void Deactivate()
    ///     {
    ///         if (Status == UserStatus.Deleted)
    ///             throw new DomainException("Cannot deactivate a deleted user");
    ///             
    ///         Status = UserStatus.Inactive;
    ///     }
    /// }
    /// 
    /// // Application Layer
    /// public async Task<Result<Unit>> Handle(DeactivateUserCommand request)
    /// {
    ///     try
    ///     {
    ///         var user = await _repository.GetByIdAsync(request.UserId);
    ///         user.Deactivate();
    ///         await _repository.SaveChangesAsync();
    ///         return Result<Unit>.Ok(Unit.Value);
    ///     }
    ///     catch (DomainException ex)
    ///     {
    ///         return Result<Unit>.Fail(ex.Message);
    ///     }
    /// }
    /// 
    /// // Presentation Layer
    /// [HttpPost("users/{id}/deactivate")]
    /// public async Task<IActionResult> Deactivate(Guid id)
    /// {
    ///     var result = await _mediator.Send(new DeactivateUserCommand(id));
    ///     
    ///     if (!result.Success)
    ///         return BadRequest(new { error = result.Error });
    ///         
    ///     return Ok();
    /// }
    /// ```
    /// </remarks>
    public class DomainException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the business rule violation.</param>
        /// <remarks>
        /// Message should be:
        /// - Clear and descriptive
        /// - Written in business language
        /// - Safe to display to users (no technical details)
        /// - Actionable (user knows what to fix)
        /// <summary>
        /// Initializes a new DomainException with a descriptive business-rule violation message.
        /// </summary>
        /// <param name="message">A user-facing message describing the domain rule violation.</param>
        public DomainException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <remarks>
        /// Use this constructor when a domain exception is triggered by another exception.
        /// Example:
        /// ```csharp
        /// try
        /// {
        ///     var email = Email.Parse(emailString);
        /// }
        /// catch (FormatException ex)
        /// {
        ///     throw new DomainException("Invalid email format", ex);
        /// }
        /// ```
        /// <summary>
        /// Initializes a new DomainException with a descriptive message and an underlying cause.
        /// </summary>
        /// <param name="message">A descriptive, user-facing message that explains the domain rule violation.</param>
        /// <param name="innerException">The underlying exception that caused this domain exception, if any.</param>
        public DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a requested entity cannot be found.
    /// </summary>
    /// <remarks>
    /// Specific type of domain exception for "not found" scenarios.
    /// Useful for differentiated error handling (404 vs 400 responses).
    /// 
    /// Usage Example:
    /// ```csharp
    /// public async Task<User> GetByIdAsync(Guid id)
    /// {
    ///     var user = await _context.Users.FindAsync(id);
    ///     
    ///     if (user == null)
    ///         throw new EntityNotFoundException(nameof(User), id);
    ///         
    ///     return user;
    /// }
    /// ```
    /// </remarks>
    public class EntityNotFoundException : DomainException
    {
        /// <summary>
        /// Gets the name of the entity type that was not found.
        /// </summary>
        public string EntityName { get; }

        /// <summary>
        /// Gets the identifier of the entity that was not found.
        /// </summary>
        public object EntityId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
        /// </summary>
        /// <param name="entityName">The name of the entity type (e.g., "User", "Order").</param>
        /// <summary>
        /// Initializes a new <see cref="EntityNotFoundException"/> for a missing entity and composes a descriptive message.
        /// </summary>
        /// <param name="entityName">The type or name of the entity that could not be located.</param>
        /// <param name="entityId">The identifier value of the missing entity.</param>
        public EntityNotFoundException(string entityName, object entityId)
            : base($"{entityName} with ID '{entityId}' was not found")
        {
            EntityName = entityName;
            EntityId = entityId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a custom message.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of <see cref="EntityNotFoundException"/> with a custom message.
        /// </summary>
        /// <param name="message">Custom error message describing the not-found condition.</param>
        /// <remarks>When constructed through this overload, <see cref="EntityName"/> and <see cref="EntityId"/> are initialized to empty strings.</remarks>
        public EntityNotFoundException(string message)
            : base(message)
        {
            EntityName = string.Empty;
            EntityId = string.Empty;
        }
    }

    /// <summary>
    /// Exception thrown when an invalid operation is attempted on a domain entity.
    /// </summary>
    /// <remarks>
    /// Used for state-dependent operations that cannot be performed in current state.
    /// 
    /// Usage Example:
    /// ```csharp
    /// public void Cancel()
    /// {
    ///     if (Status == OrderStatus.Completed)
    ///         throw new InvalidDomainOperationException(
    ///             "Cannot cancel order", 
    ///             "Order is already completed");
    ///             
    ///     Status = OrderStatus.Cancelled;
    /// }
    /// ```
    /// </remarks>
    public class InvalidDomainOperationException : DomainException
    {
        /// <summary>
        /// Gets the reason why the operation is invalid.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDomainOperationException"/> class.
        /// </summary>
        /// <param name="operation">The operation that was attempted.</param>
        /// <summary>
        /// Initializes a new <see cref="InvalidDomainOperationException"/> for an attempted operation that is invalid for the specified reason.
        /// </summary>
        /// <param name="operation">The name or description of the operation that was attempted.</param>
        /// <param name="reason">The reason why the operation is invalid.</param>
        /// <remarks>The exception message is composed as "&lt;operation&gt;: &lt;reason&gt;".</remarks>
        public InvalidDomainOperationException(string operation, string reason)
            : base($"{operation}: {reason}")
        {
            Reason = reason;
        }
    }
}