using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a retained node in a compound content tree.
/// </summary>
public sealed class ContentCompoundNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundNode"/> class.
    /// </summary>
    public ContentCompoundNode(
        ContentEntryRecord record,
        ContentEntryId? parentId,
        IEnumerable<ContentEntryId>? childIds)
    {
        Record = record ?? throw new ArgumentNullException(nameof(record));
        ParentId = parentId;
        ChildIds = (childIds ?? Enumerable.Empty<ContentEntryId>()).ToArray();
    }

    /// <summary>
    /// Gets the retained record stored by the node.
    /// </summary>
    public ContentEntryRecord Record { get; }

    /// <summary>
    /// Gets the parent node ID, or <see langword="null"/> when this is a root node.
    /// </summary>
    public ContentEntryId? ParentId { get; }

    /// <summary>
    /// Gets the node's child IDs in the structure's configured sibling read order.
    /// </summary>
    public IReadOnlyList<ContentEntryId> ChildIds { get; }
}
