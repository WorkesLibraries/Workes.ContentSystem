using System;
using System.Collections.Generic;

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

    /// <summary>
    /// Occurs after the active structure commits a content mutation.
    /// </summary>
    public event EventHandler<ContentChangedEventArgs>? Changed;

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
    /// Captures the active structure snapshot when supported.
    /// </summary>
    /// <returns>The captured snapshot.</returns>
    /// <exception cref="ContentOperationException">Thrown when capture is rejected or unsupported.</exception>
    public ContentStructureSnapshot CaptureSnapshot()
    {
        return ContentStructureSnapshots.Capture(Structure);
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
    /// Attempts to clear retained records when the active structure supports clearing.
    /// </summary>
    /// <param name="removedRecords">The records removed by the clear operation.</param>
    /// <param name="failure">The structured failure when rejected or unsupported; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the clear operation is accepted.</returns>
    public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        if (Structure is IContentClearableStructure clearable)
        {
            return clearable.TryClear(out removedRecords, out failure);
        }

        removedRecords = Array.Empty<ContentEntryRecord>();
        failure = UnsupportedMutation("The active content structure does not support clearing.");
        return false;
    }

    /// <summary>
    /// Clears retained records when the active structure supports clearing.
    /// </summary>
    /// <returns>The records removed by the clear operation.</returns>
    /// <exception cref="ContentOperationException">Thrown when clearing is rejected or unsupported.</exception>
    public IReadOnlyList<ContentEntryRecord> Clear()
    {
        if (TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure))
        {
            return removedRecords;
        }

        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Attempts to remove a retained record when the active structure supports removal.
    /// </summary>
    /// <param name="id">The retained record ID.</param>
    /// <param name="removedRecord">The removed record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected or unsupported; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is removed.</returns>
    public bool TryRemove(ContentEntryId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        if (Structure is IContentRecordRemovalStructure removable)
        {
            return removable.TryRemove(id, out removedRecord, out failure);
        }

        removedRecord = null;
        failure = UnsupportedMutation("The active content structure does not support record removal.");
        return false;
    }

    /// <summary>
    /// Removes a retained record when the active structure supports removal.
    /// </summary>
    /// <param name="id">The retained record ID.</param>
    /// <returns>The removed record.</returns>
    /// <exception cref="ContentOperationException">Thrown when removal is rejected or unsupported.</exception>
    public ContentEntryRecord Remove(ContentEntryId id)
    {
        if (TryRemove(id, out ContentEntryRecord? removedRecord, out ContentFailure? failure))
        {
            return removedRecord!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Attempts to change one runtime parameter on the active structure.
    /// </summary>
    /// <param name="parameterId">The stable structure parameter ID.</param>
    /// <param name="value">The proposed parameter value.</param>
    /// <param name="removedRecords">Records removed while applying the parameter change.</param>
    /// <param name="failure">The structured failure when rejected or unsupported; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the parameter change is committed or no change is needed.</returns>
    public bool TrySetStructureParameter(
        string parameterId,
        object? value,
        out IReadOnlyList<ContentEntryRecord> removedRecords,
        out ContentFailure? failure)
    {
        if (string.IsNullOrWhiteSpace(parameterId))
        {
            removedRecords = Array.Empty<ContentEntryRecord>();
            failure = ContentFailures.Configuration("Structure parameter ID cannot be empty.");
            return false;
        }

        if (Structure is not IParameterizedContentStructure parameterized)
        {
            removedRecords = Array.Empty<ContentEntryRecord>();
            failure = UnsupportedMutation("The active content structure does not support runtime parameters.");
            return false;
        }

        IContentStructure previous = Structure;
        if (!parameterized.TryCreateWithParameter(parameterId, value, out IContentStructure? replacement, out removedRecords, out failure))
        {
            return false;
        }

        if (replacement is null || ReferenceEquals(replacement, Structure))
        {
            failure = null;
            return true;
        }

        if (!TryAcceptStructureReplacement(replacement, out failure))
        {
            return false;
        }

        ReplaceStructure(replacement);
        Changed?.Invoke(this, new ContentChangedEventArgs(
            removedRecords: removedRecords,
            kind: ContentChangeKind.ConfigurationChanged,
            configurationChanged: new[]
            {
                new ContentConfigurationChanged(
                    ContentConfigurationChangeKind.StructureParameter,
                    parameterId,
                    value,
                    previous,
                    replacement,
                    requiresFullRefresh: true)
            },
            requiresFullRefresh: true));
        failure = null;
        return true;
    }

    /// <summary>
    /// Changes one runtime parameter on the active structure.
    /// </summary>
    /// <param name="parameterId">The stable structure parameter ID.</param>
    /// <param name="value">The proposed parameter value.</param>
    /// <returns>Records removed while applying the parameter change.</returns>
    /// <exception cref="ContentOperationException">Thrown when the parameter change is rejected or unsupported.</exception>
    public IReadOnlyList<ContentEntryRecord> SetStructureParameter(string parameterId, object? value)
    {
        if (TrySetStructureParameter(parameterId, value, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure))
        {
            return removedRecords;
        }

        throw new ContentOperationException(failure!);
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

    private void HandleStructureChanged(object? sender, ContentChangedEventArgs args)
    {
        Changed?.Invoke(this, args);
    }

    private void ReplaceStructure(IContentStructure structure)
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

    private static ContentFailure UnsupportedMutation(string message)
    {
        return ContentFailures.StructureUnsupportedOperation(message);
    }
}
