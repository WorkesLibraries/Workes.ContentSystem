using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Base implementation for snapshot factories that restore map structure-family structures.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
/// <typeparam name="TStructure">The map structure-family structure restored by the factory.</typeparam>
public abstract class ContentMapStructureSnapshotFactoryBase<TId, TStructure> :
    ContentStructureSnapshotFactoryBase<TStructure>
    where TStructure : ContentMapStructureBase<TId>, IContentStructure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentMapStructureSnapshotFactoryBase{TId, TStructure}"/> class.
    /// </summary>
    /// <param name="kind">The stable structure snapshot kind.</param>
    /// <param name="dataVersion">The supported structure snapshot data version.</param>
    /// <param name="idStrategy">The ID strategy used to validate restored normalized IDs.</param>
    protected ContentMapStructureSnapshotFactoryBase(
        string kind,
        int dataVersion,
        IContentEntryIdStrategy<TId> idStrategy)
        : base(kind, dataVersion)
    {
        IdStrategy = idStrategy ?? throw new ArgumentNullException(nameof(idStrategy));
    }

    /// <summary>
    /// Gets the ID strategy used to validate restored normalized IDs.
    /// </summary>
    protected IContentEntryIdStrategy<TId> IdStrategy { get; }

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

        foreach (ContentEntryRecord record in records)
        {
            if (!IdStrategy.TryValidateNormalized(record.Id, out failure))
            {
                return false;
            }
        }

        if (!ContentSnapshotRecords.TryValidateUniqueIds(records, out failure))
        {
            return false;
        }

        return TryRestoreValidatedMapSnapshot(snapshot, records, out structure, out failure);
    }

    /// <summary>
    /// Attempts to restore the concrete map structure after shared record and ID validation has succeeded.
    /// </summary>
    /// <param name="snapshot">The validated structure snapshot.</param>
    /// <param name="records">The restored retained records in insertion order.</param>
    /// <param name="structure">The restored structure.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when restore succeeded.</returns>
    protected abstract bool TryRestoreValidatedMapSnapshot(
        ContentStructureSnapshot snapshot,
        ContentEntryRecord[] records,
        out TStructure? structure,
        out ContentFailure? failure);
}
