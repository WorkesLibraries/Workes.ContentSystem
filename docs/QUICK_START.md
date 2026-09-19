# Quick Start

This package is a reusable content-entry backend. The first useful core supports simple in-memory streams now, while leaving room for console history, chat, logs, feeds, forums, notifications, and similar entry streams.

## Install

```bash
dotnet add package Workes.ContentSystem --version 0.5.0
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ContentSystem" Version="0.5.0" />
```

## Mental Model

A ContentSystem application has three core ideas:

- A content entry is one item in a content collection.
- A content structure owns how entries are stored, ordered, found, and retained.
- A content manager is the normal root object that gives users a simple workflow over one chosen structure category.

The common sequence workflow uses a `ContentSequenceStructure` where the structure assigns IDs. Retention is explicit: use `ContentOverflowPolicy.None` to retain everything or `ContentOverflowPolicy.DropOldest(capacity)` for bounded history. Keyed workflows use caller-provided typed IDs. Later structures may be threaded, indexed, grouped, snapshot-aware, grid-like, or forum-like.

## Sequence Workflow

Use `ContentManager.For(...)` when the structure assigns IDs for added entries and exposes a natural lookup ID type:

```csharp
var content = ContentManager.For(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "User submitted a command."));

ContentEntryRecord first = content.Get(1);

foreach (ContentEntryRecord record in content.Records)
{
    Render(record.Entry);
}
```

You can still provide a different structure-assigned-ID structure. If you prefer the explicit form, this is equivalent for the built-in sequence:

```csharp
var content = new ContentManager<long>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));

content.Add(new ChatContentEntry(channel: "global", text: "Hello!"));
content.Add(new CollapsibleStackTraceEntry(exception));
```

Most users should prefer `ContentManager.For(...)` because the structure owns the correct natural ID type.

## Keyed Workflow

Use `KeyedContentManager<TId>` when IDs are caller-provided and first-class:

```csharp
var content = new KeyedContentManager<string>();

content.Add("thread-main", new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));

ContentEntryRecord record = content.Get("thread-main");
```

Use `KeyedContentManager<long>` when positive integer IDs are a better fit:

```csharp
var content = new KeyedContentManager<long>();

content.Add(8, new PlainContentEntry(DateTimeOffset.UtcNow, "Eighth entry."));
```

`ContentManagerBase` is the shared ancestor for manager-agnostic code. Most users should construct `ContentManager` or `KeyedContentManager<TId>` directly, then use `ContentManagerBase` only when existing managers should be processed through their common read, lookup, event, and supported mutation surface.

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

## What To Read Next

- [Concepts](CONCEPTS.md) explains the package at a high level.
- [Content Entries](CONTENT_ENTRIES.md) explains the entry extension path.
- [Content Identity](CONTENT_IDENTITY.md) explains stored IDs and keyed ID strategies.
- [Content Structures](CONTENT_STRUCTURES.md) explains the storage abstraction.
- [Content Managers](CONTENT_MANAGERS.md) explains the manager workflow split.
- [Content Changes](CONTENT_CHANGES.md) explains optional committed-change hooks.
- [Content Snapshots](CONTENT_SNAPSHOTS.md) explains entry, record, and built-in structure snapshot round trips.
- [Extension Authoring](EXTENSION_AUTHORING.md) explains how custom structures participate in the implemented contracts.
- [Failures](FAILURES.md) explains expected failures and exceptions.
- [Export And Attachments](EXPORT_AND_ATTACHMENTS.md) explains optional bridge and export ideas.
