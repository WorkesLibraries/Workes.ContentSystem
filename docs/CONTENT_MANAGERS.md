# Content Managers

Content managers are the normal root objects for using Workes.ContentSystem.

## Purpose

A manager owns one active content structure and exposes the write workflow that structure category supports.

The shared read and lookup behavior is common across managers, but adding entries is not universal. Some structures assign IDs internally. Other structures require callers to provide IDs. ContentSystem keeps those workflows separate so unsupported operations do not appear available.

## Manager Types

Use `ContentManager.For(...)` for structure-assigned-ID workflows where the structure exposes a natural ID type.

```csharp
var content = ContentManager.For(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

ContentEntryRecord record = content.Add(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
```

`ContentSequenceStructure` assigns increasing numeric IDs stored as `ContentEntryId` values. The structure is passed explicitly so the manager never hides which storage or retention policy is active.

`ContentManager.For(...)` lets C# infer the correct manager ID type from the structure:

```csharp
var content = ContentManager.For(
    new ContentSequenceStructure(ContentOverflowPolicy.None));

ContentEntryRecord record = content.Add(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));

ContentEntryRecord found = content.Get(1);
ContentEntryRecord removed = content.Remove(1);
```

This is equivalent to constructing the typed manager explicitly:

```csharp
var content = new ContentManager<long>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));
```

The explicit form is legal, but `ContentManager.For(...)` is the recommended path because the structure, not the user, owns the correct natural ID type.

Use `KeyedContentManager<TId>` for caller-provided typed IDs.

```csharp
var content = new KeyedContentManager<string>();

ContentEntryRecord record = content.Add(
    "server-started",
    new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
```

`KeyedContentManager<TId>` supports the same typed ID shapes as `KeyedContentStructure<TId>`. Built-in ID strategies are resolved for `string` and `long`. For custom ID types, pass a fully configured keyed structure or a custom `IContentEntryIdStrategy<TId>`.

## Shared Base

`ContentManagerBase` is the shared ancestor for already-created managers.

It exposes:

- `Structure`;
- `Records`;
- `TryGet(ContentEntryId, ...)`;
- `Get(ContentEntryId)`;
- `TryClear(...)` and `Clear()`;
- `TryRemove(ContentEntryId, ...)` and `Remove(ContentEntryId)`;
- `TrySetStructureParameter(...)` and `SetStructureParameter(...)`;
- `TryCaptureSnapshot(...)` and `CaptureSnapshot()`;
- `TryRestoreSnapshot(...)` and `RestoreSnapshot(...)`;
- `Changed`.

This is useful when code receives `ContentManager`, `ContentManager<TId>`, or `KeyedContentManager<TId>` and only needs to read records or look up records by the normalized `ContentEntryId`:

```csharp
void Render(ContentManagerBase content)
{
    foreach (ContentEntryRecord record in content.Records)
    {
        RenderRecord(record);
    }
}
```

Most application code should construct `ContentManager` or `KeyedContentManager<TId>` directly. `ContentManagerBase` is abstract, so it is not constructed directly; it exists so both manager workflows can be processed through their common read and shared mutation surface.

## Runtime Mutation

Managers own the normal mutation workflow. Structures opt into the underlying focused contracts, and manager APIs return `StructureUnsupportedOperation` when the active structure does not support a requested mutation.

Shared manager mutations include:

- `Clear`, when the structure implements `IContentClearableStructure`;
- `Remove(ContentEntryId)`, when the structure implements `IContentRecordRemovalStructure`;
- `SetStructureParameter`, when the structure implements `IParameterizedContentStructure`.

```csharp
ContentManagerBase content = new ContentManager(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.SetStructureParameter(
    ContentSequenceStructure.OverflowPolicyParameterId,
    ContentOverflowPolicy.DropOldest(capacity: 500));
content.Remove(new ContentEntryId("1"));
content.Clear();
```

Parameterized mutation mirrors InventorySystem's runtime configuration style: structures expose stable parameter IDs, and managers coordinate the commit. The first built-in parameter is `ContentSequenceStructure.OverflowPolicyParameterId`, whose value must be a `ContentOverflowPolicy`.

Typed managers expose natural ID overloads where the workflow has a natural ID shape:

```csharp
var sequence = ContentManager.For(
    new ContentSequenceStructure(ContentOverflowPolicy.None));

sequence.Remove(1);

var keyed = new KeyedContentManager<string>();
keyed.Remove("thread-main");
```

Typed keyed removal requires the active keyed structure to implement `IKeyedContentRecordRemovalStructure<TId>`. The built-in keyed structure does. Custom keyed structures that do not opt in remain valid, and the typed removal manager API returns `StructureUnsupportedOperation`.

## Change Hooks

`ContentManagerBase.Changed` forwards events from the active structure when that structure implements `IContentChangeSource`.

```csharp
ContentManagerBase content = new ContentManager(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.Changed += (_, args) =>
{
    foreach (ContentEntryRecord record in args.AddedRecords)
    {
        RenderRecord(record);
    }
};
```

Forwarded events use the manager as `sender` and preserve the structure's `ContentChangedEventArgs`.

Structures that do not implement `IContentChangeSource` remain valid. Managers over those structures simply have no structure events to forward.

See [Content Changes](CONTENT_CHANGES.md) for event payload and timing details.

## Snapshots

Managers own the normal whole-structure restore workflow.

`ContentManagerBase.CaptureSnapshot()` captures the active structure when it implements `IContentStructureSnapshotRoundTrippable`. `RestoreSnapshot(snapshot)` uses that same active structure's `SnapshotFactory`, verifies that the restored structure is compatible with the concrete manager, replaces the active structure atomically, and emits one full-refresh snapshot-restored event after commit.

```csharp
ContentStructureSnapshot snapshot = content.CaptureSnapshot();

content.RestoreSnapshot(snapshot);
```

Register custom entry snapshot factories once before restoring structure snapshots that contain those entry kinds:

```csharp
ContentEntrySnapshotFactories.Register(MyEntry.Factory);

content.RestoreSnapshot(snapshot);
```

The package registers built-ins such as `PlainContentEntry.Factory` automatically. Custom entry registration is application composition state; capture does not require registration, but restore does.

Explicit structure factories remain available for migration and advanced restore scenarios where the active structure's factory is intentionally not the target:

```csharp
content.RestoreSnapshot(snapshot, NewStructureVersion.Factory);
```

## Try And Expected-Success APIs

Managers follow the package failure pattern.

Try APIs return structured failure data for expected rejection:

```csharp
bool added = content.TryAdd(entry, out ContentEntryRecord? record, out ContentFailure? failure);
```

Expected-success APIs return the value or throw `ContentOperationException` carrying the same `ContentFailure`:

```csharp
ContentEntryRecord record = content.Get(id);
```

Programmer misuse, such as a null entry or null structure, uses standard .NET exceptions.

## Choosing A Manager

Choose based on who owns entry IDs:

- use `ContentManager` when the structure assigns IDs;
- use `ContentManager.For(...)` when a structure-assigned-ID structure exposes a natural ID type;
- use `KeyedContentManager<TId>` when callers provide IDs;
- use `ContentManagerBase` when code only needs shared read, lookup, clear/remove, or structure-parameter mutation behavior.

This split keeps the API explicit. It avoids one broad manager with add methods that only work for some structures.
