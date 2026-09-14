# Content Managers

Content managers are the normal root objects for using Workes.ContentSystem.

## Purpose

A manager owns one active content structure and exposes the write workflow that structure category supports.

The shared read and lookup behavior is common across managers, but adding entries is not universal. Some structures assign IDs internally. Other structures require callers to provide IDs. ContentSystem keeps those workflows separate so unsupported operations do not appear available.

## Manager Types

Use `ContentManager` for structure-assigned-ID workflows.

```csharp
var content = new ContentManager(new BoundedFifoContentStructure());

ContentEntryRecord record = content.Add(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
```

`BoundedFifoContentStructure` assigns increasing numeric IDs stored as `ContentEntryId` values. The structure is passed explicitly so the manager never hides which storage policy is active.

Use `KeyedContentManager<TId>` for caller-provided typed IDs.

```csharp
var content = new KeyedContentManager<string>();

ContentEntryRecord record = content.Add(
    "server-started",
    new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
```

`KeyedContentManager<TId>` supports the same typed ID shapes as `KeyedContentStructure<TId>`. Built-in ID strategies are resolved for `string` and `long`. For custom ID types, pass a fully configured keyed structure or a custom `IContentEntryIdStrategy<TId>`.

## Shared Base

`ContentManagerBase` is the shared read and lookup ancestor for already-created managers.

It exposes:

- `Structure`;
- `Records`;
- `TryGet(ContentEntryId, ...)`;
- `Get(ContentEntryId)`;
- `Changed`.

This is useful when code receives either `ContentManager` or `KeyedContentManager<TId>` and only needs to read records or look up records by the normalized `ContentEntryId`:

```csharp
void Render(ContentManagerBase content)
{
    foreach (ContentEntryRecord record in content.Records)
    {
        RenderRecord(record);
    }
}
```

Most application code should construct `ContentManager` or `KeyedContentManager<TId>` directly. `ContentManagerBase` is abstract, so it is not constructed directly; it exists so both manager workflows can be processed through their common read surface.

## Change Hooks

`ContentManagerBase.Changed` forwards events from the active structure when that structure implements `IContentChangeSource`.

```csharp
ContentManagerBase content = new ContentManager(new BoundedFifoContentStructure());

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
- use `KeyedContentManager<TId>` when callers provide IDs;
- use `ContentManagerBase` when code only needs shared read/lookup behavior.

This split keeps the API explicit. It avoids one broad manager with add methods that only work for some structures.
