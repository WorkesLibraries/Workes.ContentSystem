using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Stores content records as an ordered sequence with configurable retention behavior.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public class ContentSequenceStructure<TId> :
    ContentSequenceStructureBase<TId>,
    IContentRetentionPolicyStructure,
    IContentReadOrderStructure,
    IParameterizedContentStructure,
    IContentStructureSnapshotRoundTrippable,
    IContentChangeSource
{
    /// <summary>
    /// Stable structure snapshot kind for content sequence structures.
    /// </summary>
    public const string SnapshotKind = ContentFailureCodes.PackagePrefix + "structure.sequence";

    /// <summary>
    /// Current structure snapshot data version for content sequence structures.
    /// </summary>
    public const int SnapshotDataVersion = 1;

    /// <summary>
    /// Stable parameter ID for changing the sequence overflow policy.
    /// </summary>
    public const string OverflowPolicyParameterId = "overflowPolicy";

    private static readonly IReadOnlyCollection<ContentParameterDefinition> s_parameters =
        new[]
        {
            new ContentParameterDefinition(
                OverflowPolicyParameterId,
                typeof(ContentOverflowPolicy),
                "The sequence overflow policy.")
        };

    private readonly List<ContentEntryRecord> _records = new List<ContentEntryRecord>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceStructure{TId}"/> class.
    /// </summary>
    /// <param name="idSource">The generated ID source used by id-less add workflows.</param>
    /// <param name="overflowPolicy">The retention policy for accepted records.</param>
    /// <param name="readOrder">The order used when exposing retained records.</param>
    public ContentSequenceStructure(
        IContentGeneratedIdSource<TId> idSource,
        ContentOverflowPolicy overflowPolicy,
        ContentSequenceReadOrder readOrder = ContentSequenceReadOrder.OldestFirst)
        : this(idSource, overflowPolicy, readOrder, Enumerable.Empty<ContentEntryRecord>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceStructure{TId}"/> class with retained records.
    /// </summary>
    /// <param name="idSource">The generated ID source used by id-less add workflows.</param>
    /// <param name="overflowPolicy">The retention policy for accepted records.</param>
    /// <param name="readOrder">The order used when exposing retained records.</param>
    /// <param name="records">The retained records to seed into the structure.</param>
    protected ContentSequenceStructure(
        IContentGeneratedIdSource<TId> idSource,
        ContentOverflowPolicy overflowPolicy,
        ContentSequenceReadOrder readOrder,
        IEnumerable<ContentEntryRecord> records)
    {
        IdSource = idSource ?? throw new ArgumentNullException(nameof(idSource));
        OverflowPolicy = overflowPolicy ?? throw new ArgumentNullException(nameof(overflowPolicy));

        if (!Enum.IsDefined(typeof(ContentSequenceReadOrder), readOrder))
        {
            throw new ArgumentOutOfRangeException(nameof(readOrder), readOrder, "Content sequence read order is not supported.");
        }

        ReadOrder = readOrder;
        _records.AddRange(records ?? throw new ArgumentNullException(nameof(records)));
    }

    /// <summary>
    /// Creates a snapshot factory for generic sequence structures using the supplied generated-ID source factory.
    /// </summary>
    /// <param name="idSourceFactory">The generated-ID source factory used to restore source state.</param>
    /// <returns>A structure snapshot factory for the configured ID source.</returns>
    public static IContentStructureSnapshotFactory CreateSnapshotFactory(IContentGeneratedIdSourceFactory<TId> idSourceFactory)
    {
        return new ContentSequenceStructureSnapshotFactory(idSourceFactory);
    }

    /// <summary>
    /// Gets the generated ID source used by id-less add workflows.
    /// </summary>
    public IContentGeneratedIdSource<TId> IdSource { get; }

    /// <inheritdoc />
    public ContentSequenceReadOrder ReadOrder { get; }

    /// <inheritdoc />
    public ContentOverflowPolicy OverflowPolicy { get; private set; }

    /// <summary>
    /// Gets the number of retained records.
    /// </summary>
    public int Count => _records.Count;

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentSequenceManager<TId>(this);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ContentParameterDefinition> Parameters => s_parameters;

    /// <inheritdoc />
    public virtual IContentStructureSnapshotFactory SnapshotFactory => CreateSnapshotFactory(IdSource.SnapshotFactory);

    /// <inheritdoc />
    public override IReadOnlyList<ContentEntryRecord> Records => CreateReadSnapshot();

    /// <inheritdoc />
    public event EventHandler<ContentChangedEventArgs>? Changed;

    /// <inheritdoc />
    public override bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        record = null;
        if (!IdSource.TryCreateNext(out TId id, out failure))
        {
            return false;
        }

        return TryAddAcceptedId(id, entry, observeId: false, out record, out failure);
    }

    /// <inheritdoc />
    public override ContentEntryRecord Add(IContentEntry entry)
    {
        if (TryAdd(entry, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryAdd(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        return TryAddAcceptedId(id, entry, observeId: true, out record, out failure);
    }

    /// <inheritdoc />
    public override ContentEntryRecord Add(TId id, IContentEntry entry)
    {
        if (TryAdd(id, entry, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
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

    /// <inheritdoc />
    public override bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (!IdSource.IdStrategy.TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            record = null;
            return false;
        }

        return TryGet(normalizedId, out record, out failure);
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        if (!IdSource.IdStrategy.TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            removedRecord = null;
            return false;
        }

        return TryRemove(normalizedId, out removedRecord, out failure);
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

    /// <inheritdoc />
    public override ContentEntryRecord Remove(TId id)
    {
        if (TryRemove(id, out ContentEntryRecord? removedRecord, out ContentFailure? failure))
        {
            return removedRecord!;
        }

        throw new ContentOperationException(failure!);
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

        if (!TryCloneIdSource(out IContentGeneratedIdSource<TId>? clonedSource, out failure))
        {
            return false;
        }

        ContentEntryRecord[] retained = _records.ToArray();
        ContentEntryRecord[] removed = TrimForOverflowPolicy(overflowPolicy, ref retained);
        removedRecords = removed;
        structure = CreateReplacement(clonedSource!, overflowPolicy, ReadOrder, retained);
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public bool TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure)
    {
        snapshot = null;

        if (!ContentSnapshotRecords.TryCapture(_records, out List<ContentRecordSnapshot>? records, out failure)
            || !IdSource.TryCaptureSnapshot(out ContentSnapshotValue? sourceSnapshot, out failure))
        {
            return false;
        }

        snapshot = new ContentStructureSnapshot
        {
            Kind = SnapshotKind,
            DataVersion = SnapshotDataVersion,
            Records = records!,
            Data = ContentSnapshotValue.Object(new[]
            {
                ContentSnapshotProperties.Named("idSourceKind", ContentSnapshotCodecs.Encode(IdSource.SnapshotFactory.Kind)),
                ContentSnapshotProperties.Named("idSourceData", new ContentSnapshotEncodedValue { Data = sourceSnapshot! }),
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

    /// <summary>
    /// Creates a replacement sequence structure for parameter mutations that swap structure state.
    /// </summary>
    /// <param name="idSource">The generated ID source for the replacement structure.</param>
    /// <param name="overflowPolicy">The replacement overflow policy.</param>
    /// <param name="readOrder">The replacement read order.</param>
    /// <param name="records">The retained records for the replacement structure.</param>
    /// <returns>The replacement sequence structure.</returns>
    protected virtual ContentSequenceStructure<TId> CreateReplacement(
        IContentGeneratedIdSource<TId> idSource,
        ContentOverflowPolicy overflowPolicy,
        ContentSequenceReadOrder readOrder,
        IEnumerable<ContentEntryRecord> records)
    {
        return new ContentSequenceStructure<TId>(idSource, overflowPolicy, readOrder, records);
    }

    private bool TryAddAcceptedId(TId id, IContentEntry entry, bool observeId, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        record = null;
        if (!IdSource.IdStrategy.TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            return false;
        }

        if (_records.Any(candidate => candidate.Id.Equals(normalizedId)))
        {
            failure = ContentFailures.EntryIdDuplicate($"Entry ID '{normalizedId}' already exists.", normalizedId.ToString());
            return false;
        }

        if (observeId && !IdSource.TryObserve(id, out failure))
        {
            return false;
        }

        ContentEntryRecord? removedRecord = null;
        if (OverflowPolicy.Kind == ContentOverflowPolicyKind.DropOldest && _records.Count == OverflowPolicy.Capacity)
        {
            removedRecord = _records[0];
            _records.RemoveAt(0);
        }

        record = new ContentEntryRecord(normalizedId, entry);
        _records.Add(record);
        failure = null;
        OnChanged(new ContentChangedEventArgs(
            new[] { record },
            removedRecord is null ? null : new[] { removedRecord },
            ContentChangeKind.Added));
        return true;
    }

    private static void EnsureValidId(ContentEntryId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException("Content entry ID cannot be empty.", nameof(id));
        }
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

    private bool TryCloneIdSource(out IContentGeneratedIdSource<TId>? idSource, out ContentFailure? failure)
    {
        idSource = null;
        if (!IdSource.TryCaptureSnapshot(out ContentSnapshotValue? snapshot, out failure))
        {
            return false;
        }

        return IdSource.SnapshotFactory.TryRestore(snapshot!, out idSource, out failure);
    }

    private void OnChanged(ContentChangedEventArgs args)
    {
        Changed?.Invoke(this, args);
    }

    private sealed class ContentSequenceStructureSnapshotFactory :
        ContentSequenceStructureSnapshotFactoryBase<TId, ContentSequenceStructure<TId>>
    {
        public ContentSequenceStructureSnapshotFactory(IContentGeneratedIdSourceFactory<TId> idSourceFactory)
            : base(SnapshotKind, SnapshotDataVersion, idSourceFactory)
        {
        }

        protected override bool TryRestoreValidatedSequenceSnapshot(
            ContentStructureSnapshot snapshot,
            ContentEntryRecord[] records,
            IContentGeneratedIdSource<TId> idSource,
            out ContentSequenceStructure<TId>? structure,
            out ContentFailure? failure)
        {
            structure = null;

            if (!TryRestoreData(snapshot.Data, out ContentSequenceReadOrder readOrder, out ContentOverflowPolicy overflowPolicy, out failure))
            {
                return false;
            }

            if (overflowPolicy.Kind == ContentOverflowPolicyKind.DropOldest && records.Length > overflowPolicy.Capacity!.Value)
            {
                failure = ContentFailures.SnapshotMalformed("Sequence structure snapshot retains more records than its overflow policy allows.");
                return false;
            }

            structure = new ContentSequenceStructure<TId>(idSource, overflowPolicy, readOrder, records);
            failure = null;
            return true;
        }

        protected override bool TryRestoreIdSource(
            ContentStructureSnapshot snapshot,
            out IContentGeneratedIdSource<TId>? idSource,
            out ContentFailure? failure)
        {
            if (TryGetOptional(snapshot.Data, "idSourceKind", out _))
            {
                return base.TryRestoreIdSource(snapshot, out idSource, out failure);
            }

            if (typeof(TId) == typeof(long)
                && ContentSnapshotProperties.TryDecodeRequiredInt64(snapshot.Data, "nextId", out long legacyNextId, out _))
            {
                if (legacyNextId <= 0)
                {
                    idSource = null;
                    failure = ContentFailures.SnapshotMalformed("Sequence structure snapshot next ID must be greater than zero.");
                    return false;
                }

                var legacyData = ContentSnapshotValue.Object(new[]
                {
                    ContentSnapshotProperties.Named("dataVersion", ContentSnapshotCodecs.Encode(1)),
                    ContentSnapshotProperties.Named("nextId", ContentSnapshotCodecs.Encode(legacyNextId))
                });
                bool restored = LongContentGeneratedIdSource.Factory.TryRestore(
                    legacyData,
                    out IContentGeneratedIdSource<long>? restoredSource,
                    out failure);
                idSource = (IContentGeneratedIdSource<TId>?)(object?)restoredSource;
                return restored;
            }

            idSource = null;
            failure = ContentFailures.SnapshotMalformed("Sequence structure snapshot is missing generated ID source data.");
            return false;
        }

        private static bool TryRestoreData(
            ContentSnapshotValue data,
            out ContentSequenceReadOrder readOrder,
            out ContentOverflowPolicy overflowPolicy,
            out ContentFailure? failure)
        {
            readOrder = ContentSequenceReadOrder.OldestFirst;
            overflowPolicy = ContentOverflowPolicy.None;

            if (data is null || data.Kind != ContentSnapshotValueKind.Object)
            {
                failure = ContentFailures.SnapshotMalformed("Sequence structure snapshot data must be an object.");
                return false;
            }

            if (!TryRestoreReadOrder(data, out readOrder, out failure)
                || !TryRestoreOverflowPolicy(data, out overflowPolicy, out failure))
            {
                return false;
            }

            failure = null;
            return true;
        }

        private static bool TryRestoreReadOrder(
            ContentSnapshotValue data,
            out ContentSequenceReadOrder readOrder,
            out ContentFailure? failure)
        {
            readOrder = ContentSequenceReadOrder.OldestFirst;

            if (!ContentSnapshotProperties.TryDecodeRequiredString(data, "readOrder", out string readOrderText, out failure))
            {
                return false;
            }

            if (!Enum.TryParse(readOrderText, out readOrder) || !Enum.IsDefined(typeof(ContentSequenceReadOrder), readOrder))
            {
                failure = ContentFailures.SnapshotMalformed($"Sequence structure snapshot read order '{readOrderText}' is not supported.");
                return false;
            }

            failure = null;
            return true;
        }

        private static bool TryRestoreOverflowPolicy(
            ContentSnapshotValue data,
            out ContentOverflowPolicy overflowPolicy,
            out ContentFailure? failure)
        {
            overflowPolicy = ContentOverflowPolicy.None;

            if (!ContentSnapshotProperties.TryDecodeRequiredString(data, "overflowKind", out string overflowKindText, out failure))
            {
                return false;
            }

            if (!Enum.TryParse(overflowKindText, out ContentOverflowPolicyKind overflowKind)
                || !Enum.IsDefined(typeof(ContentOverflowPolicyKind), overflowKind))
            {
                failure = ContentFailures.SnapshotMalformed($"Sequence structure snapshot overflow kind '{overflowKindText}' is not supported.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredInt32(data, "overflowCapacity", out int overflowCapacity, out failure))
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

        private static bool TryGetOptional(
            ContentSnapshotValue data,
            string name,
            out ContentSnapshotEncodedValue? value)
        {
            value = data.Properties.FirstOrDefault(candidate => candidate.Name == name)?.Value;
            return value is not null;
        }
    }
}

/// <summary>
/// Stores content records as a long-ID ordered sequence with configurable retention behavior.
/// </summary>
public sealed class ContentSequenceStructure : ContentSequenceStructure<long>
{
    /// <summary>
    /// Gets the snapshot factory for the built-in long-ID sequence structure.
    /// </summary>
    public static IContentStructureSnapshotFactory Factory { get; } = new LongContentSequenceStructureSnapshotFactory();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceStructure"/> class.
    /// </summary>
    /// <param name="overflowPolicy">The retention policy for accepted records.</param>
    /// <param name="readOrder">The order used when exposing retained records.</param>
    public ContentSequenceStructure(
        ContentOverflowPolicy overflowPolicy,
        ContentSequenceReadOrder readOrder = ContentSequenceReadOrder.OldestFirst)
        : base(new LongContentGeneratedIdSource(), overflowPolicy, readOrder)
    {
    }

    private ContentSequenceStructure(
        IContentGeneratedIdSource<long> idSource,
        ContentOverflowPolicy overflowPolicy,
        ContentSequenceReadOrder readOrder,
        IEnumerable<ContentEntryRecord> records)
        : base(idSource, overflowPolicy, readOrder, records)
    {
    }

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentSequenceManager(this);
    }

    /// <inheritdoc />
    public override IContentStructureSnapshotFactory SnapshotFactory => Factory;

    /// <inheritdoc />
    public override bool TryGet(long id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Content sequence entry IDs must be greater than zero.");
        }

        return base.TryGet(id, out record, out failure);
    }

    /// <inheritdoc />
    public override ContentEntryRecord Get(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Content sequence entry IDs must be greater than zero.");
        }

        return base.Get(id);
    }

    /// <inheritdoc />
    protected override ContentSequenceStructure<long> CreateReplacement(
        IContentGeneratedIdSource<long> idSource,
        ContentOverflowPolicy overflowPolicy,
        ContentSequenceReadOrder readOrder,
        IEnumerable<ContentEntryRecord> records)
    {
        return new ContentSequenceStructure(idSource, overflowPolicy, readOrder, records);
    }

    private sealed class LongContentSequenceStructureSnapshotFactory : IContentStructureSnapshotFactory
    {
        private readonly IContentStructureSnapshotFactory _inner =
            ContentSequenceStructure<long>.CreateSnapshotFactory(LongContentGeneratedIdSource.Factory);

        public string Kind => SnapshotKind;

        public bool TryRestore(
            ContentStructureSnapshot snapshot,
            out IContentStructure? structure,
            out ContentFailure? failure)
        {
            structure = null;

            if (!_inner.TryRestore(snapshot, out IContentStructure? restored, out failure))
            {
                return false;
            }

            var generic = (ContentSequenceStructure<long>)restored!;
            if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure))
            {
                return false;
            }

            structure = new ContentSequenceStructure(generic.IdSource, generic.OverflowPolicy, generic.ReadOrder, records);
            failure = null;
            return true;
        }

        public IContentStructure Restore(ContentStructureSnapshot snapshot)
        {
            if (TryRestore(snapshot, out IContentStructure? structure, out ContentFailure? failure) && structure is not null)
            {
                return structure;
            }

            throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
        }
    }
}
