using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the shared structure family surface for single-entry content structures.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public abstract class ContentSingleStructureBase<TId> :
    IContentNaturalIdRemovalStructure<TId>,
    IContentClearableStructure
{
    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Records { get; }

    /// <inheritdoc />
    public abstract ContentManagerBase CreateManager();

    /// <summary>
    /// Attempts to set the current entry with a generated ID.
    /// </summary>
    public abstract bool TrySet(IContentEntry entry, out ContentEntryRecord? record, out ContentEntryRecord? replacedRecord, out ContentFailure? failure);

    /// <summary>
    /// Sets the current entry with a generated ID.
    /// </summary>
    public abstract ContentEntryRecord Set(IContentEntry entry);

    /// <summary>
    /// Attempts to set the current entry with an explicit ID.
    /// </summary>
    public abstract bool TrySet(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentEntryRecord? replacedRecord, out ContentFailure? failure);

    /// <summary>
    /// Sets the current entry with an explicit ID.
    /// </summary>
    public abstract ContentEntryRecord Set(TId id, IContentEntry entry);

    /// <summary>
    /// Attempts to get the current record.
    /// </summary>
    public abstract bool TryGetCurrent(out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Gets the current record.
    /// </summary>
    public abstract ContentEntryRecord GetCurrent();

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
/// Provides the long-ID single-entry structure family surface.
/// </summary>
public abstract class ContentSingleStructureBase : ContentSingleStructureBase<long>
{
}
