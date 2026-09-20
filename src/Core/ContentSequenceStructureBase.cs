using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the shared structure family surface for sequence-like content structures.
/// </summary>
public abstract class ContentSequenceStructureBase :
    IStructureAssignedIdContentStructure<long>,
    IContentNaturalIdRemovalStructure<long>,
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

    /// <inheritdoc />
    public abstract bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Get(ContentEntryId id);

    /// <inheritdoc />
    public abstract bool TryGet(long id, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Get(long id);

    /// <inheritdoc />
    public abstract bool TryRemove(ContentEntryId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Remove(ContentEntryId id);

    /// <inheritdoc />
    public abstract bool TryRemove(long id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract ContentEntryRecord Remove(long id);

    /// <inheritdoc />
    public abstract bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

    /// <inheritdoc />
    public abstract IReadOnlyList<ContentEntryRecord> Clear();
}
