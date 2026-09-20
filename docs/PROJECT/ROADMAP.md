# ROADMAP

## Purpose

Record the implementation roadmap that connects the current prerelease package to the intended 1.0.0 package.

Trello remains the task-state source of truth. This document records the architectural sequence so project docs and future implementation stay aligned.

## 1.0 Roadmap

### Stage 10: Refactor bounded structure configuration

Reframe the FIFO-specific bounded structure into a configurable content sequence. Completed implementation uses `ContentSequenceStructure` with explicit `ContentOverflowPolicy.None` or `ContentOverflowPolicy.DropOldest(capacity)`.

### Stage 11: Add structure capability contracts

Define focused opt-in contracts for structure behavior beyond the base `IContentStructure` contract. Avoid a separate capability metadata object unless a later concrete need appears.

### Stage 12: Add manager-coordinated runtime mutation and richer change events

Add manager-coordinated mutation workflows, with structures owning retained-state changes through focused mutation contracts. Successful mutations should emit coherent events; rejected mutations should be atomic and quiet.

### Stage 13: Add entry snapshot round-trip contracts

Define entry-level snapshot support for portable entry payloads. Completed implementation uses Inventory-style snapshot value DTOs, `IContentEntrySnapshotSerializable`, explicit restore factories, and `PlainContentEntry` round trips.

### Stage 14: Add record and structure snapshot DTOs

Define serializer-friendly DTOs for stored records and whole structures without adding disk I/O or serializer ownership to core. Completed implementation adds `ContentRecordSnapshot` and `ContentStructureSnapshot`; Stage 15 builds capture and restore on top of these DTOs.

### Stage 15: Add snapshot capture and restore for built-in structures

Completed implementation adds exact snapshot capture and restore for built-in sequence and keyed structures, package-wide entry factory registration, keyed restore ID validation, and manager-coordinated atomic restore. Stage 16 refines the normal restore path so round-trippable structures expose their own restore factory.

### Stage 16: Add structure extension authoring support

Completed implementation makes custom structures practical to implement in the same capacity as built-ins, scoped to the extension systems that already exist. It adds a dedicated extension-author guide, structure snapshot/factory helpers, validation helpers for custom structure snapshot data, `IContentStructureSnapshotRoundTrippable`, factory-less manager restore, example custom structures with full snapshot support, and pitfalls/invariants documentation around ID ownership, ordering, atomic restore, failures, events, mutation opt-ins, and versioning.

Later stages should expand the extension guide as new extension systems land.

### Pre-17.1: Reconcile manager workflow instruction sets

Record the need for a breaking manager-workflow direction before adding more built-in structures. The original planned direction used workflow descriptors, but Pre-17.2 replaced that with direct structure-created managers after the design review found the descriptor layer too indirect.

### Pre-17.2A: Add structure-family bases and dedicated manager bases

Completed implementation adds the first manager/structure family foundation. Structures still create their tailored concrete manager through `IContentStructure.CreateManager()`, but sequence and keyed workflows now share structure-family bases and manager-family bases so future related structures can reduce redundancy without losing dedicated managers.

### Pre-17.2B: Reconcile snapshots with structure-family bases

Completed implementation adds sequence-family and keyed-family snapshot factory bases. Built-in snapshot wire shapes stay compatible, normal manager APIs remain `CaptureSnapshot()` and `RestoreSnapshot(snapshot)`, and concrete managers keep strict restore compatibility while family manager bases can accept compatible family replacements.

### Pre-17.2C: Add flexible ID strategies and generated ID sources

Implement the flexible ID direction: natural defaults for normal users, optional custom typed ID strategy/source support for advanced users, and generated-ID behavior owned by ID strategies/sources where families or concrete structures opt into automatic IDs.

### Pre-17.3: Reconcile documentation after manager/structure redo

Sweep all user-facing and project-control documentation after Pre-17.2A through Pre-17.2C are complete. Update examples to use the final preferred structure, manager, snapshot, and ID paths before Stage 17 adds more structures.

### Stage 17: Add selected remaining built-in structures

Add selected built-in structures after configuration, opt-in contracts, mutation, snapshots, and manager workflow resolution are stable.

### Stage 18: Add additional built-in ID strategies

Add selected low-assumption ID strategies after core structure and snapshot contracts are stable.

### Stage 19: Add validation and preflight APIs

Add preflight APIs for mutation and snapshot workflows. Preflight should not mutate state or emit events, and final commit should still revalidate.

### Stage 20: Add manager read-query helpers

Add manager-side helpers for materialized filtered and sorted record views without mutating the active structure.

### Stage 21: Add bulk operations and mutation helper APIs

Add high-value helper operations such as range workflows and predicate-based removal while preserving atomicity and event semantics.

### Stage 22: Add optional structure sorting support

Add opt-in structure-owned sorting only for structures where reordering retained records is meaningful.

### Stage 23: Add export helpers and attachment abstractions

Add optional export helpers and attachment abstractions after portable snapshots exist. Export and attachments should not become the serialization foundation.

### Stage 24: Evaluate threaded/forum-like content structure

Decide whether threaded content belongs in core, needs custom managers, or should be deferred to examples or companion packages.

### Stage 25: Add optional grouped content structure

Add grouped content if the structure-owned content model and manager workflow remain clean and domain-neutral.

### Stage 26: Add example tests and usage docs

Add examples and focused usage docs for the implemented 1.0 feature set.

### Stage 27: Prepare 1.0.0 release

Audit API names, docs, examples, XML docs, metadata, changelog, compatibility notes, package build, release branch, and tag.

## Deferred Work

The following work remains future or post-1.0 unless promoted by a later decision:

- broader chat and feed examples;
- platform adapter packages;
- rebuilding ConsoleSystem on top of ContentSystem.
