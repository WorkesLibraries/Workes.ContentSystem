namespace Workes.ContentSystem.Core;

/// <summary>
/// Defines how a single-entry content structure handles an existing retained record.
/// </summary>
public enum ContentSingleReplacementPolicy
{
    /// <summary>
    /// Replace the existing record.
    /// </summary>
    Replace = 0,

    /// <summary>
    /// Reject new records while one is already retained.
    /// </summary>
    Reject
}
