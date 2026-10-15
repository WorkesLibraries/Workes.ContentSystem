# Content Managers

Content managers are the normal root objects for using Workes.ContentSystem.

## Purpose

A manager wraps one active content structure and exposes the workflow created by that structure.

The shared read and lookup behavior is common across managers, but write workflows are structure-specific. Some structures assign IDs internally. Other structures require callers to provide IDs. ContentSystem keeps those workflows separate by letting each structure create its normal manager.

## Manager Types

Use `ContentManagers.ForStructure(...)` for structure-driven construction.

The untyped resolver asks the structure for its natural manager and returns the shared base:

```csharp
ContentManagerBase content = ContentManagers.ForStructure(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

if (content is ContentSequenceManager sequence)
{
    ContentEntryRecord record = sequence.Add(
        new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
}
```

`ContentSequenceStructure` assigns increasing numeric IDs stored as `ContentEntryId` values. The structure is passed explicitly so the manager never hides which storage or retention policy is active.

The typed resolver is the same structure-driven path, but it validates the manager type you expect up front:

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));

ContentEntryRecord record = content.Add(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));

ContentEntryRecord found = content.Get(1);
ContentEntryRecord removed = content.Remove(1);
```

This is equivalent to constructing the typed manager explicitly:

```csharp
var content = new ContentSequenceManager(
    new ContentSequenceStructure(ContentOverflowPolicy.None));
```

The generic type argument does not choose the manager; it asserts what the structure should create. The explicit constructor form is legal, but `ContentManagers.ForStructure(...)` is the recommended path because the structure creates its correct manager.

Use map structures for caller-provided typed IDs.

```csharp
var content = ContentManagers.ForStructure<ContentMapManager<string>>(
    new ContentMapStructure<string>());

ContentEntryRecord record = content.Add(
    "server-started",
    new PlainContentEntry(DateTimeOffset.UtcNow, "Server started."));
```

`ContentMapManager<TId>` supports the typed ID shapes supported by `ContentMapStructure<TId>`. Built-in ID strategies are resolved for `string`, `long`, `Guid`, and `ContentEntryId`. For custom ID types, pass a fully configured map structure or a custom `IContentEntryIdStrategy<TId>`.

Use single-entry managers for current-state workflows:

```csharp
var current = ContentManagers.ForStructure<ContentSingleManager>(
    new ContentSingleStructure(ContentSingleReplacementPolicy.Replace));

current.Set(new PlainContentEntry(DateTimeOffset.UtcNow, "Current objective"));
ContentEntryRecord active = current.GetCurrent();
```

Use stack managers for last-in-first-out workflows:

```csharp
var stack = ContentManagers.ForStructure<ContentStackManager>(
    new ContentStackStructure(ContentOverflowPolicy.Reject(capacity: 20)));

stack.Push(new PlainContentEntry(DateTimeOffset.UtcNow, "Opened menu"));
ContentEntryRecord top = stack.Peek();
ContentEntryRecord popped = stack.Pop();
```

Use compound managers for owned-tree workflows:

```csharp
var tree = ContentManagers.ForStructure<ContentCompoundManager>(
    new ContentCompoundStructure(ContentCompoundChildRemovalPolicy.RemoveSubtree));

ContentCompoundNode topic = tree.AddRoot(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Topic"));
tree.AddChild(1, new PlainContentEntry(DateTimeOffset.UtcNow, "Reply"));
```

## Shared Base

`ContentManagerBase` is the shared ancestor for already-created managers.

It exposes:

- `Structure`;
- `Records`;
- `GetFilteredRecords(predicate)`, a materialized filter over the active read order;
- `QueryRecords()`, a composable materialized query over a snapshot of the active records;
- `GetSortedRecords(IComparer<ContentEntryRecord>)`, a stable, materialized, read-only sorted view;
- `Count`, `IsEmpty`, and `Contains(ContentEntryId)`;
- `TryGet(ContentEntryId, ...)`;
- `Get(ContentEntryId)`;
- `TryCaptureSnapshot(...)` and `CaptureSnapshot()`;
- `TryRestoreSnapshot(...)` and `RestoreSnapshot(...)`;
- `Changed`.

This is useful when code receives `ContentSequenceManager`, `ContentMapManager<TId>`, or a custom manager and only needs to read records or look up records by the normalized `ContentEntryId`:

```csharp
void Render(ContentManagerBase content)
{
    foreach (ContentEntryRecord record in content.Records)
    {
        RenderRecord(record);
    }
}
```

`GetSortedRecords` sorts a copy using the supplied comparer. It does not reorder the active structure, alter snapshots, or emit an event. Equal records retain their prior relative order. Persistent sorting is a separate opt-in workflow: map managers expose `AssessSort`, `TrySort`, and `Sort`; compound managers expose `AssessSortSiblings`, `TrySortSiblings`, and `SortSiblings` to reorder roots and child groups.

For filtering and query composition, use the same read API on every built-in manager:

```csharp
IReadOnlyList<ContentEntryRecord> matching = content.GetFilteredRecords(
    record => record.Entry.PlainText.Contains("warning", StringComparison.OrdinalIgnoreCase));

IReadOnlyList<ContentEntryRecord> newestWarnings = content.QueryRecords()
    .Where(record => record.Entry.PlainText.Contains("warning", StringComparison.OrdinalIgnoreCase))
    .OrderBy(Comparer<ContentEntryRecord>.Create(
        (left, right) => right.Timestamp.CompareTo(left.Timestamp)))
    .ThenBy(Comparer<ContentEntryRecord>.Create(
        (left, right) => StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value)))
    .ToReadOnlyList();
```

`QueryRecords()` snapshots the active `Records` membership and order immediately, retaining record references rather than cloning entry objects. Each `Where`, `OrderBy`, and `ThenBy` operation is eager and returns a new materialized query. Filtering preserves the current order; sorting is stable, and `ThenBy` requires an earlier `OrderBy`. The final read-only result's membership and ordering are unaffected by later structure mutations. Query operations do not change structure state or emit events; exceptions from caller predicates/comparers propagate, and any side effects those callbacks perform are caller-owned. Null predicates and comparers throw `ArgumentNullException`.

Query sorting affects only the returned view. It does not configure a standing order or mutate map and compound structures. Compound queries start from the depth-first flattened `Records` order; use `GetRecordViews()` and compound traversal APIs when a query needs explicit hierarchy metadata. See [Content Queries](CONTENT_QUERIES.md) for the full contract and examples.

```csharp
IReadOnlyList<ContentEntryRecord> byTimestamp = content.GetSortedRecords(
    Comparer<ContentEntryRecord>.Create((left, right) => left.Timestamp.CompareTo(right.Timestamp)));

map.Sort(timestampComparer);       // changes persistent map order
tree.SortSiblings(timestampComparer); // independently changes each tree sibling group
```

The one-comparer compound overload sorts every group using the same rule. To select a rule by group, pass a selector receiving the normalized parent ID (`null` for roots):

```csharp
tree.SortSiblings(parentId => parentId is null
    ? rootComparer
    : childComparers[parentId.Value]);
```

To sort just one group, pass a nullable parent ID and comparer. `null` targets roots; an ID targets that parent's children. Assessment and try forms report an unknown parent with `ContentFailureCodes.EntryNotFound`. Per-group selectors and comparers run before commit, so an exception leaves the entire tree unchanged. Sorting is explicit: new nodes are not automatically moved to match a previous comparer.

```csharp
tree.SortSiblings(parentId: null, rootComparer);
tree.SortSiblings(parentId: topic.Record.Id, childComparer);
```

Most application code should resolve managers from structures. `ContentManagerBase` is abstract, so it is not constructed directly; it exists so multiple manager workflows can be processed through their common read and lookup surface.

Reusable workflow families can also have manager bases. `ContentSequenceManagerBase`, `ContentMapManagerBase<TId>`, `ContentSingleManagerBase<TId>`, `ContentStackManagerBase<TId>`, and `ContentCompoundManagerBase<TId>` hold shared family behavior such as add/set/push/tree operations, typed lookup, typed removal, and clear. The concrete managers remain the normal user-facing types because they can expose concrete structure features without forcing those features onto the whole family.

## Convenience Helpers

Convenience helpers are intentionally small and live where their semantics are obvious.

All managers expose `Count`, `IsEmpty`, and normalized-ID `Contains`. Manager families with natural typed IDs also expose `Contains(TId)`.

Sequence managers expose first/last helpers over the configured read order:

```csharp
ContentEntryRecord firstVisible = sequence.GetFirst();
ContentEntryRecord lastVisible = sequence.GetLast();
```

For `ContentSequenceReadOrder.NewestFirst`, `GetFirst()` returns the newest retained record because it is first in the current read order.

Map managers expose get-or-create helpers:

```csharp
ContentEntryRecord record = map.GetOrSet(
    "thread-main",
    () => new PlainContentEntry(DateTimeOffset.UtcNow, "First post."));
```

If the record already exists, the factory is not invoked and no change event is raised. If the record is missing, the helper adds it through the normal map add workflow.

Single managers expose `HasCurrent`. Stack managers expose `CanPeek` and `CanPop`.

Compound managers expose node-based overloads so callers can keep useful node references instead of extracting IDs everywhere:

```csharp
ContentCompoundNode topic = tree.AddRoot(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Topic"));

ContentCompoundNode reply = tree.AddChild(
    topic,
    new PlainContentEntry(DateTimeOffset.UtcNow, "Reply"));

ContentCompoundNode parent = tree.GetParent(reply);
IReadOnlyList<ContentCompoundNode> replies = tree.GetChildren(topic);
```

Compound managers also expose `GetRecordViews()` for depth-first readouts with hierarchy metadata such as depth, parent ID, child IDs, root status, and child presence. These views are read models only; they are not snapshot DTOs.

## Runtime Mutation

Managers coordinate the normal mutation workflow. Structures own retained content state, and concrete managers expose only the mutations that make sense for their structure family.

`ContentSequenceManager` exposes sequence add, numeric lookup/removal, clear, and runtime sequence-parameter mutation:

```csharp
var content = new ContentSequenceManager(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.SetStructureParameter(
    ContentSequenceStructure.OverflowPolicyParameterId,
    ContentOverflowPolicy.DropOldest(capacity: 500));
content.Remove(1);
content.Clear();
```

Parameterized mutation mirrors InventorySystem's runtime configuration style: structures expose stable parameter IDs, and managers coordinate the commit. The first built-in parameter is `ContentSequenceStructure.OverflowPolicyParameterId`, whose value must be a `ContentOverflowPolicy`.

`ContentMapManager<TId>` exposes typed map add, lookup, removal, and clear for the built-in map structure:

```csharp
var map = ContentManagers.ForStructure<ContentMapManager<string>>(
    new ContentMapStructure<string>());
map.Remove("thread-main");
```

`ContentSingleManager<TId>` exposes set, get-current, remove, and clear. `ContentStackManager<TId>` exposes push, peek, pop, typed lookup/removal, and clear. `ContentCompoundManager<TId>` exposes root/child creation, node traversal, subtree-aware removal, and clear.

Custom structures should expose their own manager when their mutation vocabulary differs from the built-ins.

## Atomic Batch Operations

Batch helpers consume the supplied enumerable once and materialize the inputs before asking the structure to commit. A rejected batch leaves retained records and generated-ID state unchanged and emits no event. A successful batch emits one coherent event, and returned record/node lists are read-only snapshots.

```csharp
var sequence = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));
var map = ContentManagers.ForStructure<ContentMapManager<string>>(
    new ContentMapStructure<string>());

IReadOnlyList<ContentEntryRecord> added = sequence.AddRange(new IContentEntry[]
{
    new PlainContentEntry(DateTimeOffset.UtcNow, "First"),
    new PlainContentEntry(DateTimeOffset.UtcNow, "Second")
});

IReadOnlyList<ContentEntryRecord> set = map.SetRange(new[]
{
    new KeyValuePair<string, IContentEntry>("topic-a", new PlainContentEntry(DateTimeOffset.UtcNow, "Updated")),
    new KeyValuePair<string, IContentEntry>("topic-b", new PlainContentEntry(DateTimeOffset.UtcNow, "New topic"))
});

IReadOnlyList<ContentEntryRecord> removed = sequence.RemoveWhere(
    record => record.Entry.PlainText.StartsWith("temporary:", StringComparison.Ordinal));

var stack = ContentManagers.ForStructure<ContentStackManager>(
    new ContentStackStructure(ContentOverflowPolicy.None));
IReadOnlyList<ContentEntryRecord> pushed = stack.PushRange(new IContentEntry[]
{
    new PlainContentEntry(DateTimeOffset.UtcNow, "Bottom"),
    new PlainContentEntry(DateTimeOffset.UtcNow, "Top")
});
```

Sequence managers expose `AddRange`; map managers expose `AddRange` and `SetRange`; stack managers expose `PushRange`; compound managers expose `AddRoots` and `AddChildren(parentId, ...)`. Input order matches repeated individual operations: stack pushes follow input order (the last item is on top), and compound sibling visibility still follows the configured read direction. Map range inputs must use distinct normalized IDs; duplicate IDs within a range reject the entire operation. Map `SetRange` replaces existing records in place and appends new IDs in input order.

`RemoveWhere` is available on sequence, map, single, stack, and compound managers. The predicate is evaluated against a snapshot before any removal commits; if it throws, the structure is unchanged. An empty match set succeeds without an event. Compound removal follows `ContentCompoundChildRemovalPolicy`: `RemoveSubtree` removes descendants of each match, while `Reject` rejects a match if it has a child that does not also match. This allows a fully selected subtree to be removed atomically under `Reject`.

Each batch workflow has matching `Assess...`, `Try...`, and expected-success APIs. Assessments can execute a removal predicate against the current snapshot but never commit changes or events; commit re-evaluates it, so assessments are advisory. These APIs are optional structure capabilities: custom structures remain source-compatible and can opt in by implementing the matching `IContent...BatchStructure<TId>` or `IContentPredicateRemovalStructure` contract. Generated-ID workflows additionally require `IContentGeneratedIdRangeSource<TId>` so reserving a range is atomic. See [Content Structures](CONTENT_STRUCTURES.md) and [Content Changes](CONTENT_CHANGES.md) for capability and event contracts.

## Preflight

Managers and structures expose `Assess...` APIs for advisory, side-effect-free validation before committing a mutation:

```csharp
ContentPreflightResult assessment = content.AssessAdd(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));

if (assessment.CanCommit)
{
    content.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
}
```

Preflight checks report the same structured failure style as the eventual mutation, but they do not retain records, advance generated ID sources, observe manual IDs, replace active structures, or emit change events. Preflight is not a lock; code should still use `Try...` or expected-success APIs for the actual commit because state can change between assessment and mutation.

Built-in managers expose assessment methods for their own workflow vocabulary:

- sequence: generated/explicit add, remove, clear, and structure-parameter mutation;
- map: add, set, get-or-set, remove, and clear;
- single: generated/explicit set, remove, and clear;
- stack: generated/explicit push, peek, pop, remove, and clear;
- compound: root/child creation, remove, and clear.

`ContentManagerBase` exposes snapshot preflight:

```csharp
ContentPreflightResult restore = content.AssessRestoreSnapshot(snapshot);
```

Restore assessment validates the snapshot and manager compatibility without replacing the active structure or firing a snapshot-restored event.

## Change Hooks

`ContentManagerBase.Changed` forwards events from the active structure when that structure implements `IContentChangeSource`.

```csharp
ContentManagerBase content = new ContentSequenceManager(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

content.Changed += (_, args) =>
{
    foreach (ContentEntryRecord record in args.AddedRecords)
    {
        RenderRecord(record);
    }
};
```

Forwarded events use the manager as `sender` and preserve the structure's `ContentChangedEventArgs`.

Structures that do not implement `IContentChangeSource` remain valid. Managers over those structures simply have no structure events to forward.

See [Content Changes](CONTENT_CHANGES.md) for event payload and timing details.

## Snapshots

Managers own the normal whole-structure restore workflow.

`ContentManagerBase.CaptureSnapshot()` captures the active structure when it implements `IContentStructureSnapshotRoundTrippable`. `RestoreSnapshot(snapshot)` uses that same active structure's `SnapshotFactory`, verifies that the restored structure is compatible with the concrete manager, replaces the active structure atomically, and emits one full-refresh snapshot-restored event after commit.

```csharp
ContentStructureSnapshot snapshot = content.CaptureSnapshot();

content.RestoreSnapshot(snapshot);
```

Register custom entry snapshot factories once before restoring structure snapshots that contain those entry kinds:

```csharp
ContentEntrySnapshotFactories.Register(MyEntry.Factory);

content.RestoreSnapshot(snapshot);
```

The package registers built-ins such as `PlainContentEntry.Factory` automatically. Custom entry registration is application composition state; capture does not require registration, but restore does.

Explicit structure factories remain available for migration and advanced restore scenarios where the active structure's factory is intentionally not the target:

```csharp
content.RestoreSnapshot(snapshot, NewStructureVersion.Factory);
```

## Try And Expected-Success APIs

Managers follow the package failure pattern:

- `Assess...` APIs are advisory preflight checks and do not commit;
- `Try...` APIs commit when accepted and return structured failure data for expected rejection;
- expected-success APIs commit when accepted and throw `ContentOperationException` carrying the same `ContentFailure` when rejected.

Try APIs return structured failure data for expected rejection:

```csharp
bool added = content.TryAdd(entry, out ContentEntryRecord? record, out ContentFailure? failure);
```

Expected-success APIs return the value or throw `ContentOperationException` carrying the same `ContentFailure`:

```csharp
ContentEntryRecord record = content.Get(id);
```

Programmer misuse, such as a null entry or null structure, uses standard .NET exceptions.

## Choosing A Manager

Choose based on the structure:

- use `ContentManagers.ForStructure(...)` for normal structure-driven resolution;
- use typed `ContentManagers.ForStructure<TManager>(...)` when your code expects a specific manager from the start;
- use direct manager constructors only for explicit setup, tests, or advanced scenarios;
- use `ContentManagerBase` when code only needs shared read, lookup, events, or snapshot behavior.

This split keeps the API explicit. It avoids one broad manager with add methods that only work for some structures.

## Manager Resolution

Structures create their normal manager through `IContentStructure.CreateManager()`.

This keeps manager selection close to the structure-owned content model without adding a registry or broad capability metadata. Focused contracts such as map add, structure-assigned add, mutation, change hooks, and snapshots still describe the behavior a structure supports.

The untyped path resolves the structure's natural manager:

```csharp
ContentManagerBase content = ContentManagers.ForStructure(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));
```

Use pattern matching when shared code needs to branch into manager-specific APIs:

```csharp
if (content is ContentSequenceManager sequence)
{
    sequence.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
    sequence.Get(1);
}
```

Callers that already expect a specific manager can use a typed resolver rather than a manual cast:

```csharp
ContentSequenceManager content =
    ContentManagers.ForStructure<ContentSequenceManager>(sequence);
```

The typed resolver fails with a structured manager-mismatch failure if the structure creates a different manager.

Custom structure authors implement `CreateManager()` to return their custom manager. Direct manager constructors remain available for explicit setup and tests where they fit.

## Exporting Records

Every manager exposes `TryExport(exporter, out result, out failure)` and `Export(exporter)`. The exporter receives a materialized read-only copy in the manager's current record order. This is a read workflow: export does not change state or raise events. Built-in plain-text and Markdown exporters support `PlainContentEntry`; other entry types can be handled by custom `IContentRecordExporter` implementations. See [Export And Attachments](EXPORT_AND_ATTACHMENTS.md) for examples and the expected failure behavior.
