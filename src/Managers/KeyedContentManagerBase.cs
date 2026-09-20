using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides shared manager behavior for keyed content structures.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public abstract class KeyedContentManagerBase<TId> : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentManagerBase{TId}"/> class.
    /// </summary>
    /// <param name="structure">The keyed structure.</param>
    protected KeyedContentManagerBase(KeyedContentStructureBase<TId> structure)
        : base(structure)
    {
        if (structure is null)
        {
            throw new ArgumentNullException(nameof(structure));
        }
    }

    /// <summary>
    /// Gets the active keyed structure.
    /// </summary>
    protected KeyedContentStructureBase<TId> KeyedStructure => (KeyedContentStructureBase<TId>)Structure;

    /// <summary>
    /// Attempts to add an entry with a caller-provided ID.
    /// </summary>
    public bool TryAdd(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return KeyedStructure.TryAdd(id, entry, out record, out failure);
    }

    /// <summary>
    /// Adds an entry with a caller-provided ID.
    /// </summary>
    public ContentEntryRecord Add(TId id, IContentEntry entry)
    {
        return KeyedStructure.Add(id, entry);
    }

    /// <summary>
    /// Attempts to get a retained record by caller-facing ID.
    /// </summary>
    public bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return KeyedStructure.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by caller-facing ID.
    /// </summary>
    public ContentEntryRecord Get(TId id)
    {
        return KeyedStructure.Get(id);
    }

    /// <summary>
    /// Attempts to remove a retained record by caller-facing ID.
    /// </summary>
    public bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        return KeyedStructure.TryRemove(id, out removedRecord, out failure);
    }

    /// <summary>
    /// Removes a retained record by caller-facing ID.
    /// </summary>
    public ContentEntryRecord Remove(TId id)
    {
        return KeyedStructure.Remove(id);
    }

    /// <summary>
    /// Attempts to clear retained records.
    /// </summary>
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        return KeyedStructure.TryClear(out removedRecords, out failure);
    }

    /// <summary>
    /// Clears retained records.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Clear()
    {
        return KeyedStructure.Clear();
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is KeyedContentStructureBase<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not a keyed-family content structure for the active ID type.");
        return false;
    }
}
