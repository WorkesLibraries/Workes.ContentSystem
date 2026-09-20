# Content Changes

Content change hooks are optional synchronous notifications for committed mutations.

They let UI layers, bridges, attachments, and shared manager code observe additions, removals, clears, retention drops, and runtime structure parameter changes without changing the normal pull-based `Records` and `Get` workflow.

## Change Source

Observable structures implement `IContentChangeSource`:

```csharp
public interface IContentChangeSource
{
    event EventHandler<ContentChangedEventArgs>? Changed;
}
```

The built-in structures are observable:

- `ContentSequenceStructure`;
- `KeyedContentStructure<TId>`.

Custom structures do not need to implement `IContentChangeSource`. They remain valid content structures without change hooks.

## Event Payload

`ContentChangedEventArgs` contains:

- `Kind`;
- `AddedRecords`;
- `RemovedRecords`;
- `Cleared`;
- `ConfigurationChanged`;
- `RequiresFullRefresh`.

Collections are read-only snapshots. Null collections passed to the event args constructor become empty collections.

For a normal add, `Kind` is `ContentChangeKind.Added`, `AddedRecords` contains the committed record, and `RemovedRecords` is empty.

For `ContentOverflowPolicy.DropOldest(capacity)` overflow, one event is raised with:

- the newly added record in `AddedRecords`;
- the dropped oldest record in `RemovedRecords`.

For removal, `Kind` is `ContentChangeKind.Removed` and `RemovedRecords` contains the removed record.

For clear, `Kind` is `ContentChangeKind.Cleared`, `Cleared` is true, `RemovedRecords` contains all cleared records, and `RequiresFullRefresh` is true.

For runtime structure parameter changes, `Kind` is `ContentChangeKind.ConfigurationChanged`, `ConfigurationChanged` contains a `ContentConfigurationChanged` entry with `ContentConfigurationChangeKind.StructureParameter`, and `RequiresFullRefresh` is true. The configuration change reports the parameter ID, committed value, previous component, and current component. If the committed parameter change removes retained records, those records also appear in `RemovedRecords`.

For manager-coordinated structure snapshot restore, `Kind` is `ContentChangeKind.SnapshotRestored`, `RequiresFullRefresh` is true, `RemovedRecords` contains the records from the previous active structure, and `AddedRecords` contains the restored records.

## Subscribing Through A Manager

Managers forward structure events through `ContentManagerBase.Changed` when the active structure implements `IContentChangeSource`:

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

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
var content = ContentManagers.ForStructure<KeyedContentManager<string>>(
    new KeyedContentStructure<string>());

content.Changed += OnContentChanged;
content.Add("thread-main", new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));
```

## Rejected Operations

Rejected or no-op operations do not raise change events.

For example:

- a duplicate keyed ID returns `EntryIdDuplicate` and emits no event;
- an invalid keyed ID returns `EntryIdInvalid` and emits no event;
- removing a missing record returns `EntryNotFound` and emits no event;
- clearing an already-empty structure succeeds and emits no event;
- setting a structure parameter to the already-committed value succeeds and emits no event;
- a failed snapshot restore leaves the active structure unchanged and emits no event;
- null entry misuse throws a standard .NET exception before any event is emitted.

## Event Semantics

Change events are intentionally small:

- events are raised synchronously;
- events are raised after the structure state has been committed;
- handler exceptions are not swallowed;
- no dispatcher, background queue, buffering, or thread marshaling is added;
- reads and lookups do not raise events.

Applications that need UI-thread dispatch, async fan-out, event buffering, storage integration, or bridge behavior should add that behavior in their own integration layer or a future attachment.
