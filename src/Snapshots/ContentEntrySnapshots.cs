using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Helper methods for entry snapshot capture and restore workflows.
/// </summary>
public static class ContentEntrySnapshots
{
    /// <summary>
    /// Attempts to capture an entry snapshot.
    /// </summary>
    /// <param name="entry">The entry to capture.</param>
    /// <param name="snapshot">The captured snapshot.</param>
    /// <param name="failure">The structured failure when capture is rejected.</param>
    /// <returns><see langword="true"/> when the snapshot was captured.</returns>
    public static bool TryCapture(
        IContentEntry entry,
        out ContentEntrySnapshot? snapshot,
        out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (entry is not IContentEntrySnapshotSerializable serializable)
        {
            snapshot = null;
            failure = ContentFailures.SnapshotUnsupportedEntry(
                $"Entry type '{entry.GetType().FullName}' does not support content entry snapshots.");
            return false;
        }

        try
        {
            if (serializable.TryCaptureSnapshot(out snapshot, out failure))
            {
                if (snapshot is not null)
                {
                    return true;
                }

                failure = ContentFailures.SnapshotMalformed("Entry snapshot capture succeeded without a snapshot.");
                return false;
            }

            snapshot = null;
            failure ??= ContentFailures.Snapshot();
            return false;
        }
        catch (Exception ex)
        {
            snapshot = null;
            failure = ContentFailure.Wrap(
                ContentFailureKind.Snapshot,
                ContentFailureCodes.SnapshotRejected,
                "Entry snapshot capture failed.",
                ContentFailure.FromException(ex));
            return false;
        }
    }

    /// <summary>
    /// Captures an entry snapshot or throws when capture is rejected.
    /// </summary>
    /// <param name="entry">The entry to capture.</param>
    /// <returns>The captured snapshot.</returns>
    public static ContentEntrySnapshot Capture(IContentEntry entry)
    {
        if (TryCapture(entry, out ContentEntrySnapshot? snapshot, out ContentFailure? failure) && snapshot is not null)
        {
            return snapshot;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    /// <summary>
    /// Attempts to restore an entry snapshot with an explicit factory.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore.</param>
    /// <param name="factory">The factory that restores the snapshot kind.</param>
    /// <param name="entry">The restored entry.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when the entry was restored.</returns>
    public static bool TryRestore(
        ContentEntrySnapshot snapshot,
        IContentEntrySnapshotFactory factory,
        out IContentEntry? entry,
        out ContentFailure? failure)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        entry = null;
        failure = null;

        if (string.IsNullOrWhiteSpace(snapshot.Kind))
        {
            failure = ContentFailures.SnapshotMalformed("Entry snapshot is missing a kind.");
            return false;
        }

        if (snapshot.Data is null)
        {
            failure = ContentFailures.SnapshotMalformed("Entry snapshot is missing data.");
            return false;
        }

        if (!string.Equals(snapshot.Kind, factory.Kind, StringComparison.Ordinal))
        {
            failure = ContentFailures.SnapshotMalformed(
                $"Entry snapshot kind '{snapshot.Kind}' does not match factory kind '{factory.Kind}'.");
            return false;
        }

        try
        {
            if (factory.TryRestore(snapshot, out entry, out failure))
            {
                if (entry is not null)
                {
                    return true;
                }

                failure = ContentFailures.SnapshotMalformed("Entry snapshot restore succeeded without an entry.");
                return false;
            }

            entry = null;
            failure ??= ContentFailures.Snapshot();
            return false;
        }
        catch (Exception ex)
        {
            entry = null;
            failure = ContentFailure.Wrap(
                ContentFailureKind.Snapshot,
                ContentFailureCodes.SnapshotRejected,
                "Entry snapshot restore failed.",
                ContentFailure.FromException(ex));
            return false;
        }
    }

    /// <summary>
    /// Restores an entry snapshot with an explicit factory or throws when restore is rejected.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore.</param>
    /// <param name="factory">The factory that restores the snapshot kind.</param>
    /// <returns>The restored entry.</returns>
    public static IContentEntry Restore(ContentEntrySnapshot snapshot, IContentEntrySnapshotFactory factory)
    {
        if (TryRestore(snapshot, factory, out IContentEntry? entry, out ContentFailure? failure) && entry is not null)
        {
            return entry;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }
}
