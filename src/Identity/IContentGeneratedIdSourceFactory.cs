namespace Workes.ContentSystem.Core;

/// <summary>
/// Restores generated ID sources from snapshot state.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public interface IContentGeneratedIdSourceFactory<TId>
{
    /// <summary>
    /// Gets the stable generated ID source snapshot kind.
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Attempts to restore a generated ID source from snapshot state.
    /// </summary>
    bool TryRestore(ContentSnapshotValue snapshot, out IContentGeneratedIdSource<TId>? source, out ContentFailure? failure);

    /// <summary>
    /// Restores a generated ID source from snapshot state.
    /// </summary>
    IContentGeneratedIdSource<TId> Restore(ContentSnapshotValue snapshot);
}
