using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides shared manager behavior for stack content structures.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public abstract class ContentStackManagerBase<TId> : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStackManagerBase{TId}"/> class.
    /// </summary>
    protected ContentStackManagerBase(ContentStackStructureBase<TId> structure)
        : base(structure ?? throw new ArgumentNullException(nameof(structure)))
    {
    }

    /// <summary>
    /// Gets the active stack structure.
    /// </summary>
    protected ContentStackStructureBase<TId> Stack => (ContentStackStructureBase<TId>)Structure;

    /// <summary>
    /// Attempts to push an entry using the active stack's generated ID source.
    /// </summary>
    public bool TryPush(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure) => Stack.TryPush(entry, out record, out failure);

    /// <summary>
    /// Assesses whether an entry can be pushed with a generated ID without committing the push.
    /// </summary>
    public ContentPreflightResult AssessPush(IContentEntry entry) => Stack.AssessPush(entry);

    /// <summary>
    /// Pushes an entry using the active stack's generated ID source.
    /// </summary>
    public ContentEntryRecord Push(IContentEntry entry) => Stack.Push(entry);

    /// <summary>
    /// Attempts to push an entry with an explicit ID.
    /// </summary>
    public bool TryPush(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure) => Stack.TryPush(id, entry, out record, out failure);

    /// <summary>
    /// Assesses whether an entry can be pushed with an explicit ID without committing the push.
    /// </summary>
    public ContentPreflightResult AssessPush(TId id, IContentEntry entry) => Stack.AssessPush(id, entry);

    /// <summary>
    /// Pushes an entry with an explicit ID.
    /// </summary>
    public ContentEntryRecord Push(TId id, IContentEntry entry) => Stack.Push(id, entry);

    /// <summary>Assesses pushing an entry range atomically.</summary>
    public ContentPreflightResult AssessPushRange(IEnumerable<IContentEntry> entries)
    {
        IReadOnlyList<IContentEntry> batch = ContentBatchInputs.ReadEntries(entries, nameof(entries));
        return Structure is IContentStackBatchStructure<TId> batchStructure
            ? batchStructure.AssessPushRange(batch)
            : ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The active stack structure does not support atomic push ranges."));
    }

    /// <summary>Attempts to push an entry range atomically in input order.</summary>
    public bool TryPushRange(IEnumerable<IContentEntry> entries, out IReadOnlyList<ContentEntryRecord> records, out ContentFailure? failure)
    {
        IReadOnlyList<IContentEntry> batch = ContentBatchInputs.ReadEntries(entries, nameof(entries));
        if (Structure is IContentStackBatchStructure<TId> batchStructure) return batchStructure.TryPushRange(batch, out records, out failure);
        records = Array.Empty<ContentEntryRecord>(); failure = ContentFailures.StructureUnsupportedOperation("The active stack structure does not support atomic push ranges."); return false;
    }

    /// <summary>Pushes an entry range atomically in input order.</summary>
    public IReadOnlyList<ContentEntryRecord> PushRange(IEnumerable<IContentEntry> entries)
    {
        if (TryPushRange(entries, out IReadOnlyList<ContentEntryRecord> records, out ContentFailure? failure)) return records;
        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Gets a value indicating whether the stack has a record that can be peeked.
    /// </summary>
    public bool CanPeek => Count > 0;

    /// <summary>
    /// Gets a value indicating whether the stack has a record that can be popped.
    /// </summary>
    public bool CanPop => Count > 0;

    /// <summary>
    /// Attempts to read the top retained record without removing it.
    /// </summary>
    public bool TryPeek(out ContentEntryRecord? record, out ContentFailure? failure) => Stack.TryPeek(out record, out failure);

    /// <summary>
    /// Assesses whether a peek can currently commit.
    /// </summary>
    public ContentPreflightResult AssessPeek() => Stack.AssessPeek();

    /// <summary>
    /// Reads the top retained record without removing it.
    /// </summary>
    public ContentEntryRecord Peek() => Stack.Peek();

    /// <summary>
    /// Attempts to remove and return the top retained record.
    /// </summary>
    public bool TryPop(out ContentEntryRecord? record, out ContentFailure? failure) => Stack.TryPop(out record, out failure);

    /// <summary>
    /// Assesses whether a pop can currently commit without committing the pop.
    /// </summary>
    public ContentPreflightResult AssessPop() => Stack.AssessPop();

    /// <summary>
    /// Removes and returns the top retained record.
    /// </summary>
    public ContentEntryRecord Pop() => Stack.Pop();

    /// <summary>
    /// Determines whether the stack retains a record with the supplied natural ID.
    /// </summary>
    public bool Contains(TId id) => TryGet(id, out _, out _);

    /// <summary>
    /// Attempts to get a retained record by the stack's natural ID type.
    /// </summary>
    public bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure) => Stack.TryGet(id, out record, out failure);

    /// <summary>
    /// Gets a retained record by the stack's natural ID type.
    /// </summary>
    public ContentEntryRecord Get(TId id) => Stack.Get(id);

    /// <summary>
    /// Attempts to remove a retained record by the stack's natural ID type.
    /// </summary>
    public bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure) => Stack.TryRemove(id, out removedRecord, out failure);

    /// <summary>
    /// Assesses whether a retained record can be removed by natural ID without committing the removal.
    /// </summary>
    public ContentPreflightResult AssessRemove(TId id) => Stack.AssessRemove(id);

    /// <summary>
    /// Removes a retained record by the stack's natural ID type.
    /// </summary>
    public ContentEntryRecord Remove(TId id) => Stack.Remove(id);

    /// <summary>Assesses predicate-based removal over the active record snapshot.</summary>
    public ContentPreflightResult AssessRemoveWhere(Func<ContentEntryRecord, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return Structure is IContentPredicateRemovalStructure removal ? removal.AssessRemoveWhere(predicate)
            : ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The active stack structure does not support predicate removal."));
    }

    /// <summary>Attempts predicate-based removal over the pre-mutation record snapshot.</summary>
    public bool TryRemoveWhere(Func<ContentEntryRecord, bool> predicate, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        if (Structure is IContentPredicateRemovalStructure removal) return removal.TryRemoveWhere(predicate, out removedRecords, out failure);
        removedRecords = Array.Empty<ContentEntryRecord>(); failure = ContentFailures.StructureUnsupportedOperation("The active stack structure does not support predicate removal."); return false;
    }

    /// <summary>Removes records selected by a predicate over the pre-mutation record snapshot.</summary>
    public IReadOnlyList<ContentEntryRecord> RemoveWhere(Func<ContentEntryRecord, bool> predicate)
    {
        if (TryRemoveWhere(predicate, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)) return removedRecords;
        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Attempts to clear all retained records.
    /// </summary>
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure) => Stack.TryClear(out removedRecords, out failure);

    /// <summary>
    /// Assesses whether retained records can be cleared without committing the clear.
    /// </summary>
    public ContentPreflightResult AssessClear() => Stack.AssessClear();

    /// <summary>
    /// Clears all retained records.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Clear() => Stack.Clear();

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentStackStructureBase<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not a stack content structure for the active ID type.");
        return false;
    }
}

/// <summary>
/// Provides shared manager behavior for long-ID stack content structures.
/// </summary>
public abstract class ContentStackManagerBase : ContentStackManagerBase<long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStackManagerBase"/> class.
    /// </summary>
    protected ContentStackManagerBase(ContentStackStructureBase<long> structure)
        : base(structure)
    {
    }
}
