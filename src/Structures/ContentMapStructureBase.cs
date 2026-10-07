using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the shared structure family surface for map content structures.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public abstract class ContentMapStructureBase<TId> :
    IContentMapRecordRemovalStructure<TId>,
    IContentClearableStructure
{
    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Records { get; }

    /// <inheritdoc />
    public abstract ContentManagerBase CreateManager();

    /// <summary>
    /// Assesses whether an entry can be added with the supplied ID without committing the add.
    /// </summary>
    public abstract ContentPreflightResult AssessAdd(TId id, IContentEntry entry);

    /// <inheritdoc />
    public abstract bool TryAdd(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Add(TId id, IContentEntry entry);

    /// <summary>
    /// Assesses whether an entry can be set with the supplied ID without committing the set.
    /// </summary>
    public abstract ContentPreflightResult AssessSet(TId id, IContentEntry entry);

    /// <summary>
    /// Assesses whether GetOrSet would succeed for the supplied ID without invoking an entry factory.
    /// </summary>
    public abstract ContentPreflightResult AssessGetOrSet(TId id);

    /// <inheritdoc />
    public abstract bool TrySet(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentEntryRecord? replacedRecord, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Set(TId id, IContentEntry entry);

    /// <inheritdoc />
    public abstract bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Get(ContentEntryId id);

    /// <inheritdoc />
    public abstract bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Get(TId id);

    /// <inheritdoc />
    public abstract bool TryRemove(ContentEntryId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a retained record can be removed by normalized ID without committing the removal.
    /// </summary>
    public abstract ContentPreflightResult AssessRemove(ContentEntryId id);

    /// <inheritdoc />
    public abstract ContentEntryRecord Remove(ContentEntryId id);

    /// <inheritdoc />
    public abstract bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a retained record can be removed by caller-facing ID without committing the removal.
    /// </summary>
    public abstract ContentPreflightResult AssessRemove(TId id);

    /// <inheritdoc />
    public abstract ContentEntryRecord Remove(TId id);

    /// <inheritdoc />
    public abstract bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether retained records can be cleared without committing the clear.
    /// </summary>
    public abstract ContentPreflightResult AssessClear();

    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Clear();
}
