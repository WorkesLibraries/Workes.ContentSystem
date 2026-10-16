# ROADMAP

## Purpose

Record the implementation roadmap for the current stable package and planned release stages, including breaking changes.

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

Completed implementation adds exact snapshot capture and restore for built-in sequence and map structures, package-wide entry factory registration, map restore ID validation, and manager-coordinated atomic restore. Stage 16 refines the normal restore path so round-trippable structures expose their own restore factory.

### Stage 16: Add structure extension authoring support

Completed implementation makes custom structures practical to implement in the same capacity as built-ins, scoped to the extension systems that already exist. It adds a dedicated extension-author guide, structure snapshot/factory helpers, validation helpers for custom structure snapshot data, `IContentStructureSnapshotRoundTrippable`, factory-less manager restore, example custom structures with full snapshot support, and pitfalls/invariants documentation around ID ownership, ordering, atomic restore, failures, events, mutation opt-ins, and versioning.

Later stages should expand the extension guide as new extension systems land.

### Pre-17.1: Reconcile manager workflow instruction sets

Record the need for a breaking manager-workflow direction before adding more built-in structures. The original planned direction used workflow descriptors, but Pre-17.2 replaced that with direct structure-created managers after the design review found the descriptor layer too indirect.

### Pre-17.2A: Add structure-family bases and dedicated manager bases

Completed implementation adds the first manager/structure family foundation. Structures still create their tailored concrete manager through `IContentStructure.CreateManager()`, but sequence and map workflows now share structure-family bases and manager-family bases so future related structures can reduce redundancy without losing dedicated managers.

### Pre-17.2B: Reconcile snapshots with structure-family bases

Completed implementation adds sequence-family and map-family snapshot factory bases. Built-in snapshot wire shapes stay compatible, normal manager APIs remain `CaptureSnapshot()` and `RestoreSnapshot(snapshot)`, and concrete managers keep strict restore compatibility while family manager bases can accept compatible family replacements.

### Pre-17.2C: Add flexible ID strategies and generated ID sources

Completed implementation adds generated ID source contracts, a built-in long generated ID source, and generic sequence structure/manager types for custom sequence ID models while preserving the normal non-generic long-ID sequence path.

### Pre-17.3: Reconcile documentation after manager/structure redo

Completed implementation reconciles user-facing and project-control documentation after Pre-17.2A through Pre-17.2C, updates package metadata for `0.6.0`, and makes the final preferred structure, manager, snapshot, and ID paths the documented baseline before Stage 17 adds more structures.

### Pre-17.4: Add generic sequence snapshot extension parity

Completed implementation adds a generic sequence-family snapshot factory base for custom generated ID sources. This closes the extension gap where built-in generic sequences could restore custom ID source state and validate restored IDs, but custom sequence-family structures only had a long-ID numeric helper.

### Stage 17: Add simple configurable built-in structures

Completed implementation adds the next simple built-in structure set: map naming for direct ID-addressed content, sequence reject overflow, single-entry structures, and stack structures. The stage keeps the principle that configurable state is preferred over separate bounded/unbounded or replacing/rejecting types unless the workflow language truly changes.

### Stage 18A: Add compound hierarchy core

Completed implementation adds the in-memory compound hierarchy workflow: tree-owned nodes, generated and explicit root/child creation, hierarchy traversal, configurable child-removal policy, configurable sibling read direction, depth-first flattened records, manager workflow, events, docs, and tests.

### Stage 18B: Add compound snapshots and 0.7.0 release sweep

Completed implementation adds compound structure snapshot capture/restore, compound snapshot extension helpers, documentation reconciliation, and `0.7.0` metadata for the Stage 17 plus Stage 18 built-in expansion.

### Stage 19: Add additional built-in ID strategies

Completed implementation adds the remaining selected low-assumption ID support: GUID ID strategy, `ContentEntryId` identity strategy, GUID generated ID source, default map strategy resolution for `Guid` and `ContentEntryId`, and docs/tests for the narrow built-in ID set.

### Stage 20: Add convenience methods for all implemented structures

Completed implementation adds ergonomic helper APIs across the implemented structures. Base manager helpers stay read-only and universal; mutation and navigation helpers remain on structure-specific managers where semantics differ.

### Stage 21: Add validation and preflight APIs

Completed implementation adds `ContentPreflightResult`, advisory `Assess...` APIs for mutation workflows, generated-ID source preflight methods, and snapshot preflight on `ContentManagerBase`. Preflight does not mutate retained state, advance generated ID source state, replace structures, or emit events; final commit still revalidates.

## Pre-1.0 Release Work

### Stage 24: Add optional structure sorting support — complete

Add the stable, materialized read-only `ContentManagerBase.GetSortedRecords` view to every manager and opt-in, structure-owned persistent sorting for maps and compound sibling groups before the 1.0 API is frozen. Compound sorting supports one comparer for all groups, comparer selection by parent ID, and a targeted roots-or-parent-children operation. Only structures whose semantics allow reordering should implement persistent sorting. Keep it manager-coordinated, atomic, observable through one coherent change event, practical for extension authors through focused contracts, and compatible with exact snapshot capture/restore.

### Stage 27: Prepare 1.0.0 release candidate — released

Released as 1.0.0 after the package-wide readiness sweep: public APIs, XML docs, metadata, changelog, compatibility notes, build, tests, and package contents were reviewed; the guides were reconciled; and the extension-authoring guide was rebuilt as a practical reference. The release established the supported public API line.

## Post-1.0 Minor Releases

The planned feature sequence after 1.0.0 is:

### Stage 22 / 1.1.0: Add manager read-query helpers

Completed in 1.1.0: added shared manager filtering and composable filtered/sorted read views over `ContentEntryRecord` snapshots. Every built-in manager inherits the common query surface, which preserves its structure's read order by default. Query operations are eager, materialized, read-only, and separate from persistent structure sorting. The generic query does not infer compound hierarchy metadata or provide indexing/search semantics.

### Stage 28 / 1.2.0: Add atomic bulk operations and mutation helpers

Released in 1.2.0: family-native batches include sequence `AddRange`, map `AddRange` / `SetRange`, stack `PushRange`, and compound `AddRoots` / `AddChildren`, with `RemoveWhere` across the applicable families. Inputs are materialized once and staged before commit; rejected operations preserve retained state and generated-ID state. A committed batch emits one coherent full-refresh event. Compound predicate removal follows its configured child-removal policy. New structure behavior is opt-in through focused contracts so existing custom structure implementations remain compatible.

Release checks completed: focused guides and extension recipes were reconciled; API, event, snapshot, and rejection tests were added; compatibility and changelog notes were updated; Release project builds and tests passed; and the 1.2.0 package contents were inspected.

### Stage 29 / 1.3.0: Add export helpers and attachment abstractions

Released in 1.3.0: optional manager-level text exporters and entry-owned attachment references. Built-in plain-text and Markdown exporters handle `PlainContentEntry`; custom exporters can handle application entries and formats. Attachment references carry opaque host-defined locators and descriptive metadata, while applications own the referenced bytes and their lifecycle.

### Stage 30 / 2.0.0: Make Plain Text Entry-Specific

Prepare a 2.0.0 release that removes plain text from the shared entry and record contracts while retaining timestamps as shared metadata. Preserve `PlainContentEntry` text and its snapshot format, and document that generic filters and exporters must handle application-owned entry types explicitly.

## Deferred Work

The following ideas remain deferred unless separately prioritized:

- broader chat and feed examples;
- platform adapter packages;
- rebuilding ConsoleSystem on top of ContentSystem.
