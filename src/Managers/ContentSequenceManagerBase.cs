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
    /// Assesses whether an entry can be added with a generated ID without committing the add.
    /// </summary>
    public ContentPreflightResult AssessAdd(IContentEntry entry)
    {
        return Sequence.AssessAdd(entry);
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
    /// Assesses whether an entry can be added with an explicit sequence ID without committing the add.
    /// </summary>
    public ContentPreflightResult AssessAdd(TId id, IContentEntry entry)
    {
        return Sequence.AssessAdd(id, entry);
    }

    /// <summary>
    /// Adds an entry with an explicit sequence ID.
    /// </summary>
    public ContentEntryRecord Add(TId id, IContentEntry entry)
    {
        return Sequence.Add(id, entry);
    }

    /// <summary>
    /// Determines whether the sequence retains a record with the supplied natural ID.
    /// </summary>
    public bool Contains(TId id)
    {
        return TryGet(id, out _, out _);
    }

    /// <summary>
    /// Attempts to get the first retained record in the sequence's configured read order.
    /// </summary>
    public bool TryGetFirst(out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (Records.Count > 0)
        {
            record = Records[0];
            failure = null;
            return true;
        }

        record = null;
        failure = ContentFailures.EntryNotFound("The sequence contains no retained records.");
        return false;
    }

    /// <summary>
    /// Gets the first retained record in the sequence's configured read order.
    /// </summary>
    public ContentEntryRecord GetFirst()
    {
        if (TryGetFirst(out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Attempts to get the last retained record in the sequence's configured read order.
    /// </summary>
    public bool TryGetLast(out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (Records.Count > 0)
        {
            record = Records[Records.Count - 1];
            failure = null;
            return true;
        }

        record = null;
        failure = ContentFailures.EntryNotFound("The sequence contains no retained records.");
        return false;
    }

    /// <summary>
    /// Gets the last retained record in the sequence's configured read order.
    /// </summary>
    public ContentEntryRecord GetLast()
    {
        if (TryGetLast(out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
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
    /// Assesses whether a retained record can be removed by sequence ID without committing the removal.
    /// </summary>
    public ContentPreflightResult AssessRemove(TId id)
    {
        return Sequence.AssessRemove(id);
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
    /// Assesses whether retained records can be cleared without committing the clear.
    /// </summary>
    public ContentPreflightResult AssessClear()
    {
        return Sequence.AssessClear();
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
