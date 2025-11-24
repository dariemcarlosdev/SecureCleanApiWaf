namespace CleanArchitecture.ApiTemplate.Core.Domain.Entities
{
    /// <summary>
    /// Represents the root entity of an aggregate in a domain-driven design context.
    /// </summary>
    /// <remarks>An aggregate root serves as the entry point for accessing and modifying related entities
    /// within an aggregate. All changes to the aggregate should be performed through the aggregate root to maintain
    /// consistency and enforce business invariants.</remarks>
    internal interface IAggregateRoot
    {
        // Intentionally left blank to serve as a marker interface for aggregate roots.
    }
}
