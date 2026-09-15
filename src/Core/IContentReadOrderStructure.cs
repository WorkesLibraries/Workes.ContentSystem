namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content structure that exposes its retained-record read order.
/// </summary>
public interface IContentReadOrderStructure : IContentStructure
{
    /// <summary>
    /// Gets the order used when reading retained records.
    /// </summary>
    ContentSequenceReadOrder ReadOrder { get; }
}
