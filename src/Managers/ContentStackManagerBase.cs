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
