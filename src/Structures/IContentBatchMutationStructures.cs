using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>Opt-in atomic batch operations for sequence structures.</summary>
public interface IContentSequenceBatchStructure<TId>
{
    /// <summary>Assesses a sequence add range without committing it.</summary>
    ContentPreflightResult AssessAddRange(IReadOnlyList<IContentEntry> entries);
    /// <summary>Commits a complete sequence add range or leaves structure state unchanged.</summary>
    bool TryAddRange(IReadOnlyList<IContentEntry> entries, out IReadOnlyList<ContentEntryRecord> records, out ContentFailure? failure);
}

/// <summary>Opt-in atomic batch operations for map structures.</summary>
public interface IContentMapBatchStructure<TId>
{
    /// <summary>Assesses an add range without committing it.</summary>
    ContentPreflightResult AssessAddRange(IReadOnlyList<KeyValuePair<TId, IContentEntry>> entries);
    /// <summary>Commits a complete add range or leaves structure state unchanged.</summary>
    bool TryAddRange(IReadOnlyList<KeyValuePair<TId, IContentEntry>> entries, out IReadOnlyList<ContentEntryRecord> records, out ContentFailure? failure);
    /// <summary>Assesses a set range without committing it.</summary>
    ContentPreflightResult AssessSetRange(IReadOnlyList<KeyValuePair<TId, IContentEntry>> entries);
    /// <summary>Commits a complete set range or leaves structure state unchanged.</summary>
    bool TrySetRange(IReadOnlyList<KeyValuePair<TId, IContentEntry>> entries, out IReadOnlyList<ContentEntryRecord> records, out IReadOnlyList<ContentEntryRecord> replacedRecords, out ContentFailure? failure);
}

/// <summary>Opt-in atomic batch operations for stack structures.</summary>
public interface IContentStackBatchStructure<TId>
{
    /// <summary>Assesses a push range without committing it.</summary>
    ContentPreflightResult AssessPushRange(IReadOnlyList<IContentEntry> entries);
    /// <summary>Commits a complete push range or leaves structure state unchanged.</summary>
    bool TryPushRange(IReadOnlyList<IContentEntry> entries, out IReadOnlyList<ContentEntryRecord> records, out ContentFailure? failure);
}

/// <summary>Opt-in atomic batch operations for compound structures.</summary>
public interface IContentCompoundBatchStructure<TId>
{
    /// <summary>Assesses adding multiple roots without committing them.</summary>
    ContentPreflightResult AssessAddRoots(IReadOnlyList<IContentEntry> entries);
    /// <summary>Commits all new roots or leaves structure state unchanged.</summary>
    bool TryAddRoots(IReadOnlyList<IContentEntry> entries, out IReadOnlyList<ContentCompoundNode> nodes, out ContentFailure? failure);
    /// <summary>Assesses adding multiple children under one parent without committing them.</summary>
    ContentPreflightResult AssessAddChildren(TId parentId, IReadOnlyList<IContentEntry> entries);
    /// <summary>Commits all new children under one parent or leaves structure state unchanged.</summary>
    bool TryAddChildren(TId parentId, IReadOnlyList<IContentEntry> entries, out IReadOnlyList<ContentCompoundNode> nodes, out ContentFailure? failure);
}

/// <summary>Opt-in atomic predicate-based removal for structures with record removal semantics.</summary>
public interface IContentPredicateRemovalStructure
{
    /// <summary>Assesses removing records selected by a predicate over the current record snapshot.</summary>
    ContentPreflightResult AssessRemoveWhere(Func<ContentEntryRecord, bool> predicate);
    /// <summary>Removes selected records atomically; a predicate exception leaves structure state unchanged.</summary>
    bool TryRemoveWhere(Func<ContentEntryRecord, bool> predicate, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);
}
