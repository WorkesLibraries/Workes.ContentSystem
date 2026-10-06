# ARCHITECTURE

## Purpose

Describe how the project is intended to be structured internally and how the main systems relate to each other.

This is a project-control document for maintaining architectural consistency. It is not intended to replace focused user documentation under `docs/`.

## Current Architecture

Workes.ContentSystem now has its first useful core: public entry, failure, structure, manager, and change-hook pieces are implemented. This document records both the current architecture and the intended 1.0 direction.

The package should be engine-neutral and centered on manager workflows that own one active content structure and expose simple APIs for normal users.

## Source Organization

The source tree is split by subsystem, matching the style used by sibling Workes packages.

`Core` is reserved for package-wide primitives and root coordination: failures, exceptions, `ContentManagerBase`, and manager resolution. Subsystem folders own their contracts, bases, DTOs, and helper APIs: `Entries`, `Identity`, `Structures`, `Managers`, `Changes`, `Snapshots`, and `Parameters`.

Concrete package-provided implementations live in `BuiltIn` folders under the subsystem they implement. For example, `PlainContentEntry` is under `Entries/BuiltIn`, built-in ID strategies are under `Identity/BuiltIn`, and built-in structures/managers are under `Structures/BuiltIn` and `Managers/BuiltIn`.

This is a physical organization rule, not a namespace rule. Public namespaces remain stable unless a later explicit prerelease namespace cleanup decision changes them.

## Main Concepts

- `IContentEntry` is the core content payload abstraction.
- `IContentEntrySnapshotSerializable` is the optional entry payload snapshot capture contract.
- `IContentEntrySnapshotFactory` restores entries from explicit entry snapshot factories.
- `ContentEntryRecord` pairs a stored entry with the active structure's ID.
- `ContentEntryId` is the shared stored-record identity representation.
- `IContentEntryIdStrategy<TId>` validates and normalizes caller-provided IDs for map structures and validates normalized IDs restored from snapshots.
- `IContentStructure` is the read/lookup storage abstraction and creates the structure's tailored manager.
- `ContentSequenceStructureBase<TId>`, `ContentMapStructureBase<TId>`, `ContentSingleStructureBase<TId>`, `ContentStackStructureBase<TId>`, and `ContentCompoundStructureBase<TId>` are structure-family bases for reusable workflow surfaces.
- `IContentChangeSource` is the optional committed-change notification abstraction.
- `IContentRetentionPolicyStructure` is the optional retention-policy inspection contract.
- `IContentReadOrderStructure` is the optional read-order inspection contract.
- `IContentNaturalIdStructure<TId>` is the optional natural retained-record ID lookup contract.
- `IContentNaturalIdRemovalStructure<TId>` is the optional natural retained-record ID removal contract.
- `IContentClearableStructure` is the optional clear mutation contract.
- `IContentRecordRemovalStructure` is the optional record removal contract.
- `IContentMapRecordRemovalStructure<TId>` is the optional typed map record removal contract.
- `IParameterizedContentStructure` is the optional runtime structure-parameter contract.
- `ContentManagerBase` is the shared manager read/lookup and snapshot lifecycle base.
- `ContentSequenceManagerBase<TId>`, `ContentMapManagerBase<TId>`, `ContentSingleManagerBase<TId>`, `ContentStackManagerBase<TId>`, and `ContentCompoundManagerBase<TId>` are manager-family bases for shared workflow behavior.
- `ContentSequenceManager` is the tailored manager for `ContentSequenceStructure`.
- `ContentMapManager<TId>` is the manager for caller-provided typed-ID workflows.
- `ContentSingleManager` is the normal long-ID manager for single-entry state.
- `ContentStackManager` is the normal long-ID manager for stack workflows.
- `ContentCompoundManager` is the normal long-ID manager for owned-tree workflows.
- `ContentManagers.ForStructure(...)` is the preferred structure-driven manager resolver.
- `ContentSequenceStructure` provides configurable sequence retention and generated long IDs.
- `ContentMapStructure<TId>` provides configurable typed-ID validation for direct ID-addressed records.
- `ContentSingleStructure` provides configurable single-entry replacement behavior.
- `ContentStackStructure` provides configurable stack read order and reject-capacity behavior.
- `ContentCompoundStructure` provides tree-shaped content with configurable child-removal and sibling-read behavior.
- A shared failure model should represent expected content-system rejection.
- Entry snapshots are implemented. Record and structure snapshot DTOs are implemented. Built-in sequence, map, single, stack, and compound structures support exact snapshot capture and normal manager-coordinated restore through their round-trippable `SnapshotFactory`.
- Optional attachments should support export, bridges, and platform adapters without making those features mandatory.

## Intended Data Flow

The normal in-memory flow should be:

```text
host application
-> ContentSequenceManager, ContentMapManager<TId>, ContentSingleManager, ContentStackManager, ContentCompoundManager, or a custom manager
-> IContentStructure
-> retained ContentEntryRecord values
-> host UI, exporter, bridge, or adapter
```

When a structure implements `IContentChangeSource`, mutations can also notify observers synchronously after commit. Managers forward those structure events through `ContentManagerBase.Changed`.

The manager should be the convenient root. The structure should own retained content state: ordering, retention, lookup, ID assignment or validation, mutability rules, indexes, nesting, and supported opt-in contracts. Runtime mutation is manager-coordinated for normal callers, with managers delegating only when the active structure implements the relevant focused contract.

Manager resolution is direct. A structure creates the manager that knows its natural workflow, while focused contracts continue to answer which optional operations the structure supports.

## Structures

The structure abstraction follows the same extension discipline as InventorySystem, but not the same ownership split. InventorySystem inventories own item instances while layouts place them. ContentSystem structures own retained content state because future structures may be sequences, maps, stacks, grouped feeds, or threaded/forum-like graphs rather than simple placements over one flat store.

The current sequence implementation should stay small and useful:

- append entries;
- retain all records, drop oldest, or reject when full through explicit overflow policy;
- read retained records oldest-first or newest-first;
- assign structure-owned IDs.

Write workflows remain structure-specific. The sequence structure exposes append, map structures expose direct ID-addressed add/set, single-entry structures expose set/current-record operations, and stack structures expose push/peek/pop.

Managers mirror this split through structure-owned manager creation. `ContentSequenceStructure` creates `ContentSequenceManager`, `ContentMapStructure<TId>` creates `ContentMapManager<TId>`, `ContentSingleStructure` creates `ContentSingleManager`, `ContentStackStructure` creates `ContentStackManager`, `ContentCompoundStructure` creates `ContentCompoundManager`, and custom structures can return custom managers. Direct constructors remain explicit/manual paths where they fit cleanly. Shared read, lookup, event forwarding, and snapshot lifecycle behavior belongs on `ContentManagerBase`.

Reusable workflow families can introduce family bases to reduce duplication. The current sequence, map, single-entry, stack, and compound families have both structure-family bases and manager-family bases, while their concrete structures still resolve to dedicated concrete managers.

Change hooks are optional structure contracts. Built-in mutable structures implement `IContentChangeSource`; custom structures can opt in without changing the base `IContentStructure` contract.

Retention policy and read order are optional structure contracts. `ContentSequenceStructure` implements `IContentRetentionPolicyStructure` and `IContentReadOrderStructure`; custom structures can implement either contract when those concepts are meaningful.

Clear, removal, and parameterized structure mutation are optional structure contracts coordinated through managers. The manager does not clear a manager-owned record store; it asks the structure to perform the mutation according to that structure's own model. `ContentSequenceStructure` supports all three and exposes `overflowPolicy` as a stable runtime parameter. The built-in map, single, stack, and compound structures support clear and natural removals where the workflow makes sense.

FIFO-style history is a sequence plus `ContentOverflowPolicy.DropOldest(capacity)`, not a separate type. Reject-at-capacity behavior is `ContentOverflowPolicy.Reject(capacity)`. The package should keep preferring configurable structure state over separate bounded/unbounded structures until a workflow truly needs different vocabulary.

Future structures may be grouped, threaded, indexed, snapshot-aware, channel-based, or grid-like.

## Entries

Entries should be extensible content payloads. `PlainContentEntry` is the only planned 1.0 built-in entry type. Host applications should define entries for their own richer domains.

ContentSystem should not include a built-in user/role model. If a host needs users, authors, permissions, channels, moderation data, or ownership, it can represent those through custom entries, custom structures, or higher-level packages.

Entry snapshot capture is opt-in through `IContentEntrySnapshotSerializable`. Restore uses an `IContentEntrySnapshotFactory`, such as `PlainContentEntry.Factory`. Structure restore resolves entry factories through `ContentEntrySnapshotFactories`, a package-owned static registry seeded with built-ins and extended by applications during setup.

The entry factory registry is still needed even though entries own their capture behavior: after a structure snapshot has been serialized and loaded, there is no live entry instance to ask for a factory. The loaded snapshot only carries the entry kind string, so restore needs a package-level map from kind to factory.

Structure snapshot round trips are opt-in through `IContentStructureSnapshotRoundTrippable`. Normal manager restore uses the active structure's `SnapshotFactory`; explicit structure factories remain available for migrations and advanced restore targets.

Structure extension authoring is supported through focused contracts plus helper APIs. `ContentStructureSnapshotFactoryBase<TStructure>` handles common restore validation, `ContentSequenceStructureSnapshotFactoryBase<TId, TStructure>` handles generic sequence restore with generated ID sources, `ContentSequenceStructureSnapshotFactoryBase<TStructure>` handles the long-ID numeric convenience path, `ContentMapStructureSnapshotFactoryBase<TId, TStructure>` handles map restore invariants, `ContentCompoundStructureSnapshotFactoryBase<TId, TStructure>` handles compound record/source/relationship restore invariants, and `ContentSnapshotRecords` / `ContentSnapshotProperties` expose the same retained-record and structure-data helper patterns used by built-in structures.

## Failure Model

The package should mirror the error style used in Workes.InventorySystem and Workes.ConsoleSystem:

- expected operation rejection is structured failure data;
- try APIs return failure values;
- expected-success APIs throw package-owned exceptions carrying the same failure;
- programmer misuse uses standard .NET exceptions.

## Attachments

Portable snapshots are the core serialization foundation. Entry snapshots are implemented with serializer-friendly value DTOs; record and structure snapshots preserve retained IDs, retained entries, and structure-owned state for built-in structures. Applications choose how to serialize and store snapshot objects.

Attachments are planned optional capabilities around the core model.

Examples include file export, append-only logging, host logging bridges, snapshot exporters, Unity adapters, Godot adapters, and .NET logging adapters.

Core should make these possible without requiring every user or every structure to configure them.

## Relationship To ConsoleSystem

Workes.ConsoleSystem is expected to be replaced or rebuilt later on top of Workes.ContentSystem.

Console history, logs, command input, command output, command failures, semantic text, and custom console entries map naturally to the content-entry model. ContentSystem should therefore focus on the reusable content foundation and leave console-specific commands, parsing, permissions, autocomplete, and help generation to ConsoleSystem.
