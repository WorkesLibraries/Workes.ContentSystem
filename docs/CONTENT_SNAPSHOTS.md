# Content Snapshots

Content snapshots are the serialization foundation for Workes.ContentSystem.

Entry snapshot round trips are implemented. Record and structure snapshot DTOs are implemented. Built-in structure snapshot capture and restore are implemented for the sequence and keyed structures.

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

It does not preserve structure-owned state such as stored IDs, generated ID source state, grouping, ordering, or capacity configuration.

Entries opt into capture with `IContentEntrySnapshotSerializable`. Restore is explicit through an `IContentEntrySnapshotFactory`.

```csharp
var entry = new PlainContentEntry(DateTimeOffset.UtcNow, "Server started.");

ContentEntrySnapshot snapshot = ContentEntrySnapshots.Capture(entry);

IContentEntry restored = ContentEntrySnapshots.Restore(
    snapshot,
    PlainContentEntry.Factory);
```

`PlainContentEntry` supports entry snapshot round trips out of the box. It uses stable snapshot kind `workes.content.entry.plain` and data version `1`.

Custom entries that do not implement `IContentEntrySnapshotSerializable` fail capture with `ContentFailureCodes.SnapshotUnsupportedEntry`. Custom entries that do support snapshots should provide one static factory and register it once before restoring serialized structure snapshots:

```csharp
ContentEntrySnapshotFactories.Register(MyEntry.Factory);
```

Capture does not require registration because the entry instance owns capture. Restore does require registration because a loaded snapshot only contains the entry snapshot kind, not a live entry instance that can expose its factory.

## Record Snapshots

A record snapshot represents a stored record.

`ContentRecordSnapshot` pairs the stored record ID with an entry snapshot:

- `EntryId`, stored as a serializer-friendly string;
- `Entry`, stored as a `ContentEntrySnapshot`.

Record snapshots are useful when the caller wants to preserve the stored identity of retained records, not just the entry payload.

Record snapshots are captured and restored as part of built-in structure snapshots. Core does not expose record-only restore as a normal manager workflow because record identity belongs to the active structure.

## Structure Snapshots

A structure snapshot represents whole-structure state.

`ContentStructureSnapshot` contains:

- `Kind`, the stable structure snapshot kind;
- `DataVersion`, the structure snapshot data version;
- `Records`, retained `ContentRecordSnapshot` values;
- `Data`, a `ContentSnapshotValue` envelope for structure-owned state.

For built-in structures, a structure snapshot should preserve retained records and structure-owned state such as:

- structure kind and snapshot version;
- generated ID source state;
- capacity, bounds, placement, ordering, and overflow settings;
- keyed or grouped state where applicable.

Custom structures opt into structure snapshot round trips with `IContentStructureSnapshotRoundTrippable`. The structure exposes the `SnapshotFactory` used by normal manager restore. Unsupported structures fail capture or factory-less restore with `ContentFailureCodes.SnapshotUnsupportedStructure`.

Built-in structure snapshot kinds are stable package-prefixed strings:

- `ContentSequenceStructure.SnapshotKind`, `workes.content.structure.sequence`;
- `KeyedContentStructure<TId>.SnapshotKind`, `workes.content.structure.keyed`.

Both currently use data version `1`. Sequence snapshots include generated ID source state so future generated IDs remain coherent after restore.

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));

ContentStructureSnapshot snapshot = content.CaptureSnapshot();

var restored = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));

restored.RestoreSnapshot(snapshot);
```

Keyed structure restore uses the active structure's typed factory so the restored structure keeps the right caller-facing ID workflow:

```csharp
var content = ContentManagers.ForStructure<KeyedContentManager<string>>(
    new KeyedContentStructure<string>());
content.Add("server-started", new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));

ContentStructureSnapshot snapshot = content.CaptureSnapshot();

var restored = ContentManagers.ForStructure<KeyedContentManager<string>>(
    new KeyedContentStructure<string>());
restored.RestoreSnapshot(snapshot);
```

Restore uses the package-wide entry factory registry. `PlainContentEntry.Factory` is registered by the package. Custom entries register their factories during application setup:

```csharp
ContentEntrySnapshotFactories.Register(MyEntry.Factory);

content.RestoreSnapshot(snapshot);
```

Explicit structure factories remain available for migration and advanced restore targets:

```csharp
content.RestoreSnapshot(snapshot, MyStructureV2.Factory);
```

## Saving And Loading

ContentSystem does not choose a serializer or write to disk. The normal workflow is to capture a serializer-friendly snapshot, save it with the application serializer, load it later, then restore it into a manager with a compatible round-trippable structure.

```csharp
using System.Text.Json;

var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));

ContentStructureSnapshot snapshot = content.CaptureSnapshot();
string json = JsonSerializer.Serialize(snapshot);

ContentStructureSnapshot loaded = JsonSerializer.Deserialize<ContentStructureSnapshot>(json)!;

var restored = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

restored.RestoreSnapshot(loaded);
```

Custom entries must register their factories before restore:

```csharp
ContentEntrySnapshotFactories.Register(QuestEntry.Factory);

ContentStructureSnapshot loaded = LoadSnapshot();
content.RestoreSnapshot(loaded);
```

## Restore Expectations

Entry restore recreates an entry payload.

Record restore preserves stored record identity as part of a structure restore.

Whole-structure restore through managers is atomic. A failed restore leaves the active structure unchanged and emits no change event. A successful restore replaces the active structure, resubscribes manager event forwarding, and emits `ContentChangeKind.SnapshotRestored` with `RequiresFullRefresh = true`.

For keyed structures, restore validates stored snapshot IDs through the configured `IContentEntryIdStrategy<TId>`. Custom strategies must ensure restored normalized IDs describe the same ID language as caller-provided IDs.

Custom structure authors can use `ContentStructureSnapshotFactoryBase<TStructure>` for common factory validation, `ContentSequenceStructureSnapshotFactoryBase<TId, TStructure>` for generated-ID sequence restore, `ContentSequenceStructureSnapshotFactoryBase<TStructure>` for long-ID sequence restore, `KeyedContentStructureSnapshotFactoryBase<TId, TStructure>` for keyed restore invariants, `ContentSnapshotRecords` for retained record capture/restore, and `ContentSnapshotProperties` for structure-owned snapshot data. See [Extension Authoring](EXTENSION_AUTHORING.md).

## Relationship To Export And Attachments

Snapshots are the serialization and state-transfer foundation.

Export helpers and attachments can consume snapshots or change events later, but they should not replace the portable snapshot model.
