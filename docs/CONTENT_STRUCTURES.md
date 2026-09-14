# Content Structures

Content structures define how entries are stored, ordered, found, retained, and removed.

## Purpose

`IContentStructure` is the planned abstraction for content storage behavior.

The structure owns the rules. The manager should provide a convenient root workflow, but the structure decides what operations are supported and what entry IDs mean.

## First Structure

The first implementation should be a bounded FIFO content structure.

Expected behavior:

- entries are appended chronologically;
- retained entries are read oldest to newest;
- capacity is configurable;
- when capacity is exceeded, the oldest retained entries are dropped;
- entry IDs are assigned by the structure.

This covers console history, simple logs, chat scrollback, notification feeds, and other common streams.

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
