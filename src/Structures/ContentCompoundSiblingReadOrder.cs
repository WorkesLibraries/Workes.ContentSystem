namespace Workes.ContentSystem.Core;

/// <summary>
/// Defines the sibling read order used by compound content structures.
/// </summary>
public enum ContentCompoundSiblingReadOrder
{
    /// <summary>
    /// Reads siblings in insertion order.
    /// </summary>
    OldestFirst = 0,

    /// <summary>
    /// Reads siblings in reverse insertion order.
    /// </summary>
    NewestFirst = 1
}
