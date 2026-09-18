# Content Snapshots

Content snapshots are the serialization foundation for Workes.ContentSystem.

Entry snapshot round trips are implemented. Record snapshots and whole-structure snapshots are planned for later stages.

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

## Planned Record Snapshots

A record snapshot will represent a stored record.

It should pair the stored `ContentEntryId` with an entry snapshot. Record snapshots are useful when the caller wants to preserve the stored identity of retained records, not just the entry payload.

Record snapshots are not implemented yet.

## Planned Structure Snapshots

A structure snapshot will represent whole-structure state.

For built-in structures, a structure snapshot should preserve retained records and structure-owned state such as:

- structure kind and snapshot version;
- generated ID state;
- capacity, bounds, placement, ordering, and overflow settings;
- keyed or grouped state where applicable.

Custom structures should opt into structure snapshots. Unsupported structures should fail capture or restore with structured failures.

Structure snapshots are not implemented yet.

## Restore Expectations

Entry restore recreates an entry payload.

Record restore should preserve stored record identity only when the target workflow supports it.

Whole-structure restore should be atomic. A failed restore should leave the active structure unchanged and emit no change event. A successful restore should emit coherent change information after the restored state is committed.

## Relationship To Export And Attachments

Snapshots are the serialization and state-transfer foundation.

Export helpers and attachments can consume snapshots or change events later, but they should not replace the portable snapshot model.
