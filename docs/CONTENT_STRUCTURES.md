# Content Structures

Content structures define how entries are stored, ordered, found, and retained.

## Purpose

`IContentStructure` is the shared abstraction for reading retained content records and looking them up by ID.

The structure owns the rules. The manager should provide a convenient root workflow, but the structure decides what operations are supported and what entry IDs mean.

The common abstraction exposes:

- retained `ContentEntryRecord` values in the structure's read order;
- `TryGet` lookup by `ContentEntryId`;
- expected-success `Get` lookup by `ContentEntryId`.

Structures may also implement `IContentChangeSource` when they can notify observers about committed mutations.

Write workflows are structure-specific. This lets structure-assigned-ID structures and caller-provided-ID structures expose honest APIs without forcing every structure into one add method.

Prefer a concrete structure's natural lookup overload when working with that structure directly. Use `ContentEntryId` lookup through `IContentStructure` when writing structure-agnostic code.

Manager workflows follow the same split:

- `ContentManager` works with `IStructureAssignedIdContentStructure`;
- `KeyedContentManager<TId>` works with `IKeyedContentStructure<TId>`;
- `ContentManagerBase` provides shared read and lookup behavior for manager-agnostic code.

See [Content Managers](CONTENT_MANAGERS.md) for manager usage.

## First Structure

The first implementation is `BoundedFifoContentStructure`.

Expected behavior:

- entries are appended chronologically with `Add`;
- retained records are read oldest to newest;
- capacity is configurable and must be greater than zero;
- when capacity is exceeded, the oldest retained record is dropped;
- entry IDs are assigned internally as increasing decimal strings.
- successful adds raise `Changed` after the new record is retained.

This covers console history, simple logs, chat scrollback, notification feeds, and other common streams.

`BoundedFifoContentStructure` implements `IStructureAssignedIdContentStructure`, so it can be used directly or through the default manager:

```csharp
var content = new ContentManager(new BoundedFifoContentStructure());

content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
```

FIFO lookup only finds retained records. A record that was dropped by capacity overflow is treated as not found.

When capacity overflow drops the oldest record, FIFO emits one change event containing both the removed oldest record and the added new record.

Because FIFO IDs are sequential numbers, `BoundedFifoContentStructure` exposes numeric lookup:

```csharp
ContentEntryRecord record = fifo.Get(1);
```

The shared `ContentEntryId` lookup remains available for code that works through `IContentStructure`.

## Keyed Structure

`KeyedContentStructure<TId>` stores records using caller-provided IDs.

It uses an `IContentEntryIdStrategy<TId>` to validate and normalize typed IDs before records are stored or fetched. ID strategies validate caller-provided IDs only; they do not generate IDs.

Built-in strategies include:

- `StringContentEntryIdStrategy`, for non-empty string IDs;
- `IntegerContentEntryIdStrategy`, for positive integer IDs normalized as invariant decimal strings.

Built-in default strategies are available for `string` and `long`.

String-keyed usage:

```csharp
var keyed = new KeyedContentStructure<string>();

keyed.Add("thread-main", new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));

ContentEntryRecord record = keyed.Get("thread-main");
```

Integer-keyed usage:

```csharp
var keyed = new KeyedContentStructure<long>();

keyed.Add(8, new PlainContentEntry(DateTimeOffset.UtcNow, "Eighth entry."));

ContentEntryRecord record = keyed.Get(8);
```

Custom ID types are supported by passing a custom `IContentEntryIdStrategy<TId>` to the constructor.

See [Content Identity](CONTENT_IDENTITY.md) for the identity model and strategy guidance.

`KeyedContentStructure<TId>` implements `IKeyedContentStructure<TId>`, so it can also be used through `KeyedContentManager<TId>`:

```csharp
var content = new KeyedContentManager<string>();

content.Add("thread-main", new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));
```

Duplicate IDs and IDs rejected by the active strategy fail through `ContentFailure`. Missing lookups still use `EntryNotFound`.

Successful keyed adds raise `Changed` with the added record. Duplicate IDs and invalid IDs are rejected without raising change events.

## Future Structures

The abstraction should leave room for other useful structures:

- unbounded in-memory sequence;
- grouped feed;
- channel-based chat history;
- threaded conversation/forum structure;
- indexed/searchable structure;
- persistent file-backed structure;
- grid-like structure for forum or board-style UIs;
- composite structures that mirror entries into more than one view.

These should grow from the existing abstractions rather than making the first FIFO implementation complicated.

## Capabilities

Not every structure should support every operation.

A structure can be append-only, mutable, searchable, persistent, exportable, or none of those. Capability metadata can let consumers ask what a structure supports without forcing every structure into one large interface.

This mirrors the strategy used in other Workes packages: keep the central abstraction small, then add optional capabilities where they are genuinely needed.
