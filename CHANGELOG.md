# Changelog

This file records notable user-facing changes to `Workes.ContentSystem`.

## 0.1.0 - Planned

Initial package release target.

Planned first release shape:

- content entries as the core extension model;
- a bounded FIFO content structure;
- a manager-owned workflow for normal use;
- a shared failure and exception model;
- focused docs and examples for normal usage.

Added during foundation work:

- package-wide `ContentFailure` and content exception types for structured expected failures.
- `ContentEntryId`, `ContentEntryRecord`, `IContentEntry`, and `PlainContentEntry` as the first entry foundation.
- `IContentStructure` and `BoundedFifoContentStructure` for retained-record lookup and bounded FIFO storage.
- ID strategies and `KeyedContentStructure` for caller-provided entry IDs.
- `ContentManagerBase`, `ContentManager`, and `KeyedContentManager<TId>` for shared read/lookup behavior and workflow-specific entry adds.
