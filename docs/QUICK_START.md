# Quick Start

This package is a reusable content-entry backend. The first useful core supports simple in-memory streams now, while leaving room for console history, chat, logs, feeds, forums, notifications, and similar entry streams.

## Install

```bash
dotnet add package Workes.ContentSystem --version 0.8.0
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ContentSystem" Version="0.8.0" />
```

## Mental Model

A ContentSystem application has three core ideas:

- A content entry is one item in a content collection.
- A content structure owns how entries are stored, ordered, found, and retained.
- A content manager is the normal root object created by the chosen structure.

The common sequence workflow uses a `ContentSequenceStructure` where the structure assigns IDs. Retention is explicit: use `ContentOverflowPolicy.None` to retain everything, `ContentOverflowPolicy.DropOldest(capacity)` for bounded history, or `ContentOverflowPolicy.Reject(capacity)` when a full sequence should reject more entries. Map workflows use caller-provided typed IDs. Single-entry, stack, and compound workflows cover current-state, last-in-first-out, and tree-shaped storage. Later structures may be indexed, snapshot-aware, grid-like, or forum-like.

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

## What To Read Next

- [Concepts](CONCEPTS.md) explains the package at a high level.
- [Content Entries](CONTENT_ENTRIES.md) explains the entry extension path.
- [Content Identity](CONTENT_IDENTITY.md) explains stored IDs and map ID strategies.
- [Content Structures](CONTENT_STRUCTURES.md) explains the storage abstraction.
- [Content Managers](CONTENT_MANAGERS.md) explains structure-driven manager resolution.
- [Content Changes](CONTENT_CHANGES.md) explains optional committed-change hooks.
- [Content Snapshots](CONTENT_SNAPSHOTS.md) explains entry, record, and built-in structure snapshot round trips.
- [Extension Authoring](EXTENSION_AUTHORING.md) explains how custom structures participate in the implemented contracts.
- [Failures](FAILURES.md) explains expected failures and exceptions.
- [Export And Attachments](EXPORT_AND_ATTACHMENTS.md) explains optional bridge and export ideas.
