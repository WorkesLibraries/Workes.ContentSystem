using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides shared manager behavior for compound content structures.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public abstract class ContentCompoundManagerBase<TId> : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundManagerBase{TId}"/> class.
    /// </summary>
    protected ContentCompoundManagerBase(ContentCompoundStructureBase<TId> structure)
        : base(structure ?? throw new ArgumentNullException(nameof(structure)))
    {
    }

    /// <summary>
    /// Gets the active compound structure.
    /// </summary>
    protected ContentCompoundStructureBase<TId> Compound => (ContentCompoundStructureBase<TId>)Structure;

    /// <summary>Assesses whether the active compound structure supports sorting each sibling group.</summary>
    public ContentPreflightResult AssessSortSiblings(IComparer<ContentEntryRecord> comparer)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        return Structure is IContentCompoundSortableStructure sortable
            ? sortable.AssessSortSiblings(comparer)
            : ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The active compound structure does not support sibling sorting."));
    }

    /// <summary>Attempts to sort roots and each node's children independently.</summary>
    public bool TrySortSiblings(IComparer<ContentEntryRecord> comparer, out bool orderChanged, out ContentFailure? failure)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        if (Structure is not IContentCompoundSortableStructure sortable)
        {
            orderChanged = false;
            failure = ContentFailures.StructureUnsupportedOperation("The active compound structure does not support sibling sorting.");
            return false;
        }
        return sortable.TrySortSiblings(comparer, out orderChanged, out failure);
    }

    /// <summary>Sorts roots and every node's children independently.</summary>
    public bool SortSiblings(IComparer<ContentEntryRecord> comparer)
    {
        if (!TrySortSiblings(comparer, out bool changed, out ContentFailure? failure)) throw new ContentOperationException(failure!);
        return changed;
    }

    /// <summary>Assesses sorting all sibling groups with a comparer selected by parent ID; null identifies roots.</summary>
    public ContentPreflightResult AssessSortSiblings(Func<ContentEntryId?, IComparer<ContentEntryRecord>> comparerSelector)
    {
        if (comparerSelector is null) throw new ArgumentNullException(nameof(comparerSelector));
        return Structure is IContentCompoundSortableStructure sortable
            ? sortable.AssessSortSiblings(comparerSelector)
            : ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The active compound structure does not support sibling sorting."));
    }

    /// <summary>Attempts to sort all sibling groups with a comparer selected by parent ID; null identifies roots.</summary>
    public bool TrySortSiblings(Func<ContentEntryId?, IComparer<ContentEntryRecord>> comparerSelector, out bool orderChanged, out ContentFailure? failure)
    {
        if (comparerSelector is null) throw new ArgumentNullException(nameof(comparerSelector));
        if (Structure is not IContentCompoundSortableStructure sortable)
        {
            orderChanged = false;
            failure = ContentFailures.StructureUnsupportedOperation("The active compound structure does not support sibling sorting.");
            return false;
        }
        return sortable.TrySortSiblings(comparerSelector, out orderChanged, out failure);
    }

    /// <summary>Sorts all sibling groups with a comparer selected by parent ID; null identifies roots.</summary>
    public bool SortSiblings(Func<ContentEntryId?, IComparer<ContentEntryRecord>> comparerSelector)
    {
        if (!TrySortSiblings(comparerSelector, out bool changed, out ContentFailure? failure)) throw new ContentOperationException(failure!);
        return changed;
    }

    /// <summary>Assesses sorting roots (null) or the children of the specified parent.</summary>
    public ContentPreflightResult AssessSortSiblings(ContentEntryId? parentId, IComparer<ContentEntryRecord> comparer)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        return Structure is IContentCompoundSortableStructure sortable
            ? sortable.AssessSortSiblings(parentId, comparer)
            : ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The active compound structure does not support sibling sorting."));
    }

    /// <summary>Attempts to sort roots (null) or the children of the specified parent.</summary>
    public bool TrySortSiblings(ContentEntryId? parentId, IComparer<ContentEntryRecord> comparer, out bool orderChanged, out ContentFailure? failure)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        if (Structure is not IContentCompoundSortableStructure sortable)
        {
            orderChanged = false;
            failure = ContentFailures.StructureUnsupportedOperation("The active compound structure does not support sibling sorting.");
            return false;
        }
        return sortable.TrySortSiblings(parentId, comparer, out orderChanged, out failure);
    }

    /// <summary>Sorts roots (null) or the children of the specified parent.</summary>
    public bool SortSiblings(ContentEntryId? parentId, IComparer<ContentEntryRecord> comparer)
    {
        if (!TrySortSiblings(parentId, comparer, out bool changed, out ContentFailure? failure)) throw new ContentOperationException(failure!);
        return changed;
    }

    /// <summary>
    /// Attempts to add a root node with a generated ID.
    /// </summary>
    public bool TryAddRoot(IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => Compound.TryAddRoot(entry, out node, out failure);

    /// <summary>
    /// Assesses whether a root node can be added with a generated ID without committing the add.
    /// </summary>
    public ContentPreflightResult AssessAddRoot(IContentEntry entry) => Compound.AssessAddRoot(entry);

    /// <summary>
    /// Adds a root node with a generated ID.
    /// </summary>
    public ContentCompoundNode AddRoot(IContentEntry entry) => Compound.AddRoot(entry);

    /// <summary>
    /// Attempts to add a root node with an explicit ID.
    /// </summary>
    public bool TryAddRoot(TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => Compound.TryAddRoot(id, entry, out node, out failure);

    /// <summary>
    /// Assesses whether a root node can be added with an explicit ID without committing the add.
    /// </summary>
    public ContentPreflightResult AssessAddRoot(TId id, IContentEntry entry) => Compound.AssessAddRoot(id, entry);

    /// <summary>
    /// Adds a root node with an explicit ID.
    /// </summary>
    public ContentCompoundNode AddRoot(TId id, IContentEntry entry) => Compound.AddRoot(id, entry);

    /// <summary>Assesses adding multiple root nodes atomically.</summary>
    public ContentPreflightResult AssessAddRoots(IEnumerable<IContentEntry> entries)
    {
        IReadOnlyList<IContentEntry> batch = ContentBatchInputs.ReadEntries(entries, nameof(entries));
        return Structure is IContentCompoundBatchStructure<TId> batchStructure
            ? batchStructure.AssessAddRoots(batch)
            : ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The active compound structure does not support atomic root batches."));
    }

    /// <summary>Attempts to add multiple root nodes atomically.</summary>
    public bool TryAddRoots(IEnumerable<IContentEntry> entries, out IReadOnlyList<ContentCompoundNode> nodes, out ContentFailure? failure)
    {
        IReadOnlyList<IContentEntry> batch = ContentBatchInputs.ReadEntries(entries, nameof(entries));
        if (Structure is IContentCompoundBatchStructure<TId> batchStructure) return batchStructure.TryAddRoots(batch, out nodes, out failure);
        nodes = Array.Empty<ContentCompoundNode>(); failure = ContentFailures.StructureUnsupportedOperation("The active compound structure does not support atomic root batches."); return false;
    }

    /// <summary>Adds multiple root nodes atomically.</summary>
    public IReadOnlyList<ContentCompoundNode> AddRoots(IEnumerable<IContentEntry> entries)
    {
        if (TryAddRoots(entries, out IReadOnlyList<ContentCompoundNode> nodes, out ContentFailure? failure)) return nodes;
        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Attempts to add a child node with a generated ID.
    /// </summary>
    public bool TryAddChild(TId parentId, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => Compound.TryAddChild(parentId, entry, out node, out failure);

    /// <summary>
    /// Assesses whether a child node can be added with a generated ID without committing the add.
    /// </summary>
    public ContentPreflightResult AssessAddChild(TId parentId, IContentEntry entry) => Compound.AssessAddChild(parentId, entry);

    /// <summary>
    /// Adds a child node with a generated ID.
    /// </summary>
    public ContentCompoundNode AddChild(TId parentId, IContentEntry entry) => Compound.AddChild(parentId, entry);

    /// <summary>Assesses adding multiple children to one parent atomically.</summary>
    public ContentPreflightResult AssessAddChildren(TId parentId, IEnumerable<IContentEntry> entries)
    {
        IReadOnlyList<IContentEntry> batch = ContentBatchInputs.ReadEntries(entries, nameof(entries));
        return Structure is IContentCompoundBatchStructure<TId> batchStructure
            ? batchStructure.AssessAddChildren(parentId, batch)
            : ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The active compound structure does not support atomic child batches."));
    }

    /// <summary>Attempts to add multiple children to one parent atomically.</summary>
    public bool TryAddChildren(TId parentId, IEnumerable<IContentEntry> entries, out IReadOnlyList<ContentCompoundNode> nodes, out ContentFailure? failure)
    {
        IReadOnlyList<IContentEntry> batch = ContentBatchInputs.ReadEntries(entries, nameof(entries));
        if (Structure is IContentCompoundBatchStructure<TId> batchStructure) return batchStructure.TryAddChildren(parentId, batch, out nodes, out failure);
        nodes = Array.Empty<ContentCompoundNode>(); failure = ContentFailures.StructureUnsupportedOperation("The active compound structure does not support atomic child batches."); return false;
    }

    /// <summary>Adds multiple children to one parent atomically.</summary>
    public IReadOnlyList<ContentCompoundNode> AddChildren(TId parentId, IEnumerable<IContentEntry> entries)
    {
        if (TryAddChildren(parentId, entries, out IReadOnlyList<ContentCompoundNode> nodes, out ContentFailure? failure)) return nodes;
        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Attempts to add a child node under the supplied parent node with a generated ID.
    /// </summary>
    public bool TryAddChild(ContentCompoundNode parent, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        return Compound.TryAddChild(parent.Record.Id, entry, out node, out failure);
    }

    /// <summary>
    /// Assesses whether a child node can be added under the supplied parent node without committing the add.
    /// </summary>
    public ContentPreflightResult AssessAddChild(ContentCompoundNode parent, IContentEntry entry)
    {
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        return Compound.AssessAddChild(parent.Record.Id, entry);
    }

    /// <summary>
    /// Adds a child node under the supplied parent node with a generated ID.
    /// </summary>
    public ContentCompoundNode AddChild(ContentCompoundNode parent, IContentEntry entry)
    {
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        return Compound.AddChild(parent.Record.Id, entry);
    }

    /// <summary>
    /// Attempts to add a child node with an explicit ID.
    /// </summary>
    public bool TryAddChild(TId parentId, TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => Compound.TryAddChild(parentId, id, entry, out node, out failure);

    /// <summary>
    /// Assesses whether a child node can be added with an explicit ID without committing the add.
    /// </summary>
    public ContentPreflightResult AssessAddChild(TId parentId, TId id, IContentEntry entry) => Compound.AssessAddChild(parentId, id, entry);

    /// <summary>
    /// Adds a child node with an explicit ID.
    /// </summary>
    public ContentCompoundNode AddChild(TId parentId, TId id, IContentEntry entry) => Compound.AddChild(parentId, id, entry);

    /// <summary>
    /// Attempts to add a child node under the supplied parent node with an explicit ID.
    /// </summary>
    public bool TryAddChild(ContentCompoundNode parent, TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        return Compound.TryAddChild(parent.Record.Id, id, entry, out node, out failure);
    }

    /// <summary>
    /// Assesses whether a child node can be added under the supplied parent node with an explicit ID without committing the add.
    /// </summary>
    public ContentPreflightResult AssessAddChild(ContentCompoundNode parent, TId id, IContentEntry entry)
    {
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        return Compound.AssessAddChild(parent.Record.Id, id, entry);
    }

    /// <summary>
    /// Adds a child node under the supplied parent node with an explicit ID.
    /// </summary>
    public ContentCompoundNode AddChild(ContentCompoundNode parent, TId id, IContentEntry entry)
    {
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        return Compound.AddChild(parent.Record.Id, id, entry);
    }

    /// <summary>
    /// Attempts to get a node by natural ID.
    /// </summary>
    public bool TryGetNode(TId id, out ContentCompoundNode? node, out ContentFailure? failure) => Compound.TryGetNode(id, out node, out failure);

    /// <summary>
    /// Gets a node by natural ID.
    /// </summary>
    public ContentCompoundNode GetNode(TId id) => Compound.GetNode(id);

    /// <summary>
    /// Gets root nodes in configured sibling read order.
    /// </summary>
    public IReadOnlyList<ContentCompoundNode> GetRoots() => Compound.GetRoots();

    /// <summary>
    /// Gets child nodes for the parent ID in configured sibling read order.
    /// </summary>
    public IReadOnlyList<ContentCompoundNode> GetChildren(TId parentId) => Compound.GetChildren(parentId);

    /// <summary>
    /// Gets child nodes for the supplied parent node in configured sibling read order.
    /// </summary>
    public IReadOnlyList<ContentCompoundNode> GetChildren(ContentCompoundNode parent)
    {
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        return Compound.GetChildren(parent.Record.Id);
    }

    /// <summary>
    /// Attempts to get the supplied node's parent.
    /// </summary>
    public bool TryGetParent(ContentCompoundNode node, out ContentCompoundNode? parent, out ContentFailure? failure)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.ParentId is null)
        {
            parent = null;
            failure = ContentFailures.EntryNotFound($"Compound node '{node.Record.Id}' has no parent.", node.Record.Id.ToString());
            return false;
        }

        return Compound.TryGetNode(node.ParentId.Value, out parent, out failure);
    }

    /// <summary>
    /// Gets the supplied node's parent.
    /// </summary>
    public ContentCompoundNode GetParent(ContentCompoundNode node)
    {
        if (TryGetParent(node, out ContentCompoundNode? parent, out ContentFailure? failure))
        {
            return parent!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Gets retained records with hierarchy metadata in deterministic depth-first order.
    /// </summary>
    public IReadOnlyList<ContentCompoundRecordView> GetRecordViews() => Compound.GetRecordViews();

    /// <summary>
    /// Attempts to get a retained record by natural ID.
    /// </summary>
    public bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure) => Compound.TryGet(id, out record, out failure);

    /// <summary>
    /// Gets a retained record by natural ID.
    /// </summary>
    public ContentEntryRecord Get(TId id) => Compound.Get(id);

    /// <summary>
    /// Attempts to remove a node by natural ID.
    /// </summary>
    public bool TryRemove(TId id, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure) => Compound.TryRemove(id, out removedRecords, out failure);

    /// <summary>
    /// Assesses whether a node can be removed by natural ID without committing the removal.
    /// </summary>
    public ContentPreflightResult AssessRemove(TId id) => Compound.AssessRemove(id);

    /// <summary>
    /// Removes a node by natural ID.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Remove(TId id) => Compound.Remove(id);

    /// <summary>Assesses predicate-based removal over the active depth-first record snapshot.</summary>
    public ContentPreflightResult AssessRemoveWhere(Func<ContentEntryRecord, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return Structure is IContentPredicateRemovalStructure removal ? removal.AssessRemoveWhere(predicate)
            : ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The active compound structure does not support predicate removal."));
    }

    /// <summary>Attempts predicate-based removal over the pre-mutation depth-first record snapshot.</summary>
    public bool TryRemoveWhere(Func<ContentEntryRecord, bool> predicate, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        if (Structure is IContentPredicateRemovalStructure removal) return removal.TryRemoveWhere(predicate, out removedRecords, out failure);
        removedRecords = Array.Empty<ContentEntryRecord>(); failure = ContentFailures.StructureUnsupportedOperation("The active compound structure does not support predicate removal."); return false;
    }

    /// <summary>Removes records selected by a predicate over the pre-mutation depth-first snapshot.</summary>
    public IReadOnlyList<ContentEntryRecord> RemoveWhere(Func<ContentEntryRecord, bool> predicate)
    {
        if (TryRemoveWhere(predicate, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)) return removedRecords;
        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Attempts to clear all retained nodes.
    /// </summary>
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure) => Compound.TryClear(out removedRecords, out failure);

    /// <summary>
    /// Assesses whether all retained nodes can be cleared without committing the clear.
    /// </summary>
    public ContentPreflightResult AssessClear() => Compound.AssessClear();

    /// <summary>
    /// Clears all retained nodes.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Clear() => Compound.Clear();

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentCompoundStructureBase<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not a compound content structure for the active ID type.");
        return false;
    }
}

/// <summary>
/// Provides shared manager behavior for long-ID compound content structures.
/// </summary>
public abstract class ContentCompoundManagerBase : ContentCompoundManagerBase<long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundManagerBase"/> class.
    /// </summary>
    protected ContentCompoundManagerBase(ContentCompoundStructureBase<long> structure)
        : base(structure)
    {
    }
}
