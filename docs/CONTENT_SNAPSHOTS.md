# Content Snapshots

Content snapshots are the planned serialization foundation for Workes.ContentSystem.

This guide describes intended architecture only. Snapshot APIs are not implemented yet.

## Purpose

Applications should be able to capture content state into serializer-friendly objects, then save those objects with their own serializer, file system, database, save slots, compression, or cloud layer.

Core ContentSystem should not own disk I/O or require one serializer. It should provide portable snapshot objects and round-trip contracts.

## Snapshot Layers

Snapshots are planned in three layers.

### Entry Snapshots

An entry snapshot represents one entry payload.

It does not preserve structure-owned state such as stored IDs, generated ID counters, grouping, ordering, or capacity configuration.

`PlainContentEntry` is planned to support entry snapshot round trips out of the box. Custom entries will need to opt in explicitly. Unsupported custom entries should make capture fail with a structured failure rather than silently losing data.

### Record Snapshots

A record snapshot represents a stored record.

It should pair the stored `ContentEntryId` with an entry snapshot. Record snapshots are useful when the caller wants to preserve the stored identity of retained records, not just the entry payload.

### Structure Snapshots

A structure snapshot represents whole-structure state.

For built-in structures, a structure snapshot should preserve retained records and structure-owned state such as:

- structure kind and snapshot version;
- generated ID state;
- capacity, bounds, placement, ordering, and overflow settings;
- keyed or grouped state where applicable.

Custom structures should opt into structure snapshots. Unsupported structures should fail capture or restore with structured failures.

## Restore Expectations

Entry restore recreates an entry payload.

Record restore preserves stored record identity only when the target workflow supports it.

Whole-structure restore should be atomic. A failed restore should leave the active structure unchanged and emit no change event. A successful restore should emit coherent change information after the restored state is committed.

## Relationship To Export And Attachments

Snapshots are the serialization and state-transfer foundation.

Export helpers and attachments can consume snapshots or change events later, but they should not replace the portable snapshot model.
