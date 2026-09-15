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

## Sequence Structure

The first structure is `ContentSequenceStructure`.

Expected behavior:

- entries are appended chronologically with `Add`;
- retention is configured through `ContentOverflowPolicy`;
- `ContentOverflowPolicy.None` retains all records;
- `ContentOverflowPolicy.DropOldest(capacity)` drops the oldest retained record when the configured capacity is exceeded;
- retained records are read oldest to newest by default;
- `ContentSequenceReadOrder.NewestFirst` can expose retained records newest to oldest;
- entry IDs are assigned internally as increasing decimal strings.
- successful adds raise `Changed` after the new record is retained.

This covers console history, simple logs, chat scrollback, notification feeds, and other common streams.

FIFO-style history is now expressed as `ContentSequenceStructure` plus `ContentOverflowPolicy.DropOldest(capacity)` rather than as a separate type.

`ContentSequenceStructure` implements `IStructureAssignedIdContentStructure`, so it can be used directly or through `ContentManager`:

```csharp
var content = new ContentManager(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
```

Unbounded usage is explicit:

```csharp
var content = new ContentManager(
    new ContentSequenceStructure(ContentOverflowPolicy.None));
```

Lookup only finds retained records. A record that was dropped by capacity overflow is treated as not found.

When `DropOldest` overflow drops the oldest record, the structure emits one change event containing both the removed oldest record and the added new record.

Because sequence-generated IDs are sequential numbers, `ContentSequenceStructure` exposes numeric lookup:

```csharp
ContentEntryRecord record = sequence.Get(1);
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

- bounded keyed sequence;
- grouped feed;
- channel-based chat history;
- threaded conversation/forum structure;
- indexed/searchable structure;
- snapshot-aware structure;
- grid-like structure for forum or board-style UIs;
- composite structures that mirror entries into more than one view.

These should grow from the existing abstractions rather than making the sequence implementation complicated. Grouped and threaded structures should wait until focused structure contracts, manager-owned runtime mutation, and snapshot contracts are stable.

## Structure Contracts

Not every structure should support every operation.

`IContentStructure` is the base minimum useful contract. It covers retained records and lookup.

Additional behavior should be exposed through focused opt-in contracts, mirroring the InventorySystem style already used by:

- `IStructureAssignedIdContentStructure`;
- `IKeyedContentStructure<TId>`;
- `IContentChangeSource`;
- `IContentRetentionPolicyStructure`;
- `IContentReadOrderStructure`.

`ContentSequenceStructure` implements the retention policy and read-order contracts so structure-agnostic code can inspect those configured behaviors without depending on the concrete sequence type.

Future contracts can cover clearing, removing, retention mutation, snapshots, sorting, searching, or export only where a structure genuinely supports that behavior.

Runtime mutation should be manager-owned for normal callers, with structures opting into the underlying contracts that managers coordinate.

Built-in structures that support future snapshots should capture their own state, including retained records and structure-owned configuration.

This mirrors the strategy used in other Workes packages: keep the central abstraction small, then add focused optional contracts where they are genuinely needed. Avoid a broad capability metadata object unless a future stage finds a concrete use case that opt-in contracts cannot solve cleanly.
