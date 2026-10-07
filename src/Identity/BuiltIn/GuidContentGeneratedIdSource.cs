using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Generates GUID content entry IDs.
/// </summary>
public sealed class GuidContentGeneratedIdSource : IContentGeneratedIdSource<Guid>
{
    /// <summary>
    /// Stable generated ID source snapshot kind for <see cref="GuidContentGeneratedIdSource"/>.
    /// </summary>
    public const string SnapshotKind = ContentFailureCodes.PackagePrefix + "id_source.guid";

    private const int SnapshotDataVersion = 1;

    /// <summary>
    /// Gets the snapshot factory for <see cref="GuidContentGeneratedIdSource"/>.
    /// </summary>
    public static IContentGeneratedIdSourceFactory<Guid> Factory { get; } = new GuidContentGeneratedIdSourceFactory();

    /// <inheritdoc />
    public IContentEntryIdStrategy<Guid> IdStrategy { get; } = new GuidContentEntryIdStrategy();

    /// <inheritdoc />
    public IContentGeneratedIdSourceFactory<Guid> SnapshotFactory => Factory;

    /// <inheritdoc />
    public bool TryCreateNext(out Guid id, out ContentFailure? failure)
    {
        id = Guid.NewGuid();
        if (id == Guid.Empty)
        {
            failure = ContentFailures.EntryIdInvalid("Generated GUID ID source produced an empty GUID.");
            return false;
        }

        failure = null;
        return true;
    }

    /// <inheritdoc />
    public bool TryObserve(Guid id, out ContentFailure? failure)
    {
        return IdStrategy.TryNormalize(id, out _, out failure);
    }

    /// <inheritdoc />
    public bool TryObserveNormalized(ContentEntryId id, out ContentFailure? failure)
    {
        return IdStrategy.TryValidateNormalized(id, out failure);
    }

    /// <inheritdoc />
    public bool TryCaptureSnapshot(out ContentSnapshotValue? snapshot, out ContentFailure? failure)
    {
        snapshot = ContentSnapshotValue.Object(new[]
        {
            ContentSnapshotProperties.Named("dataVersion", ContentSnapshotCodecs.Encode(SnapshotDataVersion))
        });
        failure = null;
        return true;
    }

    private sealed class GuidContentGeneratedIdSourceFactory : IContentGeneratedIdSourceFactory<Guid>
    {
        public string Kind => SnapshotKind;

        public bool TryRestore(ContentSnapshotValue snapshot, out IContentGeneratedIdSource<Guid>? source, out ContentFailure? failure)
        {
            source = null;

            if (snapshot is null || snapshot.Kind != ContentSnapshotValueKind.Object)
            {
                failure = ContentFailures.SnapshotMalformed("GUID generated ID source snapshot data must be an object.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredInt32(snapshot, "dataVersion", out int dataVersion, out failure))
            {
                return false;
            }

            if (dataVersion != SnapshotDataVersion)
            {
                failure = ContentFailures.SnapshotUnsupportedVersion($"GUID generated ID source snapshot version {dataVersion} is not supported.");
                return false;
            }

            source = new GuidContentGeneratedIdSource();
            failure = null;
            return true;
        }

        public IContentGeneratedIdSource<Guid> Restore(ContentSnapshotValue snapshot)
        {
            if (TryRestore(snapshot, out IContentGeneratedIdSource<Guid>? source, out ContentFailure? failure) && source is not null)
            {
                return source;
            }

            throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
        }
    }
}
