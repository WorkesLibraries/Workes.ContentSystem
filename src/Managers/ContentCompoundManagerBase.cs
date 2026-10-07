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

    /// <summary>
    /// Attempts to add a root node with a generated ID.
    /// </summary>
    public bool TryAddRoot(IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => Compound.TryAddRoot(entry, out node, out failure);

    /// <summary>
    /// Adds a root node with a generated ID.
    /// </summary>
    public ContentCompoundNode AddRoot(IContentEntry entry) => Compound.AddRoot(entry);

    /// <summary>
    /// Attempts to add a root node with an explicit ID.
    /// </summary>
    public bool TryAddRoot(TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => Compound.TryAddRoot(id, entry, out node, out failure);

    /// <summary>
    /// Adds a root node with an explicit ID.
    /// </summary>
    public ContentCompoundNode AddRoot(TId id, IContentEntry entry) => Compound.AddRoot(id, entry);

    /// <summary>
    /// Attempts to add a child node with a generated ID.
    /// </summary>
    public bool TryAddChild(TId parentId, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => Compound.TryAddChild(parentId, entry, out node, out failure);

    /// <summary>
    /// Adds a child node with a generated ID.
    /// </summary>
    public ContentCompoundNode AddChild(TId parentId, IContentEntry entry) => Compound.AddChild(parentId, entry);

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
    /// Removes a node by natural ID.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Remove(TId id) => Compound.Remove(id);

    /// <summary>
    /// Attempts to clear all retained nodes.
    /// </summary>
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure) => Compound.TryClear(out removedRecords, out failure);

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
