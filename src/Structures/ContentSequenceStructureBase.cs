using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the shared structure family surface for sequence-like content structures.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public abstract class ContentSequenceStructureBase<TId> :
    IStructureAssignedIdContentStructure<TId>,
    IContentNaturalIdRemovalStructure<TId>,
    IContentClearableStructure
{
    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Records { get; }

    /// <inheritdoc />
    public abstract ContentManagerBase CreateManager();

    /// <inheritdoc />
    public abstract bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Add(IContentEntry entry);

    /// <summary>
    /// Attempts to add an entry with an explicit sequence ID.
    /// </summary>
    public abstract bool TryAdd(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Adds an entry with an explicit sequence ID.
    /// </summary>
    public abstract ContentEntryRecord Add(TId id, IContentEntry entry);

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

    /// <inheritdoc />
    public abstract ContentEntryRecord Remove(ContentEntryId id);

    /// <inheritdoc />
    public abstract bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Remove(TId id);

    /// <inheritdoc />
    public abstract bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Clear();
}

/// <summary>
/// Provides the long-ID sequence structure family surface.
/// </summary>
public abstract class ContentSequenceStructureBase : ContentSequenceStructureBase<long>
{
}
