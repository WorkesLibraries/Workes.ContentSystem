namespace Workes.ContentSystem.Core;

/// <summary>
/// Restores content entries from portable entry snapshots.
/// </summary>
public interface IContentEntrySnapshotFactory
{
    /// <summary>
    /// Gets the stable entry snapshot kind restored by this factory.
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Attempts to restore an entry from a portable snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore.</param>
    /// <param name="entry">The restored entry.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when the entry was restored.</returns>
    bool TryRestore(ContentEntrySnapshot snapshot, out IContentEntry? entry, out ContentFailure? failure);

    /// <summary>
    /// Restores an entry from a portable snapshot or throws when restore is rejected.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore.</param>
    /// <returns>The restored entry.</returns>
    IContentEntry Restore(ContentEntrySnapshot snapshot);
}
