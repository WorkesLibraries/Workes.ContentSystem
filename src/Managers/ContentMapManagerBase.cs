using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides shared manager behavior for map content structures.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public abstract class ContentMapManagerBase<TId> : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentMapManagerBase{TId}"/> class.
    /// </summary>
    /// <param name="structure">The map structure.</param>
    protected ContentMapManagerBase(ContentMapStructureBase<TId> structure)
        : base(structure)
    {
        if (structure is null)
        {
            throw new ArgumentNullException(nameof(structure));
        }
    }

    /// <summary>
    /// Gets the active map structure.
    /// </summary>
    protected ContentMapStructureBase<TId> MapStructure => (ContentMapStructureBase<TId>)Structure;

    /// <summary>
    /// Attempts to add an entry with a caller-provided ID.
    /// </summary>
    public bool TryAdd(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return MapStructure.TryAdd(id, entry, out record, out failure);
    }

    /// <summary>
    /// Adds an entry with a caller-provided ID.
    /// </summary>
    public ContentEntryRecord Add(TId id, IContentEntry entry)
    {
        return MapStructure.Add(id, entry);
    }

    /// <summary>
    /// Attempts to set an entry with a caller-provided ID, replacing an existing record when present.
    /// </summary>
    public bool TrySet(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentEntryRecord? replacedRecord, out ContentFailure? failure)
    {
        return MapStructure.TrySet(id, entry, out record, out replacedRecord, out failure);
    }

    /// <summary>
    /// Sets an entry with a caller-provided ID, replacing an existing record when present.
    /// </summary>
    public ContentEntryRecord Set(TId id, IContentEntry entry)
    {
        return MapStructure.Set(id, entry);
    }

    /// <summary>
    /// Attempts to get an existing record, or creates one with the supplied factory when the ID is missing.
    /// </summary>
    public bool TryGetOrSet(
        TId id,
        Func<IContentEntry> entryFactory,
        out ContentEntryRecord? record,
        out bool added,
        out ContentFailure? failure)
    {
        if (entryFactory is null)
        {
            throw new ArgumentNullException(nameof(entryFactory));
        }

        if (TryGet(id, out record, out failure))
        {
            added = false;
            return true;
        }

        if (failure is not null && failure.Code != ContentFailureCodes.EntryNotFound)
        {
            added = false;
            return false;
        }

        IContentEntry entry = entryFactory() ?? throw new InvalidOperationException("The entry factory returned null.");
        if (TryAdd(id, entry, out record, out failure))
        {
            added = true;
            return true;
        }

        added = false;
        return false;
    }

    /// <summary>
    /// Gets an existing record, or creates one with the supplied factory when the ID is missing.
    /// </summary>
    public ContentEntryRecord GetOrSet(TId id, Func<IContentEntry> entryFactory)
    {
        if (TryGetOrSet(id, entryFactory, out ContentEntryRecord? record, out _, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Determines whether the map retains a record with the supplied caller-facing ID.
    /// </summary>
    public bool Contains(TId id)
    {
        return TryGet(id, out _, out _);
    }

    /// <summary>
    /// Attempts to get a retained record by caller-facing ID.
    /// </summary>
    public bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return MapStructure.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by caller-facing ID.
    /// </summary>
    public ContentEntryRecord Get(TId id)
    {
        return MapStructure.Get(id);
    }

    /// <summary>
    /// Attempts to remove a retained record by caller-facing ID.
    /// </summary>
    public bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        return MapStructure.TryRemove(id, out removedRecord, out failure);
    }

    /// <summary>
    /// Removes a retained record by caller-facing ID.
    /// </summary>
    public ContentEntryRecord Remove(TId id)
    {
        return MapStructure.Remove(id);
    }

    /// <summary>
    /// Attempts to clear retained records.
    /// </summary>
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        return MapStructure.TryClear(out removedRecords, out failure);
    }

    /// <summary>
    /// Clears retained records.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Clear()
    {
        return MapStructure.Clear();
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentMapStructureBase<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not a map-family content structure for the active ID type.");
        return false;
    }
}
