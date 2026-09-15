using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Defines how a content sequence handles retention and overflow.
/// </summary>
public sealed class ContentOverflowPolicy : IEquatable<ContentOverflowPolicy>
{
    private static readonly ContentOverflowPolicy s_none = new ContentOverflowPolicy(ContentOverflowPolicyKind.None, null);

    private ContentOverflowPolicy(ContentOverflowPolicyKind kind, int? capacity)
    {
        Kind = kind;
        Capacity = capacity;
    }

    /// <summary>
    /// Gets an overflow policy that retains all records.
    /// </summary>
    public static ContentOverflowPolicy None => s_none;

    /// <summary>
    /// Gets the policy kind.
    /// </summary>
    public ContentOverflowPolicyKind Kind { get; }

    /// <summary>
    /// Gets the maximum retained record count for bounded policies.
    /// </summary>
    public int? Capacity { get; }

    /// <summary>
    /// Creates a policy that drops the oldest retained record when capacity is full.
    /// </summary>
    /// <param name="capacity">The maximum number of records to retain.</param>
    /// <returns>The overflow policy.</returns>
    public static ContentOverflowPolicy DropOldest(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity values must be greater than zero.");
        }

        return new ContentOverflowPolicy(ContentOverflowPolicyKind.DropOldest, capacity);
    }

    /// <inheritdoc />
    public bool Equals(ContentOverflowPolicy? other)
    {
        if (other is null)
        {
            return false;
        }

        return Kind == other.Kind && Capacity == other.Capacity;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as ContentOverflowPolicy);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Kind * 397) ^ Capacity.GetHashCode();
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Capacity.HasValue ? $"{Kind}({Capacity.Value})" : Kind.ToString();
    }
}
