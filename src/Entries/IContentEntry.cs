using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents one item stored in a content structure.
/// </summary>
public interface IContentEntry
{
    /// <summary>
    /// Gets the time associated with the entry.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}
