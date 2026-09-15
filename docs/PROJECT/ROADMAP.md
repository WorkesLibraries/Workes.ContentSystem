# ROADMAP

## Purpose

Record the implementation roadmap that connects the current prerelease package to the intended 1.0.0 package.

Trello remains the task-state source of truth. This document records the architectural sequence so project docs and future implementation stay aligned.

## 1.0 Roadmap

### Stage 10: Refactor bounded structure configuration

Reframe the FIFO-specific bounded structure into a configurable bounded content structure. FIFO should become a placement and overflow configuration rather than the entire type identity.

### Stage 11: Add structure capability metadata

Expose what an active structure supports without forcing every structure into one large interface.

### Stage 12: Add manager-owned runtime mutation and richer change events

Add manager-owned mutation workflows, with structures opting into underlying capabilities. Successful mutations should emit coherent events; rejected mutations should be atomic and quiet.

### Stage 13: Add entry snapshot round-trip contracts

Define entry-level snapshot support for portable entry payloads. `PlainContentEntry` should round-trip out of the box. Custom entries should opt in explicitly.

### Stage 14: Add record and structure snapshot DTOs

Define serializer-friendly DTOs for stored records and whole structures without adding disk I/O or serializer ownership to core.

### Stage 15: Add snapshot capture and restore for built-in structures

Implement exact snapshot capture and restore for built-in structures that opt into structure snapshots.

### Stage 16: Add selected remaining built-in structures

Add selected built-in structures after configuration, capability, mutation, and snapshot contracts are stable.

### Stage 17: Add optional grouped content structure

Add grouped content if the manager-owned workflow remains clean and domain-neutral.

### Stage 18: Evaluate threaded/forum-like content structure

Decide whether threaded content belongs in core, needs custom managers, or should be deferred to examples or companion packages.

### Stage 19: Add additional built-in ID strategies

Add selected low-assumption ID strategies after core structure and snapshot contracts are stable.

### Stage 20: Add bulk operations and mutation helper APIs

Add high-value helper operations such as range workflows and predicate-based removal while preserving atomicity and event semantics.

### Stage 21: Add validation and preflight APIs

Add preflight APIs for mutation and snapshot workflows. Preflight should not mutate state or emit events, and final commit should still revalidate.

### Stage 22: Add manager read-query helpers

Add manager-side helpers for materialized filtered and sorted record views without mutating the active structure.

### Stage 23: Add optional structure sorting support

Add opt-in structure-owned sorting only for structures where reordering retained records is meaningful.

### Stage 24: Add export helpers and attachment abstractions

Add optional export helpers and attachment abstractions after portable snapshots exist. Export and attachments should not become the persistence foundation.

### Stage 25: Add example tests and usage docs

Add examples and focused usage docs for the implemented 1.0 feature set.

### Stage 26: Add extension author documentation

Document custom entries, custom structures, snapshot opt-ins, capabilities, mutation opt-ins, sorting opt-ins, failures, events, and compatibility expectations.

### Stage 27: Prepare 1.0.0 release

Audit API names, docs, examples, XML docs, metadata, changelog, compatibility notes, package build, release branch, and tag.

## Deferred Work

The following work remains future or post-1.0 unless promoted by a later decision:

- broader chat and feed examples;
- platform adapter packages;
- rebuilding ConsoleSystem on top of ContentSystem.
