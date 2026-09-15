namespace Workes.ContentSystem.Core;

/// <summary>
/// Identifies the high-level kind of committed content change.
/// </summary>
public enum ContentChangeKind
{
    /// <summary>
    /// The change kind is unspecified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Records were added.
    /// </summary>
    Added,

    /// <summary>
    /// Records were removed.
    /// </summary>
    Removed,

    /// <summary>
    /// All retained records were cleared.
    /// </summary>
    Cleared,

    /// <summary>
    /// Runtime content configuration changed.
    /// </summary>
    ConfigurationChanged
}
