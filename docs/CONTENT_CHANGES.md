# Content Changes

Content change hooks are optional synchronous notifications for committed mutations.

They let UI layers, bridges, attachments, and shared manager code observe additions and retention drops without changing the normal pull-based `Records` and `Get` workflow.

## Change Source

Observable structures implement `IContentChangeSource`:

```csharp
public interface IContentChangeSource
{
    event EventHandler<ContentChangedEventArgs>? Changed;
}
```

The built-in structures are observable:

- `BoundedFifoContentStructure`;
- `KeyedContentStructure<TId>`.

Custom structures do not need to implement `IContentChangeSource`. They remain valid content structures without change hooks.

## Event Payload

`ContentChangedEventArgs` contains:

- `AddedRecords`;
- `RemovedRecords`.

Both collections are read-only snapshots. Null collections passed to the event args constructor become empty collections.

For a normal add, `AddedRecords` contains the committed record and `RemovedRecords` is empty.

For FIFO overflow, one event is raised with:

- the newly added record in `AddedRecords`;
- the dropped oldest record in `RemovedRecords`.

## Subscribing Through A Manager

Managers forward structure events through `ContentManagerBase.Changed` when the active structure implements `IContentChangeSource`:

```csharp
var content = new ContentManager(new BoundedFifoContentStructure(capacity: 200));

content.Changed += OnContentChanged;

content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));

void OnContentChanged(object? sender, ContentChangedEventArgs args)
{
    foreach (ContentEntryRecord record in args.AddedRecords)
    {
        RenderAdded(record);
    }
}
```

Forwarded manager events use the manager as `sender` and preserve the original `ContentChangedEventArgs`.

This also works for keyed managers:

```csharp
var content = new KeyedContentManager<string>();

content.Changed += OnContentChanged;
content.Add("thread-main", new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));
```

## Rejected Operations

Rejected or failed no-op operations do not raise change events.

For example:

- a duplicate keyed ID returns `EntryIdDuplicate` and emits no event;
- an invalid keyed ID returns `EntryIdInvalid` and emits no event;
- null entry misuse throws a standard .NET exception before any event is emitted.

## Event Semantics

Change events are intentionally small:

- events are raised synchronously;
- events are raised after the structure state has been committed;
- handler exceptions are not swallowed;
- no dispatcher, background queue, buffering, or thread marshaling is added;
- reads and lookups do not raise events.

Applications that need UI-thread dispatch, async fan-out, event buffering, persistence, or bridge behavior should add that behavior in their own integration layer or a future attachment.
