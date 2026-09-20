# Content Managers

Content managers are the normal root objects for using Workes.ContentSystem.

## Purpose

A manager wraps one active content structure and exposes the workflow created by that structure.

The shared read and lookup behavior is common across managers, but write workflows are structure-specific. Some structures assign IDs internally. Other structures require callers to provide IDs. ContentSystem keeps those workflows separate by letting each structure create its normal manager.

## Manager Types

Use `ContentManagers.ForStructure(...)` for structure-driven construction.

The untyped resolver asks the structure for its natural manager and returns the shared base:

```csharp
ContentManagerBase content = ContentManagers.ForStructure(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

if (content is ContentSequenceManager sequence)
{
    ContentEntryRecord record = sequence.Add(
        new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
}
```

`ContentSequenceStructure` assigns increasing numeric IDs stored as `ContentEntryId` values. The structure is passed explicitly so the manager never hides which storage or retention policy is active.

The typed resolver is the same structure-driven path, but it validates the manager type you expect up front:

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));

ContentEntryRecord record = content.Add(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));

ContentEntryRecord found = content.Get(1);
ContentEntryRecord removed = content.Remove(1);
```

This is equivalent to constructing the typed manager explicitly:

```csharp
var content = new ContentSequenceManager(
    new ContentSequenceStructure(ContentOverflowPolicy.None));
```

The generic type argument does not choose the manager; it asserts what the structure should create. The explicit constructor form is legal, but `ContentManagers.ForStructure(...)` is the recommended path because the structure creates its correct manager.

Use keyed structures for caller-provided typed IDs.

```csharp
var content = ContentManagers.ForStructure<KeyedContentManager<string>>(
    new KeyedContentStructure<string>());

ContentEntryRecord record = content.Add(
    "server-started",
    new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
```

`KeyedContentManager<TId>` supports the same typed ID shapes as the built-in `KeyedContentStructure<TId>`. Built-in ID strategies are resolved for `string` and `long`. For custom ID types, pass a fully configured keyed structure or a custom `IContentEntryIdStrategy<TId>`.

## Shared Base

`ContentManagerBase` is the shared ancestor for already-created managers.

It exposes:

- `Structure`;
- `Records`;
- `TryGet(ContentEntryId, ...)`;
- `Get(ContentEntryId)`;
- `TryCaptureSnapshot(...)` and `CaptureSnapshot()`;
- `TryRestoreSnapshot(...)` and `RestoreSnapshot(...)`;
- `Changed`.

This is useful when code receives `ContentSequenceManager`, `KeyedContentManager<TId>`, or a custom manager and only needs to read records or look up records by the normalized `ContentEntryId`:

```csharp
void Render(ContentManagerBase content)
{
    foreach (ContentEntryRecord record in content.Records)
    {
        RenderRecord(record);
    }
}
```

Most application code should resolve managers from structures. `ContentManagerBase` is abstract, so it is not constructed directly; it exists so multiple manager workflows can be processed through their common read and lookup surface.

Reusable workflow families can also have manager bases. `ContentSequenceManagerBase` and `KeyedContentManagerBase<TId>` hold shared family behavior such as add, typed lookup, typed removal, and clear. The concrete managers remain the normal user-facing types because they can expose concrete structure features without forcing those features onto the whole family.

## Runtime Mutation

Managers coordinate the normal mutation workflow. Structures own retained content state, and concrete managers expose only the mutations that make sense for their structure family.

`ContentSequenceManager` exposes sequence add, numeric lookup/removal, clear, and runtime sequence-parameter mutation:

```csharp
var content = new ContentSequenceManager(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.SetStructureParameter(
    ContentSequenceStructure.OverflowPolicyParameterId,
    ContentOverflowPolicy.DropOldest(capacity: 500));
content.Remove(1);
content.Clear();
```

Parameterized mutation mirrors InventorySystem's runtime configuration style: structures expose stable parameter IDs, and managers coordinate the commit. The first built-in parameter is `ContentSequenceStructure.OverflowPolicyParameterId`, whose value must be a `ContentOverflowPolicy`.

`KeyedContentManager<TId>` exposes typed keyed add, lookup, removal, and clear for the built-in keyed structure:

```csharp
var keyed = ContentManagers.ForStructure<KeyedContentManager<string>>(
    new KeyedContentStructure<string>());
keyed.Remove("thread-main");
```

Custom structures should expose their own manager when their mutation vocabulary differs from the built-ins.

## Change Hooks

`ContentManagerBase.Changed` forwards events from the active structure when that structure implements `IContentChangeSource`.

```csharp
ContentManagerBase content = new ContentSequenceManager(
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

Choose based on the structure:

- use `ContentManagers.ForStructure(...)` for normal structure-driven resolution;
- use typed `ContentManagers.ForStructure<TManager>(...)` when your code expects a specific manager from the start;
- use direct manager constructors only for explicit setup, tests, or advanced scenarios;
- use `ContentManagerBase` when code only needs shared read, lookup, events, or snapshot behavior.

This split keeps the API explicit. It avoids one broad manager with add methods that only work for some structures.

## Manager Resolution

Structures create their normal manager through `IContentStructure.CreateManager()`.

This keeps manager selection close to the structure-owned content model without adding a registry or broad capability metadata. Focused contracts such as keyed add, structure-assigned add, mutation, change hooks, and snapshots still describe the behavior a structure supports.

The untyped path resolves the structure's natural manager:

```csharp
ContentManagerBase content = ContentManagers.ForStructure(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));
```

Use pattern matching when shared code needs to branch into manager-specific APIs:

```csharp
if (content is ContentSequenceManager sequence)
{
    sequence.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
    sequence.Get(1);
}
```

Callers that already expect a specific manager can use a typed resolver rather than a manual cast:

```csharp
ContentSequenceManager content =
    ContentManagers.ForStructure<ContentSequenceManager>(sequence);
```

The typed resolver fails with a structured manager-mismatch failure if the structure creates a different manager.

Custom structure authors implement `CreateManager()` to return their custom manager. Direct manager constructors remain available for explicit setup and tests where they fit.
