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

The core abstraction is a content entry, likely represented by `IContentEntry`.

#### Reasoning

Entries allow the package to store meaning without owning the host's rendering, theme, user model, or domain-specific fields.

#### Consequences

Custom entries should be first-class. Core should avoid making every entry look like a log entry, chat message, or console output line.

### D-003: Every Stored Entry Has A Structure-Owned ID

#### Context

Different structures may need different identity models. A FIFO structure can use a simple increasing value, while persistent or distributed structures may need durable IDs.

#### Decision

Every stored entry has an ID, but each structure defines what that ID means and how it is assigned or accepted.

#### Reasoning

This preserves a common way to reference stored entries while avoiding a one-size-fits-all identity model.

#### Consequences

The first implementation must be careful not to make FIFO indexing the permanent package-wide ID concept. D-010 clarifies that IDs live on stored records rather than entry payloads.

### D-004: Structures Are Abstracted Through IContentStructure

#### Context

The package should eventually support more than one storage shape, including FIFO history, chat channels, forums, indexed structures, and persistent structures.

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

### D-007: Export, Persistence, And Bridges Are Optional Attachments

#### Context

Some hosts need file export, append-only logs, persistence, or external logging bridges. Others only need in-memory content.

#### Decision

Export, persistence, and bridge behavior should be opt-in attachment-style capabilities.

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

A string-backed value type keeps identity flexible for FIFO, persistent, distributed, and host-defined structures without exposing raw `object` values or making numeric FIFO IDs the package-wide model.

Keeping IDs on records instead of entry payloads keeps structures responsible for identity policy. This avoids making every caller part of the ID strategy.

#### Consequences

Stored records expose a stable typed ID while structures remain responsible for ID assignment or validation. Stage 3 does not define automatic ID generation or configurable ID strategies; those belong with structure implementation.

### D-011: Structure Abstraction Is Read And Lookup First

#### Context

ContentSystem needs one shared structure abstraction, but not every structure will add entries the same way. FIFO can assign IDs internally, while keyed or forum-like structures may require caller-provided IDs.

#### Decision

`IContentStructure` exposes retained records and lookup by `ContentEntryId`. Append workflows are structure-specific rather than part of the base abstraction.

#### Reasoning

This keeps the common structure API honest. It avoids forcing keyed/manual-ID structures to expose an unsupported generated-ID append method while still allowing normal consumers to read records and look them up consistently.

#### Consequences

`BoundedFifoContentStructure` exposes its own `Add(IContentEntry)` method and assigns IDs internally. Later structures can expose different add workflows without changing the shared read/lookup contract.

Concrete structures may expose natural lookup overloads for their ID model. The shared `ContentEntryId` lookup remains the structure-agnostic path.

### D-012: ID Strategies Validate Caller-Provided IDs

#### Context

Keyed structures need caller-provided IDs, but different hosts may prefer string IDs, integer-like IDs, or other stable ID shapes.

#### Decision

ContentSystem uses `IContentEntryIdStrategy<TId>` to validate and normalize typed caller-provided IDs. The first built-in strategies are `StringContentEntryIdStrategy` and `IntegerContentEntryIdStrategy`.

`KeyedContentStructure<TId>` is the first strategy-backed structure. It requires callers to provide IDs and rejects invalid or duplicate IDs through structured failures.

Default keyed structure constructors resolve built-in strategies for `string` and `long`. Custom ID types require an explicit custom strategy.

#### Reasoning

This proves configurable identity with a simple structure before introducing richer forum, chat, or persistent structures. Keeping strategy non-generating avoids over-designing until a structure needs configurable generated IDs.

#### Consequences

FIFO remains internally generated and strategy-free. Keyed structures get clean typed ID APIs while preserving `ContentEntryId` as the shared structure-agnostic ID value.

### D-013: Manager Workflows Follow ID Ownership

#### Context

`IContentStructure` is intentionally read and lookup focused because structures do not all add entries the same way. FIFO-style structures assign IDs when entries are added, while keyed structures require caller-provided typed IDs.

#### Decision

ContentSystem exposes separate manager workflows for the two implemented write categories.

`ContentManager` works with `IStructureAssignedIdContentStructure` and uses `BoundedFifoContentStructure` by default. `KeyedContentManager<TId>` works with `IKeyedContentStructure<TId>`.

`ContentManagerBase` is a public abstract base for shared read and lookup behavior across managers.

#### Reasoning

This keeps write APIs honest and avoids a single manager with methods that only work for some structures. It also keeps normal usage small: choose the manager that matches the ID ownership model, then use typed methods from there.

#### Consequences

Users choose between `ContentManager` and `KeyedContentManager<TId>` when constructing the root workflow. Shared code can accept `ContentManagerBase` when it only needs records or lookup by `ContentEntryId`.

### D-014: 0.1.0 Is The First Useful Prerelease

#### Context

The project is being built in stages before a stable 1.0.0 release. The first prerelease should become useful once entries, failures, structures, and manager workflows are implemented.

#### Decision

Version `0.1.0` represents the first useful prerelease, not the completed package. The following release-prep stage should polish metadata, docs, examples, packaging, and verification for that prerelease.

#### Reasoning

This keeps the pre-0.1.0 stages focused on reaching the first usable core. Later pre-1.0.0 work can add polish, optional capabilities, richer examples, hooks, exports, and attachments before the minimum completed 1.0.0 package.

#### Consequences

After Stage 6, Trello should prioritize `Prepare 0.1.0 release` before optional hooks, capability metadata, attachments, and broader examples.
