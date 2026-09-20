namespace Workes.ContentSystem.Core;

/// <summary>
/// Defines the read order exposed by a content sequence.
/// </summary>
public enum ContentSequenceReadOrder
{
    /// <summary>
    /// Read retained records from oldest to newest.
    /// </summary>
    OldestFirst,

    /// <summary>
    /// Read retained records from newest to oldest.
    /// </summary>
    NewestFirst
}
