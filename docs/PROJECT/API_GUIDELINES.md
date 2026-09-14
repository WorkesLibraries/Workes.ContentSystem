# API GUIDELINES

## Purpose

Record API style guidance for Workes.ContentSystem.

These guidelines keep the package consistent with the other Workes packages while leaving room to adjust exact type names during implementation.

## Root Workflow

Prefer a manager-owned workflow for normal use.

The likely root type is `ContentManager`. It should have a simple default constructor and an options-based constructor once options are needed. Passing no options should create the normal useful default.

Advanced behavior should be opt-in through options, structures, or attachments.

## Structures

Represent storage behavior through an abstraction, likely `IContentStructure`.

Avoid baking FIFO assumptions into the whole package. FIFO is the first implementation, not the whole model.

A structure should own:

- entry retention;
- ordering;
- lookup;
- ID assignment or validation;
- mutability rules;
- capability support.

## Entries

Use content entries as the primary extension path.

Do not force all entries into a chat-message or log-message shape. Custom entries should be ordinary, supported usage.

Stored entry records should have IDs. Entry payloads should not require callers to invent IDs before a structure stores them.

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
