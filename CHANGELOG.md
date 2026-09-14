# Changelog

This file records notable user-facing changes to `Workes.ContentSystem`.

## Unreleased

No unreleased changes yet.

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
