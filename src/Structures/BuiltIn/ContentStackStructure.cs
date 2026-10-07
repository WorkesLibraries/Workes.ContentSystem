using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Stores content records as a stack with push, peek, and pop behavior.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public class ContentStackStructure<TId> :
    ContentStackStructureBase<TId>,
    IContentRetentionPolicyStructure,
    IContentStructureSnapshotRoundTrippable,
    IContentChangeSource
{
    /// <summary>
    /// Gets the stable snapshot kind for stack structures.
    /// </summary>
    public const string SnapshotKind = ContentFailureCodes.PackagePrefix + "structure.stack";

    /// <summary>
    /// Gets the snapshot data version for stack structures.
    /// </summary>
    public const int SnapshotDataVersion = 1;

    private readonly List<ContentEntryRecord> _records = new List<ContentEntryRecord>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStackStructure{TId}"/> class.
    /// </summary>
    public ContentStackStructure(
        IContentGeneratedIdSource<TId> idSource,
        ContentOverflowPolicy overflowPolicy,
        ContentStackReadOrder readOrder = ContentStackReadOrder.TopFirst)
        : this(idSource, overflowPolicy, readOrder, Enumerable.Empty<ContentEntryRecord>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStackStructure{TId}"/> class with retained records.
    /// </summary>
    protected ContentStackStructure(
        IContentGeneratedIdSource<TId> idSource,
        ContentOverflowPolicy overflowPolicy,
        ContentStackReadOrder readOrder,
        IEnumerable<ContentEntryRecord> records)
    {
        IdSource = idSource ?? throw new ArgumentNullException(nameof(idSource));
        OverflowPolicy = overflowPolicy ?? throw new ArgumentNullException(nameof(overflowPolicy));
        if (OverflowPolicy.Kind == ContentOverflowPolicyKind.DropOldest)
        {
            throw new ArgumentException("Stack structures support only None and Reject overflow policies.", nameof(overflowPolicy));
        }

        if (!Enum.IsDefined(typeof(ContentStackReadOrder), readOrder))
        {
            throw new ArgumentOutOfRangeException(nameof(readOrder), readOrder, "Content stack read order is not supported.");
        }

        ReadOrder = readOrder;
        _records.AddRange(records ?? throw new ArgumentNullException(nameof(records)));
    }

    /// <summary>
    /// Creates a snapshot factory for stack structures that use the provided generated-ID source factory.
    /// </summary>
    public static IContentStructureSnapshotFactory CreateSnapshotFactory(IContentGeneratedIdSourceFactory<TId> idSourceFactory)
    {
        return new ContentStackStructureSnapshotFactory(idSourceFactory);
    }

    /// <summary>
    /// Gets the generated ID source used by id-less pushes.
    /// </summary>
    public IContentGeneratedIdSource<TId> IdSource { get; }

    /// <summary>
    /// Gets the stack overflow policy.
    /// </summary>
    public ContentOverflowPolicy OverflowPolicy { get; }

    /// <summary>
    /// Gets the retained-record read order.
    /// </summary>
    public ContentStackReadOrder ReadOrder { get; }

    /// <summary>
    /// Gets the number of retained records.
    /// </summary>
    public int Count => _records.Count;

    /// <inheritdoc />
    public virtual IContentStructureSnapshotFactory SnapshotFactory => CreateSnapshotFactory(IdSource.SnapshotFactory);

    /// <inheritdoc />
    public override IReadOnlyList<ContentEntryRecord> Records => CreateReadSnapshot();

    /// <inheritdoc />
    public event EventHandler<ContentChangedEventArgs>? Changed;

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentStackManager<TId>(this);
    }

    /// <inheritdoc />
    public override bool TryPush(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        record = null;
        if (!TryValidateCapacityForPush(out failure))
        {
            return false;
        }

        if (!IdSource.TryCreateNext(out TId id, out failure))
        {
            return false;
        }

        return TryPushAcceptedId(id, entry, observeId: false, out record, out failure);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessPush(IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (!TryValidateCapacityForPush(out ContentFailure? failure)
            || !IdSource.CanCreateNext(out failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public override ContentEntryRecord Push(IContentEntry entry)
    {
        if (TryPush(entry, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryPush(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        return TryPushAcceptedId(id, entry, observeId: true, out record, out failure);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessPush(TId id, IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (!IdSource.IdStrategy.TryNormalize(id, out ContentEntryId normalizedId, out ContentFailure? failure)
            || !AssessDuplicateFree(normalizedId, out failure)
            || !TryValidateCapacityForPush(out failure)
            || !IdSource.CanObserve(id, out failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public override ContentEntryRecord Push(TId id, IContentEntry entry)
    {
        if (TryPush(id, entry, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryPeek(out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (_records.Count == 0)
        {
            record = null;
            failure = ContentFailures.EntryNotFound("The content stack is empty.");
            return false;
        }

        record = _records[_records.Count - 1];
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessPeek()
    {
        return _records.Count == 0
            ? ContentPreflightResult.Rejected(ContentFailures.EntryNotFound("The content stack is empty."))
            : ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public override ContentEntryRecord Peek()
    {
        if (TryPeek(out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryPop(out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (!TryPeek(out record, out failure))
        {
            return false;
        }

        _records.RemoveAt(_records.Count - 1);
        failure = null;
        OnChanged(new ContentChangedEventArgs(removedRecords: new[] { record! }, kind: ContentChangeKind.Removed));
        return true;
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessPop()
    {
        return AssessPeek();
    }

    /// <inheritdoc />
    public override ContentEntryRecord Pop()
    {
        if (TryPop(out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        EnsureValidId(id);
        record = _records.FirstOrDefault(candidate => candidate.Id == id);
        if (record is not null)
        {
            failure = null;
            return true;
        }

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
    public override bool TryRemove(ContentEntryId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        EnsureValidId(id);
        removedRecord = _records.FirstOrDefault(candidate => candidate.Id == id);
        if (removedRecord is null)
        {
            failure = ContentFailures.EntryNotFound($"Entry '{id}' was not found.", id.ToString());
            return false;
        }

        _records.Remove(removedRecord);
        failure = null;
        OnChanged(new ContentChangedEventArgs(removedRecords: new[] { removedRecord }, kind: ContentChangeKind.Removed));
        return true;
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessRemove(ContentEntryId id)
    {
        EnsureValidId(id);
        return _records.Any(candidate => candidate.Id == id)
            ? ContentPreflightResult.Success()
            : ContentPreflightResult.Rejected(ContentFailures.EntryNotFound($"Entry '{id}' was not found.", id.ToString()));
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
    public override ContentPreflightResult AssessRemove(TId id)
    {
        if (!IdSource.IdStrategy.TryNormalize(id, out ContentEntryId normalizedId, out ContentFailure? failure))
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
    public bool TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure)
    {
        if (!ContentSnapshotRecords.TryCapture(_records, out List<ContentRecordSnapshot>? records, out failure)
            || !IdSource.TryCaptureSnapshot(out ContentSnapshotValue? sourceSnapshot, out failure))
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

    private bool TryPushAcceptedId(TId id, IContentEntry entry, bool observeId, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        record = null;
        if (!IdSource.IdStrategy.TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            return false;
        }

        if (_records.Any(candidate => candidate.Id == normalizedId))
        {
            failure = ContentFailures.EntryIdDuplicate($"Entry ID '{normalizedId}' already exists.", normalizedId.ToString());
            return false;
        }

        if (!TryValidateCapacityForPush(out failure))
        {
            return false;
        }

        if (observeId && !IdSource.TryObserve(id, out failure))
        {
            return false;
        }

        record = new ContentEntryRecord(normalizedId, entry);
        _records.Add(record);
        failure = null;
        OnChanged(new ContentChangedEventArgs(new[] { record }, kind: ContentChangeKind.Added));
        return true;
    }

    private bool AssessDuplicateFree(ContentEntryId normalizedId, out ContentFailure? failure)
    {
        if (_records.Any(candidate => candidate.Id == normalizedId))
        {
            failure = ContentFailures.EntryIdDuplicate($"Entry ID '{normalizedId}' already exists.", normalizedId.ToString());
            return false;
        }

        failure = null;
        return true;
    }

    private bool TryValidateCapacityForPush(out ContentFailure? failure)
    {
        if (OverflowPolicy.Kind == ContentOverflowPolicyKind.Reject && _records.Count >= OverflowPolicy.Capacity!.Value)
        {
            failure = ContentFailures.StructureCapacityReached(
                $"Content stack capacity {OverflowPolicy.Capacity.Value} has been reached.",
                OverflowPolicy.Capacity.Value.ToString(CultureInfo.InvariantCulture));
            return false;
        }

        failure = null;
        return true;
    }

    private IReadOnlyList<ContentEntryRecord> CreateReadSnapshot()
    {
        ContentEntryRecord[] snapshot = _records.ToArray();
        if (ReadOrder == ContentStackReadOrder.TopFirst)
        {
            Array.Reverse(snapshot);
        }

        return snapshot;
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

    private sealed class ContentStackStructureSnapshotFactory :
        ContentStructureSnapshotFactoryBase<ContentStackStructure<TId>>
    {
        private readonly IContentGeneratedIdSourceFactory<TId> _idSourceFactory;

        public ContentStackStructureSnapshotFactory(IContentGeneratedIdSourceFactory<TId> idSourceFactory)
            : base(SnapshotKind, SnapshotDataVersion)
        {
            _idSourceFactory = idSourceFactory ?? throw new ArgumentNullException(nameof(idSourceFactory));
        }

        protected override bool TryRestoreValidatedSnapshot(
            ContentStructureSnapshot snapshot,
            out ContentStackStructure<TId>? structure,
            out ContentFailure? failure)
        {
            structure = null;
            if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure)
                || !TryRestoreData(snapshot.Data, out ContentStackReadOrder readOrder, out ContentOverflowPolicy overflowPolicy, out IContentGeneratedIdSource<TId>? idSource, out failure))
            {
                return false;
            }

            if (overflowPolicy.Kind == ContentOverflowPolicyKind.Reject && records.Length > overflowPolicy.Capacity!.Value)
            {
                failure = ContentFailures.SnapshotMalformed("Stack structure snapshot retains more records than its reject overflow policy allows.");
                return false;
            }

            foreach (ContentEntryRecord record in records)
            {
                if (!idSource!.TryObserveNormalized(record.Id, out failure))
                {
                    return false;
                }
            }

            structure = new ContentStackStructure<TId>(idSource!, overflowPolicy, readOrder, records);
            failure = null;
            return true;
        }

        private bool TryRestoreData(
            ContentSnapshotValue data,
            out ContentStackReadOrder readOrder,
            out ContentOverflowPolicy overflowPolicy,
            out IContentGeneratedIdSource<TId>? idSource,
            out ContentFailure? failure)
        {
            readOrder = ContentStackReadOrder.TopFirst;
            overflowPolicy = ContentOverflowPolicy.None;
            idSource = null;
            if (data is null || data.Kind != ContentSnapshotValueKind.Object)
            {
                failure = ContentFailures.SnapshotMalformed("Stack structure snapshot data must be an object.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredString(data, "readOrder", out string readOrderText, out failure)
                || !Enum.TryParse(readOrderText, out readOrder)
                || !Enum.IsDefined(typeof(ContentStackReadOrder), readOrder))
            {
                failure = failure ?? ContentFailures.SnapshotMalformed($"Stack structure snapshot read order '{readOrderText}' is not supported.");
                return false;
            }

            if (!TryRestoreOverflowPolicy(data, out overflowPolicy, out failure))
            {
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredString(data, "idSourceKind", out string sourceKind, out failure))
            {
                return false;
            }

            if (sourceKind != _idSourceFactory.Kind)
            {
                failure = ContentFailures.SnapshotMalformed($"Stack structure snapshot generated ID source '{sourceKind}' does not match the configured source factory.");
                return false;
            }

            ContentSnapshotEncodedValue? sourceData = data.Properties.FirstOrDefault(candidate => candidate.Name == "idSourceData")?.Value;
            if (sourceData?.Data is null)
            {
                failure = ContentFailures.SnapshotMalformed("Stack structure snapshot is missing generated ID source data.");
                return false;
            }

            return _idSourceFactory.TryRestore(sourceData.Data, out idSource, out failure);
        }

        private static bool TryRestoreOverflowPolicy(ContentSnapshotValue data, out ContentOverflowPolicy overflowPolicy, out ContentFailure? failure)
        {
            overflowPolicy = ContentOverflowPolicy.None;
            if (!ContentSnapshotProperties.TryDecodeRequiredString(data, "overflowKind", out string overflowKindText, out failure)
                || !Enum.TryParse(overflowKindText, out ContentOverflowPolicyKind overflowKind)
                || !Enum.IsDefined(typeof(ContentOverflowPolicyKind), overflowKind))
            {
                failure = failure ?? ContentFailures.SnapshotMalformed($"Stack structure snapshot overflow kind '{overflowKindText}' is not supported.");
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
                    failure = ContentFailures.SnapshotMalformed("Unbounded stack snapshots must use overflow capacity 0.");
                    return false;
                }

                overflowPolicy = ContentOverflowPolicy.None;
                return true;
            }

            if (overflowKind == ContentOverflowPolicyKind.Reject)
            {
                if (overflowCapacity <= 0)
                {
                    failure = ContentFailures.SnapshotMalformed("Reject stack snapshots must use a positive overflow capacity.");
                    return false;
                }

                overflowPolicy = ContentOverflowPolicy.Reject(overflowCapacity);
                return true;
            }

            failure = ContentFailures.SnapshotMalformed($"Stack structure snapshot overflow kind '{overflowKind}' is not supported.");
            return false;
        }
    }
}

/// <summary>
/// Stores content records as a long-ID stack.
/// </summary>
public sealed class ContentStackStructure : ContentStackStructure<long>
{
    /// <summary>
    /// Gets the snapshot factory for long-ID stack structures.
    /// </summary>
    public static IContentStructureSnapshotFactory Factory { get; } = new LongContentStackStructureSnapshotFactory();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStackStructure"/> class.
    /// </summary>
    public ContentStackStructure(
        ContentOverflowPolicy overflowPolicy,
        ContentStackReadOrder readOrder = ContentStackReadOrder.TopFirst)
        : base(new LongContentGeneratedIdSource(), overflowPolicy, readOrder)
    {
    }

    private ContentStackStructure(
        IContentGeneratedIdSource<long> idSource,
        ContentOverflowPolicy overflowPolicy,
        ContentStackReadOrder readOrder,
        IEnumerable<ContentEntryRecord> records)
        : base(idSource, overflowPolicy, readOrder, records)
    {
    }

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentStackManager(this);
    }

    /// <inheritdoc />
    public override IContentStructureSnapshotFactory SnapshotFactory => Factory;

    private sealed class LongContentStackStructureSnapshotFactory : IContentStructureSnapshotFactory
    {
        private readonly IContentStructureSnapshotFactory _inner =
            ContentStackStructure<long>.CreateSnapshotFactory(LongContentGeneratedIdSource.Factory);

        public string Kind => SnapshotKind;

        public bool TryRestore(ContentStructureSnapshot snapshot, out IContentStructure? structure, out ContentFailure? failure)
        {
            structure = null;
            if (!_inner.TryRestore(snapshot, out IContentStructure? restored, out failure))
            {
                return false;
            }

            var generic = (ContentStackStructure<long>)restored!;
            if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure))
            {
                return false;
            }

            structure = new ContentStackStructure(generic.IdSource, generic.OverflowPolicy, generic.ReadOrder, records);
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
