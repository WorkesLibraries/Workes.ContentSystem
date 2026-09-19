namespace Workes.ContentSystem.Core;

/// <summary>
/// Restores content structures from portable structure snapshots.
/// </summary>
public interface IContentStructureSnapshotFactory
{
    /// <summary>
    /// Gets the snapshot kind restored by this factory.
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Attempts to restore a content structure from a snapshot.
    /// </summary>
    /// <param name="snapshot">The structure snapshot.</param>
    /// <param name="structure">The restored structure.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when the structure was restored.</returns>
    bool TryRestore(
        ContentStructureSnapshot snapshot,
        out IContentStructure? structure,
        out ContentFailure? failure);

    /// <summary>
    /// Restores a content structure from a snapshot or throws when restore is rejected.
    /// </summary>
    /// <param name="snapshot">The structure snapshot.</param>
    /// <returns>The restored structure.</returns>
    IContentStructure Restore(ContentStructureSnapshot snapshot);
}
