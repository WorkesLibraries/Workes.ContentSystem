# Changelog

This file records notable user-facing changes to `Workes.ContentSystem`.

## Unreleased

## 0.4.0 - 2026-09-15

Added:

- Added `IContentRetentionPolicyStructure` and `IContentReadOrderStructure` as focused opt-in structure contracts.
- Added manager-owned runtime mutation for clear, remove, and generic structure parameter changes.
- Added focused mutation contracts for clear, record removal, typed keyed record removal, and parameterized structures.
- Added richer content change event metadata for change kind, clear events, configuration changes with previous/current components, and full-refresh guidance.
- Added inferred natural-ID managers for structure-assigned-ID structures through `ContentManager.For(...)`.

Documentation:

- Reframed structure capabilities as focused opt-in contracts instead of broad metadata.
- Documented manager-owned mutation and richer event semantics.

## 0.3.0 - 2026-09-15

Changed:

- Replaced `BoundedFifoContentStructure` with `ContentSequenceStructure`.
- Added sequence read-order configuration through `ContentSequenceReadOrder`.
- Added policy-owned retention through `ContentOverflowPolicy.None` and `ContentOverflowPolicy.DropOldest(capacity)`.

Documentation:

- Reconciled the 1.0 architecture roadmap and documented the planned built-in surface.
- Added roadmap and snapshot planning docs.

## 0.2.0 - 2026-09-15

Added:

- Added optional committed-change hooks through `IContentChangeSource`, `ContentChangedEventArgs`, and `ContentManagerBase.Changed`.
- Built-in FIFO and keyed structures now emit change events after successful adds.

Documentation:

- Added a focused guide for content change hooks.
- Added focused guides for content manager workflows and content identity.
- Refreshed documentation links so each major public system has a dedicated guide.
- Clarified that `ContentManager` requires explicit structure selection and `ContentManagerBase` is for shared processing of existing managers.

## 0.1.0 - 2026-09-14

Initial package release.

This release introduces the first useful ContentSystem core:

- content entries as the core extension model;
- a bounded FIFO content structure;
- keyed content structures with typed ID strategies;
- manager-owned workflows for normal and keyed use;
- a shared failure and exception model;
- focused docs and examples for normal usage.

Included:

- package-wide `ContentFailure` and content exception types for structured expected failures.
- `ContentEntryId`, `ContentEntryRecord`, `IContentEntry`, and `PlainContentEntry` as the first entry foundation.
- `IContentStructure`, `IStructureAssignedIdContentStructure`, `IKeyedContentStructure<TId>`, and `BoundedFifoContentStructure` for retained-record lookup and bounded FIFO storage.
- ID strategies and `KeyedContentStructure` for caller-provided entry IDs.
- `ContentManagerBase`, `ContentManager`, and `KeyedContentManager<TId>` for shared read/lookup behavior and workflow-specific entry adds.
