namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content structure that can capture its retained state as a portable snapshot.
/// </summary>
public interface IContentStructureSnapshotSerializable : IContentStructure
{
    /// <summary>
    /// Attempts to capture the structure snapshot.
    /// </summary>
    /// <param name="snapshot">The captured structure snapshot.</param>
    /// <param name="failure">The structured failure when capture is rejected.</param>
    /// <returns><see langword="true"/> when the snapshot was captured.</returns>
    bool TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure);

    /// <summary>
    /// Captures the structure snapshot or throws when capture is rejected.
    /// </summary>
    /// <returns>The captured structure snapshot.</returns>
    ContentStructureSnapshot CaptureSnapshot();
}
