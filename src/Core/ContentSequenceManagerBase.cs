using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides shared manager behavior for sequence-like content structures.
/// </summary>
public abstract class ContentSequenceManagerBase : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceManagerBase"/> class.
    /// </summary>
    /// <param name="structure">The sequence structure.</param>
    protected ContentSequenceManagerBase(ContentSequenceStructureBase structure)
        : base(structure)
    {
        if (structure is null)
        {
            throw new ArgumentNullException(nameof(structure));
        }
    }

    /// <summary>
    /// Gets the active sequence structure.
    /// </summary>
    protected ContentSequenceStructureBase Sequence => (ContentSequenceStructureBase)Structure;

    /// <summary>
    /// Attempts to add an entry and return the retained record created for it.
    /// </summary>
    public bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return Sequence.TryAdd(entry, out record, out failure);
    }

    /// <summary>
    /// Adds an entry and returns the retained record created for it.
    /// </summary>
    public ContentEntryRecord Add(IContentEntry entry)
    {
        return Sequence.Add(entry);
    }

    /// <summary>
    /// Attempts to get a retained record by sequence-assigned numeric ID.
    /// </summary>
    public bool TryGet(long id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return Sequence.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by sequence-assigned numeric ID.
    /// </summary>
    public ContentEntryRecord Get(long id)
    {
        return Sequence.Get(id);
    }

    /// <summary>
    /// Attempts to remove a retained record by sequence-assigned numeric ID.
    /// </summary>
    public bool TryRemove(long id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        return Sequence.TryRemove(id, out removedRecord, out failure);
    }

    /// <summary>
    /// Removes a retained record by sequence-assigned numeric ID.
    /// </summary>
    public ContentEntryRecord Remove(long id)
    {
        return Sequence.Remove(id);
    }

    /// <summary>
    /// Attempts to clear retained records.
    /// </summary>
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        return Sequence.TryClear(out removedRecords, out failure);
    }

    /// <summary>
    /// Clears retained records.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Clear()
    {
        return Sequence.Clear();
    }
}
