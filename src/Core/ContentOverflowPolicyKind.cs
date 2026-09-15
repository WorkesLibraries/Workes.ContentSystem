namespace Workes.ContentSystem.Core;

/// <summary>
/// Identifies the overflow behavior used by a content sequence.
/// </summary>
public enum ContentOverflowPolicyKind
{
    /// <summary>
    /// Retain all records; no overflow occurs.
    /// </summary>
    None,

    /// <summary>
    /// Drop the oldest retained record when capacity is full.
    /// </summary>
    DropOldest
}
