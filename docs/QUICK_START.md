# Quick Start

This package is a reusable content-entry backend for in-memory streams and structured content, with extension points for console history, chat, logs, feeds, forums, notifications, and similar workflows.

## Install

```bash
dotnet add package Workes.ContentSystem --version 1.3.0
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ContentSystem" Version="1.3.0" />
```

## Mental Model

A ContentSystem application has three core ideas:

- A content entry is one item in a content collection.
- A content structure owns how entries are stored, ordered, found, and retained.
- A content manager is the normal root object created by the chosen structure.

The common sequence workflow uses a `ContentSequenceStructure` where the structure assigns IDs. Retention is explicit: use `ContentOverflowPolicy.None` to retain everything, `ContentOverflowPolicy.DropOldest(capacity)` for bounded history, or `ContentOverflowPolicy.Reject(capacity)` when a full sequence should reject more entries. Map workflows use caller-provided typed IDs. Single-entry, stack, and compound workflows cover current-state, last-in-first-out, and tree-shaped storage. Every manager supports a read-only sorted view; persistent sorting is available for map and compound structures that opt into the sorting contracts.

## Sequence Workflow

Create a sequence structure, then let `ContentManagers.ForStructure(...)` resolve its natural manager:

```csharp
ContentManagerBase content = ContentManagers.ForStructure(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

if (content is ContentSequenceManager sequence)
{
    sequence.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
    sequence.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "User submitted a command."));

    ContentEntryRecord first = sequence.Get(1);
}

foreach (ContentEntryRecord record in content.Records)
{
    Render(record.Entry);
}
```

If your code expects a sequence manager immediately, use the typed resolver:

```csharp
ContentSequenceManager sequence =
    ContentManagers.ForStructure<ContentSequenceManager>(
        new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));
```

You can still provide a different structure-assigned-ID structure. If you prefer the explicit form, this is equivalent for the built-in sequence:

```csharp
var content = new ContentSequenceManager(
    new ContentSequenceStructure(ContentOverflowPolicy.None));

content.Add(new ChatContentEntry(channel: "global", text: "Hello!"));
content.Add(new CollapsibleStackTraceEntry(exception));
```

Most users should prefer `ContentManagers.ForStructure(...)` because the structure creates the correct manager.

## Map Workflow

Use a map structure when IDs are caller-provided and first-class:

```csharp
var content = ContentManagers.ForStructure<ContentMapManager<string>>(
    new ContentMapStructure<string>());

content.Add("thread-main", new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));

ContentEntryRecord record = content.Get("thread-main");
```

Use `ContentMapManager<TId>` when positive integer IDs are a better fit:

```csharp
var content = ContentManagers.ForStructure<ContentMapManager<long>>(
    new ContentMapStructure<long>());

content.Add(8, new PlainContentEntry(DateTimeOffset.UtcNow, "Eighth entry."));
```

## Single And Stack Workflows

Use a single-entry structure when the structure should retain one current record:

```csharp
var current = ContentManagers.ForStructure<ContentSingleManager>(
    new ContentSingleStructure(ContentSingleReplacementPolicy.Replace));

current.Set(new PlainContentEntry(DateTimeOffset.UtcNow, "Current objective"));
ContentEntryRecord active = current.GetCurrent();
```

Use a stack structure for last-in-first-out workflows:

```csharp
var stack = ContentManagers.ForStructure<ContentStackManager>(
    new ContentStackStructure(ContentOverflowPolicy.Reject(capacity: 20)));

stack.Push(new PlainContentEntry(DateTimeOffset.UtcNow, "Opened menu"));
ContentEntryRecord top = stack.Peek();
ContentEntryRecord popped = stack.Pop();
```

Use a compound structure for simple hierarchy-shaped content:

```csharp
var tree = ContentManagers.ForStructure<ContentCompoundManager>(
    new ContentCompoundStructure(ContentCompoundChildRemovalPolicy.RemoveSubtree));

ContentCompoundNode topic = tree.AddRoot(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Topic"));
ContentCompoundNode reply = tree.AddChild(
    topic,
    new PlainContentEntry(DateTimeOffset.UtcNow, "Reply"));
```

`ContentManagerBase` is the shared ancestor for manager-agnostic code. Most users should resolve a manager from a structure, then use `ContentManagerBase` only when existing managers should be processed through their common read, lookup, event, and snapshot surface.

## Observing Changes

Managers expose `Changed` for structures that support change hooks:

```csharp
content.Changed += (_, args) =>
{
    foreach (ContentEntryRecord record in args.AddedRecords)
    {
        Render(record);
    }
};
```

Events are raised synchronously after a mutation is committed. Rejected operations do not raise events.

## Preflight

Use `Assess...` APIs when code needs an advisory check before committing:

```csharp
ContentPreflightResult assessment = stack.AssessPop();

if (assessment.CanCommit)
{
    ContentEntryRecord popped = stack.Pop();
}
```

Preflight does not mutate records, advance generated IDs, replace structures, or emit events. It is not a lock, so the actual `Try...` or expected-success operation still revalidates.

## Sorted Views

Every manager can return a stable sorted copy for display or export without changing the structure:

```csharp
IReadOnlyList<ContentEntryRecord> newestFirst = content.GetSortedRecords(
    Comparer<ContentEntryRecord>.Create((left, right) => right.Timestamp.CompareTo(left.Timestamp)));
```

Map managers can persistently reorder records with `Sort(comparer)`. Compound managers can sort all sibling groups, choose a comparer per group, or target roots/a parent's children with `SortSiblings(...)`. Persistent sorting is an explicit mutation and is not available on sequence, stack, or single-entry workflows. See [Content Managers](CONTENT_MANAGERS.md) for the full API and behavior.

## Filtered And Composed Views

Every manager can filter its current records or compose filtering and ordering without changing the structure:

```csharp
IReadOnlyList<ContentEntryRecord> visible = content.QueryRecords()
    .Where(record => record.Timestamp >= cutoff)
    .OrderBy(Comparer<ContentEntryRecord>.Create(
        (left, right) => right.Timestamp.CompareTo(left.Timestamp)))
    .ToReadOnlyList();
```

Queries are materialized from a snapshot when `QueryRecords()` is called. Filtering preserves the active structure's read order unless you add an explicit sort. See [Content Queries](CONTENT_QUERIES.md) for predicate filtering, secondary ordering, and the boundary between query sorting and persistent structure sorting.

## Batch Mutations

Use family-specific batch methods when several changes should commit together:

```csharp
IReadOnlyList<ContentEntryRecord> added = sequence.AddRange(new IContentEntry[]
{
    new PlainContentEntry(DateTimeOffset.UtcNow, "First event"),
    new PlainContentEntry(DateTimeOffset.UtcNow, "Second event")
});

DateTimeOffset retentionCutoff = DateTimeOffset.UtcNow.AddDays(-30);
IReadOnlyList<ContentEntryRecord> expired = sequence.RemoveWhere(
    record => record.Timestamp < retentionCutoff);
```

Map managers provide `AddRange` and `SetRange`; stack managers provide `PushRange`; compound managers provide `AddRoots` and `AddChildren(parentId, entries)`. Each successful batch emits one full-refresh change event. Rejected batches leave records and generated-ID state unchanged. Compound predicate removal follows the configured child-removal policy. See [Content Managers](CONTENT_MANAGERS.md) for detailed ordering and preflight behavior.

## What To Read Next

- [Concepts](CONCEPTS.md) explains the package at a high level.
- [Content Entries](CONTENT_ENTRIES.md) explains the entry extension path.
- [Content Identity](CONTENT_IDENTITY.md) explains stored IDs and map ID strategies.
- [Content Structures](CONTENT_STRUCTURES.md) explains the storage abstraction.
- [Content Managers](CONTENT_MANAGERS.md) explains structure-driven manager resolution.
- [Content Queries](CONTENT_QUERIES.md) explains filtered and composed read views.
- [Content Changes](CONTENT_CHANGES.md) explains optional committed-change hooks.
- [Content Snapshots](CONTENT_SNAPSHOTS.md) explains entry, record, and built-in structure snapshot round trips.
- [Extension Authoring](EXTENSION_AUTHORING.md) explains how custom structures participate in the implemented contracts.
- [Failures](FAILURES.md) explains expected failures and exceptions.
- [Compatibility And Upgrades](COMPATIBILITY.md) explains the 1.0 compatibility line and snapshot considerations.
- [Export And Attachments](EXPORT_AND_ATTACHMENTS.md) explains optional bridge and export ideas.
