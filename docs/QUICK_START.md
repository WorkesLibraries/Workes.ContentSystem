# Quick Start

This package is being designed as a reusable content-entry backend. The first implementation goal is a small, useful core that can later support console history, chat, logs, feeds, forums, notifications, and similar entry streams.

## Install

```bash
dotnet add package Workes.ContentSystem --version 0.1.0
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ContentSystem" Version="0.1.0" />
```

## Mental Model

A ContentSystem application has three core ideas:

- A content entry is one item in a content collection.
- A content structure owns how entries are stored, ordered, found, and removed.
- A content manager is the normal root object that gives users a simple workflow over one chosen structure.

The first structure will be a bounded FIFO structure. Later structures may be threaded, indexed, grouped, persistent, grid-like, or forum-like.

## Intended Shape

The exact API is not implemented yet, but normal usage is expected to look like this:

```csharp
var content = new ContentManager();

content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "User submitted a command."));

foreach (ContentEntryRecord record in content.Entries)
{
    Render(record.Entry);
}
```

Advanced hosts should be able to provide custom entries and custom structures:

```csharp
var content = new ContentManager(new BoundedFifoContentStructure(capacity: 200));

content.Add(new ChatContentEntry(channel: "global", text: "Hello!"));
content.Add(new CollapsibleStackTraceEntry(exception));
```

## What To Read Next

- [Concepts](CONCEPTS.md) explains the package at a high level.
- [Content Entries](CONTENT_ENTRIES.md) explains the entry extension path.
- [Content Structures](CONTENT_STRUCTURES.md) explains the storage abstraction.
- [Failures](FAILURES.md) explains expected failures and exceptions.
- [Export And Attachments](EXPORT_AND_ATTACHMENTS.md) explains optional bridge and export ideas.
