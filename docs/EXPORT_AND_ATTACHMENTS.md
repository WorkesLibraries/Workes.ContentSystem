# Export And Attachments

Export creates readable text from retained records. Attachments are optional references to host-owned resources. Neither capability changes the portable snapshot format or adds storage to ContentSystem.

## Export Records

Every manager can export a stable snapshot of its records through an `IContentRecordExporter`. The exporter receives a read-only copy in the manager's current read order. Export does not change the manager or raise change events. Expected rejection is returned as a structured `ContentFailure` with the export failure kind; `Export(...)` throws a `ContentOperationException` carrying that same failure.

Built-in `PlainTextContentRecordExporter` and `MarkdownContentRecordExporter` handle `PlainContentEntry` records. Plain text joins each entry's text with line feeds. Markdown writes one heading with the entry's round-trip timestamp followed by its text, escaping Markdown punctuation and raw HTML characters so plain text remains text. Both reject a record containing another entry type rather than silently dropping or flattening data.

```csharp
using System;
using Workes.ContentSystem.Core;

ContentSequenceManager content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));
content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));

ContentExportResult export = content.Export(new MarkdownContentRecordExporter());
Console.WriteLine(export.FormatId); // workes.content.export.markdown
Console.WriteLine(export.Text);
```

Implement `IContentRecordExporter` for another entry type or format. Use its stable format ID to label the returned output. Return `false` and a structured failure for expected rejection; the manager wraps that cause in the package's standard export failure. Exceptions are allowed to propagate for programmer errors or unexpected faults. The record objects and order are snapshotted, but entry payloads are not cloned.

```csharp
using System.Collections.Generic;
using System.Linq;
using Workes.ContentSystem.Core;

public sealed class QuestLogExporter : IContentRecordExporter
{
    public string FormatId => "example.quest-log.text";

    public bool TryExport(
        IReadOnlyList<ContentEntryRecord> records,
        out string? text,
        out ContentFailure? failure)
    {
        text = string.Join("\n", records.Select(record => record.PlainText));
        failure = null;
        return true;
    }
}

ContentExportResult questExport = content.Export(new QuestLogExporter());
```

Export returns text only. Writing files, streaming, encoding, compression, and delivery remain the host application's responsibility. Exporting snapshots as JSON or another persistence representation is also a host concern; use [Content Snapshots](CONTENT_SNAPSHOTS.md) as the portable state model.

## Attachment References

An entry may implement `IContentEntryAttachmentSource` to expose `ContentAttachmentReference` values. Each immutable reference contains a required opaque locator and optional media type, file name, and display name.

```csharp
using System;
using System.Collections.Generic;
using Workes.ContentSystem.Core;

public sealed class ImageEntry : IContentEntry, IContentEntryAttachmentSource
{
    public ImageEntry(string text, ContentAttachmentReference image)
    {
        PlainText = text;
        Attachments = Array.AsReadOnly(new[] { image });
    }

    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
    public string PlainText { get; }
    public IReadOnlyList<ContentAttachmentReference> Attachments { get; }
}

var entry = new ImageEntry(
    "Found a portrait.",
    new ContentAttachmentReference(
        locator: "asset:characters/hero-portrait",
        mediaType: "image/png",
        fileName: "hero.png",
        displayName: "Hero portrait"));

IReadOnlyList<ContentAttachmentReference> references = ContentEntryAttachments.GetReferences(entry);
```

`ContentEntryAttachments.GetReferences` returns an immutable snapshot and an empty list for entries without the optional capability. The locator is not interpreted as a URI, path, or storage key by the package. ContentSystem does not retrieve, copy, write, or validate attachment bytes; the host resolves references and owns their lifecycle.

If attachment references must survive snapshots, include them in the entry's own `IContentEntrySnapshotSerializable` data and restore them through that entry's factory. The package does not add attachment fields to entry or structure snapshot DTOs.

## Boundaries

| Capability | Purpose | Owner |
|---|---|---|
| Structure snapshots | Portable content state, IDs, order, and structure configuration | ContentSystem contracts and entry/structure implementations |
| Record export | Human-readable text in a selected format | Supplied exporter; host handles delivery |
| Attachment references | Describe how an entry relates to a host resource | Entry supplies metadata; host owns bytes and resolution |

Disk I/O, save slots, append policies, cloud sync, compression, and platform adapters remain outside this package. Exporters and attachment sources are optional and do not add required members to manager, structure, or entry contracts.
