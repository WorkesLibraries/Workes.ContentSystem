using System;
using System.Collections.Generic;
using System.Globalization;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Helper methods for capturing and restoring retained content records in structure snapshots.
/// </summary>
public static class ContentSnapshotRecords
{
    /// <summary>
    /// Attempts to capture retained records as serializer-friendly record snapshots.
    /// </summary>
    /// <param name="records">The records to capture.</param>
    /// <param name="snapshots">The captured record snapshots.</param>
    /// <param name="failure">The structured failure when capture is rejected.</param>
    /// <returns><see langword="true"/> when all records were captured.</returns>
    public static bool TryCapture(
        IEnumerable<ContentEntryRecord> records,
        out List<ContentRecordSnapshot>? snapshots,
        out ContentFailure? failure)
    {
        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        snapshots = new List<ContentRecordSnapshot>();
        foreach (ContentEntryRecord record in records)
        {
            if (!ContentEntrySnapshots.TryCapture(record.Entry, out ContentEntrySnapshot? entrySnapshot, out failure))
            {
                snapshots = null;
                return false;
            }

            snapshots.Add(new ContentRecordSnapshot
            {
                EntryId = record.Id.Value,
                Entry = entrySnapshot!
            });
        }

        failure = null;
        return true;
    }

    /// <summary>
    /// Captures retained records as serializer-friendly record snapshots or throws when capture is rejected.
    /// </summary>
    /// <param name="records">The records to capture.</param>
    /// <returns>The captured record snapshots.</returns>
    public static List<ContentRecordSnapshot> Capture(IEnumerable<ContentEntryRecord> records)
    {
        if (TryCapture(records, out List<ContentRecordSnapshot>? snapshots, out ContentFailure? failure) && snapshots is not null)
        {
            return snapshots;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    /// <summary>
    /// Attempts to restore retained records from record snapshots using registered entry snapshot factories.
    /// </summary>
    /// <param name="snapshots">The record snapshots.</param>
    /// <param name="records">The restored records.</param>
    /// <param name="failure">The structured failure when restore is rejected.</param>
    /// <returns><see langword="true"/> when all records were restored.</returns>
    public static bool TryRestore(
        IList<ContentRecordSnapshot>? snapshots,
        out ContentEntryRecord[] records,
        out ContentFailure? failure)
    {
        records = Array.Empty<ContentEntryRecord>();

        if (snapshots is null)
        {
            failure = ContentFailures.SnapshotMalformed("Structure snapshot records cannot be null.");
            return false;
        }

        var restored = new List<ContentEntryRecord>(snapshots.Count);
        var seenIds = new HashSet<ContentEntryId>();
        for (int i = 0; i < snapshots.Count; i++)
        {
            ContentRecordSnapshot recordSnapshot = snapshots[i];
            if (recordSnapshot is null)
            {
                failure = ContentFailures.SnapshotMalformed($"Structure snapshot record {i} cannot be null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(recordSnapshot.EntryId))
            {
                failure = ContentFailures.SnapshotMalformed($"Structure snapshot record {i} is missing an entry ID.");
                return false;
            }

            var id = new ContentEntryId(recordSnapshot.EntryId);
            if (!seenIds.Add(id))
            {
                failure = ContentFailures.SnapshotMalformed($"Structure snapshot contains duplicate entry ID '{id}'.");
                return false;
            }

            if (recordSnapshot.Entry is null)
            {
                failure = ContentFailures.SnapshotMalformed($"Structure snapshot record '{id}' is missing an entry snapshot.");
                return false;
            }

            if (!ContentEntrySnapshotFactories.TryGet(recordSnapshot.Entry.Kind, out IContentEntrySnapshotFactory? entryFactory))
            {
                failure = ContentFailures.SnapshotFactoryMissing(
                    $"No entry snapshot factory is registered for kind '{recordSnapshot.Entry.Kind}'.",
                    recordSnapshot.Entry.Kind);
                return false;
            }

            if (!ContentEntrySnapshots.TryRestore(recordSnapshot.Entry, entryFactory!, out IContentEntry? entry, out failure))
            {
                return false;
            }

            restored.Add(new ContentEntryRecord(id, entry!));
        }

        records = restored.ToArray();
        failure = null;
        return true;
    }

    /// <summary>
    /// Restores retained records from record snapshots or throws when restore is rejected.
    /// </summary>
    /// <param name="snapshots">The record snapshots.</param>
    /// <returns>The restored records.</returns>
    public static ContentEntryRecord[] Restore(IList<ContentRecordSnapshot>? snapshots)
    {
        if (TryRestore(snapshots, out ContentEntryRecord[] records, out ContentFailure? failure))
        {
            return records;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    /// <summary>
    /// Attempts to verify that restored record IDs are unique.
    /// </summary>
    /// <param name="records">The records to validate.</param>
    /// <param name="failure">The structured failure when a duplicate is found.</param>
    /// <returns><see langword="true"/> when all IDs are unique.</returns>
    public static bool TryValidateUniqueIds(
        IEnumerable<ContentEntryRecord> records,
        out ContentFailure? failure)
    {
        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        var seenIds = new HashSet<ContentEntryId>();
        foreach (ContentEntryRecord record in records)
        {
            if (!seenIds.Add(record.Id))
            {
                failure = ContentFailures.SnapshotMalformed($"Structure snapshot contains duplicate entry ID '{record.Id}'.");
                return false;
            }
        }

        failure = null;
        return true;
    }

    /// <summary>
    /// Attempts to read the greatest positive numeric record ID.
    /// </summary>
    /// <param name="records">The records to inspect.</param>
    /// <param name="maximumId">The greatest numeric ID.</param>
    /// <param name="failure">The structured failure when any ID is not a positive numeric ID.</param>
    /// <returns><see langword="true"/> when all IDs are positive numeric IDs.</returns>
    public static bool TryGetMaximumPositiveNumericId(
        IEnumerable<ContentEntryRecord> records,
        out long maximumId,
        out ContentFailure? failure)
    {
        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        maximumId = 0;
        foreach (ContentEntryRecord record in records)
        {
            if (!long.TryParse(record.Id.Value, NumberStyles.None, CultureInfo.InvariantCulture, out long id) || id <= 0)
            {
                failure = ContentFailures.SnapshotMalformed(
                    $"Structure snapshot record ID '{record.Id}' is not a positive numeric ID.",
                    record.Id.Value);
                return false;
            }

            maximumId = Math.Max(maximumId, id);
        }

        failure = null;
        return true;
    }
}
