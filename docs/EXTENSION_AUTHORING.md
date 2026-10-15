# Extension Authoring

Workes.ContentSystem is designed for applications to define their own entry types and for libraries to define their own content workflows. This guide takes you from choosing an extension point to implementing and testing a small custom structure. It also gives focused recipes for identity, failures, events, sorting, and snapshots, and points to the closest references when an extension needs more.

For ordinary package use, start with [Quick Start](QUICK_START.md). The guide is organized around extension work:

1. Choose the narrowest extension point that owns the behavior.
2. Implement an entry, structure, and tailored manager.
3. Add optional capabilities only when the structure can honor their contract.
4. Define stable failures and verify atomic behavior.
5. Test the extension and consult analogous built-in structures for details beyond these recipes.

## Choose An Extension Point

| You need to... | Start with... | The extension owns... |
|---|---|---|
| Store domain-specific content | `IContentEntry` | Entry data and its plain-text representation |
| Keep caller-provided typed IDs | `IContentEntryIdStrategy<TId>` and a map structure | ID validation and normalization |
| Generate structure-owned IDs | `IContentGeneratedIdSource<TId>` | ID generation, observation, and source state |
| Define a new storage workflow | `IContentStructure` and a custom manager | Retained state, lookup, order, identity, and workflow methods |
| Add one optional behavior | A focused structure capability interface | Only the behavior represented by that contract |
| Notify observers | `IContentChangeSource` | Synchronous notifications after committed changes |
| Filter and order visible records | `ContentManagerBase.GetFilteredRecords` and `QueryRecords()` | A materialized read view over the structure's `Records` order |
| Add atomic batch workflows | A family-specific `IContent...BatchStructure<TId>` contract | Staged records and one committed event for the complete batch |
| Remove matching records | `IContentPredicateRemovalStructure` | Snapshot evaluation, family removal rules, and one committed event |
| Persist entry payloads | `IContentEntrySnapshotSerializable` and a factory | Entry snapshot capture and restore |
| Persist whole structures | `IContentStructureSnapshotRoundTrippable` and a factory | Exact structure snapshot round trips |

The structure owns retained state. The manager coordinates the user-facing workflow. A capability interface declares supported behavior; there is no broad capability flags object. If a built-in family already expresses your workflow and you can honor its complete contract, use its family base. Implement `IContentStructure` directly when your workflow differs materially.

## The Shared Structure Contract

Every custom structure implements these four members:

```csharp
public interface IContentStructure
{
    ContentManagerBase CreateManager();
    IReadOnlyList<ContentEntryRecord> Records { get; }
    bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure);
    ContentEntryRecord Get(ContentEntryId id);
}
```

`Records` is the visible read order. Lookup must be consistent with that retained state. `TryGet` returns structured failure data for an expected missing-ID rejection; `Get` returns the record or throws `ContentOperationException` carrying that failure. `CreateManager` returns the tailored manager for this structure.

The shared interface intentionally does not define a universal add/remove API. A custom structure and its manager expose operations that fit the structure's meaning.

## Complete Example: A Quest Log

This example implements a generated-ID append workflow, an archived-state rejection, a tailored manager, and the optional `IContentChangeSource` capability. It is deliberately small: it has no snapshot format and does not claim to support clear, removal, or sorting.

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Workes.ContentSystem.Core;

public static class QuestLogFailureCodes
{
    public const string Archived = "example.questlog.content.archived";
}

public sealed class QuestLogStructure : IContentStructure, IContentChangeSource
{
    private readonly List<ContentEntryRecord> _records = new();
    private readonly bool _isArchived;
    private long _nextId = 1;

    public QuestLogStructure(bool isArchived = false)
    {
        _isArchived = isArchived;
    }

    public event EventHandler<ContentChangedEventArgs>? Changed;

    public IReadOnlyList<ContentEntryRecord> Records => Array.AsReadOnly(_records.ToArray());

    public ContentManagerBase CreateManager() => new QuestLogManager(this);

    public ContentPreflightResult AssessAppend(IContentEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        return _isArchived
            ? ContentPreflightResult.Rejected(ArchivedFailure())
            : ContentPreflightResult.Success();
    }

    public bool TryAppend(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        if (_isArchived)
        {
            record = null;
            failure = ArchivedFailure();
            return false;
        }

        record = new ContentEntryRecord(
            new ContentEntryId("quest/" + _nextId.ToString(CultureInfo.InvariantCulture)),
            entry);
        _nextId++;
        _records.Add(record);
        failure = null;
        Changed?.Invoke(this, new ContentChangedEventArgs(
            addedRecords: new[] { record },
            kind: ContentChangeKind.Added));
        return true;
    }

    public ContentEntryRecord Append(IContentEntry entry)
    {
        if (TryAppend(entry, out ContentEntryRecord? record, out ContentFailure? failure)) return record!;
        throw new ContentOperationException(failure!);
    }

    public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        record = _records.FirstOrDefault(candidate => candidate.Id.Equals(id));
        if (record is not null)
        {
            failure = null;
            return true;
        }

        failure = ContentFailure.Create(
            ContentFailureKind.Entry,
            ContentFailureCodes.EntryNotFound,
            $"Quest log entry '{id}' was not found.",
            component: nameof(QuestLogStructure));
        return false;
    }

    public ContentEntryRecord Get(ContentEntryId id)
    {
        if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure)) return record!;
        throw new ContentOperationException(failure!);
    }

    private static ContentFailure ArchivedFailure() => ContentFailure.Create(
        ContentFailureKind.Extension,
        QuestLogFailureCodes.Archived,
        "The quest log is archived and cannot accept new entries.",
        component: nameof(QuestLogStructure));
}

public sealed class QuestLogManager : ContentManagerBase
{
    public QuestLogManager(QuestLogStructure structure) : base(structure) { }

    private QuestLogStructure QuestLog => (QuestLogStructure)Structure;

    public ContentPreflightResult AssessAppend(IContentEntry entry) => QuestLog.AssessAppend(entry);

    public bool TryAppend(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure) =>
        QuestLog.TryAppend(entry, out record, out failure);

    public ContentEntryRecord Append(IContentEntry entry) => QuestLog.Append(entry);
}
```

Resolve the manager through the structure, subscribe through the manager, and use whichever failure style fits the call site:

```csharp
var log = ContentManagers.ForStructure<QuestLogManager>(new QuestLogStructure());
log.Changed += (_, change) => Console.WriteLine(change.Kind);

ContentPreflightResult assessment = log.AssessAppend(
    new PlainContentEntry(DateTimeOffset.UtcNow, "The gate is open."));

ContentEntryRecord entry = log.Append(
    new PlainContentEntry(DateTimeOffset.UtcNow, "The gate is open."));
```

The `QuestLogStructure(isArchived: true)` case demonstrates an expected domain rejection. Assessment returns a rejected `ContentPreflightResult`; `TryAppend` returns `false` and its `ContentFailure`; `Append` throws `ContentOperationException` containing that failure. All three paths leave records and events unchanged. The NUnit fixture [QuestLogExtensionExampleTests.cs](../tests/Examples/QuestLogExtensionExampleTests.cs) exercises manager resolution, success events, failure consistency, and misuse behavior.

### What The Example Demonstrates

- The structure controls stored identity, visible order, and retained state.
- `ContentEntryId` is the normalized ID exposed by stored records. The example generates stable string IDs itself; use an ID strategy or generated-ID source when the built-in generic workflows fit better.
- `Records` returns a read-only snapshot rather than exposing the mutable list.
- The optional `IContentChangeSource` event is raised only after the record is retained.
- The failure code is stable and extension-owned; it does not add a new package failure kind or change `ContentFailureCodes`.
- The manager contains no duplicate retained-state logic. It delegates the workflow to its active structure.

The example is a teaching baseline, not a universal implementation. A production structure should additionally decide and document concurrency expectations, retention, removal, ID collision rules, and snapshot compatibility when those concerns apply.

## Extension-Authored Failures

Expected domain rejection is data, not an exception. Use a failure for normal conditions callers may handle: a domain rule rejects an entry, a structure is closed, or a snapshot cannot represent extension-owned state. Use standard .NET exceptions for programmer misuse and invalid construction, such as null arguments or malformed setup.

### Choose A Kind And Code

`ContentFailureKind` is a fixed set of broad package categories. Extensions do not extend that enum. Use the most specific existing category that honestly describes the failing subsystem; use `ContentFailureKind.Extension` for extension-owned behavior that does not fit another category.

Define stable extension-owned codes in your own static class and namespace. The `workes.content.` prefix is reserved for package-owned codes. Consumers should branch on `Kind` and `Code`, never on the display message.

```csharp
public static class QuestLogFailureCodes
{
    public const string Archived = "example.questlog.content.archived";
}

ContentFailure failure = ContentFailure.Create(
    ContentFailureKind.Extension,
    QuestLogFailureCodes.Archived,
    "The quest log is archived and cannot accept new entries.",
    component: nameof(QuestLogStructure));
```

Keep codes stable after callers may depend on them. Keep `Message` useful to a person, and use `Component` or `Source` for stable origin information rather than embedding it only in prose.

### Preserve Lower-Level Causes

When your extension rejects an operation because a lower-level component rejected it, wrap the lower-level failure instead of discarding it:

```csharp
ContentFailure wrapped = ContentFailure.Wrap(
    ContentFailureKind.Extension,
    "example.questlog.content.append_rejected",
    "The quest update could not be appended.",
    cause: lowerLevelFailure,
    component: nameof(QuestLogStructure));
```

`Cause` preserves the nested failure for diagnostics and programmatic inspection. Do not flatten a useful code chain into a concatenated message.

### Convert Exceptions Only At An Intentional Boundary

`ContentFailure.FromException(...)` preserves the failure when given a `ContentSystemException`. For another exception, it creates structured failure data using the requested kind and code, defaulting to `Extension` and `ContentFailureCodes.ExtensionRejected`.

Use it only when your contract intentionally converts an exception from an expected extension path—for example, a documented callback failure—into a normal rejection. Do not catch every exception or convert argument errors, null misuse, or programming bugs into expected failures. If exceptions are meant to propagate, let them propagate.

### Carry Failures Through The Public Workflow

- `Assess...` returns `ContentPreflightResult.Rejected(failure)` without changing state.
- `Try...` returns `false` and exposes the same failure through its `out ContentFailure?` value.
- An expected-success wrapper throws `ContentOperationException(failure)` with that structured failure.
- Callers inspect `exception.Failure.Code`, `.Kind`, and `.Cause`; they should not parse exception messages.

If there is no lower-level failure, do not invent one. If an operation succeeds, set its failure output to `null`. If it rejects, return no committed record and do not emit a change event. The [Failure Handling](FAILURES.md#extension-authored-failures) reference contains the complete failure model and code conventions.

## Focused Extension Recipes

Each recipe states the extension point and the invariant it adds. The linked guide gives the package-level workflow details; use the closest built-in family and matching tests as implementation references for the remaining specifics.

### Extend A Built-In Family

Use `ContentSequenceStructureBase<TId>`, `ContentMapStructureBase<TId>`, `ContentSingleStructureBase<TId>`, `ContentStackStructureBase<TId>`, or `ContentCompoundStructureBase<TId>` when the built-in family contract matches your workflow. Implement the abstract methods and matching `Assess...` methods, then return your tailored manager from `CreateManager()`. Do not derive from a family base only to reuse a small helper: inheritance means taking on its complete workflow and invariants.

Read [Content Structures](CONTENT_STRUCTURES.md) and the matching built-in structure in `src/Structures/BuiltIn/`. For reusable workflows, compare the related family base under `src/Structures/` and the manager base under `src/Managers/`.

### Validate Caller-Provided IDs

Implement `IContentEntryIdStrategy<TId>` when callers provide typed IDs. `TryNormalize` validates and maps a caller value to `ContentEntryId`; `TryValidateNormalized` validates a stored ID restored from a snapshot. Both methods must accept the same stored identity language so restored entries remain addressable. Strategies validate IDs; they do not generate them.

Read [Content Identity](CONTENT_IDENTITY.md). Compare the built-in string, integer, GUID, and `ContentEntryId` strategies in `src/Identity/BuiltIn/` and their cases in `tests/Identity/ContentEntryIdStrategyTests.cs`.

### Generate IDs

Implement `IContentGeneratedIdSource<TId>` when the structure assigns IDs. Provide non-mutating `CanCreateNext`, `CanObserve`, and `CanObserveNormalized` methods as well as committing `TryCreateNext`, `TryObserve`, and `TryObserveNormalized` methods. Preflight must not advance or observe state. If IDs are persisted, implement a source snapshot factory so restore preserves future generation behavior.

Read [Content Identity](CONTENT_IDENTITY.md) and [Content Snapshots](CONTENT_SNAPSHOTS.md). Compare `LongContentGeneratedIdSource` and its tests, then inspect the sequence-family snapshot factory when adding generated IDs to a custom sequence.

### Add Assessments And Mutation Capabilities

Add a focused contract for a behavior only when your structure can honor it. Common contracts include clear, record removal, typed map operations, parameterized mutation, persistent sorting, and structure snapshots. For each committing operation, expose assessment when callers need advisory validation. Assessments are side-effect-free and commits revalidate; a preflight result is not a reservation.

Read [Content Structures](CONTENT_STRUCTURES.md), [Content Managers](CONTENT_MANAGERS.md), and [Failures](FAILURES.md). Compare the matching contract and manager family. Never mutate a live list in place before all validation or caller-controlled comparison work has completed.

### Publish Change Hooks

Implement `IContentChangeSource` when observers need committed mutation notifications. Raise the event after state commit, include records and metadata needed to describe the change, and remain silent for rejection and no-op paths. The manager forwards the structure's args and becomes the event sender.

Read [Content Changes](CONTENT_CHANGES.md) and `tests/Changes/ContentChangedEventArgsTests.cs`.

### Add Atomic Batch Mutations

Use `IContentSequenceBatchStructure<TId>`, `IContentMapBatchStructure<TId>`, `IContentStackBatchStructure<TId>`, or `IContentCompoundBatchStructure<TId>` when your structure can guarantee all-or-nothing behavior for that batch. Implement matching assessment and try methods without adding required members to existing structure-family bases. Materialize caller input once, validate the complete batch, stage records and relationships, then commit retained state and raise one event. A rejected batch must preserve records, generated-ID state, snapshots, and event silence.

Generated-ID batches need an atomic source range. Implement `IContentGeneratedIdRangeSource<TId>` so `TryCreateRange` either returns the complete unique, valid ID sequence or leaves the source unchanged. Structures using generated IDs should return a structured unsupported-operation failure when the source cannot provide this guarantee.

Implement `IContentPredicateRemovalStructure` only when predicate matches can be evaluated against a stable record snapshot before mutation. Predicate exceptions must leave structure state untouched. Preserve the family’s ordinary removal policies; for compounds, apply the configured child-removal policy and validate the selected subtree set before detaching any node.

The built-in implementations in `src/Structures/BuiltIn/` and `tests/Managers/ContentBulkMutationTests.cs` show batch event aggregation, generated-ID range reservation, and compound removal checks. Read [Content Managers](CONTENT_MANAGERS.md), [Content Structures](CONTENT_STRUCTURES.md), and [Content Changes](CONTENT_CHANGES.md) for the public workflows and event contract.

### Add Sorting

Use `GetSortedRecords` for a temporary view; no structure capability is needed. Implement `IContentSortableStructure` for persistent map-like reordering or `IContentCompoundSortableStructure` for sibling sorting. Keep order stable for equal comparer keys, prepare candidate state before commit, and preserve the result in snapshots. Sequence, stack, and single-entry semantics should not opt in when order changes violate their meaning.

Read [Content Managers](CONTENT_MANAGERS.md), [Content Structures](CONTENT_STRUCTURES.md), and [Content Changes](CONTENT_CHANGES.md). Compare map and compound implementations in `src/Structures/BuiltIn/` and scenarios in `tests/Managers/ContentSortingTests.cs`.

### Add Or Use Read Queries

Custom managers inherit `GetFilteredRecords` and `QueryRecords` from `ContentManagerBase`; no new structure capability is required. The query snapshots the custom structure's `Records` membership and order, retaining the record references rather than cloning entry objects. Implement `Records` with the order your manager should expose. Queries preserve that order until an explicit `OrderBy` is applied and their operations do not mutate the structure.

Read [Content Queries](CONTENT_QUERIES.md) for the eager `Where` / `OrderBy` / `ThenBy` behavior, and [Content Managers](CONTENT_MANAGERS.md) for the shared manager contract. If the workflow needs additional domain information such as compound depth or parent IDs, add a focused family-specific read API rather than teaching the generic record query to infer it.

### Add Snapshot Round Trips

Implement `IContentEntrySnapshotSerializable` for an entry payload and register its factory before restore. Implement `IContentStructureSnapshotRoundTrippable` for whole-structure state; expose the matching factory through `SnapshotFactory`. Capture must include enough portable data to reconstruct IDs, order, topology, policies, and generated-ID state owned by your structure. Restore should validate a complete candidate before manager replacement.

Read [Content Snapshots](CONTENT_SNAPSHOTS.md). Use the sequence/map/compound factory bases and `ContentSnapshotRecords` / `ContentSnapshotProperties` where they match. Compare `tests/Snapshots/ContentSnapshotTests.cs` and the corresponding built-in structure snapshot tests.

## Test Your Extension

Test the public manager workflow as well as structure-specific methods. At minimum, cover:

- manager resolution and record lookup through normalized IDs;
- successful commit, expected rejection, state preservation, and event metadata/silence;
- assessment side-effect freedom and commit-time revalidation;
- all-or-nothing batch state and generated-ID preservation, plus predicate exception atomicity, when batch/removal capabilities are supported;
- stable failure kind/code and preservation of nested causes;
- snapshot round trips, ID continuity, and malformed payload rejection if snapshots are supported;
- sorting stability and comparer exception atomicity if persistent sorting is supported.

The Quest Log example has a corresponding NUnit fixture at `tests/Examples/QuestLogExtensionExampleTests.cs`. Put representative documentation examples in tests when they should remain compile-checked. Keep the public guide understandable without opening the test source; use the fixture as a runnable second reference.

## Extension Checklist

- [ ] The structure owns its retained state, visible order, identity, and lookup.
- [ ] The manager delegates and exposes only supported workflows.
- [ ] Each optional behavior has a focused capability contract.
- [ ] Expected rejection returns stable structured failures; misuse remains an exception.
- [ ] Assessments are side-effect-free and committing APIs revalidate.
- [ ] Events occur once after commit and remain absent for rejection/no-op.
- [ ] Snapshots preserve exact portable state if the extension supports them.
- [ ] Examples and XML docs match the actual contracts and tests.

## Where To Go Next

| Extension task | Start with | Closest implementation/test reference |
|---|---|---|
| Add an entry | [Content Entries](CONTENT_ENTRIES.md) | `PlainContentEntry` and `tests/Entries/ContentEntryTests.cs` |
| Implement structure/manager | [Content Structures](CONTENT_STRUCTURES.md), [Content Managers](CONTENT_MANAGERS.md) | Quest Log example; then `src/Structures/BuiltIn/` and `tests/Structures/BuiltIn/` |
| Add typed or generated IDs | [Content Identity](CONTENT_IDENTITY.md) | `src/Identity/BuiltIn/` and `tests/Identity/ContentEntryIdStrategyTests.cs` |
| Add custom failures | [Failure Handling](FAILURES.md#extension-authored-failures) | Quest Log example and `tests/Core/ContentFailureTests.cs` |
| Add events | [Content Changes](CONTENT_CHANGES.md) | Built-in structures implementing `IContentChangeSource` and `tests/Changes/` |
| Add snapshots | [Content Snapshots](CONTENT_SNAPSHOTS.md) | `src/Structures/*SnapshotFactoryBase.cs` and `tests/Snapshots/` |
| Filter or compose record views | [Content Queries](CONTENT_QUERIES.md) | Shared `ContentManagerBase` implementation and `tests/Managers/ContentRecordQueryTests.cs` |
| Add persistent sorting | [Content Managers](CONTENT_MANAGERS.md) | Built-in map/compound structures and `tests/Managers/ContentSortingTests.cs` |
| Add record export | [Export And Attachments](EXPORT_AND_ATTACHMENTS.md) | `IContentRecordExporter` and built-in text exporters |
| Expose attachment references | [Export And Attachments](EXPORT_AND_ATTACHMENTS.md) | `IContentEntryAttachmentSource` and `ContentEntryAttachments` |

Read the interface and focused guide before copying a built-in implementation. Implementations show how the package applies a contract in one workflow; they do not define a broader contract than the public interface and documentation. Tests are especially useful for edge cases and event expectations, but do not treat private implementation details as extension guarantees.
