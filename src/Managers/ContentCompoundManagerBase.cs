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
    /// Attempts to add a child node with an explicit ID.
    /// </summary>
    public bool TryAddChild(TId parentId, TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => Compound.TryAddChild(parentId, id, entry, out node, out failure);

    /// <summary>
    /// Adds a child node with an explicit ID.
    /// </summary>
    public ContentCompoundNode AddChild(TId parentId, TId id, IContentEntry entry) => Compound.AddChild(parentId, id, entry);

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
