# Export And Attachments

Export and bridges should be optional extensions around the core content model.

Portable snapshots are the planned serialization and state-transfer foundation. See [Content Snapshots](CONTENT_SNAPSHOTS.md).

## Purpose

Some applications only need in-memory content. Others need to write logs to disk, append chat transcripts, export forum threads, mirror entries into a host logging framework, or bridge entries into another system.

Those features should be possible without forcing every user or every structure to configure them.

## Attachments

An attachment is a future optional component that works with content entries, structures, snapshots, or change events.

Examples:

- file exporter;
- append-only text writer;
- JSON snapshot exporter;
- host logging bridge;
- Unity adapter;
- Godot adapter;
- .NET logging adapter.

The exact attachment API is not designed yet. The important design direction is that attachments are opt-in and modular.

## Export Modes

Two export shapes are worth preserving in the design:

- snapshot export, where a structure snapshot is converted into a file or object in one operation;
- append export, where new entries are written as they arrive.

Not every structure needs to support both. A bounded in-memory stream might export a snapshot. A log bridge might append continuously.

## Core Boundary

Core ContentSystem should own entries, structures, manager workflow, portable snapshot DTOs, and failure semantics.

Platform-specific or host-specific conversion should live in attachments or adapter packages unless a very small engine-neutral abstraction clearly belongs in core.
