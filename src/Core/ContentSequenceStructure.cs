using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Stores content records as an ordered sequence with configurable retention behavior.
/// </summary>
public sealed class ContentSequenceStructure :
    ContentSequenceStructureBase,
    IContentRetentionPolicyStructure,
    IContentReadOrderStructure,
    IParameterizedContentStructure,
    IContentStructureSnapshotRoundTrippable,
    IContentChangeSource
{
    /// <summary>
    /// Stable structure snapshot kind for <see cref="ContentSequenceStructure"/>.
    /// </summary>
    public const string SnapshotKind = ContentFailureCodes.PackagePrefix + "structure.sequence";

    /// <summary>
    /// Current sequence structure snapshot data version.
    /// </summary>
    public const int SnapshotDataVersion = 1;

    /// <summary>
    /// Stable parameter ID for the sequence overflow policy.
    /// </summary>
    public const string OverflowPolicyParameterId = "overflowPolicy";

    /// <summary>
    /// Gets the snapshot factory for <see cref="ContentSequenceStructure"/>.
    /// </summary>
    public static IContentStructureSnapshotFactory Factory { get; } = new ContentSequenceStructureSnapshotFactory();

    private static readonly IReadOnlyCollection<ContentParameterDefinition> s_parameters =
        new[]
        {
            new ContentParameterDefinition(
                OverflowPolicyParameterId,
                typeof(ContentOverflowPolicy),
                "The sequence overflow policy.")
        };

    private readonly List<ContentEntryRecord> _records = new List<ContentEntryRecord>();
    private long _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceStructure"/> class.
    /// </summary>
    /// <param name="overflowPolicy">The policy used for retention and overflow.</param>
    /// <param name="readOrder">The order used when reading retained records.</param>
    public ContentSequenceStructure(
        ContentOverflowPolicy overflowPolicy,
        ContentSequenceReadOrder readOrder = ContentSequenceReadOrder.OldestFirst)
    {
        OverflowPolicy = overflowPolicy ?? throw new ArgumentNullException(nameof(overflowPolicy));

        if (!Enum.IsDefined(typeof(ContentSequenceReadOrder), readOrder))
        {
            throw new ArgumentOutOfRangeException(nameof(readOrder), readOrder, "Content sequence read order is not supported.");
        }

        ReadOrder = readOrder;
    }

    private ContentSequenceStructure(
        ContentOverflowPolicy overflowPolicy,
        ContentSequenceReadOrder readOrder,
        IEnumerable<ContentEntryRecord> records,
        long nextId)
    {
        OverflowPolicy = overflowPolicy ?? throw new ArgumentNullException(nameof(overflowPolicy));
        ReadOrder = readOrder;
        _records.AddRange(records);
        _nextId = nextId;
    }

    /// <summary>
    /// Gets the order used when reading retained records.
    /// </summary>
    public ContentSequenceReadOrder ReadOrder { get; }

    /// <summary>
    /// Gets the policy used for retention and overflow.
    /// </summary>
    public ContentOverflowPolicy OverflowPolicy { get; private set; }

    /// <summary>
    /// Gets the number of records currently retained by the structure.
    /// </summary>
    public int Count => _records.Count;

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentSequenceManager(this);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ContentParameterDefinition> Parameters => s_parameters;

    /// <inheritdoc />
    public IContentStructureSnapshotFactory SnapshotFactory => Factory;

    /// <inheritdoc />
    public override IReadOnlyList<ContentEntryRecord> Records => CreateReadSnapshot();

    /// <inheritdoc />
    public event EventHandler<ContentChangedEventArgs>? Changed;

    /// <summary>
    /// Adds an entry and returns the retained record created for it.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <returns>The retained record.</returns>
    public override ContentEntryRecord Add(IContentEntry entry)
    {
        if (TryAdd(entry, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        ContentEntryRecord? removedRecord = null;
        if (OverflowPolicy.Kind == ContentOverflowPolicyKind.DropOldest && _records.Count == OverflowPolicy.Capacity)
        {
            removedRecord = _records[0];
            _records.RemoveAt(0);
        }

        ContentEntryId id = new ContentEntryId(_nextId.ToString(CultureInfo.InvariantCulture));
        _nextId++;

        record = new ContentEntryRecord(id, entry);
        _records.Add(record);
        failure = null;
        OnChanged(new ContentChangedEventArgs(
            new[] { record },
            removedRecord is null ? null : new[] { removedRecord },
            ContentChangeKind.Added));
        return true;
    }

    /// <inheritdoc />
    public override bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        EnsureValidId(id);

        for (int i = 0; i < _records.Count; i++)
        {
            ContentEntryRecord candidate = _records[i];
            if (candidate.Id == id)
            {
                record = candidate;
                failure = null;
                return true;
            }
        }

        record = null;
        failure = ContentFailures.EntryNotFound($"Entry '{id}' was not found.", id.ToString());
        return false;
    }

    /// <summary>
    /// Attempts to get a retained record by its structure-assigned numeric ID.
    /// </summary>
    /// <param name="id">The structure-assigned numeric entry ID.</param>
    /// <param name="record">The retained record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is found.</returns>
    public override bool TryGet(long id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return TryGet(CreateNumericId(id), out record, out failure);
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
    /// Gets a retained record by its structure-assigned numeric ID.
    /// </summary>
    /// <param name="id">The structure-assigned numeric entry ID.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found.</exception>
    public override ContentEntryRecord Get(long id)
    {
        return Get(CreateNumericId(id));
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

        _records.Clear();
        OnChanged(new ContentChangedEventArgs(
            removedRecords: removedRecords,
            kind: ContentChangeKind.Cleared,
            cleared: true,
            requiresFullRefresh: true));
        return true;
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

        for (int i = 0; i < _records.Count; i++)
        {
            ContentEntryRecord candidate = _records[i];
            if (candidate.Id == id)
            {
                _records.RemoveAt(i);
                removedRecord = candidate;
                failure = null;
                OnChanged(new ContentChangedEventArgs(
                    removedRecords: new[] { candidate },
                    kind: ContentChangeKind.Removed));
                return true;
            }
        }

        removedRecord = null;
        failure = ContentFailures.EntryNotFound($"Entry '{id}' was not found.", id.ToString());
        return false;
    }

    /// <summary>
    /// Attempts to remove a retained record by its structure-assigned numeric ID.
    /// </summary>
    /// <param name="id">The structure-assigned numeric entry ID.</param>
    /// <param name="removedRecord">The removed record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is removed.</returns>
    public override bool TryRemove(long id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        return TryRemove(CreateNumericId(id), out removedRecord, out failure);
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
    /// Removes a retained record by its structure-assigned numeric ID.
    /// </summary>
    /// <param name="id">The structure-assigned numeric entry ID.</param>
    /// <returns>The removed record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be removed.</exception>
    public override ContentEntryRecord Remove(long id)
    {
        return Remove(CreateNumericId(id));
    }

    /// <inheritdoc />
    public bool TryCreateWithParameter(
        string parameterId,
        object? value,
        out IContentStructure? structure,
        out IReadOnlyList<ContentEntryRecord> removedRecords,
        out ContentFailure? failure)
    {
        structure = null;
        removedRecords = Array.Empty<ContentEntryRecord>();

        if (parameterId != OverflowPolicyParameterId)
        {
            failure = ContentFailures.Configuration($"Parameter '{parameterId}' is not supported by ContentSequenceStructure.");
            return false;
        }

        if (value is not ContentOverflowPolicy overflowPolicy)
        {
            failure = ContentFailures.Configuration($"Parameter '{OverflowPolicyParameterId}' expects value type '{nameof(ContentOverflowPolicy)}'.");
            return false;
        }

        if (OverflowPolicy.Equals(overflowPolicy))
        {
            structure = this;
            failure = null;
            return true;
        }

        ContentEntryRecord[] retained = _records.ToArray();
        ContentEntryRecord[] removed = TrimForOverflowPolicy(overflowPolicy, ref retained);
        removedRecords = removed;
        structure = new ContentSequenceStructure(overflowPolicy, ReadOrder, retained, _nextId);
        failure = null;
        return true;
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
            Data = ContentSnapshotValue.Object(new[]
            {
                ContentSnapshotProperties.Named("nextId", ContentSnapshotCodecs.Encode(_nextId)),
                ContentSnapshotProperties.Named("readOrder", ContentSnapshotCodecs.Encode(ReadOrder.ToString())),
                ContentSnapshotProperties.Named("overflowKind", ContentSnapshotCodecs.Encode(OverflowPolicy.Kind.ToString())),
                ContentSnapshotProperties.Named("overflowCapacity", ContentSnapshotCodecs.Encode(OverflowPolicy.Capacity ?? 0))
            })
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

    private static void EnsureValidId(ContentEntryId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException("Content entry ID cannot be empty.", nameof(id));
        }
    }

    private static ContentEntryId CreateNumericId(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Content sequence entry IDs must be greater than zero.");
        }

        return new ContentEntryId(id.ToString(CultureInfo.InvariantCulture));
    }

    private IReadOnlyList<ContentEntryRecord> CreateReadSnapshot()
    {
        ContentEntryRecord[] snapshot = _records.ToArray();
        if (ReadOrder == ContentSequenceReadOrder.NewestFirst)
        {
            Array.Reverse(snapshot);
        }

        return snapshot;
    }

    private static ContentEntryRecord[] TrimForOverflowPolicy(
        ContentOverflowPolicy overflowPolicy,
        ref ContentEntryRecord[] retained)
    {
        if (overflowPolicy.Kind != ContentOverflowPolicyKind.DropOldest || retained.Length <= overflowPolicy.Capacity!.Value)
        {
            return Array.Empty<ContentEntryRecord>();
        }

        int removeCount = retained.Length - overflowPolicy.Capacity!.Value;
        ContentEntryRecord[] removed = retained.Take(removeCount).ToArray();
        retained = retained.Skip(removeCount).ToArray();
        return removed;
    }

    private void OnChanged(ContentChangedEventArgs args)
    {
        Changed?.Invoke(this, args);
    }

    private sealed class ContentSequenceStructureSnapshotFactory : ContentStructureSnapshotFactoryBase<ContentSequenceStructure>
    {
        public ContentSequenceStructureSnapshotFactory()
            : base(SnapshotKind, SnapshotDataVersion)
        {
        }

        protected override bool TryRestoreValidatedSnapshot(
            ContentStructureSnapshot snapshot,
            out ContentSequenceStructure? structure,
            out ContentFailure? failure)
        {
            structure = null;
            failure = null;

            if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure))
            {
                return false;
            }

            if (!ContentSnapshotRecords.TryGetMaximumPositiveNumericId(records, out long maximumId, out failure))
            {
                return false;
            }

            if (!TryRestoreData(snapshot, out long nextId, out ContentSequenceReadOrder readOrder, out ContentOverflowPolicy overflowPolicy, out failure))
            {
                return false;
            }

            if (nextId <= maximumId)
            {
                failure = ContentFailures.SnapshotMalformed("Sequence structure snapshot next ID must be greater than retained record IDs.");
                return false;
            }

            if (overflowPolicy.Kind == ContentOverflowPolicyKind.DropOldest && records.Length > overflowPolicy.Capacity!.Value)
            {
                failure = ContentFailures.SnapshotMalformed("Sequence structure snapshot retains more records than its overflow policy allows.");
                return false;
            }

            structure = new ContentSequenceStructure(overflowPolicy, readOrder, records, nextId);
            return true;
        }

        private static bool TryRestoreData(
            ContentStructureSnapshot snapshot,
            out long nextId,
            out ContentSequenceReadOrder readOrder,
            out ContentOverflowPolicy overflowPolicy,
            out ContentFailure? failure)
        {
            nextId = 0;
            readOrder = ContentSequenceReadOrder.OldestFirst;
            overflowPolicy = ContentOverflowPolicy.None;
            failure = null;

            if (snapshot.Data is null || snapshot.Data.Kind != ContentSnapshotValueKind.Object)
            {
                failure = ContentFailures.SnapshotMalformed("Sequence structure snapshot data must be an object.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredInt64(snapshot.Data, "nextId", out nextId, out failure))
            {
                return false;
            }

            if (nextId <= 0)
            {
                failure = ContentFailures.SnapshotMalformed("Sequence structure snapshot next ID must be greater than zero.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredString(snapshot.Data, "readOrder", out string readOrderText, out failure))
            {
                return false;
            }

            if (!Enum.TryParse(readOrderText, out readOrder) || !Enum.IsDefined(typeof(ContentSequenceReadOrder), readOrder))
            {
                failure = ContentFailures.SnapshotMalformed($"Sequence structure snapshot read order '{readOrderText}' is not supported.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredString(snapshot.Data, "overflowKind", out string overflowKindText, out failure))
            {
                return false;
            }

            if (!Enum.TryParse(overflowKindText, out ContentOverflowPolicyKind overflowKind)
                || !Enum.IsDefined(typeof(ContentOverflowPolicyKind), overflowKind))
            {
                failure = ContentFailures.SnapshotMalformed($"Sequence structure snapshot overflow kind '{overflowKindText}' is not supported.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredInt32(snapshot.Data, "overflowCapacity", out int overflowCapacity, out failure))
            {
                return false;
            }

            if (overflowKind == ContentOverflowPolicyKind.None)
            {
                if (overflowCapacity != 0)
                {
                    failure = ContentFailures.SnapshotMalformed("Unbounded sequence snapshots must use overflow capacity 0.");
                    return false;
                }

                overflowPolicy = ContentOverflowPolicy.None;
                return true;
            }

            if (overflowKind == ContentOverflowPolicyKind.DropOldest)
            {
                if (overflowCapacity <= 0)
                {
                    failure = ContentFailures.SnapshotMalformed("DropOldest sequence snapshots must use a positive overflow capacity.");
                    return false;
                }

                overflowPolicy = ContentOverflowPolicy.DropOldest(overflowCapacity);
                return true;
            }

            failure = ContentFailures.SnapshotMalformed($"Sequence structure snapshot overflow kind '{overflowKind}' is not supported.");
            return false;
        }
    }
}
