using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Base implementation for snapshot factories that restore compound structure-family structures.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type used by the compound family.</typeparam>
/// <typeparam name="TStructure">The compound-family structure restored by the factory.</typeparam>
public abstract class ContentCompoundStructureSnapshotFactoryBase<TId, TStructure> :
    ContentStructureSnapshotFactoryBase<TStructure>
    where TStructure : ContentCompoundStructureBase<TId>, IContentStructure
{
    private readonly IContentGeneratedIdSourceFactory<TId> _idSourceFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundStructureSnapshotFactoryBase{TId, TStructure}"/> class.
    /// </summary>
    protected ContentCompoundStructureSnapshotFactoryBase(
        string kind,
        int dataVersion,
        IContentGeneratedIdSourceFactory<TId> idSourceFactory)
        : base(kind, dataVersion)
    {
        _idSourceFactory = idSourceFactory ?? throw new ArgumentNullException(nameof(idSourceFactory));
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
        if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure)
            || !TryRestoreIdSource(snapshot, out IContentGeneratedIdSource<TId>? idSource, out failure)
            || !TryRestoreRelationships(snapshot, records, out IReadOnlyDictionary<ContentEntryId, ContentEntryId?>? parentIds, out failure))
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

        return TryRestoreValidatedCompoundSnapshot(snapshot, records, parentIds!, idSource!, out structure, out failure);
    }

    /// <summary>
    /// Attempts to restore the concrete compound-family structure after shared record, relationship, and source validation has succeeded.
    /// </summary>
    protected abstract bool TryRestoreValidatedCompoundSnapshot(
        ContentStructureSnapshot snapshot,
        ContentEntryRecord[] records,
        IReadOnlyDictionary<ContentEntryId, ContentEntryId?> parentIds,
        IContentGeneratedIdSource<TId> idSource,
        out TStructure? structure,
        out ContentFailure? failure);

    /// <summary>
    /// Attempts to restore the generated ID source for the compound snapshot.
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

        if (!string.Equals(sourceKind, _idSourceFactory.Kind, StringComparison.Ordinal))
        {
            failure = ContentFailures.SnapshotMalformed(
                $"Compound structure snapshot ID source kind '{sourceKind}' does not match factory kind '{_idSourceFactory.Kind}'.");
            return false;
        }

        if (!ContentSnapshotProperties.TryGetRequired(snapshot.Data, "idSourceData", out ContentSnapshotEncodedValue? sourceData, out failure))
        {
            return false;
        }

        return _idSourceFactory.TryRestore(sourceData!.Data, out idSource, out failure);
    }

    private static bool TryRestoreRelationships(
        ContentStructureSnapshot snapshot,
        ContentEntryRecord[] records,
        out IReadOnlyDictionary<ContentEntryId, ContentEntryId?>? parentIds,
        out ContentFailure? failure)
    {
        parentIds = null;
        if (!ContentSnapshotProperties.TryGetRequired(snapshot.Data, "relationships", out ContentSnapshotEncodedValue? encodedRelationships, out failure))
        {
            return false;
        }

        if (encodedRelationships!.Data.Kind != ContentSnapshotValueKind.List)
        {
            failure = ContentFailures.SnapshotMalformed("Compound structure snapshot relationships must be a list.");
            return false;
        }

        if (encodedRelationships.Data.Items is null)
        {
            failure = ContentFailures.SnapshotMalformed("Compound structure snapshot relationships list cannot be null.");
            return false;
        }

        if (encodedRelationships.Data.Items.Count != records.Length)
        {
            failure = ContentFailures.SnapshotMalformed("Compound structure snapshot relationship count must match retained record count.");
            return false;
        }

        var recordIds = new HashSet<ContentEntryId>(records.Select(record => record.Id));
        var relationships = new Dictionary<ContentEntryId, ContentEntryId?>();

        foreach (ContentSnapshotEncodedValue item in encodedRelationships.Data.Items)
        {
            if (item is null || item.Data is null || item.Data.Kind != ContentSnapshotValueKind.Object)
            {
                failure = ContentFailures.SnapshotMalformed("Compound structure snapshot relationship entries must be objects.");
                return false;
            }

            if (!ContentSnapshotProperties.TryDecodeRequiredString(item.Data, "id", out string idText, out failure)
                || !ContentSnapshotProperties.TryDecodeRequiredBoolean(item.Data, "hasParent", out bool hasParent, out failure)
                || !ContentSnapshotProperties.TryDecodeRequiredString(item.Data, "parentId", out string parentIdText, out failure))
            {
                return false;
            }

            ContentEntryId id;
            try
            {
                id = new ContentEntryId(idText);
            }
            catch (ArgumentException ex)
            {
                failure = ContentFailure.Create(
                    ContentFailureKind.Snapshot,
                    ContentFailureCodes.SnapshotMalformed,
                    "Compound structure snapshot relationship contains an invalid node ID.",
                    cause: ContentFailure.FromException(ex));
                return false;
            }

            if (!recordIds.Contains(id))
            {
                failure = ContentFailures.SnapshotMalformed($"Compound structure snapshot relationship references unknown node '{id}'.");
                return false;
            }

            if (relationships.ContainsKey(id))
            {
                failure = ContentFailures.SnapshotMalformed($"Compound structure snapshot contains duplicate relationship for node '{id}'.");
                return false;
            }

            ContentEntryId? parentId = null;
            if (hasParent)
            {
                try
                {
                    parentId = new ContentEntryId(parentIdText);
                }
                catch (ArgumentException ex)
                {
                    failure = ContentFailure.Create(
                        ContentFailureKind.Snapshot,
                        ContentFailureCodes.SnapshotMalformed,
                        "Compound structure snapshot relationship contains an invalid parent ID.",
                        cause: ContentFailure.FromException(ex));
                    return false;
                }

                if (parentId.Value == id)
                {
                    failure = ContentFailures.SnapshotMalformed("Compound structure snapshot node cannot be its own parent.");
                    return false;
                }

                if (!recordIds.Contains(parentId.Value))
                {
                    failure = ContentFailures.SnapshotMalformed($"Compound structure snapshot parent '{parentId.Value}' was not found.");
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(parentIdText))
            {
                failure = ContentFailures.SnapshotMalformed("Compound structure snapshot root relationship must use an empty parent ID.");
                return false;
            }

            relationships.Add(id, parentId);
        }

        if (relationships.Count != records.Length)
        {
            failure = ContentFailures.SnapshotMalformed("Compound structure snapshot is missing one or more node relationships.");
            return false;
        }

        if (!TryValidateAcyclic(relationships, out failure))
        {
            return false;
        }

        parentIds = relationships;
        failure = null;
        return true;
    }

    private static bool TryValidateAcyclic(
        IReadOnlyDictionary<ContentEntryId, ContentEntryId?> parentIds,
        out ContentFailure? failure)
    {
        foreach (ContentEntryId id in parentIds.Keys)
        {
            var visited = new HashSet<ContentEntryId>();
            ContentEntryId current = id;

            while (parentIds.TryGetValue(current, out ContentEntryId? parent) && parent is not null)
            {
                if (!visited.Add(current))
                {
                    failure = ContentFailures.SnapshotMalformed("Compound structure snapshot contains a parent cycle.");
                    return false;
                }

                current = parent.Value;
            }
        }

        failure = null;
        return true;
    }
}
