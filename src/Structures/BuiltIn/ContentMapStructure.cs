using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Stores content records in a map keyed by caller-provided IDs validated by an ID strategy.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public sealed class ContentMapStructure<TId> :
    ContentMapStructureBase<TId>,
    IContentSortableStructure,
    IContentMapBatchStructure<TId>,
    IContentPredicateRemovalStructure,
    IContentStructureSnapshotRoundTrippable,
    IContentChangeSource
{
    /// <summary>
    /// Stable structure snapshot kind for <see cref="ContentMapStructure{TId}"/>.
    /// </summary>
    public const string SnapshotKind = ContentFailureCodes.PackagePrefix + "structure.map";

    /// <summary>
    /// Current map structure snapshot data version.
    /// </summary>
    public const int SnapshotDataVersion = 1;

    private readonly Dictionary<ContentEntryId, ContentEntryRecord> _recordsById = new Dictionary<ContentEntryId, ContentEntryRecord>();
    private readonly List<ContentEntryRecord> _records = new List<ContentEntryRecord>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentMapStructure{TId}"/> class with the default strategy for <typeparamref name="TId"/>.
    /// </summary>
    public ContentMapStructure()
        : this(ContentEntryIdStrategies.GetDefault<TId>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentMapStructure{TId}"/> class.
    /// </summary>
    /// <param name="idStrategy">The strategy used to validate and normalize caller-provided IDs.</param>
    public ContentMapStructure(IContentEntryIdStrategy<TId> idStrategy)
    {
        IdStrategy = idStrategy ?? throw new ArgumentNullException(nameof(idStrategy));
    }

    private ContentMapStructure(
        IContentEntryIdStrategy<TId> idStrategy,
        IEnumerable<ContentEntryRecord> records)
        : this(idStrategy)
    {
        foreach (ContentEntryRecord record in records)
        {
            _recordsById.Add(record.Id, record);
            _records.Add(record);
        }
    }

    /// <summary>
    /// Creates a snapshot factory for <see cref="ContentMapStructure{TId}"/> using the default ID strategy for <typeparamref name="TId"/>.
    /// </summary>
    /// <returns>The created snapshot factory.</returns>
    public static IContentStructureSnapshotFactory CreateSnapshotFactory()
    {
        return CreateSnapshotFactory(ContentEntryIdStrategies.GetDefault<TId>());
    }

    /// <summary>
    /// Creates a snapshot factory for <see cref="ContentMapStructure{TId}"/>.
    /// </summary>
    /// <param name="idStrategy">The ID strategy to use for future operations on restored structures.</param>
    /// <returns>The created snapshot factory.</returns>
    public static IContentStructureSnapshotFactory CreateSnapshotFactory(IContentEntryIdStrategy<TId> idStrategy)
    {
        return new ContentMapStructureSnapshotFactory(idStrategy);
    }

    /// <summary>
    /// Gets the ID strategy used by the structure.
    /// </summary>
    public IContentEntryIdStrategy<TId> IdStrategy { get; }

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentMapManager<TId>(this);
    }

    /// <inheritdoc />
    public IContentStructureSnapshotFactory SnapshotFactory => CreateSnapshotFactory(IdStrategy);

    /// <summary>
    /// Gets the number of records currently retained by the structure.
    /// </summary>
    public int Count => _records.Count;

    /// <inheritdoc />
    public override IReadOnlyList<ContentEntryRecord> Records => _records.ToArray();

    /// <inheritdoc />
    public ContentPreflightResult AssessSort(IComparer<ContentEntryRecord> comparer)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public bool TrySort(IComparer<ContentEntryRecord> comparer, out bool orderChanged, out ContentFailure? failure)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        // OrderBy is stable; sorting finishes on a copy before retained state is touched.
        ContentEntryRecord[] sorted = _records.OrderBy(record => record, comparer).ToArray();
        orderChanged = !_records.SequenceEqual(sorted);
        if (orderChanged)
        {
            _records.Clear();
            _records.AddRange(sorted);
            OnChanged(new ContentChangedEventArgs(kind: ContentChangeKind.Sorted, requiresFullRefresh: true));
        }
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public event EventHandler<ContentChangedEventArgs>? Changed;

    /// <inheritdoc />
    public override ContentPreflightResult AssessAdd(TId id, IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (!TryNormalize(id, out ContentEntryId normalizedId, out ContentFailure? failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        if (_recordsById.ContainsKey(normalizedId))
        {
            return ContentPreflightResult.Rejected(ContentFailures.EntryIdDuplicate($"Entry ID '{normalizedId}' already exists.", normalizedId.ToString()));
        }

        return ContentPreflightResult.Success();
    }

    /// <summary>
    /// Attempts to add an entry with a caller-provided ID.
    /// </summary>
    /// <param name="id">The caller-provided entry ID.</param>
    /// <param name="entry">The entry to add.</param>
    /// <param name="record">The retained record when accepted; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the entry is added.</returns>
    public override bool TryAdd(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
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

    /// <inheritdoc />
    public override ContentPreflightResult AssessSet(TId id, IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        return TryNormalize(id, out _, out ContentFailure? failure)
            ? ContentPreflightResult.Success()
            : ContentPreflightResult.Rejected(failure!);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessGetOrSet(TId id)
    {
        return TryNormalize(id, out _, out ContentFailure? failure)
            ? ContentPreflightResult.Success()
            : ContentPreflightResult.Rejected(failure!);
    }


    /// <summary>
    /// Adds an entry with a caller-provided ID.
    /// </summary>
    /// <param name="id">The caller-provided entry ID.</param>
    /// <param name="entry">The entry to add.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the ID is rejected.</exception>
    public override ContentEntryRecord Add(TId id, IContentEntry entry)
    {
        if (TryAdd(id, entry, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TrySet(
        TId id,
        IContentEntry entry,
        out ContentEntryRecord? record,
        out ContentEntryRecord? replacedRecord,
        out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        record = null;
        replacedRecord = null;
        if (!TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            return false;
        }

        record = new ContentEntryRecord(normalizedId, entry);
        if (_recordsById.TryGetValue(normalizedId, out replacedRecord))
        {
            int index = _records.IndexOf(replacedRecord);
            _records[index] = record;
            _recordsById[normalizedId] = record;
            failure = null;
            OnChanged(new ContentChangedEventArgs(
                addedRecords: new[] { record },
                removedRecords: new[] { replacedRecord },
                kind: ContentChangeKind.Replaced));
            return true;
        }

        _recordsById.Add(normalizedId, record);
        _records.Add(record);
        failure = null;
        OnChanged(new ContentChangedEventArgs(new[] { record }, kind: ContentChangeKind.Added));
        return true;
    }

    /// <inheritdoc />
    public override ContentEntryRecord Set(TId id, IContentEntry entry)
    {
        if (TrySet(id, entry, out ContentEntryRecord? record, out _, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public ContentPreflightResult AssessAddRange(IReadOnlyList<KeyValuePair<TId, IContentEntry>> entries)
    {
        if (!TryNormalizeBatch(entries, requireAbsent: true, out _, out ContentFailure? failure)) return ContentPreflightResult.Rejected(failure!);
        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public bool TryAddRange(IReadOnlyList<KeyValuePair<TId, IContentEntry>> entries, out IReadOnlyList<ContentEntryRecord> records, out ContentFailure? failure)
    {
        records = Array.Empty<ContentEntryRecord>();
        if (!TryNormalizeBatch(entries, requireAbsent: true, out List<(ContentEntryId Id, IContentEntry Entry)> normalized, out failure)) return false;
        if (normalized.Count == 0) { failure = null; return true; }
        ContentEntryRecord[] added = normalized.Select(pair => new ContentEntryRecord(pair.Id, pair.Entry)).ToArray();
        foreach (ContentEntryRecord record in added) _recordsById.Add(record.Id, record);
        _records.AddRange(added);
        records = Array.AsReadOnly(added);
        failure = null;
        OnChanged(new ContentChangedEventArgs(added, kind: ContentChangeKind.Added, requiresFullRefresh: true));
        return true;
    }

    /// <inheritdoc />
    public ContentPreflightResult AssessSetRange(IReadOnlyList<KeyValuePair<TId, IContentEntry>> entries)
    {
        return TryNormalizeBatch(entries, requireAbsent: false, out _, out ContentFailure? failure)
            ? ContentPreflightResult.Success()
            : ContentPreflightResult.Rejected(failure!);
    }

    /// <inheritdoc />
    public bool TrySetRange(IReadOnlyList<KeyValuePair<TId, IContentEntry>> entries, out IReadOnlyList<ContentEntryRecord> records, out IReadOnlyList<ContentEntryRecord> replacedRecords, out ContentFailure? failure)
    {
        records = Array.Empty<ContentEntryRecord>();
        replacedRecords = Array.Empty<ContentEntryRecord>();
        if (!TryNormalizeBatch(entries, requireAbsent: false, out List<(ContentEntryId Id, IContentEntry Entry)> normalized, out failure)) return false;
        if (normalized.Count == 0) { failure = null; return true; }
        var candidate = _records.ToList();
        var added = new List<ContentEntryRecord>(normalized.Count);
        var replaced = new List<ContentEntryRecord>();
        foreach ((ContentEntryId id, IContentEntry entry) in normalized)
        {
            var record = new ContentEntryRecord(id, entry);
            int index = candidate.FindIndex(existing => existing.Id == id);
            if (index >= 0) { replaced.Add(candidate[index]); candidate[index] = record; }
            else candidate.Add(record);
            added.Add(record);
        }
        _recordsById.Clear(); _recordsById.EnsureCapacity(candidate.Count);
        foreach (ContentEntryRecord record in candidate) _recordsById.Add(record.Id, record);
        _records.Clear(); _records.AddRange(candidate);
        records = Array.AsReadOnly(added.ToArray());
        replacedRecords = Array.AsReadOnly(replaced.ToArray());
        failure = null;
        OnChanged(new ContentChangedEventArgs(added, replaced, replaced.Count > 0 ? ContentChangeKind.Replaced : ContentChangeKind.Added, requiresFullRefresh: true));
        return true;
    }

    /// <inheritdoc />
    public ContentPreflightResult AssessRemoveWhere(Func<ContentEntryRecord, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        _ = _records.ToArray().Where(predicate).ToArray();
        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public bool TryRemoveWhere(Func<ContentEntryRecord, bool> predicate, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        ContentEntryRecord[] selected = _records.ToArray().Where(predicate).ToArray();
        foreach (ContentEntryRecord record in selected) _recordsById.Remove(record.Id);
        if (selected.Length > 0) _records.RemoveAll(record => selected.Contains(record));
        removedRecords = Array.AsReadOnly(selected);
        failure = null;
        if (selected.Length > 0) OnChanged(new ContentChangedEventArgs(removedRecords: removedRecords, kind: ContentChangeKind.Removed, requiresFullRefresh: selected.Length > 1));
        return true;
    }

    /// <inheritdoc />
    public override bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
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
    public override bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
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
    public override ContentEntryRecord Get(ContentEntryId id)
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
    public override ContentEntryRecord Get(TId id)
    {
        if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
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
    public override ContentPreflightResult AssessClear()
    {
        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public override IReadOnlyList<ContentEntryRecord> Clear()
    {
        if (TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure))
        {
            return removedRecords;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryRemove(ContentEntryId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
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

    /// <inheritdoc />
    public override ContentPreflightResult AssessRemove(ContentEntryId id)
    {
        EnsureValidId(id);
        return _recordsById.ContainsKey(id)
            ? ContentPreflightResult.Success()
            : ContentPreflightResult.Rejected(ContentFailures.EntryNotFound($"Entry '{id}' was not found.", id.ToString()));
    }

    /// <summary>
    /// Attempts to remove a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to remove.</param>
    /// <param name="removedRecord">The removed record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is removed.</returns>
    public override bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        if (!TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            removedRecord = null;
            return false;
        }

        return TryRemove(normalizedId, out removedRecord, out failure);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessRemove(TId id)
    {
        if (!TryNormalize(id, out ContentEntryId normalizedId, out ContentFailure? failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        return AssessRemove(normalizedId);
    }

    /// <inheritdoc />
    public override ContentEntryRecord Remove(ContentEntryId id)
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
    public override ContentEntryRecord Remove(TId id)
    {
        if (TryRemove(id, out ContentEntryRecord? removedRecord, out ContentFailure? failure))
        {
            return removedRecord!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public bool TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure)
    {
        if (!ContentSnapshotRecords.TryCapture(_records, out List<ContentRecordSnapshot>? records, out failure))
        {
            snapshot = null;
            return false;
        }

        snapshot = new ContentStructureSnapshot
        {
            Kind = SnapshotKind,
            DataVersion = SnapshotDataVersion,
            Records = records!,
            Data = ContentSnapshotValue.Object()
        };
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public ContentStructureSnapshot CaptureSnapshot()
    {
        if (TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure) && snapshot is not null)
        {
            return snapshot;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    private bool TryNormalize(TId id, out ContentEntryId normalizedId, out ContentFailure? failure)
    {
        return IdStrategy.TryNormalize(id, out normalizedId, out failure);
    }

    private bool TryNormalizeBatch(
        IReadOnlyList<KeyValuePair<TId, IContentEntry>> entries,
        bool requireAbsent,
        out List<(ContentEntryId Id, IContentEntry Entry)> normalized,
        out ContentFailure? failure)
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));
        normalized = new List<(ContentEntryId, IContentEntry)>(entries.Count);
        var seen = new HashSet<ContentEntryId>();
        foreach (KeyValuePair<TId, IContentEntry> pair in entries)
        {
            if (pair.Value is null) throw new ArgumentException("Entry batches cannot contain null values.", nameof(entries));
            if (!TryNormalize(pair.Key, out ContentEntryId id, out failure)) return false;
            if (!seen.Add(id)) { failure = ContentFailures.EntryIdDuplicate($"Entry ID '{id}' occurs more than once in the range.", id.ToString()); return false; }
            if (requireAbsent && _recordsById.ContainsKey(id)) { failure = ContentFailures.EntryIdDuplicate($"Entry ID '{id}' already exists.", id.ToString()); return false; }
            normalized.Add((id, pair.Value));
        }
        failure = null;
        return true;
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

    private sealed class ContentMapStructureSnapshotFactory : ContentMapStructureSnapshotFactoryBase<TId, ContentMapStructure<TId>>
    {
        public ContentMapStructureSnapshotFactory(IContentEntryIdStrategy<TId> idStrategy)
            : base(SnapshotKind, SnapshotDataVersion, idStrategy)
        {
        }

        protected override bool TryRestoreValidatedMapSnapshot(
            ContentStructureSnapshot snapshot,
            ContentEntryRecord[] records,
            out ContentMapStructure<TId>? structure,
            out ContentFailure? failure)
        {
            structure = null;
            failure = null;

            if (snapshot.Data is null || snapshot.Data.Kind != ContentSnapshotValueKind.Object)
            {
                failure = ContentFailures.SnapshotMalformed("Map structure snapshot data must be an object.");
                return false;
            }

            structure = new ContentMapStructure<TId>(IdStrategy, records);
            return true;
        }
    }
}
