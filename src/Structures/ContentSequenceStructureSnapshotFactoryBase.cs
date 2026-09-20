namespace Workes.ContentSystem.Core;

/// <summary>
/// Base implementation for snapshot factories that restore sequence-family structures with generated ID sources.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type used by the sequence family.</typeparam>
/// <typeparam name="TStructure">The sequence-family structure restored by the factory.</typeparam>
public abstract class ContentSequenceStructureSnapshotFactoryBase<TId, TStructure> :
    ContentStructureSnapshotFactoryBase<TStructure>
    where TStructure : ContentSequenceStructureBase<TId>, IContentStructure
{
    private readonly IContentGeneratedIdSourceFactory<TId> _idSourceFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceStructureSnapshotFactoryBase{TId, TStructure}"/> class.
    /// </summary>
    /// <param name="kind">The stable structure snapshot kind.</param>
    /// <param name="dataVersion">The supported structure snapshot data version.</param>
    /// <param name="idSourceFactory">The generated ID source factory used to restore source state.</param>
    protected ContentSequenceStructureSnapshotFactoryBase(
        string kind,
        int dataVersion,
        IContentGeneratedIdSourceFactory<TId> idSourceFactory)
        : base(kind, dataVersion)
    {
        _idSourceFactory = idSourceFactory ?? throw new System.ArgumentNullException(nameof(idSourceFactory));
    }

    /// <summary>
    /// Gets the generated ID source factory used by this structure snapshot factory.
    /// </summary>
    protected IContentGeneratedIdSourceFactory<TId> IdSourceFactory => _idSourceFactory;

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

        if (!TryRestoreIdSource(snapshot, out IContentGeneratedIdSource<TId>? idSource, out failure))
        {
            return false;
        }

        foreach (ContentEntryRecord record in records)
        {
            if (!idSource!.IdStrategy.TryValidateNormalized(record.Id, out failure)
                || !idSource.TryObserveNormalized(record.Id, out failure))
            {
                return false;
            }
        }

        return TryRestoreValidatedSequenceSnapshot(snapshot, records, idSource!, out structure, out failure);
    }

    /// <summary>
    /// Attempts to restore the generated ID source for the sequence snapshot.
    /// </summary>
    /// <param name="snapshot">The validated structure snapshot.</param>
    /// <param name="idSource">The restored generated ID source.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when the source was restored.</returns>
    protected virtual bool TryRestoreIdSource(
        ContentStructureSnapshot snapshot,
        out IContentGeneratedIdSource<TId>? idSource,
        out ContentFailure? failure)
    {
        idSource = null;

        if (!ContentSnapshotProperties.TryDecodeRequiredString(snapshot.Data, "idSourceKind", out string sourceKind, out failure))
        {
            return false;
        }

        if (!string.Equals(sourceKind, _idSourceFactory.Kind, System.StringComparison.Ordinal))
        {
            failure = ContentFailures.SnapshotMalformed(
                $"Sequence structure snapshot ID source kind '{sourceKind}' does not match factory kind '{_idSourceFactory.Kind}'.");
            return false;
        }

        if (!ContentSnapshotProperties.TryGetRequired(snapshot.Data, "idSourceData", out ContentSnapshotEncodedValue? sourceData, out failure))
        {
            return false;
        }

        return _idSourceFactory.TryRestore(sourceData!.Data, out idSource, out failure);
    }

    /// <summary>
    /// Attempts to restore the concrete sequence-family structure after shared record and generated-ID source validation has succeeded.
    /// </summary>
    /// <param name="snapshot">The validated structure snapshot.</param>
    /// <param name="records">The restored retained records in stored order.</param>
    /// <param name="idSource">The restored generated ID source after all retained IDs have been observed.</param>
    /// <param name="structure">The restored structure.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when restore succeeded.</returns>
    protected abstract bool TryRestoreValidatedSequenceSnapshot(
        ContentStructureSnapshot snapshot,
        ContentEntryRecord[] records,
        IContentGeneratedIdSource<TId> idSource,
        out TStructure? structure,
        out ContentFailure? failure);
}

/// <summary>
/// Base implementation for snapshot factories that restore long-ID sequence-family structures.
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
