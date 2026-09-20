using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Identifies a content entry within the structure that assigned the ID.
/// </summary>
public readonly struct ContentEntryId : IEquatable<ContentEntryId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentEntryId"/> struct.
    /// </summary>
    /// <param name="value">The structure-assigned entry ID value.</param>
    public ContentEntryId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Content entry ID cannot be null or empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the structure-assigned entry ID value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a content entry ID from a string value.
    /// </summary>
    /// <param name="value">The structure-assigned entry ID value.</param>
    /// <returns>The created content entry ID.</returns>
    public static ContentEntryId FromString(string value)
    {
        return new ContentEntryId(value);
    }

    /// <summary>
    /// Determines whether two content entry IDs are equal.
    /// </summary>
    /// <param name="left">The first content entry ID.</param>
    /// <param name="right">The second content entry ID.</param>
    /// <returns><see langword="true"/> when the IDs are equal.</returns>
    public static bool operator ==(ContentEntryId left, ContentEntryId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two content entry IDs are not equal.
    /// </summary>
    /// <param name="left">The first content entry ID.</param>
    /// <param name="right">The second content entry ID.</param>
    /// <returns><see langword="true"/> when the IDs are not equal.</returns>
    public static bool operator !=(ContentEntryId left, ContentEntryId right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Converts a content entry ID to its string value.
    /// </summary>
    /// <param name="id">The content entry ID.</param>
    public static explicit operator string(ContentEntryId id)
    {
        return id.Value;
    }

    /// <summary>
    /// Determines whether this ID is equal to another ID.
    /// </summary>
    /// <param name="other">The other ID.</param>
    /// <returns><see langword="true"/> when the IDs are equal.</returns>
    public bool Equals(ContentEntryId other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ContentEntryId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    /// <summary>
    /// Returns the structure-assigned entry ID value.
    /// </summary>
    /// <returns>The entry ID value.</returns>
    public override string ToString()
    {
        return Value;
    }
}
