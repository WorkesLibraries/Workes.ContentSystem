using System;
using System.Collections.Generic;
using System.Globalization;

namespace Workes.ContentSystem.Core;

internal static class ContentSnapshotRecordRestorer
{
    public static bool TryRestoreRecords(
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

    public static bool TryGetMaximumPositiveNumericId(
        IEnumerable<ContentEntryRecord> records,
        out long maximumId,
        out ContentFailure? failure)
    {
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
