using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a retained compound record with hierarchy metadata for read-only traversal.
/// </summary>
public sealed class ContentCompoundRecordView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundRecordView"/> class.
    /// </summary>
    public ContentCompoundRecordView(
        ContentEntryRecord record,
        ContentEntryId? parentId,
        IEnumerable<ContentEntryId>? childIds,
        int depth)
    {
        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "Compound record depth cannot be negative.");
        }

        Record = record ?? throw new ArgumentNullException(nameof(record));
        ParentId = parentId;
        ChildIds = (childIds ?? Enumerable.Empty<ContentEntryId>()).ToArray();
        Depth = depth;
    }

    /// <summary>
    /// Gets the retained record.
    /// </summary>
    public ContentEntryRecord Record { get; }

    /// <summary>
    /// Gets the parent node ID, or <see langword="null"/> when this is a root node.
    /// </summary>
    public ContentEntryId? ParentId { get; }

    /// <summary>
    /// Gets the child node IDs in the compound structure's configured sibling read order.
    /// </summary>
    public IReadOnlyList<ContentEntryId> ChildIds { get; }

    /// <summary>
    /// Gets the zero-based depth of the record in the compound tree.
    /// </summary>
    public int Depth { get; }

    /// <summary>
    /// Gets a value indicating whether the record is a root node.
    /// </summary>
    public bool IsRoot => ParentId is null;

    /// <summary>
    /// Gets a value indicating whether the record has child nodes.
    /// </summary>
    public bool HasChildren => ChildIds.Count > 0;
}
