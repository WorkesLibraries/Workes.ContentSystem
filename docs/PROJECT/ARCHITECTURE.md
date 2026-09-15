# ARCHITECTURE

## Purpose

Describe how the project is intended to be structured internally and how the main systems relate to each other.

This is a project-control document for maintaining architectural consistency. It is not intended to replace focused user documentation under `docs/`.

## Current Architecture

Workes.ContentSystem now has its first useful core: public entry, failure, structure, manager, and change-hook pieces are implemented. This document records both the current architecture and the intended 1.0 direction.

The package should be engine-neutral and centered on manager workflows that own one active content structure and expose simple APIs for normal users.

## Main Concepts

- `IContentEntry` is the core content payload abstraction.
- `ContentEntryRecord` pairs a stored entry with the active structure's ID.
- `ContentEntryId` is the shared stored-record identity representation.
- `IContentEntryIdStrategy<TId>` validates and normalizes caller-provided IDs for keyed structures.
- `IContentStructure` is the read/lookup storage abstraction.
- `IContentChangeSource` is the optional committed-change notification abstraction.
- `IContentRetentionPolicyStructure` is the optional retention-policy inspection contract.
- `IContentReadOrderStructure` is the optional read-order inspection contract.
- `IContentNaturalIdStructure<TId>` is the optional natural retained-record ID lookup contract.
- `IContentNaturalIdRemovalStructure<TId>` is the optional natural retained-record ID removal contract.
- `IContentClearableStructure` is the optional clear mutation contract.
- `IContentRecordRemovalStructure` is the optional record removal contract.
- `IKeyedContentRecordRemovalStructure<TId>` is the optional typed keyed record removal contract.
- `IParameterizedContentStructure` is the optional runtime structure-parameter contract.
- `ContentManagerBase` is the shared manager read/lookup base.
- `ContentManager` is the manager for structure-assigned-ID workflows.
- `ContentManager<TId>` is the typed manager for structure-assigned-ID workflows with a natural retained-record ID type.
- `KeyedContentManager<TId>` is the manager for caller-provided typed-ID workflows.
- The first structure is a configurable sequence structure.
- `KeyedContentStructure<TId>` provides configurable typed-ID validation for caller-keyed records.
- A shared failure model should represent expected content-system rejection.
- Portable snapshots are the planned serialization foundation.
- Optional attachments should support export, bridges, and platform adapters without making those features mandatory.

## Intended Data Flow

The normal in-memory flow should be:

```text
host application
-> ContentManager or KeyedContentManager<TId>
-> IContentStructure
-> retained ContentEntryRecord values
-> host UI, exporter, bridge, or adapter
```

When a structure implements `IContentChangeSource`, mutations can also notify observers synchronously after commit. Managers forward those structure events through `ContentManagerBase.Changed`.

The manager should be the convenient root. The structure should own ordering, retention, lookup, ID assignment or validation, mutability rules, and supported opt-in contracts. Shared runtime mutation is manager-owned, with managers delegating only when the active structure implements the relevant focused contract.

## Structures

The structure abstraction should be close in spirit to the InventorySystem structure model: core behavior belongs behind an abstraction so new storage models can be introduced without changing the manager into a one-purpose container.

The current sequence implementation should stay small and useful:

- append entries;
- retain all records or drop oldest through explicit overflow policy;
- read retained records oldest-first or newest-first;
- assign structure-owned IDs.

Write workflows remain structure-specific. The sequence structure exposes structure-assigned-ID add, while keyed structures require caller-provided IDs.

Managers mirror this split. `ContentManager` accepts any explicitly provided `IStructureAssignedIdContentStructure`. `ContentManager.For(...)` infers `ContentManager<TId>` for structures such as `ContentSequenceStructure` where the structure owns the one correct natural ID type. `KeyedContentManager<TId>` accepts any `IKeyedContentStructure<TId>` or uses built-in default ID strategy resolution to create a keyed structure for supported ID types. Shared read and lookup behavior belongs on `ContentManagerBase`.

Change hooks are optional structure contracts. Built-in mutable structures implement `IContentChangeSource`; custom structures can opt in without changing the base `IContentStructure` contract.

Retention policy and read order are optional structure contracts. `ContentSequenceStructure` implements `IContentRetentionPolicyStructure` and `IContentReadOrderStructure`; custom structures can implement either contract when those concepts are meaningful.

Clear, removal, and parameterized structure mutation are optional structure contracts coordinated through managers. `ContentSequenceStructure` supports all three and exposes `overflowPolicy` as a stable runtime parameter. The built-in keyed structure supports clear, normalized removal, and typed keyed removal.

FIFO-style history is a sequence plus `ContentOverflowPolicy.DropOldest(capacity)`, not a separate type. Additional retention or placement policies can be added when a later stage needs them.

Future structures may be bounded keyed, grouped, threaded, indexed, snapshot-aware, channel-based, or grid-like. Grouped and threaded structures should wait until focused structure contracts, mutation, and snapshot contracts are stable.

## Entries

Entries should be extensible content payloads. `PlainContentEntry` is the only planned 1.0 built-in entry type. Host applications should define entries for their own richer domains.

ContentSystem should not include a built-in user/role model. If a host needs users, authors, permissions, channels, moderation data, or ownership, it can represent those through custom entries, custom structures, or higher-level packages.

## Failure Model

The package should mirror the error style used in Workes.InventorySystem and Workes.ConsoleSystem:

- expected operation rejection is structured failure data;
- try APIs return failure values;
- expected-success APIs throw package-owned exceptions carrying the same failure;
- programmer misuse uses standard .NET exceptions.

## Attachments

Portable snapshots are planned as the core serialization foundation. Snapshot capture should produce serializer-friendly objects; applications choose how to serialize and store them.

Attachments are planned optional capabilities around the core model.

Examples include file export, append-only logging, host logging bridges, snapshot exporters, Unity adapters, Godot adapters, and .NET logging adapters.

Core should make these possible without requiring every user or every structure to configure them.

## Relationship To ConsoleSystem

Workes.ConsoleSystem is expected to be replaced or rebuilt later on top of Workes.ContentSystem.

Console history, logs, command input, command output, command failures, semantic text, and custom console entries map naturally to the content-entry model. ContentSystem should therefore focus on the reusable content foundation and leave console-specific commands, parsing, permissions, autocomplete, and help generation to ConsoleSystem.
