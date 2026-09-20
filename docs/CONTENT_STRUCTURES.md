# Content Structures

Content structures define how entries are stored, ordered, found, and retained.

## Purpose

`IContentStructure` is the shared abstraction for reading retained content records, looking them up by ID, and creating the structure's tailored manager.

The structure owns retained content state and rules. The manager should provide a convenient root workflow, but the structure decides what operations are supported, what entry IDs mean, and how records are retained or organized.

The common abstraction exposes:

- retained `ContentEntryRecord` values in the structure's read order;
- `TryGet` lookup by `ContentEntryId`;
- expected-success `Get` lookup by `ContentEntryId`.

Structures may also implement focused optional contracts when they support committed mutations, configuration inspection, or change notifications.

Write workflows are structure-specific. This lets structure-assigned-ID structures and caller-provided-ID structures expose honest APIs without forcing every structure into one add method.

Prefer a concrete structure's natural lookup overload when working with that structure directly. Use `ContentEntryId` lookup through `IContentStructure` when writing structure-agnostic code.

Manager resolution follows the same split:

- `ContentManagers.ForStructure(...)` asks a structure to create its tailored manager;
- `ContentSequenceManager` works with `ContentSequenceStructure`;
- `KeyedContentManager<TId>` works with the built-in keyed structure;
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
- records can be removed by ID;
- retained records can be cleared;
- the overflow policy can be changed at runtime.

This covers console history, simple logs, chat scrollback, notification feeds, and other common streams.

FIFO-style history is now expressed as `ContentSequenceStructure` plus `ContentOverflowPolicy.DropOldest(capacity)` rather than as a separate type.

`ContentSequenceStructure` implements `IStructureAssignedIdContentStructure<long>` and creates `ContentSequenceManager`:

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
ContentEntryRecord record = content.Get(1);
```

Unbounded usage is explicit:

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));
```

Lookup only finds retained records. A record that was dropped by capacity overflow is treated as not found.

When `DropOldest` overflow drops the oldest record, the structure emits one change event containing both the removed oldest record and the added new record.

Changing the sequence `overflowPolicy` parameter through `ContentSequenceManager.SetStructureParameter` may trim oldest records immediately. Changing from `DropOldest` to `None` stops future overflow without resetting generated IDs.

Because sequence-generated IDs are sequential numbers, `ContentSequenceStructure` exposes numeric lookup:

```csharp
ContentEntryRecord record = sequence.Get(1);
```

The shared `ContentEntryId` lookup remains available for code that works through `IContentStructure`.

## Keyed Structure

`KeyedContentStructure<TId>` stores records using caller-provided IDs.

It uses an `IContentEntryIdStrategy<TId>` to validate and normalize typed IDs before records are stored or fetched. ID strategies also validate normalized stored IDs during keyed snapshot restore. They do not generate IDs.

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

Custom ID types are supported by passing a custom `IContentEntryIdStrategy<TId>` to the constructor. Custom strategies must implement both caller-facing normalization and restored normalized-ID validation.

See [Content Identity](CONTENT_IDENTITY.md) for the identity model and strategy guidance.

`KeyedContentStructure<TId>` implements `IKeyedContentStructure<TId>`, so it can also be used through `KeyedContentManager<TId>`:

```csharp
var content = ContentManagers.ForStructure<KeyedContentManager<string>>(
    new KeyedContentStructure<string>());

content.Add("thread-main", new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));
```

Duplicate IDs and IDs rejected by the active strategy fail through `ContentFailure`. Missing lookups still use `EntryNotFound`.

Successful keyed adds, removals, and clears raise `Changed`. Duplicate IDs, invalid IDs, and missing removals are rejected without raising change events.

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

These should grow from the existing abstractions rather than making the sequence implementation complicated. Grouped and threaded structures should wait until focused structure contracts, manager-coordinated runtime mutation, and snapshot contracts are stable.

## Structure Contracts

Not every structure should support every operation.

`IContentStructure` is the base minimum useful contract. It covers retained records and lookup.

Reusable structure families can use abstract bases when the workflow is shared by more than one likely structure. `ContentSequenceStructureBase` defines the current sequence-family instruction set, while `KeyedContentStructureBase<TId>` defines the keyed-family instruction set. Concrete structures still create dedicated managers.

Additional behavior should be exposed through focused opt-in contracts, mirroring the InventorySystem style already used by:

- `IStructureAssignedIdContentStructure`;
- `IKeyedContentStructure<TId>`;
- `IContentChangeSource`;
- `IContentRetentionPolicyStructure`;
- `IContentReadOrderStructure`;
- `IContentNaturalIdStructure<TId>`;
- `IContentNaturalIdRemovalStructure<TId>`;
- `IContentClearableStructure`;
- `IContentRecordRemovalStructure`;
- `IKeyedContentRecordRemovalStructure<TId>`;
- `IParameterizedContentStructure`.
- `IContentStructureSnapshotRoundTrippable`.

`ContentSequenceStructure` implements the retention policy, read-order, natural long-ID lookup/removal, clear, remove, and parameterized structure contracts. Its first parameter is `ContentSequenceStructure.OverflowPolicyParameterId`. `KeyedContentStructure<TId>` implements keyed add/lookup, clear, typed keyed removal, normalized removal, and change-source contracts.

Future contracts can cover sorting, searching, or export only where a structure genuinely supports that behavior.

Runtime mutation should be manager-coordinated for normal callers, with structures owning the actual retained-state mutation through focused opt-in contracts.

Manager resolution is part of the base shape: every structure creates the manager that knows how to operate it. Actual supported operations are still expressed through the focused contracts listed above.

`ContentStructureSnapshot` is the portable DTO shape for retained records plus structure-owned data. Built-in sequence and keyed structures capture their own state into that DTO, including retained records and structure-owned configuration. Round-trippable structures expose a `SnapshotFactory` so normal manager restore can use `RestoreSnapshot(snapshot)` while still letting custom structures own their state schema.

Custom structures can use `ContentStructureSnapshotFactoryBase<TStructure>`, the sequence/keyed family snapshot factory bases, `ContentSnapshotRecords`, and `ContentSnapshotProperties` to implement the same snapshot pattern without copying built-in structure internals. See [Extension Authoring](EXTENSION_AUTHORING.md).

This mirrors the strategy used in other Workes packages: keep the central abstraction small, then add focused optional contracts where they are genuinely needed. Avoid a broad capability metadata object unless a future stage finds a concrete use case that opt-in contracts cannot solve cleanly.

Manager creation is intentionally not broad capability metadata. It exists because manager selection itself is a base structure concern before more built-ins such as single-entry, bounded keyed, or stack-like structures are added.
