# API GUIDELINES

## Purpose

Record API style guidance for Workes.ContentSystem.

These guidelines keep the package consistent with the other Workes packages while leaving room to adjust exact type names during implementation.

## Root Workflow

Prefer manager-owned workflows for normal use.

The structure-assigned-ID root type is `ContentManager`. It should require an explicit structure so the active storage policy is visible at construction.

Use `ContentManager.For(...)` when a structure-assigned-ID structure exposes a natural retained-record ID type. This keeps normal usage ergonomic without asking users to spell a generic type that the structure already owns. Keep `new ContentManager<TId>(structure)` legal and document it briefly as the explicit equivalent.

Use `KeyedContentManager<TId>` for structures where caller-provided IDs are first-class. Do not make one manager expose write methods that only work for some structures.

`ContentManagerBase` is public shared read/lookup plumbing for code that can work with already-created managers from either workflow. It also owns shared manager workflows such as runtime mutation and structure snapshot restore when the active structure opts in. It is abstract and should stay small rather than becoming a catch-all capability surface.

Pre-17.2 should introduce the preferred structure-driven construction path: `ContentManagers.ForStructure(structure)`. Structures will declare a `ContentStructureWorkflow` through `IContentStructure`, and manager factories will resolve the correct concrete manager for that workflow. Keep direct constructors legal for explicit/manual use unless the implementation exposes a specific conflict.

The workflow descriptor is not capability metadata. It should identify the manager workflow only. Operation support remains represented by focused contracts and concrete manager APIs.

Advanced behavior should be opt-in through options, focused structure contracts, snapshots, or attachments.

Runtime mutation should be manager-owned for normal callers. Structures may expose focused opt-in contracts that managers coordinate. Unsupported manager mutations should return `StructureUnsupportedOperation` from try APIs and throw `ContentOperationException` from expected-success APIs.

## Structures

Represent shared read and lookup behavior through `IContentStructure`.

After Pre-17.2, `IContentStructure` should also expose the structure's `ContentStructureWorkflow`. This is a deliberate prerelease breaking change so every structure can participate in package-owned manager resolution.

Avoid baking FIFO assumptions into the whole package. `ContentSequenceStructure` provides the first sequence behavior, with unbounded retention and bounded drop-oldest retention expressed through `ContentOverflowPolicy`.

Use focused opt-in contracts to expose inspectable structure behavior and supported mutations. For example, retention policy belongs on `IContentRetentionPolicyStructure`, read order belongs on `IContentReadOrderStructure`, clear/remove support belongs on mutation-specific contracts, and runtime configuration belongs on `IParameterizedContentStructure`.

A structure should own:

- entry retention;
- ordering;
- lookup;
- ID assignment or validation;
- mutability rules;
- focused contract support.

Do not force one append method into the base structure abstraction. Structure-assigned-ID structures and caller-provided-ID structures should expose their own write workflows through focused interfaces.

Concrete structures should expose natural lookup and removal overloads for their ID model when removal is supported. For example, sequence generated-ID structures can support `Get(1)` and `Remove(1)` while generic code can continue using `ContentEntryId`. Keyed structures that support typed removal should opt into `IKeyedContentRecordRemovalStructure<TId>`.

ID strategies should validate and normalize typed caller-provided IDs and validate normalized stored IDs restored from snapshots. Built-in default strategy resolution is acceptable for explicitly supported ID types such as `string`, `long`, `Guid`, and `ContentEntryId`; custom ID types require custom strategies. Do not add generation behavior to that abstraction until a concrete structure needs configurable generated IDs.

## Entries

Use content entries as the primary extension path.

Do not force all entries into a chat-message or log-message shape. Custom entries should be ordinary, supported usage. `PlainContentEntry` is the only planned built-in entry type for 1.0.

Stored entry records should have IDs. Entry payloads should not require callers to invent IDs before a structure stores them.

Document first-class public concepts in focused guides:

- entries in `docs/CONTENT_ENTRIES.md`;
- identity in `docs/CONTENT_IDENTITY.md`;
- structures in `docs/CONTENT_STRUCTURES.md`;
- managers in `docs/CONTENT_MANAGERS.md`;
- change hooks in `docs/CONTENT_CHANGES.md`;
- snapshots in `docs/CONTENT_SNAPSHOTS.md`;
- failures in `docs/FAILURES.md`;
- future attachments in `docs/EXPORT_AND_ATTACHMENTS.md`.

## Failures And Exceptions

Follow the Workes package pattern:

- `Try...` methods are for expected failure and return structured failure data.
- Non-try expected-success methods throw package-owned exceptions carrying the same failure.
- Null arguments, invalid setup, and programmer misuse use standard .NET exceptions.

Failure codes should be stable, string-based, and package-prefixed.

## Optional Complexity

If a subsystem increases setup cost or is not needed by every consumer, make it optional.

This applies to:

- export;
- portable snapshots;
- host logging bridges;
- platform adapters;
- advanced structures;
- search/indexing;
- specialized mutation support;
- change hooks.

The simple use case should stay small: create a manager, add entries, read entries.

Change hooks should use ordinary synchronous .NET events. Raise them only after a mutation has committed, and do not emit events for rejected or no-op operations. Include enough event metadata for UI code to distinguish adds, removals, clears, and configuration changes. Do not add thread marshaling, buffering, or async dispatch to the core hook contract.

Snapshots should be serializer-friendly DTOs rather than direct file I/O. Entry snapshot capture should be opt-in on the entry instance, while restore should use a factory object registered in `ContentEntrySnapshotFactories`. Structure snapshot round trips should be opt-in on the structure through `IContentStructureSnapshotRoundTrippable`, and normal manager restore should use the active structure's `SnapshotFactory`. Explicit structure factories remain available for migration and advanced restore targets. Record and structure snapshots should keep stored IDs and structure data in serializer-friendly forms. Unsupported custom entries or structures should fail snapshot capture or restore with structured failures unless they opt in.

Structure extension helpers should reduce boilerplate without making inheritance mandatory. `ContentStructureSnapshotFactoryBase<TStructure>`, `ContentSnapshotRecords`, and `ContentSnapshotProperties` are convenience APIs for extension authors; direct implementation of the snapshot interfaces remains valid.

Custom manager workflows should follow the same pattern once Pre-17.2 lands: a custom structure declares a stable workflow descriptor, a custom manager factory is registered once during application or package setup, and unsupported or conflicting workflow resolution fails through structured ContentSystem failures.

## Documentation Expectations

Every first-class public concept should get a focused guide under `docs/`.

Project-control docs explain architectural intent. User-facing docs explain normal usage. Trello owns task state once the board is mapped.
