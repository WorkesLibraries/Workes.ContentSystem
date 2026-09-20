namespace Workes.ContentSystem.Core;

/// <summary>
/// Base implementation for snapshot factories that restore sequence-family structures.
/// </summary>
/// <typeparam name="TStructure">The sequence-family structure restored by the factory.</typeparam>
public abstract class ContentSequenceStructureSnapshotFactoryBase<TStructure> :
    ContentStructureSnapshotFactoryBase<TStructure>
    where TStructure : ContentSequenceStructureBase, IContentStructure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceStructureSnapshotFactoryBase{TStructure}"/> class.
    /// </summary>
    /// <param name="kind">The stable structure snapshot kind.</param>
    /// <param name="dataVersion">The supported structure snapshot data version.</param>
    protected ContentSequenceStructureSnapshotFactoryBase(string kind, int dataVersion)
        : base(kind, dataVersion)
    {
    }

    /// <inheritdoc />
    protected sealed override bool TryRestoreValidatedSnapshot(
        ContentStructureSnapshot snapshot,
        out TStructure? structure,
        out ContentFailure? failure)
    {
        structure = null;

        if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure))
        {
            return false;
        }

        if (!ContentSnapshotRecords.TryGetMaximumPositiveNumericId(records, out long maximumId, out failure))
        {
            return false;
        }

        return TryRestoreValidatedSequenceSnapshot(snapshot, records, maximumId, out structure, out failure);
    }

    /// <summary>
    /// Attempts to restore the concrete sequence-family structure after shared record and ID validation has succeeded.
    /// </summary>
    /// <param name="snapshot">The validated structure snapshot.</param>
    /// <param name="records">The restored retained records in stored order.</param>
    /// <param name="maximumId">The greatest positive numeric restored record ID, or zero when no records are retained.</param>
    /// <param name="structure">The restored structure.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when restore succeeded.</returns>
    protected abstract bool TryRestoreValidatedSequenceSnapshot(
        ContentStructureSnapshot snapshot,
        ContentEntryRecord[] records,
        long maximumId,
        out TStructure? structure,
        out ContentFailure? failure);
}
