using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Stores zero or one current content record.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public class ContentSingleStructure<TId> :
    ContentSingleStructureBase<TId>,
    IContentStructureSnapshotRoundTrippable,
    IContentChangeSource
{
    /// <summary>
    /// Stable structure snapshot kind for content single structures.
    /// </summary>
    public const string SnapshotKind = ContentFailureCodes.PackagePrefix + "structure.single";

    /// <summary>
    /// Current structure snapshot data version for content single structures.
    /// </summary>
    public const int SnapshotDataVersion = 1;

    private ContentEntryRecord? _record;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSingleStructure{TId}"/> class.
    /// </summary>
    public ContentSingleStructure(
        IContentGeneratedIdSource<TId> idSource,
        ContentSingleReplacementPolicy replacementPolicy = ContentSingleReplacementPolicy.Replace)
        : this(idSource, replacementPolicy, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSingleStructure{TId}"/> class with an existing current record.
    /// </summary>
    protected ContentSingleStructure(
        IContentGeneratedIdSource<TId> idSource,
        ContentSingleReplacementPolicy replacementPolicy,
        ContentEntryRecord? record)
    {
        IdSource = idSource ?? throw new ArgumentNullException(nameof(idSource));
        if (!Enum.IsDefined(typeof(ContentSingleReplacementPolicy), replacementPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(replacementPolicy), replacementPolicy, "Content single replacement policy is not supported.");
        }

        ReplacementPolicy = replacementPolicy;
        _record = record;
    }

    /// <summary>
    /// Creates a snapshot factory for generic single-entry structures using the supplied generated-ID source factory.
    /// </summary>
    public static IContentStructureSnapshotFactory CreateSnapshotFactory(IContentGeneratedIdSourceFactory<TId> idSourceFactory)
    {
        return new ContentSingleStructureSnapshotFactory(idSourceFactory);
    }

    /// <summary>
    /// Gets the generated ID source used by id-less set workflows.
    /// </summary>
    public IContentGeneratedIdSource<TId> IdSource { get; }

    /// <summary>
    /// Gets the replacement policy used when a record is already retained.
    /// </summary>
    public ContentSingleReplacementPolicy ReplacementPolicy { get; }

    /// <summary>
    /// Gets the number of retained records.
    /// </summary>
    public int Count => _record is null ? 0 : 1;

    /// <inheritdoc />
    public override IReadOnlyList<ContentEntryRecord> Records => _record is null
        ? Array.Empty<ContentEntryRecord>()
        : new[] { _record };

    /// <inheritdoc />
    public virtual IContentStructureSnapshotFactory SnapshotFactory => CreateSnapshotFactory(IdSource.SnapshotFactory);

    /// <inheritdoc />
    public event EventHandler<ContentChangedEventArgs>? Changed;

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentSingleManager<TId>(this);
    }

    /// <inheritdoc />
    public override bool TrySet(IContentEntry entry, out ContentEntryRecord? record, out ContentEntryRecord? replacedRecord, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        record = null;
        replacedRecord = null;
        if (_record is not null && ReplacementPolicy == ContentSingleReplacementPolicy.Reject)
        {
            failure = ContentFailures.StructureCapacityReached("Content single structure already contains a record.");
            return false;
        }

        if (!IdSource.TryCreateNext(out TId id, out failure))
        {
            return false;
        }

        return TrySetAcceptedId(id, entry, observeId: false, out record, out replacedRecord, out failure);
    }

    /// <inheritdoc />
    public override ContentEntryRecord Set(IContentEntry entry)
    {
        if (TrySet(entry, out ContentEntryRecord? record, out _, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TrySet(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentEntryRecord? replacedRecord, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        return TrySetAcceptedId(id, entry, observeId: true, out record, out replacedRecord, out failure);
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
    public override bool TryGetCurrent(out ContentEntryRecord? record, out ContentFailure? failure)
    {
        record = _record;
        if (record is null)
        {
            failure = ContentFailures.EntryNotFound("No current content record was found.");
            return false;
        }

        failure = null;
        return true;
    }

    /// <inheritdoc />
    public override ContentEntryRecord GetCurrent()
    {
        if (TryGetCurrent(out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        EnsureValidId(id);
        if (_record is not null && _record.Id == id)
        {
            record = _record;
            failure = null;
            return true;
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
    public override bool TryRemove(ContentEntryId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        EnsureValidId(id);
        if (_record is null || _record.Id != id)
        {
            removedRecord = null;
            failure = ContentFailures.EntryNotFound($"Entry '{id}' was not found.", id.ToString());
            return false;
        }

        removedRecord = _record;
        _record = null;
        failure = null;
        OnChanged(new ContentChangedEventArgs(removedRecords: new[] { removedRecord }, kind: ContentChangeKind.Removed));
        return true;
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
    public override bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        if (_record is null)
        {
            removedRecords = Array.Empty<ContentEntryRecord>();
            failure = null;
            return true;
        }

        removedRecords = new[] { _record };
        _record = null;
        failure = null;
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
    public bool TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure)
    {
        if (!ContentSnapshotRecords.TryCapture(Records, out List<ContentRecordSnapshot>? records, out failure)
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
                ContentSnapshotProperties.Named("replacementPolicy", ContentSnapshotCodecs.Encode(ReplacementPolicy.ToString()))
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

    private bool TrySetAcceptedId(
        TId id,
        IContentEntry entry,
        bool observeId,
        out ContentEntryRecord? record,
        out ContentEntryRecord? replacedRecord,
        out ContentFailure? failure)
    {
        record = null;
        replacedRecord = null;
        if (!IdSource.IdStrategy.TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            return false;
        }

        if (_record is not null && ReplacementPolicy == ContentSingleReplacementPolicy.Reject)
        {
            failure = ContentFailures.StructureCapacityReached("Content single structure already contains a record.");
            return false;
        }

        if (observeId && !IdSource.TryObserve(id, out failure))
        {
            return false;
        }

        replacedRecord = _record;
        record = new ContentEntryRecord(normalizedId, entry);
        _record = record;
        failure = null;
        OnChanged(new ContentChangedEventArgs(
            addedRecords: new[] { record },
            removedRecords: replacedRecord is null ? null : new[] { replacedRecord },
            kind: replacedRecord is null ? ContentChangeKind.Added : ContentChangeKind.Replaced));
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

    private sealed class ContentSingleStructureSnapshotFactory :
        ContentStructureSnapshotFactoryBase<ContentSingleStructure<TId>>
    {
        private readonly IContentGeneratedIdSourceFactory<TId> _idSourceFactory;

        public ContentSingleStructureSnapshotFactory(IContentGeneratedIdSourceFactory<TId> idSourceFactory)
            : base(SnapshotKind, SnapshotDataVersion)
        {
            _idSourceFactory = idSourceFactory ?? throw new ArgumentNullException(nameof(idSourceFactory));
        }

        protected override bool TryRestoreValidatedSnapshot(
            ContentStructureSnapshot snapshot,
            out ContentSingleStructure<TId>? structure,
            out ContentFailure? failure)
        {
            structure = null;
            if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure))
            {
                return false;
            }

            if (records.Length > 1)
            {
                failure = ContentFailures.SnapshotMalformed("Single structure snapshots cannot contain more than one record.");
                return false;
            }

            if (!TryRestoreData(snapshot.Data, out ContentSingleReplacementPolicy replacementPolicy, out IContentGeneratedIdSource<TId>? idSource, out failure))
            {
                return false;
            }

            foreach (ContentEntryRecord record in records)
            {
                if (!idSource!.TryObserveNormalized(record.Id, out failure))
                {
                    return false;
                }
            }

            structure = new ContentSingleStructure<TId>(idSource!, replacementPolicy, records.FirstOrDefault());
            failure = null;
            return true;
        }

        private bool TryRestoreData(
            ContentSnapshotValue data,
            out ContentSingleReplacementPolicy replacementPolicy,
            out IContentGeneratedIdSource<TId>? idSource,
            out ContentFailure? failure)
        {
            replacementPolicy = ContentSingleReplacementPolicy.Replace;
            idSource = null;
            if (data is null || data.Kind != ContentSnapshotValueKind.Object)
            {
                failure = ContentFailures.SnapshotMalformed("Single structure snapshot data must be an object.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredString(data, "replacementPolicy", out string replacementPolicyText, out failure))
            {
                return false;
            }

            if (!Enum.TryParse(replacementPolicyText, out replacementPolicy) || !Enum.IsDefined(typeof(ContentSingleReplacementPolicy), replacementPolicy))
            {
                failure = ContentFailures.SnapshotMalformed($"Single structure snapshot replacement policy '{replacementPolicyText}' is not supported.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredString(data, "idSourceKind", out string sourceKind, out failure))
            {
                return false;
            }

            if (sourceKind != _idSourceFactory.Kind)
            {
                failure = ContentFailures.SnapshotMalformed($"Single structure snapshot generated ID source '{sourceKind}' does not match the configured source factory.");
                return false;
            }

            ContentSnapshotEncodedValue? sourceData = data.Properties.FirstOrDefault(candidate => candidate.Name == "idSourceData")?.Value;
            if (sourceData?.Data is null)
            {
                failure = ContentFailures.SnapshotMalformed("Single structure snapshot is missing generated ID source data.");
                return false;
            }

            return _idSourceFactory.TryRestore(sourceData.Data, out idSource, out failure);
        }
    }
}

/// <summary>
/// Stores zero or one current content record using generated long IDs.
/// </summary>
public sealed class ContentSingleStructure : ContentSingleStructure<long>
{
    /// <summary>
    /// Gets the snapshot factory for the built-in long-ID single structure.
    /// </summary>
    public static IContentStructureSnapshotFactory Factory { get; } = new LongContentSingleStructureSnapshotFactory();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSingleStructure"/> class.
    /// </summary>
    public ContentSingleStructure(ContentSingleReplacementPolicy replacementPolicy = ContentSingleReplacementPolicy.Replace)
        : base(new LongContentGeneratedIdSource(), replacementPolicy)
    {
    }

    private ContentSingleStructure(
        IContentGeneratedIdSource<long> idSource,
        ContentSingleReplacementPolicy replacementPolicy,
        ContentEntryRecord? record)
        : base(idSource, replacementPolicy, record)
    {
    }

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentSingleManager(this);
    }

    /// <inheritdoc />
    public override IContentStructureSnapshotFactory SnapshotFactory => Factory;

    private sealed class LongContentSingleStructureSnapshotFactory : IContentStructureSnapshotFactory
    {
        private readonly IContentStructureSnapshotFactory _inner =
            ContentSingleStructure<long>.CreateSnapshotFactory(LongContentGeneratedIdSource.Factory);

        public string Kind => SnapshotKind;

        public bool TryRestore(ContentStructureSnapshot snapshot, out IContentStructure? structure, out ContentFailure? failure)
        {
            structure = null;
            if (!_inner.TryRestore(snapshot, out IContentStructure? restored, out failure))
            {
                return false;
            }

            var generic = (ContentSingleStructure<long>)restored!;
            ContentEntryRecord? record = generic.Records.Count == 0 ? null : generic.Records[0];
            structure = new ContentSingleStructure(generic.IdSource, generic.ReplacementPolicy, record);
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
