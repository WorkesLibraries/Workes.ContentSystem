using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides shared manager behavior for single-entry content structures.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public abstract class ContentSingleManagerBase<TId> : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSingleManagerBase{TId}"/> class.
    /// </summary>
    protected ContentSingleManagerBase(ContentSingleStructureBase<TId> structure)
        : base(structure ?? throw new ArgumentNullException(nameof(structure)))
    {
    }

    /// <summary>
    /// Gets the active single-entry structure.
    /// </summary>
    protected ContentSingleStructureBase<TId> Single => (ContentSingleStructureBase<TId>)Structure;

    /// <summary>
    /// Attempts to set the current entry with a generated ID.
    /// </summary>
    public bool TrySet(IContentEntry entry, out ContentEntryRecord? record, out ContentEntryRecord? replacedRecord, out ContentFailure? failure)
    {
        return Single.TrySet(entry, out record, out replacedRecord, out failure);
    }

    /// <summary>
    /// Sets the current entry with a generated ID.
    /// </summary>
    public ContentEntryRecord Set(IContentEntry entry)
    {
        return Single.Set(entry);
    }

    /// <summary>
    /// Attempts to set the current entry with an explicit ID.
    /// </summary>
    public bool TrySet(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentEntryRecord? replacedRecord, out ContentFailure? failure)
    {
        return Single.TrySet(id, entry, out record, out replacedRecord, out failure);
    }

    /// <summary>
    /// Sets the current entry with an explicit ID.
    /// </summary>
    public ContentEntryRecord Set(TId id, IContentEntry entry)
    {
        return Single.Set(id, entry);
    }

    /// <summary>
    /// Attempts to get the current record.
    /// </summary>
    public bool TryGetCurrent(out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return Single.TryGetCurrent(out record, out failure);
    }

    /// <summary>
    /// Gets the current record.
    /// </summary>
    public ContentEntryRecord GetCurrent()
    {
        return Single.GetCurrent();
    }

    /// <summary>
    /// Attempts to get a retained record by natural ID.
    /// </summary>
    public bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return Single.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by natural ID.
    /// </summary>
    public ContentEntryRecord Get(TId id)
    {
        return Single.Get(id);
    }

    /// <summary>
    /// Attempts to remove a retained record by natural ID.
    /// </summary>
    public bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        return Single.TryRemove(id, out removedRecord, out failure);
    }

    /// <summary>
    /// Removes a retained record by natural ID.
    /// </summary>
    public ContentEntryRecord Remove(TId id)
    {
        return Single.Remove(id);
    }

    /// <summary>
    /// Attempts to clear the current record.
    /// </summary>
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        return Single.TryClear(out removedRecords, out failure);
    }

    /// <summary>
    /// Clears the current record.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Clear()
    {
        return Single.Clear();
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentSingleStructureBase<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not a single-entry content structure for the active ID type.");
        return false;
    }
}

/// <summary>
/// Provides shared manager behavior for long-ID single-entry content structures.
/// </summary>
public abstract class ContentSingleManagerBase : ContentSingleManagerBase<long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSingleManagerBase"/> class.
    /// </summary>
    protected ContentSingleManagerBase(ContentSingleStructureBase<long> structure)
        : base(structure)
    {
    }
}
