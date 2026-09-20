using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Base implementation for structure snapshot factories that restore one structure kind and data version.
/// </summary>
/// <typeparam name="TStructure">The concrete structure type restored by the factory.</typeparam>
public abstract class ContentStructureSnapshotFactoryBase<TStructure> : IContentStructureSnapshotFactory
    where TStructure : class, IContentStructure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStructureSnapshotFactoryBase{TStructure}"/> class.
    /// </summary>
    /// <param name="kind">The stable structure snapshot kind.</param>
    /// <param name="dataVersion">The supported structure snapshot data version.</param>
    protected ContentStructureSnapshotFactoryBase(string kind, int dataVersion)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Structure snapshot kind cannot be empty.", nameof(kind));
        }

        if (dataVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dataVersion), dataVersion, "Structure snapshot data version must be greater than zero.");
        }

        Kind = kind;
        DataVersion = dataVersion;
    }

    /// <inheritdoc />
    public string Kind { get; }

    /// <summary>
    /// Gets the supported structure snapshot data version.
    /// </summary>
    public int DataVersion { get; }

    /// <inheritdoc />
    public bool TryRestore(
        ContentStructureSnapshot snapshot,
        out IContentStructure? structure,
        out ContentFailure? failure)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        structure = null;

        if (!TryValidateSnapshotHeader(snapshot, out failure))
        {
            return false;
        }

        try
        {
            if (TryRestoreValidatedSnapshot(snapshot, out TStructure? restored, out failure))
            {
                if (restored is not null)
                {
                    structure = restored;
                    failure = null;
                    return true;
                }

                failure = ContentFailures.SnapshotMalformed("Structure snapshot restore succeeded without a structure.");
                return false;
            }

            failure ??= ContentFailures.Snapshot();
            return false;
        }
        catch (Exception ex)
        {
            failure = ContentFailure.Wrap(
                ContentFailureKind.Snapshot,
                ContentFailureCodes.SnapshotRejected,
                "Structure snapshot restore failed.",
                ContentFailure.FromException(ex));
            return false;
        }
    }

    /// <inheritdoc />
    public IContentStructure Restore(ContentStructureSnapshot snapshot)
    {
        if (TryRestore(snapshot, out IContentStructure? structure, out ContentFailure? failure) && structure is not null)
        {
            return structure;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    /// <summary>
    /// Attempts to restore the structure after common kind, version, and data validation has succeeded.
    /// </summary>
    /// <param name="snapshot">The validated structure snapshot.</param>
    /// <param name="structure">The restored structure.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when restore succeeded.</returns>
    protected abstract bool TryRestoreValidatedSnapshot(
        ContentStructureSnapshot snapshot,
        out TStructure? structure,
        out ContentFailure? failure);

    private bool TryValidateSnapshotHeader(ContentStructureSnapshot snapshot, out ContentFailure? failure)
    {
        if (string.IsNullOrWhiteSpace(snapshot.Kind))
        {
            failure = ContentFailures.SnapshotMalformed("Structure snapshot is missing a kind.");
            return false;
        }

        if (!string.Equals(snapshot.Kind, Kind, StringComparison.Ordinal))
        {
            failure = ContentFailures.SnapshotMalformed(
                $"Structure snapshot kind '{snapshot.Kind}' does not match factory kind '{Kind}'.");
            return false;
        }

        if (snapshot.DataVersion != DataVersion)
        {
            failure = ContentFailures.SnapshotUnsupportedVersion(
                $"Structure snapshot kind '{Kind}' version {snapshot.DataVersion} is not supported.");
            return false;
        }

        if (snapshot.Data is null)
        {
            failure = ContentFailures.SnapshotMalformed("Structure snapshot is missing data.");
            return false;
        }

        failure = null;
        return true;
    }
}
