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
    /// Adds a root node with an explicit ID.
    /// </summary>
    public abstract ContentCompoundNode AddRoot(TId id, IContentEntry entry);

    /// <summary>
    /// Attempts to add a child node with a generated ID.
    /// </summary>
    public abstract bool TryAddChild(TId parentId, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Adds a child node with a generated ID.
    /// </summary>
    public abstract ContentCompoundNode AddChild(TId parentId, IContentEntry entry);

    /// <summary>
    /// Attempts to add a child node with an explicit ID.
    /// </summary>
    public abstract bool TryAddChild(TId parentId, TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure);

    /// <summary>
    /// Adds a child node with an explicit ID.
    /// </summary>
    public abstract ContentCompoundNode AddChild(TId parentId, TId id, IContentEntry entry);

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
    /// Attempts to remove a node by natural ID.
    /// </summary>
    public abstract bool TryRemove(TId id, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

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

    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Clear();
}

/// <summary>
/// Provides the long-ID compound structure family surface.
/// </summary>
public abstract class ContentCompoundStructureBase : ContentCompoundStructureBase<long>
{
}
