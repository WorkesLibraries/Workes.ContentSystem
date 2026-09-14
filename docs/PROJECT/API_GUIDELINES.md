# API GUIDELINES

## Purpose

Record API style guidance for Workes.ContentSystem.

These guidelines keep the package consistent with the other Workes packages while leaving room to adjust exact type names during implementation.

## Root Workflow

Prefer manager-owned workflows for normal use.

The structure-assigned-ID root type is `ContentManager`. It should require an explicit structure so the active storage policy is visible at construction.

Use `KeyedContentManager<TId>` for structures where caller-provided IDs are first-class. Do not make one manager expose write methods that only work for some structures.

`ContentManagerBase` is public shared read/lookup plumbing for code that can work with already-created managers from either workflow. It is abstract and should stay small rather than becoming a catch-all capability surface.

Advanced behavior should be opt-in through options, structures, or attachments.

## Structures

Represent shared read and lookup behavior through `IContentStructure`.

Avoid baking FIFO assumptions into the whole package. FIFO is the first implementation, not the whole model.

A structure should own:

- entry retention;
- ordering;
- lookup;
- ID assignment or validation;
- mutability rules;
- capability support.

Do not force one append method into the base structure abstraction. Structure-assigned-ID structures and caller-provided-ID structures should expose their own write workflows through focused interfaces.

Concrete structures should expose natural lookup overloads for their ID model. For example, FIFO can support `Get(1)` while generic code can continue using `IContentStructure.Get(ContentEntryId)`.

ID strategies should validate and normalize typed caller-provided IDs. Built-in default strategy resolution is acceptable for explicitly supported ID types such as `string` and `long`; custom ID types require custom strategies. Do not add generation behavior to that abstraction until a concrete structure needs configurable generated IDs.

## Entries

Use content entries as the primary extension path.

Do not force all entries into a chat-message or log-message shape. Custom entries should be ordinary, supported usage.

Stored entry records should have IDs. Entry payloads should not require callers to invent IDs before a structure stores them.

Document first-class public concepts in focused guides:

- entries in `docs/CONTENT_ENTRIES.md`;
- identity in `docs/CONTENT_IDENTITY.md`;
- structures in `docs/CONTENT_STRUCTURES.md`;
- managers in `docs/CONTENT_MANAGERS.md`;
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
- persistence;
- host logging bridges;
- platform adapters;
- advanced structures;
- search/indexing;
- mutation support;
- change hooks.

The simple use case should stay small: create a manager, add entries, read entries.

## Documentation Expectations

Every first-class public concept should get a focused guide under `docs/`.

Project-control docs explain architectural intent. User-facing docs explain normal usage. Trello owns task state once the board is mapped.
