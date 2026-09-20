# Workes.ContentSystem

[![NuGet](https://img.shields.io/nuget/v/Workes.ContentSystem.svg)](https://www.nuget.org/packages/Workes.ContentSystem)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/WorkesLibraries/Workes.ContentSystem/blob/main/LICENSE)

Workes.ContentSystem is an engine-neutral backend for storing and organizing extensible content entries.

It is intended to be useful anywhere an application needs an ordered or structured stream of entries: console history, chat, logs, notifications, forum-like views, feeds, audit trails, and custom UI timelines.

## Highlights

- Extensible content entries instead of one fixed message shape.
- Pluggable content structures, starting with a configurable sequence structure.
- Structure-driven manager resolution for sequence, keyed, and custom workflows.
- Structure-owned entry identity so different storage models can use the IDs that fit them.
- Optional change hooks for observing committed mutations.
- Entry, record, and built-in structure snapshots for portable serialization.
- A shared failure and exception model matching the style used in other Workes packages.
- Planned optional attachment points for export, bridges, and platform-specific integration.

## Installation

Install the package from [NuGet](https://www.nuget.org/packages/Workes.ContentSystem):

```bash
dotnet add package Workes.ContentSystem --version 0.5.2
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ContentSystem" Version="0.5.2" />
```

The package targets .NET Standard 2.1.

## Quick Example

The normal path is to create a structure, then let ContentSystem resolve its natural manager:

```csharp
ContentManagerBase content = ContentManagers.ForStructure(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

if (content is ContentSequenceManager sequence)
{
    sequence.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
    sequence.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Player joined: Workes"));

    ContentEntryRecord first = sequence.Get(1);
}

foreach (ContentEntryRecord record in content.Records)
{
    Console.WriteLine($"{record.Id}: {record.PlainText}");
}
```

If your code expects a specific manager from the start, use the typed resolver:

```csharp
ContentSequenceManager sequence =
    ContentManagers.ForStructure<ContentSequenceManager>(
        new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));
```

For caller-provided IDs, resolve a keyed structure:

```csharp
var content = ContentManagers.ForStructure<KeyedContentManager<string>>(
    new KeyedContentStructure<string>());

content.Add("server-started", new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));

ContentEntryRecord record = content.Get("server-started");
```

See the [Quick Start](docs/QUICK_START.md) for the beginner-first walkthrough.

## Documentation

Start here:

1. [Quick Start](docs/QUICK_START.md)
2. [Concepts](docs/CONCEPTS.md)

Focused guides:

- [Content Entries](docs/CONTENT_ENTRIES.md)
- [Content Identity](docs/CONTENT_IDENTITY.md)
- [Content Structures](docs/CONTENT_STRUCTURES.md)
- [Content Managers](docs/CONTENT_MANAGERS.md)
- [Content Changes](docs/CONTENT_CHANGES.md)
- [Content Snapshots](docs/CONTENT_SNAPSHOTS.md)
- [Extension Authoring](docs/EXTENSION_AUTHORING.md)
- [Failures](docs/FAILURES.md)
- [Export And Attachments](docs/EXPORT_AND_ATTACHMENTS.md)

See the [Changelog](CHANGELOG.md) for release history and migration-sensitive changes.

## License

Workes.ContentSystem is available under the [MIT License](LICENSE).
