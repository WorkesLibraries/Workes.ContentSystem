# AI CONTEXT

## Purpose

Give AI assistants the minimum project context needed before working in this repository.

## Current State

Workes.ContentSystem is a newly seeded package project. It has design foundation docs, but implementation has not started.

The package is intended to become a reusable content-entry backend for console history, chat, logs, feeds, forums, notifications, and custom UI streams.

## Current Direction

The planned core concepts are:

- `ContentManager` as the likely root object;
- `IContentEntry` as the core entry abstraction;
- `IContentStructure` as the storage and organization abstraction;
- bounded FIFO as the first concrete structure;
- a shared `ContentFailure` model matching the other Workes packages;
- optional attachments for export, persistence, bridges, and platform adapters.

## Important Boundaries

Do not start implementation before reading:

1. `docs/CONCEPTS.md`
2. `docs/PROJECT/ARCHITECTURE.md`
3. `docs/PROJECT/DECISIONS.md`
4. `docs/PROJECT/API_GUIDELINES.md`

Do not add a built-in user or role system to core. Hosts can model users with custom entries, custom structures, or higher-level packages.

Do not make export, persistence, bridges, or platform adapters mandatory core setup.

Trello owns task state once `docs/PROJECT/TRELLO_WORKFLOW.md` says the board is ready.