namespace Workes.ContentSystem.Core;

/// <summary>
/// Opt-in contract for entries that can capture a portable snapshot of their payload.
/// </summary>
public interface IContentEntrySnapshotSerializable
{
    /// <summary>
    /// Attempts to capture the entry payload as a portable snapshot.
    /// </summary>
    /// <param name="snapshot">The captured snapshot.</param>
    /// <param name="failure">The structured failure when capture is rejected.</param>
    /// <returns><see langword="true"/> when the snapshot was captured.</returns>
    bool TryCaptureSnapshot(out ContentEntrySnapshot? snapshot, out ContentFailure? failure);

    /// <summary>
    /// Captures the entry payload as a portable snapshot or throws when capture is rejected.
    /// </summary>
    /// <returns>The captured snapshot.</returns>
    ContentEntrySnapshot CaptureSnapshot();
}
