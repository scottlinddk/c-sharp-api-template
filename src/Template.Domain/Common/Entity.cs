namespace Template.Domain.Common;

/// <summary>Base type for entities identified by a <see cref="Guid"/>.</summary>
public abstract class Entity
{
    public Guid Id { get; protected init; }
}
