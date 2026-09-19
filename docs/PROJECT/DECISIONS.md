# DECISIONS

## Purpose

Record long-lived architectural and design decisions.

## Usage

Use this file to explain why important choices were made, especially when alternatives were rejected.

## Maintenance

Append new decisions as they are made.

If a decision changes, add a new entry instead of deleting the old one.

## Rules

- Use decision IDs like D-001, D-002, D-003.
- Include context, decision, reasoning, and consequences.
- Prefer append-only history.
- Do not use this as a task list.

## Decision Format

Use this format for new decisions:

### D-001: Decision title

#### Context

What situation, problem, or tradeoff led to this decision?

#### Decision

What did we decide?

#### Reasoning

Why was this chosen?

#### Consequences

What does this make easier, harder, or more constrained?

## Accepted Decisions

### D-001: Package Name Is Workes.ContentSystem

#### Context

The planned package could have been named broadly around content or more narrowly around content entries.

#### Decision

The package name is `Workes.ContentSystem`.

#### Reasoning

`ContentSystem` is short, fits the existing Workes package naming pattern, and leaves room for entries, structures, managers, attachments, exports, and bridges. `ContentEntrySystem` is more explicit but longer and not meaningfully less broad.

#### Consequences

Docs, package metadata, Trello cards, and future implementation should use `Workes.ContentSystem` and ContentSystem terminology.

### D-002: Content Entries Are The Core Abstraction

#### Context

The package is intended to support console history, chat, logs, feeds, forum-like views, and custom UI streams without forcing one message shape.

#### Decision

The core abstraction is a content entry, represented by `IContentEntry`.

#### Reasoning

Entries allow the package to store meaning without owning the host's rendering, theme, user model, or domain-specific fields.

#### Consequences

Custom entries should be first-class. Core should avoid making every entry look like a log entry, chat message, or console output line.

### D-003: Every Stored Entry Has A Structure-Owned ID

#### Context

Different structures may need different identity models. A FIFO structure can use a simple increasing value, while distributed or externally synchronized structures may need durable IDs.

#### Decision

Every stored entry has an ID, but each structure defines what that ID means and how it is assigned or accepted.

#### Reasoning

This preserves a common way to reference stored entries while avoiding a one-size-fits-all identity model.

#### Consequences

The implementation must be careful not to make FIFO indexing the permanent package-wide ID concept. D-010 clarifies that IDs live on stored records rather than entry payloads.

### D-004: Structures Are Abstracted Through IContentStructure

#### Context

The package should eventually support more than one storage shape, including FIFO history, chat channels, forums, indexed structures, and snapshot-aware structures.

#### Decision

Storage and organization behavior should be abstracted through `IContentStructure` or an equivalent structure abstraction.

#### Reasoning

This mirrors the successful direction from InventorySystem, where the core model becomes more useful when the storage structure is swappable.

#### Consequences

The first FIFO structure should be implemented as one structure, not as assumptions scattered across the manager and entries.

### D-005: Bounded FIFO Is The First Structure

#### Context

The first useful ContentSystem structure should cover common streams without over-designing the package.

#### Decision

The first structure is a bounded chronological FIFO structure.

#### Reasoning

It directly supports console history, log history, chat scrollback, notifications, and simple feeds. It is also easy to test and reason about.

#### Consequences

More complex structures should be planned but not implemented before the FIFO path feels polished.

### D-006: Core Has No Built-In User Or Role System

#### Context

Chat and forum systems often have users, authors, roles, and permissions, but those concepts vary heavily between games and applications.

#### Decision

Core ContentSystem does not include a built-in user or role system.

#### Reasoning

A built-in user model would either be too weak for serious hosts or too intrusive for simple content storage. Host applications can model users through custom entries, custom structures, or higher-level packages.

#### Consequences

Permissions and roles belong in consuming systems such as ConsoleSystem or application-specific layers, not in ContentSystem core.

### D-007: Export, Storage Integration, And Bridges Are Optional Attachments

#### Context

Some hosts need file export, append-only logs, storage integration, or external logging bridges. Others only need in-memory content.

#### Decision

Export, storage integration, and bridge behavior should be opt-in attachment-style capabilities.

D-020 refines the storage direction: portable snapshots are the core serialization foundation, while attachments and bridges can build on those snapshots or on change events.

#### Reasoning

This keeps the simple workflow clean while making advanced workflows possible.

#### Consequences

Core should avoid forcing file formats, append policies, or platform dependencies into every content structure.

### D-008: ConsoleSystem Will Be Rebuilt On ContentSystem Later

#### Context

ConsoleSystem now contains a useful entry/history model, but its long-term role overlaps with the broader ContentSystem idea.

#### Decision

A future ConsoleSystem should be rebuilt on top of ContentSystem rather than grown indefinitely as its own content backend.

#### Reasoning

Console-specific behavior such as commands, parsing, autocomplete, permissions, and help generation belongs in ConsoleSystem. Shared content storage, entry extension, history hooks, and export foundations belong in ContentSystem.

#### Consequences

ContentSystem should be developed first. ConsoleSystem can later copy lessons from the current package, then integrate the new content foundation.

### D-009: Failure Model Uses Package-Owned Structured Failures

#### Context

ContentSystem needs a package-wide way to report expected operation rejection before entries, structures, exports, persistence, or bridges are implemented.

#### Decision

ContentSystem uses `ContentFailure`, `ContentFailureKind`, `ContentFailureCodes`, `ContentSystemException`, and `ContentOperationException` under `Workes.ContentSystem.Core`.

Built-in failure codes use the stable `workes.content.` prefix. Failure kinds include separate categories for entries, structures, export, attachments, persistence, bridges, extensions, validation, configuration, and unknown failures.

#### Reasoning

This mirrors the pattern used in Workes.InventorySystem and Workes.ConsoleSystem while preserving the failure areas already named in the ContentSystem foundation docs.

#### Consequences

Try-style APIs can return structured failure data, while expected-success APIs can throw package-owned exceptions carrying the same failure. Programmer misuse remains represented by standard .NET exceptions.

### D-010: Stored Entry Identity Uses ContentEntryId

#### Context

Stored content entries need IDs, but different structures may assign or accept different ID shapes.

#### Decision

ContentSystem uses `ContentEntryId` as the public stored-entry identity representation. `ContentEntryId` is a value type that wraps a non-empty string.

`IContentEntry` does not expose an ID. A `ContentEntryRecord` pairs a structure-assigned `ContentEntryId` with an `IContentEntry`.

#### Reasoning

A string-backed value type keeps identity flexible for FIFO, distributed, externally synchronized, and host-defined structures without exposing raw `object` values or making numeric FIFO IDs the package-wide model.

Keeping IDs on records instead of entry payloads keeps structures responsible for identity policy. This avoids making every caller part of the ID strategy.

#### Consequences

Stored records expose a stable typed ID while structures remain responsible for ID assignment or validation. Entry payloads do not define automatic ID generation or configurable ID strategies; those belong with structure implementation.

### D-011: Structure Abstraction Is Read And Lookup First

#### Context

ContentSystem needs one shared structure abstraction, but not every structure will add entries the same way. FIFO can assign IDs internally, while keyed or forum-like structures may require caller-provided IDs.

#### Decision

`IContentStructure` exposes retained records and lookup by `ContentEntryId`. Append workflows are structure-specific rather than part of the base abstraction.

#### Reasoning

This keeps the common structure API honest. It avoids forcing keyed/manual-ID structures to expose an unsupported generated-ID append method while still allowing normal consumers to read records and look them up consistently.

#### Consequences

`ContentSequenceStructure` exposes its own `Add(IContentEntry)` method and assigns IDs internally. Later structures can expose different add workflows without changing the shared read/lookup contract.

Concrete structures may expose natural lookup overloads for their ID model. The shared `ContentEntryId` lookup remains the structure-agnostic path.

### D-012: ID Strategies Validate Caller-Provided IDs

#### Context

Keyed structures need caller-provided IDs, but different hosts may prefer string IDs, integer-like IDs, or other stable ID shapes.

#### Decision

ContentSystem uses `IContentEntryIdStrategy<TId>` to validate and normalize typed caller-provided IDs and to validate normalized stored IDs restored from snapshots. The first built-in strategies are `StringContentEntryIdStrategy` and `IntegerContentEntryIdStrategy`.

`KeyedContentStructure<TId>` is the first strategy-backed structure. It requires callers to provide IDs and rejects invalid or duplicate IDs through structured failures.

Default keyed structure constructors resolve built-in strategies for `string` and `long`. Custom ID types require an explicit custom strategy.

#### Reasoning

This proves configurable identity with a simple structure before introducing richer forum, chat, or snapshot-aware structures. Keeping strategy non-generating avoids over-designing until a structure needs configurable generated IDs.

#### Consequences

FIFO remains internally generated and strategy-free. Keyed structures get clean typed ID APIs while preserving `ContentEntryId` as the shared structure-agnostic ID value.

### D-013: Manager Workflows Follow ID Ownership

#### Context

`IContentStructure` is intentionally read and lookup focused because structures do not all add entries the same way. FIFO-style structures assign IDs when entries are added, while keyed structures require caller-provided typed IDs.

#### Decision

ContentSystem exposes separate manager workflows for the two implemented write categories.

`ContentManager` works with an explicitly provided `IStructureAssignedIdContentStructure`. `ContentManager.For(...)` creates `ContentManager<TId>` for structure-assigned-ID structures that expose a natural retained-record ID type. `KeyedContentManager<TId>` works with `IKeyedContentStructure<TId>` and can create a keyed structure with a built-in default ID strategy for supported ID types.

`ContentManagerBase` is a public abstract base for shared read and lookup behavior across already-created managers.

#### Reasoning

This keeps write APIs honest and avoids a single manager with methods that only work for some structures. It also keeps normal usage small: choose the manager that matches the ID ownership model, then use typed methods from there.

#### Consequences

Users choose between structure-assigned managers and `KeyedContentManager<TId>` when constructing the root workflow. Shared code can accept `ContentManagerBase` when it receives managers from either workflow and only needs records or lookup by `ContentEntryId`.

### D-014: 0.1.0 Is The First Useful Prerelease

#### Context

The project is being built in stages before a stable 1.0.0 release. The first prerelease should become useful once entries, failures, structures, and manager workflows are implemented.

#### Decision

Version `0.1.0` represents the first useful prerelease, not the completed package. The following release-prep stage should polish metadata, docs, examples, packaging, and verification for that prerelease.

#### Reasoning

This keeps the pre-0.1.0 stages focused on reaching the first usable core. Later pre-1.0.0 work can add polish, optional capabilities, richer examples, hooks, exports, and attachments before the minimum completed 1.0.0 package.

#### Consequences

After Stage 6, Trello should prioritize `Prepare 0.1.0 release` before optional hooks, focused structure contracts, attachments, and broader examples.

### D-015: Committed Change Hooks Use Synchronous Events

#### Context

ContentSystem needs a lightweight way for UI layers, bridges, attachments, and manager-agnostic code to observe mutations without changing the pull-based `Records` and lookup workflow.

#### Decision

ContentSystem uses optional synchronous .NET events for committed content changes.

Observable structures implement `IContentChangeSource`. Built-in structures raise `Changed` after successful mutations. `ContentChangedEventArgs` carries added and removed records. Managers forward structure events through `ContentManagerBase.Changed` when the active structure is observable.

Rejected operations and read-only lookups do not raise events.

#### Reasoning

This mirrors the small event style used in Workes.InventorySystem while keeping hooks opt-in. Structures remain the authoritative mutation source, and managers provide a convenient shared observation point without inventing separate change payloads.

#### Consequences

Events are synchronous and handler exceptions are not swallowed. Core does not provide dispatcher behavior, async queues, weak events, buffering, or thread marshaling. Custom structures remain valid without implementing change hooks, but observers only receive manager events when the active structure opts in.

### D-016: PlainContentEntry Is The Only Planned Built-In Entry For 1.0

#### Context

ContentSystem entries are intentionally extensible, but adding many built-in entry shapes would bake in assumptions about chat, logs, forums, users, roles, severity, metadata, or presentation.

#### Decision

For the 1.0 built-in surface, `PlainContentEntry` is the only planned built-in entry type.

#### Reasoning

Plain text plus timestamp is useful for simple streams and examples without claiming to model every domain. Custom entries remain the primary extension path for richer semantics.

#### Consequences

Core should not add built-in chat, log, notification, forum, or metadata entry types before 1.0 unless a later decision changes this direction.

### D-017: 1.0 ID Strategy Built-Ins Stay Narrow

#### Context

ID strategies make keyed structures ergonomic, but every built-in strategy implies package support for an ID shape.

#### Decision

The selected 1.0 direction is to keep the built-in ID strategy set narrow: `string`, positive `long`, `Guid`, and `ContentEntryId` identity/fallback support.

#### Reasoning

These cover common string, numeric, globally unique, and already-normalized ID workflows without overfitting to slugs, enums, timestamps, ULIDs, or other domain choices.

#### Consequences

Additional strategies are late roadmap work and should not distract from core structure, mutation, and snapshot foundations.

### D-018: Bounded FIFO Evolves Into Configurable Bounded Structure Behavior

#### Context

The first bounded structure is FIFO-specific, but users may need the same bounded retention concept with different placement, read order, or overflow behavior.

#### Decision

The 1.0 direction is to reframe the FIFO-specific bounded structure into configurable bounded structure behavior. FIFO should become a placement and overflow configuration rather than the whole type identity.

#### Reasoning

This avoids forcing users to implement near-duplicate structures just to change a bounded structure's ordering or overflow policy.

#### Consequences

D-022 completes this rename by replacing `BoundedFifoContentStructure` with `ContentSequenceStructure` and making bounded retention an overflow policy instead of a structure identity.

### D-019: Runtime Mutation Is Manager-Owned

#### Context

Runtime mutation such as clearing, removing, changing capacity, or applying snapshot state needs validation, atomicity, capability checks, and coherent events.

#### Decision

Runtime mutation should mirror the InventorySystem direction: normal callers mutate through manager-owned APIs, while structures opt into the underlying focused contracts that make those mutations possible.

#### Reasoning

Manager-owned mutation keeps the normal workflow coherent and prevents callers from bypassing validation or event semantics. Structure opt-ins keep the base abstractions small.

#### Consequences

Structures should expose focused contracts for mutation support, but normal user documentation should route runtime mutation through managers.

### D-020: Snapshots Are The Serialization Foundation

#### Context

ContentSystem users need a low-friction way to save logs, chats, feeds, or forum-like content without the core package owning files, save slots, or a serializer dependency.

#### Decision

Core serialization should be based on portable snapshots split into three layers:

- entry snapshots for entry payloads;
- record snapshots for stored IDs plus entry payloads;
- structure snapshots for retained records plus structure-owned state.

Entry and structure snapshot support should be opt-in for custom implementations. Built-in entries and structures should provide exact round-trip support where practical.

#### Reasoning

This follows the InventorySystem snapshot lesson: expose serializer-friendly state objects, let applications choose storage, and reject unsupported custom data through structured failures instead of silently losing information.

#### Consequences

Export helpers and attachments can build on snapshots later, but they should not replace snapshots as the serialization foundation.

### D-021: Grouped And Threaded Structures Are Deferred Until Core Contracts Stabilize

#### Context

Grouped and threaded content are important use cases, but they may need custom write workflows, custom managers, snapshot behavior, mutation rules, and focused structure contracts.

#### Decision

Grouped and threaded structures are deferred until sequence configuration, focused structure contracts, runtime mutation, and snapshot contracts are stable.

#### Reasoning

Implementing richer structures before the core contracts settle would risk baking in the wrong manager and serialization shapes.

#### Consequences

The roadmap keeps grouped content and threaded/forum-like content as later 1.0 stages or evaluation work. Core still avoids a built-in user or role system.

### D-022: ContentSequenceStructure Replaces BoundedFifoContentStructure

#### Context

The first bounded implementation was named around FIFO behavior, but Stage 9 established that retention should be configurable and FIFO should be a policy choice rather than the type identity.

#### Decision

`BoundedFifoContentStructure` is replaced by `ContentSequenceStructure` with explicit read order and overflow policy configuration.

The first supported read orders are `OldestFirst` and `NewestFirst`.

The first supported overflow policies are:

- `ContentOverflowPolicy.None`, which retains all records;
- `ContentOverflowPolicy.DropOldest(capacity)`, which owns the retention bound and drops the oldest retained record when full.

#### Reasoning

This keeps the simple FIFO-style workflow intact while avoiding a public type name that makes bounded retention look permanently FIFO-only. It also avoids splitting sequence storage into bounded and unbounded structures when the real choice is retention policy.

#### Consequences

This is a prerelease breaking rename. Existing callers should construct `ContentSequenceStructure` directly and choose an explicit `ContentOverflowPolicy`. Additional placement and retention policies remain future work.

### D-023: Structure Capabilities Are Focused Opt-In Contracts

#### Context

The next roadmap stage needs a way to express what structures can do beyond the base read and lookup surface. A broad capability metadata object would risk duplicating the truth already expressed by implemented interfaces.

#### Decision

ContentSystem should mirror InventorySystem's contract style: `IContentStructure` remains the base minimum useful contract, and additional structure behavior is represented by focused opt-in interfaces.

Existing examples include `IStructureAssignedIdContentStructure`, `IKeyedContentStructure<TId>`, `IContentChangeSource`, `IContentRetentionPolicyStructure`, and `IContentReadOrderStructure`. Future mutation, snapshot, sorting, searching, or export behavior should follow the same pattern unless a later concrete requirement proves metadata is needed.

#### Reasoning

Contracts are harder to desynchronize than separate feature flags. If a structure implements an interface, callers can both discover and use the behavior through the same surface.

#### Consequences

Stage 11 should be reframed from capability metadata to capability contracts. Manager-owned workflows should coordinate these contracts instead of reading a broad metadata object.

### D-024: Runtime Mutation Uses Manager-Owned Shared APIs Over Focused Contracts

#### Context

After clear, remove, and runtime structure configuration were promoted into the core roadmap, the package needed a way to expose those operations without requiring every structure to support them.

#### Decision

`ContentManagerBase` owns the shared mutation APIs for clearing, removing by `ContentEntryId`, and setting structure parameters by stable ID. Those APIs delegate only when the active structure implements the relevant focused contract: `IContentClearableStructure`, `IContentRecordRemovalStructure`, or `IParameterizedContentStructure`.

Concrete managers can add natural typed overloads where the workflow owns a natural ID shape, such as sequence numeric IDs or keyed `TId` values. Typed keyed removal is itself opt-in through `IKeyedContentRecordRemovalStructure<TId>`, so custom keyed structures are not forced to support removal.

#### Reasoning

This mirrors InventorySystem's manager-owned mutation direction while keeping `IContentStructure` small. Shared manager code can mutate managers uniformly when the active structure supports the operation, structure configuration does not leak structure-specific methods onto the manager, and unsupported operations fail through the existing structured failure model instead of being hidden behind unrelated feature flags.

#### Consequences

Try-style unsupported mutations return `StructureUnsupportedOperation`. Expected-success mutation APIs throw `ContentOperationException` carrying the same failure. Successful mutations emit synchronous committed-change events when the structure is observable; rejected and no-op mutations emit no events.

### D-025: Entry Snapshots Use Capture Contracts And Explicit Restore Factories

#### Context

ContentSystem needs a serialization foundation before record and structure snapshots can preserve retained content state. Entries are the smallest snapshot layer, but custom entries may carry arbitrary domain data and cannot be restored safely by guessing constructor shapes.

#### Decision

Entry snapshot capture is opt-in through `IContentEntrySnapshotSerializable`. Restore uses an explicit `IContentEntrySnapshotFactory`, such as `PlainContentEntry.Factory`.

Entry snapshot payloads use Inventory-style serializer-friendly DTOs: `ContentSnapshotEncodedValue`, `ContentSnapshotValue`, `ContentSnapshotNamedValue`, and built-in scalar codecs. `PlainContentEntry` supports snapshot round trips out of the box with stable kind `workes.content.entry.plain` and data version `1`.

This entry-only stage does not add structure-level restore discovery. D-027 later adds package-wide entry factory registration for whole-structure restore.

#### Reasoning

This keeps capture close to the entry instance while making restore deliberate. It avoids silently flattening unsupported custom entries and avoids requiring disk I/O or a specific serializer in core.

#### Consequences

Unsupported entries fail capture with `SnapshotUnsupportedEntry`. Malformed data, unsupported versions, and codec rejection use `ContentFailureKind.Snapshot`. Record snapshots, structure snapshots, factory registries, and manager-level snapshot APIs remain future work.

### D-026: Record And Structure Snapshots Are Serializer-Friendly DTOs

#### Context

After entry snapshots, ContentSystem needs a portable shape for retained records and whole structures before built-in structures can implement exact capture and restore.

#### Decision

`ContentRecordSnapshot` stores the retained record ID as a plain string and stores the entry payload as a `ContentEntrySnapshot`.

`ContentStructureSnapshot` stores a stable structure kind, data version, retained `ContentRecordSnapshot` values, and a `ContentSnapshotValue` envelope for structure-owned state.

Stage 14 defined DTOs only. Validation helpers, capture/restore contracts, manager APIs, and built-in structure snapshot workflows were added in later stages.

#### Reasoning

Plain string IDs are serializer-friendly and avoid making DTO consumers understand `ContentEntryId`. Restore can validate and wrap IDs later. A `ContentSnapshotValue` structure data envelope matches the entry snapshot value model while leaving each structure free to own its exact state schema.

#### Consequences

The DTO shapes gave later structure snapshot workflows a stable wire shape. Custom structures still need explicit opt-in contracts before they can participate in snapshot workflows.

### D-027: Structure Snapshot Restore Uses Explicit Factories And Registered Entry Factories

#### Context

Whole-structure snapshots need to restore retained records, entry payloads, and structure-owned state without requiring ContentSystem to own disk I/O, serializer configuration, or global type registration.

#### Decision

Structure snapshot capture is opt-in through `IContentStructureSnapshotSerializable`. Structure restore uses an explicit `IContentStructureSnapshotFactory`.

Entry payload restore during structure restore uses `ContentEntrySnapshotFactories`, a package-owned static registry. The registry includes package built-ins such as `PlainContentEntry.Factory`; custom entry factories must be registered by the application before restoring snapshots that contain those entry kinds.

Managers own the normal restore workflow. `ContentManagerBase` restores a new structure first, verifies that the concrete manager can accept it, swaps the active structure atomically, and emits one `ContentChangeKind.SnapshotRestored` event with `RequiresFullRefresh = true` after commit.

#### Reasoning

Explicit factories keep restore deliberate. A package-owned registry makes custom entry restore low-friction after one-time application setup and avoids repeated load-site plumbing. Manager-owned restore preserves the runtime mutation pattern and keeps compatibility checks, event resubscription, and full-refresh signaling in one place.

#### Consequences

Unsupported custom structures fail with `SnapshotUnsupportedStructure`. Missing entry factories fail with `SnapshotFactoryMissing`; conflicting factory registration fails with `SnapshotFactoryDuplicate`. Failed restore leaves the active manager state unchanged and emits no event. Built-in sequence and keyed structures can round-trip exact retained state while applications remain responsible for choosing how snapshots are serialized or stored.
