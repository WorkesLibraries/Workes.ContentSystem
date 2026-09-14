# Workes.ContentSystem

[![NuGet](https://img.shields.io/nuget/v/Workes.ContentSystem.svg)](https://www.nuget.org/packages/Workes.ContentSystem)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/WorkesLibraries/Workes.ContentSystem/blob/main/LICENSE)

Workes.ContentSystem is an engine-neutral backend for storing and organizing extensible content entries.

It is intended to be useful anywhere an application needs an ordered or structured stream of entries: console history, chat, logs, notifications, forum-like views, feeds, audit trails, and custom UI timelines.

## Highlights

- Extensible content entries instead of one fixed message shape.
- Pluggable content structures, starting with a bounded FIFO structure.
- Structure-assigned and caller-keyed manager workflows.
- Structure-owned entry identity so different storage models can use the IDs that fit them.
- A shared failure and exception model matching the style used in other Workes packages.
- Optional attachment points for export, persistence, bridges, and platform-specific integration.

## Installation

Install the package from [NuGet](https://www.nuget.org/packages/Workes.ContentSystem):

```bash
dotnet add package Workes.ContentSystem --version 0.1.0
```

Or add a package reference:

```xml
<PackageReference Include="Workes.ContentSystem" Version="0.1.0" />
```

The package targets .NET Standard 2.1.

## Quick Example

The default manager uses a bounded FIFO structure and assigns IDs when entries are added:

```csharp
var content = new ContentManager();

content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Player joined: Workes"));

foreach (ContentEntryRecord record in content.Records)
{
    Console.WriteLine($"{record.Id}: {record.PlainText}");
}
```

For caller-provided IDs, use a keyed manager:

```csharp
var content = new KeyedContentManager<string>();

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
- [Content Structures](docs/CONTENT_STRUCTURES.md)
- [Failures](docs/FAILURES.md)
- [Export And Attachments](docs/EXPORT_AND_ATTACHMENTS.md)

See the [Changelog](CHANGELOG.md) for release history and migration-sensitive changes.

## License

Workes.ContentSystem is available under the [MIT License](LICENSE).
