using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides a content workflow for structures that use caller-provided typed IDs.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public sealed class KeyedContentManager<TId> : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentManager{TId}"/> class with a keyed structure using the default ID strategy for <typeparamref name="TId"/>.
    /// </summary>
    public KeyedContentManager()
        : this(new KeyedContentStructure<TId>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentManager{TId}"/> class.
    /// </summary>
    /// <param name="idStrategy">The strategy used by the created keyed structure to validate and normalize caller-provided IDs.</param>
    public KeyedContentManager(IContentEntryIdStrategy<TId> idStrategy)
        : this(new KeyedContentStructure<TId>(idStrategy))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentManager{TId}"/> class.
    /// </summary>
    /// <param name="structure">The keyed content structure.</param>
    public KeyedContentManager(IKeyedContentStructure<TId> structure)
        : base(structure)
    {
        if (structure is null)
        {
            throw new ArgumentNullException(nameof(structure));
        }
    }

    private IKeyedContentStructure<TId> KeyedStructure => (IKeyedContentStructure<TId>)Structure;

    /// <summary>
    /// Attempts to add an entry with a caller-provided ID.
    /// </summary>
    /// <param name="id">The caller-provided entry ID.</param>
    /// <param name="entry">The entry to add.</param>
    /// <param name="record">The retained record when accepted; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the entry is added.</returns>
    public bool TryAdd(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return KeyedStructure.TryAdd(id, entry, out record, out failure);
    }

    /// <summary>
    /// Adds an entry with a caller-provided ID.
    /// </summary>
    /// <param name="id">The caller-provided entry ID.</param>
    /// <param name="entry">The entry to add.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the ID is rejected.</exception>
    public ContentEntryRecord Add(TId id, IContentEntry entry)
    {
        return KeyedStructure.Add(id, entry);
    }

    /// <summary>
    /// Attempts to get a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <param name="record">The retained record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is found.</returns>
    public bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return KeyedStructure.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found or the ID is rejected.</exception>
    public ContentEntryRecord Get(TId id)
    {
        return KeyedStructure.Get(id);
    }

    /// <summary>
    /// Attempts to remove a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to remove.</param>
    /// <param name="removedRecord">The removed record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is removed.</returns>
    public bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        if (Structure is IKeyedContentRecordRemovalStructure<TId> removable)
        {
            return removable.TryRemove(id, out removedRecord, out failure);
        }

        removedRecord = null;
        failure = ContentFailures.StructureUnsupportedOperation("The active keyed content structure does not support typed record removal.");
        return false;
    }

    /// <summary>
    /// Removes a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to remove.</param>
    /// <returns>The removed record.</returns>
    /// <exception cref="ContentOperationException">Thrown when removal is rejected or unsupported.</exception>
    public ContentEntryRecord Remove(TId id)
    {
        if (TryRemove(id, out ContentEntryRecord? removedRecord, out ContentFailure? failure))
        {
            return removedRecord!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is IKeyedContentStructure<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure does not support the active keyed ID workflow.");
        return false;
    }
}
