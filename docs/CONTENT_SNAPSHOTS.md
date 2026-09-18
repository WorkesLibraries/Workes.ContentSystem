# Content Snapshots

Content snapshots are the serialization foundation for Workes.ContentSystem.

Entry snapshot round trips are implemented. Record and structure snapshot DTOs are implemented. Whole-structure capture and restore workflows are planned for later stages.

## Purpose

Applications should be able to capture content state into serializer-friendly objects, then save those objects with their own serializer, file system, database, save slots, compression, or cloud layer.

Core ContentSystem does not own disk I/O or require one serializer. It provides portable snapshot objects and round-trip contracts.

## Snapshot Values

Snapshot payloads use an Inventory-style value tree:

- `ContentSnapshotEncodedValue`;
- `ContentSnapshotValue`;
- `ContentSnapshotNamedValue`;
- `ContentSnapshotValueKind`;
- `ContentSnapshotCodecs`.

The built-in scalar codecs support null, string, Boolean, `int`, `long`, and `DateTimeOffset`.

## Entry Snapshots

An entry snapshot represents one entry payload.

It does not preserve structure-owned state such as stored IDs, generated ID counters, grouping, ordering, or capacity configuration.

Entries opt into capture with `IContentEntrySnapshotSerializable`. Restore is explicit through an `IContentEntrySnapshotFactory`.

```csharp
var entry = new PlainContentEntry(DateTimeOffset.UtcNow, "Server started.");

ContentEntrySnapshot snapshot = ContentEntrySnapshots.Capture(entry);

IContentEntry restored = ContentEntrySnapshots.Restore(
    snapshot,
    PlainContentEntry.Factory);
```

`PlainContentEntry` supports entry snapshot round trips out of the box. It uses stable snapshot kind `workes.content.entry.plain` and data version `1`.

Custom entries that do not implement `IContentEntrySnapshotSerializable` fail capture with `ContentFailureCodes.SnapshotUnsupportedEntry`. Custom entries that do support snapshots should provide an explicit factory for restore.

## Record Snapshots

A record snapshot represents a stored record.

`ContentRecordSnapshot` pairs the stored record ID with an entry snapshot:

- `EntryId`, stored as a serializer-friendly string;
- `Entry`, stored as a `ContentEntrySnapshot`.

Record snapshots are useful when the caller wants to preserve the stored identity of retained records, not just the entry payload.

Stage 14 only defines the DTO. Record snapshot capture and restore workflows are planned for later stages.

## Structure Snapshots

A structure snapshot represents whole-structure state.

`ContentStructureSnapshot` contains:

- `Kind`, the stable structure snapshot kind;
- `DataVersion`, the structure snapshot data version;
- `Records`, retained `ContentRecordSnapshot` values;
- `Data`, a `ContentSnapshotValue` envelope for structure-owned state.

For built-in structures, a structure snapshot should preserve retained records and structure-owned state such as:

- structure kind and snapshot version;
- generated ID state;
- capacity, bounds, placement, ordering, and overflow settings;
- keyed or grouped state where applicable.

Custom structures should opt into structure snapshots. Unsupported structures should fail capture or restore with structured failures.

Stage 14 only defines the DTO. Structure snapshot capture, validation, and restore workflows are planned for later stages.

## Restore Expectations

Entry restore recreates an entry payload.

Record restore should preserve stored record identity only when the target workflow supports it.

Whole-structure restore should be atomic. A failed restore should leave the active structure unchanged and emit no change event. A successful restore should emit coherent change information after the restored state is committed.

## Relationship To Export And Attachments

Snapshots are the serialization and state-transfer foundation.

Export helpers and attachments can consume snapshots or change events later, but they should not replace the portable snapshot model.
