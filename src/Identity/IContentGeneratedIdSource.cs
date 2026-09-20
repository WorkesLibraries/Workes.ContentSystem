namespace Workes.ContentSystem.Core;

/// <summary>
/// Generates and tracks content entry IDs for structures that support automatic IDs.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public interface IContentGeneratedIdSource<TId>
{
    /// <summary>
    /// Gets the strategy used to validate and normalize IDs produced or observed by this source.
    /// </summary>
    IContentEntryIdStrategy<TId> IdStrategy { get; }

    /// <summary>
    /// Gets the factory used to restore this source from snapshot state.
    /// </summary>
    IContentGeneratedIdSourceFactory<TId> SnapshotFactory { get; }

    /// <summary>
    /// Attempts to create the next generated ID.
    /// </summary>
    bool TryCreateNext(out TId id, out ContentFailure? failure);

    /// <summary>
    /// Attempts to observe an accepted caller-facing ID so future generated IDs remain coherent.
    /// </summary>
    bool TryObserve(TId id, out ContentFailure? failure);

    /// <summary>
    /// Attempts to observe an accepted normalized stored ID, such as one restored from a snapshot.
    /// </summary>
    bool TryObserveNormalized(ContentEntryId id, out ContentFailure? failure);

    /// <summary>
    /// Attempts to capture source-owned snapshot state.
    /// </summary>
    bool TryCaptureSnapshot(out ContentSnapshotValue? snapshot, out ContentFailure? failure);
}
