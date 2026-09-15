using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Stores content records keyed by caller-provided IDs validated by an ID strategy.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public sealed class KeyedContentStructure<TId> :
    IKeyedContentRecordRemovalStructure<TId>,
    IContentClearableStructure,
    IContentChangeSource
{
    private readonly Dictionary<ContentEntryId, ContentEntryRecord> _recordsById = new Dictionary<ContentEntryId, ContentEntryRecord>();
    private readonly List<ContentEntryRecord> _records = new List<ContentEntryRecord>();

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentStructure{TId}"/> class with the default strategy for <typeparamref name="TId"/>.
    /// </summary>
    public KeyedContentStructure()
        : this(ContentEntryIdStrategies.GetDefault<TId>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentStructure{TId}"/> class.
    /// </summary>
    /// <param name="idStrategy">The strategy used to validate and normalize caller-provided IDs.</param>
    public KeyedContentStructure(IContentEntryIdStrategy<TId> idStrategy)
    {
        IdStrategy = idStrategy ?? throw new ArgumentNullException(nameof(idStrategy));
    }

    /// <summary>
    /// Gets the ID strategy used by the structure.
    /// </summary>
    public IContentEntryIdStrategy<TId> IdStrategy { get; }

    /// <summary>
    /// Gets the number of records currently retained by the structure.
    /// </summary>
    public int Count => _records.Count;

    /// <inheritdoc />
    public IReadOnlyList<ContentEntryRecord> Records => _records.ToArray();

    /// <inheritdoc />
    public event EventHandler<ContentChangedEventArgs>? Changed;

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
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (!TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            record = null;
            return false;
        }

        if (_recordsById.ContainsKey(normalizedId))
        {
            record = null;
            failure = ContentFailures.EntryIdDuplicate($"Entry ID '{normalizedId}' already exists.", normalizedId.ToString());
            return false;
        }

        record = new ContentEntryRecord(normalizedId, entry);
        _recordsById.Add(normalizedId, record);
        _records.Add(record);
        failure = null;
        OnChanged(new ContentChangedEventArgs(new[] { record }, kind: ContentChangeKind.Added));
        return true;
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
        if (TryAdd(id, entry, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        EnsureValidId(id);

        if (_recordsById.TryGetValue(id, out record))
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.EntryNotFound($"Entry '{id}' was not found.", id.ToString());
        return false;
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
        if (!TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            record = null;
            return false;
        }

        if (_recordsById.TryGetValue(normalizedId, out record))
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.EntryNotFound($"Entry '{normalizedId}' was not found.", normalizedId.ToString());
        return false;
    }

    /// <inheritdoc />
    public ContentEntryRecord Get(ContentEntryId id)
    {
        if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Gets a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found or the ID is rejected.</exception>
    public ContentEntryRecord Get(TId id)
    {
        if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        removedRecords = _records.ToArray();
        failure = null;

        if (_records.Count == 0)
        {
            return true;
        }

        _recordsById.Clear();
        _records.Clear();
        OnChanged(new ContentChangedEventArgs(
            removedRecords: removedRecords,
            kind: ContentChangeKind.Cleared,
            cleared: true,
            requiresFullRefresh: true));
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<ContentEntryRecord> Clear()
    {
        if (TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure))
        {
            return removedRecords;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public bool TryRemove(ContentEntryId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        EnsureValidId(id);

        if (!_recordsById.TryGetValue(id, out removedRecord))
        {
            removedRecord = null;
            failure = ContentFailures.EntryNotFound($"Entry '{id}' was not found.", id.ToString());
            return false;
        }

        _recordsById.Remove(id);
        _records.Remove(removedRecord);
        failure = null;
        OnChanged(new ContentChangedEventArgs(
            removedRecords: new[] { removedRecord },
            kind: ContentChangeKind.Removed));
        return true;
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
        if (!TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            removedRecord = null;
            return false;
        }

        return TryRemove(normalizedId, out removedRecord, out failure);
    }

    /// <inheritdoc />
    public ContentEntryRecord Remove(ContentEntryId id)
    {
        if (TryRemove(id, out ContentEntryRecord? removedRecord, out ContentFailure? failure))
        {
            return removedRecord!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Removes a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to remove.</param>
    /// <returns>The removed record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be removed or the ID is rejected.</exception>
    public ContentEntryRecord Remove(TId id)
    {
        if (TryRemove(id, out ContentEntryRecord? removedRecord, out ContentFailure? failure))
        {
            return removedRecord!;
        }

        throw new ContentOperationException(failure!);
    }

    private bool TryNormalize(TId id, out ContentEntryId normalizedId, out ContentFailure? failure)
    {
        return IdStrategy.TryNormalize(id, out normalizedId, out failure);
    }

    private static void EnsureValidId(ContentEntryId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException("Content entry ID cannot be empty.", nameof(id));
        }
    }

    private void OnChanged(ContentChangedEventArgs args)
    {
        Changed?.Invoke(this, args);
    }
}
