using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Helper methods for structure snapshot capture and restore workflows.
/// </summary>
public static class ContentStructureSnapshots
{
    /// <summary>
    /// Attempts to capture a structure snapshot.
    /// </summary>
    /// <param name="structure">The structure to capture.</param>
    /// <param name="snapshot">The captured snapshot.</param>
    /// <param name="failure">The structured failure when capture is rejected.</param>
    /// <returns><see langword="true"/> when the snapshot was captured.</returns>
    public static bool TryCapture(
        IContentStructure structure,
        out ContentStructureSnapshot? snapshot,
        out ContentFailure? failure)
    {
        if (structure is null)
        {
            throw new ArgumentNullException(nameof(structure));
        }

        if (structure is not IContentStructureSnapshotRoundTrippable roundTrippable)
        {
            snapshot = null;
            failure = ContentFailures.SnapshotUnsupportedStructure(
                $"Structure type '{structure.GetType().FullName}' does not support content structure snapshots.");
            return false;
        }

        try
        {
            if (roundTrippable.TryCaptureSnapshot(out snapshot, out failure))
            {
                if (snapshot is not null)
                {
                    return true;
                }

                failure = ContentFailures.SnapshotMalformed("Structure snapshot capture succeeded without a snapshot.");
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
                "Structure snapshot capture failed.",
                ContentFailure.FromException(ex));
            return false;
        }
    }

    /// <summary>
    /// Captures a structure snapshot or throws when capture is rejected.
    /// </summary>
    /// <param name="structure">The structure to capture.</param>
    /// <returns>The captured snapshot.</returns>
    public static ContentStructureSnapshot Capture(IContentStructure structure)
    {
        if (TryCapture(structure, out ContentStructureSnapshot? snapshot, out ContentFailure? failure) && snapshot is not null)
        {
            return snapshot;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    /// <summary>
    /// Attempts to restore a structure snapshot.
    /// </summary>
    /// <param name="snapshot">The structure snapshot.</param>
    /// <param name="factory">The structure factory.</param>
    /// <param name="structure">The restored structure.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when the structure was restored.</returns>
    public static bool TryRestore(
        ContentStructureSnapshot snapshot,
        IContentStructureSnapshotFactory factory,
        out IContentStructure? structure,
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

        structure = null;
        failure = null;

        if (string.IsNullOrWhiteSpace(snapshot.Kind))
        {
            failure = ContentFailures.SnapshotMalformed("Structure snapshot is missing a kind.");
            return false;
        }

        if (snapshot.Data is null)
        {
            failure = ContentFailures.SnapshotMalformed("Structure snapshot is missing data.");
            return false;
        }

        if (!string.Equals(snapshot.Kind, factory.Kind, StringComparison.Ordinal))
        {
            failure = ContentFailures.SnapshotMalformed(
                $"Structure snapshot kind '{snapshot.Kind}' does not match factory kind '{factory.Kind}'.");
            return false;
        }

        try
        {
            if (factory.TryRestore(snapshot, out structure, out failure))
            {
                if (structure is not null)
                {
                    return true;
                }

                failure = ContentFailures.SnapshotMalformed("Structure snapshot restore succeeded without a structure.");
                return false;
            }

            structure = null;
            failure ??= ContentFailures.Snapshot();
            return false;
        }
        catch (Exception ex)
        {
            structure = null;
            failure = ContentFailure.Wrap(
                ContentFailureKind.Snapshot,
                ContentFailureCodes.SnapshotRejected,
                "Structure snapshot restore failed.",
                ContentFailure.FromException(ex));
            return false;
        }
    }

    /// <summary>
    /// Restores a structure snapshot or throws when restore is rejected.
    /// </summary>
    /// <param name="snapshot">The structure snapshot.</param>
    /// <param name="factory">The structure factory.</param>
    /// <returns>The restored structure.</returns>
    public static IContentStructure Restore(ContentStructureSnapshot snapshot, IContentStructureSnapshotFactory factory)
    {
        if (TryRestore(snapshot, factory, out IContentStructure? structure, out ContentFailure? failure) && structure is not null)
        {
            return structure;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }
}
