using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Describes one runtime-tunable content structure parameter.
/// </summary>
public sealed class ContentParameterDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentParameterDefinition"/> class.
    /// </summary>
    /// <param name="id">The stable parameter ID.</param>
    /// <param name="valueType">The expected value type.</param>
    /// <param name="description">A short developer-facing description.</param>
    public ContentParameterDefinition(string id, Type valueType, string description)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Parameter ID cannot be null or empty.", nameof(id));
        }

        Id = id;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Gets the stable parameter ID.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the expected value type for the parameter.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    /// Gets a short developer-facing description.
    /// </summary>
    public string Description { get; }
}
