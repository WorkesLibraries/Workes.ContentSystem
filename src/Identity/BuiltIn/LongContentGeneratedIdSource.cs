using System.Globalization;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Generates positive integer content entry IDs.
/// </summary>
public sealed class LongContentGeneratedIdSource : IContentGeneratedIdSource<long>
{
    /// <summary>
    /// Stable generated ID source snapshot kind for <see cref="LongContentGeneratedIdSource"/>.
    /// </summary>
    public const string SnapshotKind = ContentFailureCodes.PackagePrefix + "id_source.long";

    private const int SnapshotDataVersion = 1;

    /// <summary>
    /// Gets the snapshot factory for <see cref="LongContentGeneratedIdSource"/>.
    /// </summary>
    public static IContentGeneratedIdSourceFactory<long> Factory { get; } = new LongContentGeneratedIdSourceFactory();

    private long _nextId;

    /// <summary>
    /// Initializes a new instance of the <see cref="LongContentGeneratedIdSource"/> class.
    /// </summary>
    public LongContentGeneratedIdSource()
        : this(1)
    {
    }

    private LongContentGeneratedIdSource(long nextId)
    {
        _nextId = nextId;
    }

    /// <inheritdoc />
    public IContentEntryIdStrategy<long> IdStrategy { get; } = new IntegerContentEntryIdStrategy();

    /// <inheritdoc />
    public IContentGeneratedIdSourceFactory<long> SnapshotFactory => Factory;

    /// <inheritdoc />
    public bool TryCreateNext(out long id, out ContentFailure? failure)
    {
        if (_nextId <= 0 || _nextId == long.MaxValue)
        {
            id = default;
            failure = ContentFailures.EntryIdInvalid("Generated long ID source has no remaining positive IDs.");
            return false;
        }

        id = _nextId;
        _nextId++;
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public bool TryObserve(long id, out ContentFailure? failure)
    {
        if (!IdStrategy.TryNormalize(id, out _, out failure))
        {
            return false;
        }

        if (id == long.MaxValue)
        {
            failure = ContentFailures.EntryIdInvalid("Generated long ID source cannot observe the maximum long value because no later positive ID exists.");
            return false;
        }

        if (id >= _nextId)
        {
            _nextId = id + 1;
        }

        failure = null;
        return true;
    }

    /// <inheritdoc />
    public bool TryObserveNormalized(ContentEntryId id, out ContentFailure? failure)
    {
        if (!IdStrategy.TryValidateNormalized(id, out failure))
        {
            return false;
        }

        long parsed = long.Parse(id.Value, NumberStyles.None, CultureInfo.InvariantCulture);
        return TryObserve(parsed, out failure);
    }

    /// <inheritdoc />
    public bool TryCaptureSnapshot(out ContentSnapshotValue? snapshot, out ContentFailure? failure)
    {
        snapshot = ContentSnapshotValue.Object(new[]
        {
            ContentSnapshotProperties.Named("dataVersion", ContentSnapshotCodecs.Encode(SnapshotDataVersion)),
            ContentSnapshotProperties.Named("nextId", ContentSnapshotCodecs.Encode(_nextId))
        });
        failure = null;
        return true;
    }

    private sealed class LongContentGeneratedIdSourceFactory : IContentGeneratedIdSourceFactory<long>
    {
        public string Kind => SnapshotKind;

        public bool TryRestore(ContentSnapshotValue snapshot, out IContentGeneratedIdSource<long>? source, out ContentFailure? failure)
        {
            source = null;

            if (snapshot is null || snapshot.Kind != ContentSnapshotValueKind.Object)
            {
                failure = ContentFailures.SnapshotMalformed("Long generated ID source snapshot data must be an object.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredInt32(snapshot, "dataVersion", out int dataVersion, out failure))
            {
                return false;
            }

            if (dataVersion != SnapshotDataVersion)
            {
                failure = ContentFailures.SnapshotUnsupportedVersion($"Long generated ID source snapshot version {dataVersion} is not supported.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredInt64(snapshot, "nextId", out long nextId, out failure))
            {
                return false;
            }

            if (nextId <= 0 || nextId == long.MaxValue)
            {
                failure = ContentFailures.SnapshotMalformed("Long generated ID source snapshot next ID must be greater than zero and less than the maximum long value.");
                return false;
            }

            source = new LongContentGeneratedIdSource(nextId);
            failure = null;
            return true;
        }

        public IContentGeneratedIdSource<long> Restore(ContentSnapshotValue snapshot)
        {
            if (TryRestore(snapshot, out IContentGeneratedIdSource<long>? source, out ContentFailure? failure) && source is not null)
            {
                return source;
            }

            throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
        }
    }
}
