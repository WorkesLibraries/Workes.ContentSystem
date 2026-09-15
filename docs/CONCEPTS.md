# Concepts

Workes.ContentSystem is a backend for extensible content streams.

The package should not assume that all content is chat, logs, console output, or forum posts. Those are all possible uses of the same deeper model: an application stores entries in a structure, then a host UI or integration decides how to present or export them.

## Content Entries

A content entry is one item in a content structure.

Entries are intentionally extensible. A simple application might only use plain text entries. A larger game or tool might add entries for chat messages, command output, stack traces, item links, moderation events, audit events, or grouped feed items.

When an entry is stored, the retained record has a `ContentEntryId`. The structure decides what that ID means. A bounded FIFO structure might use an increasing integer-like value. A distributed or externally synchronized structure might use a durable string or UUID-like value.

See [Content Identity](CONTENT_IDENTITY.md) for stored IDs, caller-provided IDs, and ID strategies.

## Content Structures

A content structure owns storage behavior.

The first implementation is a bounded chronological FIFO structure: new entries are appended, old records are dropped when capacity is reached, and consumers can read retained records in order.

The 1.0 direction is to evolve this into configurable bounded behavior where FIFO is one supported placement and overflow policy.

A keyed structure is also available for callers that want to provide typed IDs directly. It uses an ID strategy to validate and normalize those IDs.

Other structures can behave very differently. A forum-like structure might group entries by thread. A chat structure might group by channel. A searchable structure might maintain indexes. A snapshot-aware structure might capture and restore portable state.

## Content Managers

Manager workflows are separated by entry ID ownership.

Use `ContentManager` for structures that assign IDs when entries are added. Structure choice is explicit, so the simple FIFO path is create a manager with `BoundedFifoContentStructure`, add entries, and read records.

Use `KeyedContentManager<TId>` for structures where caller-provided IDs are first-class. This keeps keyed add and lookup typed without adding overloads for every possible ID shape. Built-in ID strategies are resolved for supported ID types, and custom ID types can provide custom strategies.

`ContentManagerBase` provides the shared read and lookup surface for code that can work with existing managers from either workflow. It is common infrastructure, not a construction path.

See [Content Managers](CONTENT_MANAGERS.md) for the manager workflow split.

## Content Changes

Some structures can raise committed-change notifications through `IContentChangeSource`.

Built-in structures raise synchronous `Changed` events after successful mutations. Managers forward those events through `ContentManagerBase.Changed`, using the manager as the event sender. Rejected operations and read-only lookups do not raise events.

See [Content Changes](CONTENT_CHANGES.md) for event payloads and hook semantics.

## Attachments

Portable snapshots are planned as the serialization foundation. Snapshot objects should be easy for applications to serialize, save, and load with their own tools.

Export, file appenders, log bridges, and platform integrations should be optional. The core package should make those capabilities possible without forcing every structure or every user to support them.

## Relationship To ConsoleSystem

Workes.ConsoleSystem inspired this package. Console history, command output, command failures, and custom console entries are all examples of content entries.

The long-term direction is to build a future ConsoleSystem on top of ContentSystem rather than continue growing ConsoleSystem as a one-off content backend.
