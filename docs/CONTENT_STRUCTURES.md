# Content Structures

Content structures define how entries are stored, ordered, found, retained, and removed.

## Purpose

`IContentStructure` is the shared abstraction for reading retained content records and looking them up by ID.

The structure owns the rules. The manager should provide a convenient root workflow, but the structure decides what operations are supported and what entry IDs mean.

The common abstraction exposes:

- retained `ContentEntryRecord` values in the structure's read order;
- `TryGet` lookup by `ContentEntryId`;
- expected-success `Get` lookup by `ContentEntryId`.

Append workflows are structure-specific. This lets generated-ID structures and caller-provided-ID structures expose honest APIs without forcing every structure into one add method.

Prefer a concrete structure's natural lookup overload when working with that structure directly. Use `ContentEntryId` lookup through `IContentStructure` when writing structure-agnostic code.

## First Structure

The first implementation is `BoundedFifoContentStructure`.

Expected behavior:

- entries are appended chronologically with `Add`;
- retained records are read oldest to newest;
- capacity is configurable and must be greater than zero;
- when capacity is exceeded, the oldest retained record is dropped;
- entry IDs are assigned internally as increasing decimal strings.

This covers console history, simple logs, chat scrollback, notification feeds, and other common streams.

FIFO lookup only finds retained records. A record that was dropped by capacity overflow is treated as not found.

Because FIFO IDs are sequential numbers, `BoundedFifoContentStructure` exposes numeric lookup:

```csharp
ContentEntryRecord record = fifo.Get(1);
```

The shared `ContentEntryId` lookup remains available for code that works through `IContentStructure`.

## Keyed Structure

A simple keyed content structure should be part of the MVP after the FIFO foundation.

Expected behavior:

- callers provide entry IDs when adding entries;
- the structure validates IDs through a configured ID strategy;
- retained records can be fetched by ID;
- duplicate IDs are rejected consistently.

This gives the package an early, simple structure that benefits from configurable ID strategy without making the first FIFO structure more complicated.

## Future Structures

The abstraction should leave room for other useful structures:

- unbounded in-memory sequence;
- grouped feed;
- channel-based chat history;
- threaded conversation/forum structure;
- indexed/searchable structure;
- persistent file-backed structure;
- grid-like structure for forum or board-style UIs;
- composite structures that mirror entries into more than one view.

These should not make the first FIFO implementation complicated. They should be enabled by the abstraction, not pre-implemented in the first pass.

## Capabilities

Not every structure should support every operation.

A structure can be append-only, mutable, searchable, persistent, exportable, or none of those. Capability metadata can let consumers ask what a structure supports without forcing every structure into one large interface.

This mirrors the strategy used in other Workes packages: keep the central abstraction small, then add optional capabilities where they are genuinely needed.
