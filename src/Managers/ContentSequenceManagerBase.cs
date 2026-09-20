using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides shared manager behavior for sequence-like content structures.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public abstract class ContentSequenceManagerBase<TId> : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceManagerBase{TId}"/> class.
    /// </summary>
    /// <param name="structure">The sequence structure.</param>
    protected ContentSequenceManagerBase(ContentSequenceStructureBase<TId> structure)
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
    protected ContentSequenceStructureBase<TId> Sequence => (ContentSequenceStructureBase<TId>)Structure;

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
    /// Attempts to add an entry with an explicit sequence ID.
    /// </summary>
    public bool TryAdd(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return Sequence.TryAdd(id, entry, out record, out failure);
    }

    /// <summary>
    /// Adds an entry with an explicit sequence ID.
    /// </summary>
    public ContentEntryRecord Add(TId id, IContentEntry entry)
    {
        return Sequence.Add(id, entry);
    }

    /// <summary>
    /// Attempts to get a retained record by sequence ID.
    /// </summary>
    public bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return Sequence.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by sequence ID.
    /// </summary>
    public ContentEntryRecord Get(TId id)
    {
        return Sequence.Get(id);
    }

    /// <summary>
    /// Attempts to remove a retained record by sequence ID.
    /// </summary>
    public bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        return Sequence.TryRemove(id, out removedRecord, out failure);
    }

    /// <summary>
    /// Removes a retained record by sequence ID.
    /// </summary>
    public ContentEntryRecord Remove(TId id)
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

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentSequenceStructureBase<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not a sequence-family content structure.");
        return false;
    }
}

/// <summary>
/// Provides shared manager behavior for long-ID sequence content structures.
/// </summary>
public abstract class ContentSequenceManagerBase : ContentSequenceManagerBase<long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceManagerBase"/> class.
    /// </summary>
    protected ContentSequenceManagerBase(ContentSequenceStructureBase<long> structure)
        : base(structure)
    {
    }
}
