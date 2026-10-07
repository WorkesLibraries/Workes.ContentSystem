# Changelog

This file records notable user-facing changes to `Workes.ContentSystem`.

## 0.8.0 - 07-10-2026

Added:

- Added read-only manager conveniences: `Count`, `IsEmpty`, and normalized-ID `Contains`.
- Added typed `Contains` helpers across sequence, map, single, stack, and compound manager families.
- Added map `GetOrSet` / `TryGetOrSet` helpers for get-or-create workflows.
- Added sequence `GetFirst` / `TryGetFirst` and `GetLast` / `TryGetLast` helpers over configured read order.
- Added single `HasCurrent` and stack `CanPeek` / `CanPop` helpers.
- Added compound parent-node child creation overloads, parent/child node traversal helpers, and `ContentCompoundRecordView` hierarchy readouts.
- Added `ContentPreflightResult` and advisory `Assess...` APIs for manager/structure mutation workflows.
- Added generated-ID source preflight methods so generated IDs and observed manual IDs can be assessed without advancing source state.
- Added manager-level snapshot preflight through `AssessCaptureSnapshot()` and `AssessRestoreSnapshot(...)`.

Changed:

- Custom generated ID sources must now implement non-mutating `CanCreateNext`, `CanObserve`, and `CanObserveNormalized` methods.

## 0.7.1 - 07-10-2026

Added:

- Added `GuidContentEntryIdStrategy` and default map strategy resolution for `Guid`.
- Added `ContentEntryIdContentEntryIdStrategy` and default map strategy resolution for `ContentEntryId`.
- Added `GuidContentGeneratedIdSource` for generic generated-ID structures.

## 0.7.0 - 07-10-2026

Added:

- Added `ContentOverflowPolicy.Reject(capacity)` and `ContentFailureCodes.StructureCapacityReached`.
- Added `ContentSingleStructure<TId>` / `ContentSingleManager<TId>` plus long-ID convenience types for single-entry state.
- Added `ContentStackStructure<TId>` / `ContentStackManager<TId>` plus long-ID convenience types for last-in-first-out workflows.
- Added `ContentCompoundStructure<TId>` / `ContentCompoundManager<TId>` plus long-ID convenience types for in-memory owned-tree content.
- Added compound structure snapshot capture and restore, including hierarchy relationships, generated ID source state, child-removal policy, and sibling read order.
- Added `ContentCompoundStructureSnapshotFactoryBase<TId, TStructure>` for custom compound-family snapshot restore helpers.
- Added `ContentChangeKind.Replaced` for coherent replacement events.

Changed:

- Renamed and reframed keyed built-ins as map built-ins: `ContentMapStructure<TId>`, `ContentMapManager<TId>`, map family bases, map contracts, and map snapshot helpers.
- Changed the direct ID-addressed structure snapshot kind to `workes.content.structure.map`.
- Sequence add now supports reject-at-capacity retention in addition to unbounded and drop-oldest retention.
- Compound structures support generated and explicit node IDs, root/child creation, hierarchy traversal, configurable child-removal policy, configurable sibling read direction, and depth-first flattened records.
- Compound managers now support the same manager-level `CaptureSnapshot()` and factory-less `RestoreSnapshot(snapshot)` path as the other built-in round-trippable structures.

## 0.6.0 - 21-09-2026

Added:

- Added generated ID source contracts and a built-in `LongContentGeneratedIdSource`.
- Added generic `ContentSequenceStructure<TId>` and `ContentSequenceManager<TId>` for custom sequence ID models.
- Added explicit-ID add support for sequence structures while preserving id-less generated-ID adds.
- Added `ContentSequenceStructureSnapshotFactoryBase<TId, TStructure>` for generic sequence-family snapshot restore with custom generated ID sources.
- Reorganized source files by subsystem with `BuiltIn` folders for concrete package-provided implementations.

Changed:

- `ContentSequenceStructure` and `ContentSequenceManager` remain the normal long-ID sequence path over the new generic sequence implementation.
- Sequence snapshots now preserve generated ID source state, expose reusable generic generated-ID source restore helpers for extensions, and keep legacy long sequence restore compatibility.

## 0.5.3 - 20-09-2026

Added:

- Added `ContentSequenceStructureSnapshotFactoryBase<TStructure>` for sequence-family snapshot restore helpers.
- Added `ContentMapStructureSnapshotFactoryBase<TId, TStructure>` for map-family snapshot restore helpers with normalized ID validation.

Changed:

- Refactored built-in sequence and map snapshot factories to use the family snapshot factory bases without changing the snapshot DTO wire shape.
- `ContentSequenceManagerBase` and `ContentMapManagerBase<TId>` now accept compatible family replacement structures by default, while concrete managers keep narrow concrete restore compatibility.

## 0.5.2 - 20-09-2026

Added:

- Added `IContentStructure.CreateManager()` so each structure can create its tailored manager.
- Added `ContentManagers.ForStructure(...)` and typed manager resolution over structure-created managers.
- Added `ContentSequenceManager` as the tailored manager for `ContentSequenceStructure`.
- Added `ContentSequenceStructureBase` and `ContentMapStructureBase<TId>` as structure-family bases.
- Added `ContentSequenceManagerBase` and `ContentMapManagerBase<TId>` as manager-family bases.
- Added `ManagerMismatch` structured failures for typed manager resolution mismatches.

Changed:

- `IContentStructure` now requires manager creation instead of a workflow descriptor.
- Replaced broad `ContentManager` / `ContentManager<TId>` usage with tailored managers.
- Removed workflow descriptors and package-owned manager factory registration.
- Moved clear, remove, and structure-parameter mutation off `ContentManagerBase` and onto tailored managers.
- `ContentSequenceStructure` and `ContentMapStructure<TId>` now inherit family bases while still resolving to dedicated concrete managers.
- Updated docs to make structure-created manager resolution the preferred construction path.
- Documented that ContentSystem structures own retained content state, while managers coordinate workflows over the active structure.

## 0.5.1 - 20-09-2026

Documentation:

- Clarified that normal structure snapshot restore uses the active round-trippable structure's `SnapshotFactory`.
- Added a serializer save/load example for manager-coordinated structure snapshots.
- Clarified why custom entry factories still need package-wide registration before restoring serialized structure snapshots.

## 0.5.0 - 20-09-2026

Added:

- Added structure extension-authoring helpers for snapshot factories, snapshot properties, and retained record snapshot capture/restore.
- Added structure extension-authoring documentation focused on custom structures.
- Added `IContentStructureSnapshotRoundTrippable` so snapshot-capable structures expose the factory needed for normal restore.
- Added factory-less manager restore overloads that use the active structure's snapshot factory.

## 0.4.3 - 19-09-2026

Added:

- Added structure snapshot capture and restore contracts, helpers, and package-wide entry factory registration.
- Added built-in structure snapshot round trips for `ContentSequenceStructure` and `ContentMapStructure<TId>`.
- Added manager-coordinated atomic structure snapshot restore and `ContentChangeKind.SnapshotRestored`.
- Added snapshot failures for unsupported structures and missing restore factories.
- Added package-wide entry snapshot factory registration and map snapshot restore validation through ID strategies.

## 0.4.2 - 18-09-2026

Added:

- Added record and structure snapshot DTOs for retained IDs, entry snapshots, retained records, and structure-owned snapshot state.

## 0.4.1 - 18-09-2026

Added:

- Added entry snapshot round-trip contracts, snapshot value DTOs, built-in scalar snapshot codecs, and `PlainContentEntry` snapshot support.

## 0.4.0 - 15-09-2026

Added:

- Added `IContentRetentionPolicyStructure` and `IContentReadOrderStructure` as focused opt-in structure contracts.
- Added manager-coordinated runtime mutation for clear, remove, and generic structure parameter changes.
- Added focused mutation contracts for clear, record removal, typed map record removal, and parameterized structures.
- Added richer content change event metadata for change kind, clear events, configuration changes with previous/current components, and full-refresh guidance.
- Added inferred natural-ID managers for structure-assigned-ID structures.

Documentation:

- Reframed structure capabilities as focused opt-in contracts instead of broad metadata.
- Documented manager-coordinated mutation and richer event semantics.

## 0.3.0 - 15-09-2026

Changed:

- Replaced `BoundedFifoContentStructure` with `ContentSequenceStructure`.
- Added sequence read-order configuration through `ContentSequenceReadOrder`.
- Added policy-owned retention through `ContentOverflowPolicy.None` and `ContentOverflowPolicy.DropOldest(capacity)`.

Documentation:

- Reconciled the 1.0 architecture roadmap and documented the planned built-in surface.
- Added roadmap and snapshot planning docs.

## 0.2.0 - 15-09-2026

Added:

- Added optional committed-change hooks through `IContentChangeSource`, `ContentChangedEventArgs`, and `ContentManagerBase.Changed`.
- Built-in FIFO and map structures now emit change events after successful adds.

Documentation:

- Added a focused guide for content change hooks.
- Added focused guides for content manager workflows and content identity.
- Refreshed documentation links so each major public system has a dedicated guide.
- Clarified that `ContentManager` requires explicit structure selection and `ContentManagerBase` is for shared processing of existing managers.

## 0.1.0 - 14-09-2026

Initial package release.

This release introduces the first useful ContentSystem core:

- content entries as the core extension model;
- a bounded FIFO content structure;
- map content structures with typed ID strategies;
- manager-coordinated workflows for normal and map use;
- a shared failure and exception model;
- focused docs and examples for normal usage.

Included:

- package-wide `ContentFailure` and content exception types for structured expected failures.
- `ContentEntryId`, `ContentEntryRecord`, `IContentEntry`, and `PlainContentEntry` as the first entry foundation.
- `IContentStructure`, `IStructureAssignedIdContentStructure`, `IContentMapStructure<TId>`, and `BoundedFifoContentStructure` for retained-record lookup and bounded FIFO storage.
- ID strategies and `ContentMapStructure` for caller-provided entry IDs.
- `ContentManagerBase`, `ContentSequenceManager`, and `ContentMapManager<TId>` for shared read/lookup behavior and workflow-specific entry adds.
