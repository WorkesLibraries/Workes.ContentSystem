# Content Queries

Content managers can create filtered and sorted read views over their current records. These helpers work with every built-in manager because they start from the manager's `Records` view, including the configured read order of sequences and stacks and the depth-first read order of compound structures.

## A Simple Filter

Use `GetFilteredRecords` when one predicate is enough:

```csharp
IReadOnlyList<ContentEntryRecord> warnings = content.GetFilteredRecords(
    record => record.Entry.PlainText.StartsWith("Warning:", StringComparison.Ordinal));
```

The result is materialized, read-only, and retains the active structure's order. The predicate is evaluated immediately. A null predicate throws `ArgumentNullException`; exceptions thrown by the predicate propagate.

## Compose Filters And Ordering

Use `QueryRecords()` to compose several operations:

```csharp
IReadOnlyList<ContentEntryRecord> visible = content.QueryRecords()
    .Where(record => record.Timestamp >= cutoff)
    .Where(record => !record.Entry.PlainText.StartsWith("Internal:", StringComparison.Ordinal))
    .OrderBy(Comparer<ContentEntryRecord>.Create(
        (left, right) => right.Timestamp.CompareTo(left.Timestamp)))
    .ThenBy(Comparer<ContentEntryRecord>.Create(
        (left, right) => StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value)))
    .ToReadOnlyList();
```

Each operation immediately produces a new materialized query. `Where` preserves the order it receives. `OrderBy` starts a stable ordering using the supplied comparer; comparer-equal records preserve their current relative order. `ThenBy` adds a secondary comparer and requires a preceding `OrderBy`. Further `ThenBy` calls add additional tie-breakers. Calling another `OrderBy` starts a new primary ordering.

The query captures the record references and their order from `Records` when `QueryRecords()` is called. Later structure changes do not alter an existing query's membership or ordering. This is a shallow record-list snapshot; it does not clone entry objects. `ToReadOnlyList()` returns a read-only list, and query operations do not mutate the structure, emit events, or change its snapshots. Predicate and comparer exceptions propagate. Any side effects performed by caller-supplied predicates or comparers remain caller-owned.

## Scope

These APIs operate on `ContentEntryRecord` values. They are in-memory read helpers, not database queries, indexes, or search facilities. The structure does not retain a filter or comparer policy; a query instance keeps its ordering comparers only to support later `ThenBy` composition. An explicit query sort changes only that query's view; it does not perform persistent map or compound sorting.

Filtering preserves each structure's current `Records` order unless the query explicitly calls `OrderBy`. For compound hierarchy operations that need parent, depth, or child metadata, use `ContentCompoundManager.GetRecordViews()` and its focused hierarchy APIs; the generic record query does not infer tree relationships from flat records.

See [Content Managers](CONTENT_MANAGERS.md) for how the query surface fits the shared manager API, and [Extension Authoring](EXTENSION_AUTHORING.md) for custom manager guidance.
