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
    /// Pushes an entry using the active stack's generated ID source.
    /// </summary>
    public ContentEntryRecord Push(IContentEntry entry) => Stack.Push(entry);

    /// <summary>
    /// Attempts to push an entry with an explicit ID.
    /// </summary>
    public bool TryPush(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure) => Stack.TryPush(id, entry, out record, out failure);

    /// <summary>
    /// Pushes an entry with an explicit ID.
    /// </summary>
    public ContentEntryRecord Push(TId id, IContentEntry entry) => Stack.Push(id, entry);

    /// <summary>
    /// Attempts to read the top retained record without removing it.
    /// </summary>
    public bool TryPeek(out ContentEntryRecord? record, out ContentFailure? failure) => Stack.TryPeek(out record, out failure);

    /// <summary>
    /// Reads the top retained record without removing it.
    /// </summary>
    public ContentEntryRecord Peek() => Stack.Peek();

    /// <summary>
    /// Attempts to remove and return the top retained record.
    /// </summary>
    public bool TryPop(out ContentEntryRecord? record, out ContentFailure? failure) => Stack.TryPop(out record, out failure);

    /// <summary>
    /// Removes and returns the top retained record.
    /// </summary>
    public ContentEntryRecord Pop() => Stack.Pop();

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
    /// Removes a retained record by the stack's natural ID type.
    /// </summary>
    public ContentEntryRecord Remove(TId id) => Stack.Remove(id);

    /// <summary>
    /// Attempts to clear all retained records.
    /// </summary>
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure) => Stack.TryClear(out removedRecords, out failure);

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
