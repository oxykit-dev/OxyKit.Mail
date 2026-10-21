using System.ComponentModel.DataAnnotations;

namespace OxyKit.Mail.Infrastructure.Entities;

/// <summary>
/// Base class for all domain entities providing common properties.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the entity was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
