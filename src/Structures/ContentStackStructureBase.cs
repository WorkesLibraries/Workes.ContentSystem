using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the shared structure family surface for stack content structures.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public abstract class ContentStackStructureBase<TId> :
    IContentNaturalIdRemovalStructure<TId>,
    IContentClearableStructure
{
    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Records { get; }

    /// <inheritdoc />
    public abstract ContentManagerBase CreateManager();

    /// <summary>
    /// Assesses whether an entry can be pushed with a generated ID without committing the push.
    /// </summary>
    public abstract ContentPreflightResult AssessPush(IContentEntry entry);

    /// <summary>
    /// Attempts to push an entry with a generated ID.
    /// </summary>
    public abstract bool TryPush(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Pushes an entry with a generated ID.
    /// </summary>
    public abstract ContentEntryRecord Push(IContentEntry entry);

    /// <summary>
    /// Attempts to push an entry with an explicit ID.
    /// </summary>
    public abstract bool TryPush(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether an entry can be pushed with an explicit ID without committing the push.
    /// </summary>
    public abstract ContentPreflightResult AssessPush(TId id, IContentEntry entry);

    /// <summary>
    /// Pushes an entry with an explicit ID.
    /// </summary>
    public abstract ContentEntryRecord Push(TId id, IContentEntry entry);

    /// <summary>
    /// Attempts to peek at the top record.
    /// </summary>
    public abstract bool TryPeek(out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a peek can currently commit.
    /// </summary>
    public abstract ContentPreflightResult AssessPeek();

    /// <summary>
    /// Peeks at the top record.
    /// </summary>
    public abstract ContentEntryRecord Peek();

    /// <summary>
    /// Attempts to pop the top record.
    /// </summary>
    public abstract bool TryPop(out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Assesses whether a pop can currently commit without committing the pop.
    /// </summary>
    public abstract ContentPreflightResult AssessPop();

    /// <summary>
    /// Pops the top record.
    /// </summary>
    public abstract ContentEntryRecord Pop();

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
    /// Assesses whether a retained record can be removed by natural ID without committing the removal.
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

/// <summary>
/// Provides the long-ID stack structure family surface.
/// </summary>
public abstract class ContentStackStructureBase : ContentStackStructureBase<long>
{
}
