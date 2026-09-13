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

### D-003: Every Entry Has A Structure-Owned ID

#### Context

Different structures may need different identity models. A FIFO structure can use a simple increasing value, while persistent or distributed structures may need durable IDs.

#### Decision

Every entry has an ID, but each structure defines what that ID means and how it is assigned.

#### Reasoning

This preserves a common way to reference entries while avoiding a one-size-fits-all identity model.

#### Consequences

The first implementation must be careful not to make FIFO indexing the permanent package-wide ID concept.

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