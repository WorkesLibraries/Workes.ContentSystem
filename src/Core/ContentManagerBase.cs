using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides shared read and lookup behavior for content managers.
/// </summary>
public abstract class ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentManagerBase"/> class.
    /// </summary>
    /// <param name="structure">The active content structure.</param>
    protected ContentManagerBase(IContentStructure structure)
    {
        Structure = structure ?? throw new ArgumentNullException(nameof(structure));

        if (Structure is IContentChangeSource changeSource)
        {
            changeSource.Changed += HandleStructureChanged;
        }
    }

    /// <summary>
    /// Gets the active content structure.
    /// </summary>
    public IContentStructure Structure { get; private set; }

    /// <summary>
    /// Gets retained records in the active structure's read order.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Records => Structure.Records;

    /// <summary>Returns a stable, materialized, read-only record view sorted by the supplied comparer.</summary>
    /// <param name="comparer">The record comparer.</param>
    /// <returns>Records in comparer order, with comparer-equal records retaining their source order.</returns>
    public IReadOnlyList<ContentEntryRecord> GetSortedRecords(IComparer<ContentEntryRecord> comparer)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        return QueryRecords().OrderBy(comparer).ToReadOnlyList();
    }

    /// <summary>
    /// Gets a stable, materialized, read-only view containing records that satisfy the predicate.
    /// </summary>
    /// <param name="predicate">The condition each retained record must satisfy.</param>
    /// <returns>Matching records in the active structure's read order.</returns>
    /// <remarks>The predicate is evaluated immediately. Predicate exceptions propagate. Any side effects performed by the predicate are caller-owned; the query operation itself does not mutate the structure.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
    public IReadOnlyList<ContentEntryRecord> GetFilteredRecords(Func<ContentEntryRecord, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return QueryRecords().Where(predicate).ToReadOnlyList();
    }

    /// <summary>
    /// Creates a composable query over a snapshot of the active structure's records and read order.
    /// </summary>
    /// <returns>A query over a snapshot whose operations are materialized and do not change structure state.</returns>
    /// <remarks>The query captures record references and order when this method is called. Later structure changes do not alter its membership or order; entry objects are not cloned.</remarks>
    public ContentRecordQuery QueryRecords() => new ContentRecordQuery(Records);

    /// <summary>Attempts to export a materialized snapshot of records through the supplied exporter.</summary>
    /// <param name="exporter">The exporter for the requested output format.</param>
    /// <param name="result">The export result when successful.</param>
    /// <param name="failure">The structured rejection when export fails.</param>
    /// <returns><see langword="true"/> when export succeeds.</returns>
    /// <remarks>The exporter receives a read-only copy in the active structure's record order. Export does not mutate records or raise change events. Exceptions thrown by an exporter propagate; expected rejection should be returned as a <see cref="ContentFailure"/>.</remarks>
    public bool TryExport(IContentRecordExporter exporter, out ContentExportResult? result, out ContentFailure? failure)
    {
        if (exporter is null) throw new ArgumentNullException(nameof(exporter));
        result = null;
        string formatId = exporter.FormatId;
        if (string.IsNullOrWhiteSpace(formatId))
        {
            failure = ContentFailures.Export("Exporter format ID cannot be empty.");
            return false;
        }

        IReadOnlyList<ContentEntryRecord> snapshot = Array.AsReadOnly(Records.ToArray());
        if (!exporter.TryExport(snapshot, out string? text, out failure))
        {
            failure = ContentFailure.Wrap(
                ContentFailureKind.Export,
                ContentFailureCodes.ExportRejected,
                "The exporter rejected the record snapshot.",
                failure,
                component: exporter.GetType().FullName,
                source: formatId);
            return false;
        }

        if (text is null)
        {
            failure = ContentFailure.Create(
                ContentFailureKind.Export,
                ContentFailureCodes.ExportRejected,
                "The exporter reported success without producing text.",
                source: formatId);
            return false;
        }

        result = new ContentExportResult(formatId, text);
        failure = null;
        return true;
    }

    /// <summary>Exports a materialized snapshot of records through the supplied exporter.</summary>
    /// <param name="exporter">The exporter for the requested output format.</param>
    /// <returns>The export result.</returns>
    /// <exception cref="ContentOperationException">The exporter rejects the record snapshot.</exception>
    public ContentExportResult Export(IContentRecordExporter exporter)
    {
        if (TryExport(exporter, out ContentExportResult? result, out ContentFailure? failure)) return result!;
        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Gets the number of retained records in the active structure.
    /// </summary>
    public int Count => Records.Count;

    /// <summary>
    /// Gets a value indicating whether the active structure retains no records.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Occurs after the active structure commits a content mutation.
    /// </summary>
    public event EventHandler<ContentChangedEventArgs>? Changed;

    /// <summary>
    /// Determines whether the active structure retains a record with the supplied normalized ID.
    /// </summary>
    /// <param name="id">The normalized entry ID to look up.</param>
    /// <returns><see langword="true"/> when the active structure retains the ID.</returns>
    public bool Contains(ContentEntryId id)
    {
        return TryGet(id, out _, out _);
    }

    /// <summary>
    /// Attempts to get a retained record by ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <param name="record">The retained record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is found.</returns>
    public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return Structure.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found.</exception>
    public ContentEntryRecord Get(ContentEntryId id)
    {
        return Structure.Get(id);
    }

    /// <summary>
    /// Attempts to capture the active structure snapshot when supported.
    /// </summary>
    /// <param name="snapshot">The captured snapshot.</param>
    /// <param name="failure">The structured failure when capture is rejected or unsupported.</param>
    /// <returns><see langword="true"/> when the snapshot was captured.</returns>
    public bool TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure)
    {
        return ContentStructureSnapshots.TryCapture(Structure, out snapshot, out failure);
    }

    /// <summary>
    /// Assesses whether the active structure snapshot can currently be captured without capturing it for use.
    /// </summary>
    public ContentPreflightResult AssessCaptureSnapshot()
    {
        return ContentStructureSnapshots.TryCapture(Structure, out _, out ContentFailure? failure)
            ? ContentPreflightResult.Success()
            : ContentPreflightResult.Rejected(failure!);
    }

    /// <summary>
    /// Captures the active structure snapshot when supported.
    /// </summary>
    /// <returns>The captured snapshot.</returns>
    /// <exception cref="ContentOperationException">Thrown when capture is rejected or unsupported.</exception>
    public ContentStructureSnapshot CaptureSnapshot()
    {
        return ContentStructureSnapshots.Capture(Structure);
    }

    /// <summary>
    /// Attempts to restore and replace the active structure using the active structure's snapshot factory.
    /// </summary>
    /// <param name="snapshot">The structure snapshot.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when restore commits.</returns>
    public bool TryRestoreSnapshot(
        ContentStructureSnapshot snapshot,
        out ContentFailure? failure)
    {
        if (Structure is not IContentStructureSnapshotRoundTrippable roundTrippable)
        {
            failure = ContentFailures.SnapshotUnsupportedStructure(
                $"Structure type '{Structure.GetType().FullName}' does not expose a snapshot factory.");
            return false;
        }

        return TryRestoreSnapshot(snapshot, roundTrippable.SnapshotFactory, out failure);
    }

    /// <summary>
    /// Assesses whether a snapshot can be restored using the active structure's snapshot factory without replacing state.
    /// </summary>
    public ContentPreflightResult AssessRestoreSnapshot(ContentStructureSnapshot snapshot)
    {
        if (Structure is not IContentStructureSnapshotRoundTrippable roundTrippable)
        {
            return ContentPreflightResult.Rejected(ContentFailures.SnapshotUnsupportedStructure(
                $"Structure type '{Structure.GetType().FullName}' does not expose a snapshot factory."));
        }

        return AssessRestoreSnapshot(snapshot, roundTrippable.SnapshotFactory);
    }

    /// <summary>
    /// Attempts to restore and replace the active structure using registered entry snapshot factories.
    /// </summary>
    /// <param name="snapshot">The structure snapshot.</param>
    /// <param name="factory">The structure snapshot factory.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when restore commits.</returns>
    public bool TryRestoreSnapshot(
        ContentStructureSnapshot snapshot,
        IContentStructureSnapshotFactory factory,
        out ContentFailure? failure)
    {
        IReadOnlyList<ContentEntryRecord> previousRecords = Records;
        if (!ContentStructureSnapshots.TryRestore(snapshot, factory, out IContentStructure? replacement, out failure))
        {
            return false;
        }

        if (!TryAcceptStructureReplacement(replacement!, out failure))
        {
            return false;
        }

        ReplaceStructure(replacement!);
        Changed?.Invoke(this, new ContentChangedEventArgs(
            addedRecords: replacement!.Records,
            removedRecords: previousRecords,
            kind: ContentChangeKind.SnapshotRestored,
            requiresFullRefresh: true));
        failure = null;
        return true;
    }

    /// <summary>
    /// Assesses whether a snapshot can be restored with the supplied factory without replacing state.
    /// </summary>
    public ContentPreflightResult AssessRestoreSnapshot(
        ContentStructureSnapshot snapshot,
        IContentStructureSnapshotFactory factory)
    {
        if (!ContentStructureSnapshots.TryRestore(snapshot, factory, out IContentStructure? replacement, out ContentFailure? failure)
            || !TryAcceptStructureReplacement(replacement!, out failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        return ContentPreflightResult.Success();
    }

    /// <summary>
    /// Restores and replaces the active structure using registered entry snapshot factories.
    /// </summary>
    /// <param name="snapshot">The structure snapshot.</param>
    /// <param name="factory">The structure snapshot factory.</param>
    /// <exception cref="ContentOperationException">Thrown when restore is rejected.</exception>
    public void RestoreSnapshot(ContentStructureSnapshot snapshot, IContentStructureSnapshotFactory factory)
    {
        if (!TryRestoreSnapshot(snapshot, factory, out ContentFailure? failure))
        {
            throw new ContentOperationException(failure!);
        }
    }

    /// <summary>
    /// Restores and replaces the active structure using the active structure's snapshot factory.
    /// </summary>
    /// <param name="snapshot">The structure snapshot.</param>
    /// <exception cref="ContentOperationException">Thrown when restore is rejected.</exception>
    public void RestoreSnapshot(ContentStructureSnapshot snapshot)
    {
        if (!TryRestoreSnapshot(snapshot, out ContentFailure? failure))
        {
            throw new ContentOperationException(failure!);
        }
    }

    /// <summary>
    /// Determines whether a replacement structure is compatible with the concrete manager.
    /// </summary>
    /// <param name="structure">The proposed replacement structure.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the replacement is compatible.</returns>
    protected virtual bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        failure = null;
        return true;
    }

    /// <summary>
    /// Raises a change event from this manager.
    /// </summary>
    /// <param name="args">The change event arguments.</param>
    protected void OnChanged(ContentChangedEventArgs args)
    {
        Changed?.Invoke(this, args);
    }

    private void HandleStructureChanged(object? sender, ContentChangedEventArgs args)
    {
        OnChanged(args);
    }

    /// <summary>
    /// Replaces the active structure and updates change subscriptions.
    /// </summary>
    /// <param name="structure">The replacement structure.</param>
    protected void ReplaceStructure(IContentStructure structure)
    {
        if (Structure is IContentChangeSource oldChangeSource)
        {
            oldChangeSource.Changed -= HandleStructureChanged;
        }

        Structure = structure;

        if (Structure is IContentChangeSource newChangeSource)
        {
            newChangeSource.Changed += HandleStructureChanged;
        }
    }
}
