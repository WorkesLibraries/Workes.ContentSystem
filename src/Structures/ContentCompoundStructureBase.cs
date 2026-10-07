using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the shared structure family surface for compound content structures.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public abstract class ContentCompoundStructureBase<TId> : IContentClearableStructure
{
    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Records { get; }

    /// <inheritdoc />
    public abstract ContentManagerBase CreateManager();

    /// <summary>
    /// Assesses whether a root node can be added with a generated ID without committing the add.
    /// </summary>
    public abstract ContentPreflightResult AssessAddRoot(IContentEntry entry);

    /// <summary>
    /// Attempts to add a root node with a generated ID.
    /// </summary>
    public abstract bool TryAddRoot(IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Adds a root node with a generated ID.
    /// </summary>
    public abstract ContentCompoundNode AddRoot(IContentEntry entry);

    /// <summary>
    /// Attempts to add a root node with an explicit ID.
    /// </summary>
    public abstract bool TryAddRoot(TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a root node can be added with an explicit ID without committing the add.
    /// </summary>
    public abstract ContentPreflightResult AssessAddRoot(TId id, IContentEntry entry);

    /// <summary>
    /// Adds a root node with an explicit ID.
    /// </summary>
    public abstract ContentCompoundNode AddRoot(TId id, IContentEntry entry);

    /// <summary>
    /// Attempts to add a child node with a generated ID.
    /// </summary>
    public abstract bool TryAddChild(TId parentId, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a child node can be added with a generated ID without committing the add.
    /// </summary>
    public abstract ContentPreflightResult AssessAddChild(TId parentId, IContentEntry entry);

    /// <summary>
    /// Adds a child node with a generated ID.
    /// </summary>
    public abstract ContentCompoundNode AddChild(TId parentId, IContentEntry entry);

    /// <summary>
    /// Attempts to add a child node with a generated ID under a normalized parent ID.
    /// </summary>
    public abstract bool TryAddChild(ContentEntryId parentId, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a child node can be added under a normalized parent ID without committing the add.
    /// </summary>
    public abstract ContentPreflightResult AssessAddChild(ContentEntryId parentId, IContentEntry entry);

    /// <summary>
    /// Adds a child node with a generated ID under a normalized parent ID.
    /// </summary>
    public abstract ContentCompoundNode AddChild(ContentEntryId parentId, IContentEntry entry);

    /// <summary>
    /// Attempts to add a child node with an explicit ID.
    /// </summary>
    public abstract bool TryAddChild(TId parentId, TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a child node can be added with an explicit ID without committing the add.
    /// </summary>
    public abstract ContentPreflightResult AssessAddChild(TId parentId, TId id, IContentEntry entry);

    /// <summary>
    /// Adds a child node with an explicit ID.
    /// </summary>
    public abstract ContentCompoundNode AddChild(TId parentId, TId id, IContentEntry entry);

    /// <summary>
    /// Attempts to add a child node with an explicit ID under a normalized parent ID.
    /// </summary>
    public abstract bool TryAddChild(ContentEntryId parentId, TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a child node can be added under a normalized parent ID with an explicit ID without committing the add.
    /// </summary>
    public abstract ContentPreflightResult AssessAddChild(ContentEntryId parentId, TId id, IContentEntry entry);

    /// <summary>
    /// Adds a child node with an explicit ID under a normalized parent ID.
    /// </summary>
    public abstract ContentCompoundNode AddChild(ContentEntryId parentId, TId id, IContentEntry entry);

    /// <summary>
    /// Attempts to get a node by stored ID.
    /// </summary>
    public abstract bool TryGetNode(ContentEntryId id, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Gets a node by stored ID.
    /// </summary>
    public abstract ContentCompoundNode GetNode(ContentEntryId id);

    /// <summary>
    /// Attempts to get a node by natural ID.
    /// </summary>
    public abstract bool TryGetNode(TId id, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Gets a node by natural ID.
    /// </summary>
    public abstract ContentCompoundNode GetNode(TId id);

    /// <summary>
    /// Gets root nodes in configured sibling read order.
    /// </summary>
    public abstract IReadOnlyList<ContentCompoundNode> GetRoots();

    /// <summary>
    /// Gets child nodes for the parent ID in configured sibling read order.
    /// </summary>
    public abstract IReadOnlyList<ContentCompoundNode> GetChildren(TId parentId);

    /// <summary>
    /// Gets child nodes for the normalized parent ID in configured sibling read order.
    /// </summary>
    public abstract IReadOnlyList<ContentCompoundNode> GetChildren(ContentEntryId parentId);

    /// <summary>
    /// Gets retained records with hierarchy metadata in deterministic depth-first order.
    /// </summary>
    public abstract IReadOnlyList<ContentCompoundRecordView> GetRecordViews();

    /// <summary>
    /// Attempts to remove a node by natural ID.
    /// </summary>
    public abstract bool TryRemove(TId id, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a node can be removed by natural ID without committing the removal.
    /// </summary>
    public abstract ContentPreflightResult AssessRemove(TId id);

    /// <summary>
    /// Removes a node by natural ID.
    /// </summary>
    public abstract IReadOnlyList<ContentEntryRecord> Remove(TId id);

    /// <inheritdoc />
    public abstract bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Get(ContentEntryId id);

    /// <summary>
    /// Attempts to get a retained record by natural ID.
    /// </summary>
    public abstract bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Gets a retained record by natural ID.
    /// </summary>
    public abstract ContentEntryRecord Get(TId id);

    /// <inheritdoc />
    public abstract bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether all retained nodes can be cleared without committing the clear.
    /// </summary>
    public abstract ContentPreflightResult AssessClear();

    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Clear();
}

/// <summary>
/// Provides the long-ID compound structure family surface.
/// </summary>
public abstract class ContentCompoundStructureBase : ContentCompoundStructureBase<long>
{
}
