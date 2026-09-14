using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a simple plain-text content entry.
/// </summary>
public sealed class PlainContentEntry : IContentEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlainContentEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="plainText">The plain text content.</param>
    public PlainContentEntry(DateTimeOffset timestamp, string plainText)
    {
        PlainText = plainText ?? throw new ArgumentNullException(nameof(plainText));
        Timestamp = timestamp;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <inheritdoc />
    public string PlainText { get; }
}
