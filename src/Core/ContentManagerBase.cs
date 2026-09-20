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
