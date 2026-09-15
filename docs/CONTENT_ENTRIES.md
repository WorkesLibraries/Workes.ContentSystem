# Content Entries

Content entries are the primary extension point of Workes.ContentSystem.

## Purpose

An entry represents one piece of content stored in a content structure.

The core package supports a small ordinary entry shape, but it should not try to predict every game, tool, editor, or server use case. Custom entries are expected and should feel first-class.

## Entry Identity

Stored entries expose a `ContentEntryId`.

The structure decides what IDs mean and how they are assigned. This keeps the model flexible:

- a bounded FIFO structure can use an increasing numeric ID;
- a persistent structure can use a durable string ID;
- a distributed structure can use UUID-like IDs;
- a threaded structure can use IDs that help relate replies and parents.

`ContentEntryId` wraps a non-empty string value. It stores the ID assigned by a structure without making FIFO indexes, GUIDs, or durable IDs the only package-wide identity model.

An `IContentEntry` does not carry its own ID. A `ContentEntryRecord` pairs a structure-assigned `ContentEntryId` with an `IContentEntry`.

This keeps the identity rule clear:

- entries describe content;
- structures decide how IDs are assigned or accepted;
- records represent entries after they are stored in a structure.

See [Content Identity](CONTENT_IDENTITY.md) for ID ownership, keyed ID strategies, and lookup.

## Entry Content

`IContentEntry` is the core entry abstraction. It exposes:

- `Timestamp`;
- `PlainText`.

Entries expose simple plain text for easy rendering, searching, export, or debugging.

Entries may also expose richer data. For example:

- a chat entry can contain a channel, sender display name, and message text;
- a log entry can contain severity and source;
- a stack trace entry can contain collapsed frames;
- an item-link entry can contain an item ID and display text;
- a forum entry can contain parent/thread information.

The core provides enough common shape to be useful without making every custom entry inherit unnecessary fields.

## Plain Content Entries

`PlainContentEntry` is the first built-in entry type and the only planned built-in entry type for 1.0.

It stores:

- a timestamp;
- plain text.

It is intended for simple content streams and tests. It does not include author, channel, severity, metadata, rendering data, or attachment behavior.

This keeps the core package from baking in assumptions about chat, logs, forums, notifications, or UI rendering.

## Custom Entries

Custom entries should be the normal way to support domain-specific content.

If a host wants clickable player names, expandable exceptions, embedded buttons, item tooltips, or rich moderation events, it should define an entry type that carries those semantics directly. The UI can then render that entry according to the host application's own theme and layout system.

This keeps ContentSystem focused on storing meaning rather than owning visual rendering.

Custom entries implement `IContentEntry` directly. They do not need to inherit from a package base class.

Custom entries that need future snapshot support are expected to opt into snapshot round-trip contracts. See [Content Snapshots](CONTENT_SNAPSHOTS.md) for the planned direction.
