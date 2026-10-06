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
- `ContentMapManager<TId>` works with the built-in map structure;
- `ContentManagerBase` provides shared read and lookup behavior for manager-agnostic code.

See [Content Managers](CONTENT_MANAGERS.md) for manager usage.

## Sequence Structure

The first structure is `ContentSequenceStructure`.

Expected behavior:

- entries are appended chronologically with `Add`;
- retention is configured through `ContentOverflowPolicy`;
- `ContentOverflowPolicy.None` retains all records;
- `ContentOverflowPolicy.DropOldest(capacity)` drops the oldest retained record when the configured capacity is exceeded;
- `ContentOverflowPolicy.Reject(capacity)` rejects adds once the configured capacity is reached;
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

When `Reject` capacity is reached, the add fails with `ContentFailureCodes.StructureCapacityReached`, preserves state, and emits no event.

Changing the sequence `overflowPolicy` parameter through `ContentSequenceManager.SetStructureParameter` may trim oldest records immediately. Changing from `DropOldest` to `None` stops future overflow without resetting generated ID source state.

Because the normal sequence uses generated positive long IDs, `ContentSequenceStructure` exposes numeric lookup:

```csharp
ContentEntryRecord record = sequence.Get(1);
```

Advanced users can use `ContentSequenceStructure<TId>` with a custom `IContentGeneratedIdSource<TId>` when a sequence needs a different ID model. The matching manager is `ContentSequenceManager<TId>`.

The shared `ContentEntryId` lookup remains available for code that works through `IContentStructure`.

## Map Structure

`ContentMapStructure<TId>` stores records using caller-provided IDs.

It uses an `IContentEntryIdStrategy<TId>` to validate and normalize typed IDs before records are stored or fetched. ID strategies also validate normalized stored IDs during map snapshot restore. Map structures do not generate IDs.

Built-in strategies include:

- `StringContentEntryIdStrategy`, for non-empty string IDs;
- `IntegerContentEntryIdStrategy`, for positive integer IDs normalized as invariant decimal strings.

Built-in default strategies are available for `string` and `long`.

String-map usage:

```csharp
var map = new ContentMapStructure<string>();

map.Add("thread-main", new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));

ContentEntryRecord record = map.Get("thread-main");
```

Integer-map usage:

```csharp
var map = new ContentMapStructure<long>();

map.Add(8, new PlainContentEntry(DateTimeOffset.UtcNow, "Eighth entry."));

ContentEntryRecord record = map.Get(8);
```

Custom ID types are supported by passing a custom `IContentEntryIdStrategy<TId>` to the constructor. Custom strategies must implement both caller-facing normalization and restored normalized-ID validation.

See [Content Identity](CONTENT_IDENTITY.md) for the identity model and strategy guidance.

`ContentMapStructure<TId>` implements `IContentMapStructure<TId>`, so it can also be used through `ContentMapManager<TId>`:

```csharp
var content = ContentManagers.ForStructure<ContentMapManager<string>>(
    new ContentMapStructure<string>());

content.Add("thread-main", new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));
```

Duplicate IDs and IDs rejected by the active strategy fail through `ContentFailure`. Missing lookups still use `EntryNotFound`.

Successful map adds, removals, and clears raise `Changed`. Duplicate IDs, invalid IDs, and missing removals are rejected without raising change events.

Map also exposes `Set`, which adds a missing ID or replaces an existing record in place. Replacements emit `ContentChangeKind.Replaced` with the new record in `AddedRecords` and the replaced record in `RemovedRecords`.

## Single-Entry Structure

`ContentSingleStructure<TId>` stores at most one retained record.

It is useful for current status, selected item details, current objective text, latest announcement, or any state where the structure should expose one current entry rather than a growing history.

Replacement behavior is configured through `ContentSingleReplacementPolicy`:

- `Replace` accepts a new record and replaces the current record when one exists;
- `Reject` accepts the first record and then rejects additional sets until the structure is cleared.

The non-generic `ContentSingleStructure` is the normal long-ID path:

```csharp
var current = ContentManagers.ForStructure<ContentSingleManager>(
    new ContentSingleStructure(ContentSingleReplacementPolicy.Replace));

ContentEntryRecord record = current.Set(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Current objective"));
```

Advanced users can use `ContentSingleStructure<TId>` with a custom generated ID source. Both generated-ID `Set(entry)` and explicit-ID `Set(id, entry)` are supported.

Successful replacement emits `ContentChangeKind.Replaced`. Rejected replacement uses `StructureCapacityReached`, preserves state, and emits no event.

## Stack Structure

`ContentStackStructure<TId>` stores records as a last-in-first-out stack.

It supports:

- `Push`;
- `Peek`;
- `Pop`;
- lookup and removal by ID;
- clear;
- configurable read order through `ContentStackReadOrder.TopFirst` or `BottomFirst`.

The stack uses configurable overflow state rather than separate bounded/unbounded types:

- `ContentOverflowPolicy.None` allows the stack to grow without package-owned capacity;
- `ContentOverflowPolicy.Reject(capacity)` rejects pushes once the capacity is reached.

`DropOldest` is intentionally not supported by stack because silently dropping the bottom retained record would make stack behavior surprising.

```csharp
var stack = ContentManagers.ForStructure<ContentStackManager>(
    new ContentStackStructure(ContentOverflowPolicy.Reject(capacity: 20)));

stack.Push(new PlainContentEntry(DateTimeOffset.UtcNow, "Opened menu"));
ContentEntryRecord top = stack.Peek();
ContentEntryRecord removed = stack.Pop();
```

Advanced users can use `ContentStackStructure<TId>` with a custom generated ID source. Both generated-ID `Push(entry)` and explicit-ID `Push(id, entry)` are supported.

## Compound Structure

`ContentCompoundStructure<TId>` stores records as an owned tree.

It is useful for hierarchy-shaped content such as grouped feed items, topic/post layouts, nested notes, or scene-like content trees without baking in forum, chat, user, role, or channel concepts.

The compound model is intentionally a tree:

- each node stores one `ContentEntryRecord`;
- each node has zero or one parent;
- root nodes have no parent;
- child nodes belong to exactly one parent.

Removal behavior is configured through `ContentCompoundChildRemovalPolicy`:

- `Reject` rejects removal of nodes that still have children;
- `RemoveSubtree` removes the target node and all descendants.

Sibling read direction is configured through `ContentCompoundSiblingReadOrder.OldestFirst` or `NewestFirst`.

The non-generic `ContentCompoundStructure` is the normal long-ID path:

```csharp
var tree = ContentManagers.ForStructure<ContentCompoundManager>(
    new ContentCompoundStructure(ContentCompoundChildRemovalPolicy.RemoveSubtree));

ContentCompoundNode topic = tree.AddRoot(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Topic"));
ContentCompoundNode reply = tree.AddChild(
    1,
    new PlainContentEntry(DateTimeOffset.UtcNow, "Reply"));
```

Compound structures expose hierarchy-native operations such as `AddRoot`, `AddChild`, `GetNode`, `GetRoots`, `GetChildren`, `Remove`, and `Clear`. `Records` is a deterministic depth-first flattened view of retained records using the configured sibling order.

Advanced users can use `ContentCompoundStructure<TId>` with a custom generated ID source. Both generated-ID and explicit-ID root/child creation are supported.

Compound snapshots preserve records, parent-child relationships, root/child insertion order, policies, sibling order, and generated ID source state without changing the normal compound user path.

## Future Structures

The abstraction should leave room for other useful structures:

- channel-based chat history;
- threaded conversation/forum structure;
- indexed/searchable structure;
- snapshot-aware structure;
- grid-like structure for forum or board-style UIs;
- composite structures that mirror entries into more than one view.

These should grow from the existing abstractions rather than making the sequence implementation complicated. Grouped and threaded use cases should first be evaluated against `ContentCompoundStructure`; domain-specific structures should only be added when the compound tree cannot express the workflow cleanly.

## Structure Contracts

Not every structure should support every operation.

`IContentStructure` is the base minimum useful contract. It covers retained records and lookup.

Reusable structure families can use abstract bases when the workflow is shared by more than one likely structure. `ContentSequenceStructureBase<TId>` defines the current sequence-family instruction set, while `ContentMapStructureBase<TId>` defines the map-family instruction set. Concrete structures still create dedicated managers.

Additional behavior should be exposed through focused opt-in contracts, mirroring the InventorySystem style already used by:

- `IStructureAssignedIdContentStructure`;
- `IContentMapStructure<TId>`;
- `IContentChangeSource`;
- `IContentRetentionPolicyStructure`;
- `IContentReadOrderStructure`;
- `IContentNaturalIdStructure<TId>`;
- `IContentNaturalIdRemovalStructure<TId>`;
- `IContentClearableStructure`;
- `IContentRecordRemovalStructure`;
- `IContentMapRecordRemovalStructure<TId>`;
- `IParameterizedContentStructure`.
- `IContentStructureSnapshotRoundTrippable`.

`ContentSequenceStructure` implements the retention policy, read-order, natural long-ID lookup/removal, clear, remove, and parameterized structure contracts. Its first parameter is `ContentSequenceStructure.OverflowPolicyParameterId`. `ContentMapStructure<TId>` implements map add/lookup, set, clear, typed map removal, normalized removal, and change-source contracts. `ContentSingleStructure<TId>`, `ContentStackStructure<TId>`, and `ContentCompoundStructure<TId>` add focused built-in families for single-entry state, stack workflows, and owned-tree workflows.

Future contracts can cover sorting, searching, or export only where a structure genuinely supports that behavior.

Runtime mutation should be manager-coordinated for normal callers, with structures owning the actual retained-state mutation through focused opt-in contracts.

Manager resolution is part of the base shape: every structure creates the manager that knows how to operate it. Actual supported operations are still expressed through the focused contracts listed above.

`ContentStructureSnapshot` is the portable DTO shape for retained records plus structure-owned data. Built-in sequence, map, single, stack, and compound structures capture their own state into that DTO, including retained records and structure-owned configuration. Round-trippable structures expose a `SnapshotFactory` so normal manager restore can use `RestoreSnapshot(snapshot)` while still letting custom structures own their state schema.

Custom structures can use `ContentStructureSnapshotFactoryBase<TStructure>`, the sequence/map family snapshot factory bases, `ContentSnapshotRecords`, and `ContentSnapshotProperties` to implement the same snapshot pattern without copying built-in structure internals. Sequence extensions can choose the generic generated-ID source helper or the long-ID numeric convenience helper. See [Extension Authoring](EXTENSION_AUTHORING.md).

This mirrors the strategy used in other Workes packages: keep the central abstraction small, then add focused optional contracts where they are genuinely needed. Avoid a broad capability metadata object unless a future stage finds a concrete use case that opt-in contracts cannot solve cleanly.

Manager creation is intentionally not broad capability metadata. It exists because manager selection itself is a base structure concern across sequence, map, single-entry, stack, compound, and custom structures.
