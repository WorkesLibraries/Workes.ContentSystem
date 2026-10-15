using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides composable, materialized read operations over a snapshot of content records.
/// </summary>
/// <remarks>The query snapshots record references and order, not mutable entry payloads.</remarks>
public sealed class ContentRecordQuery
{
    private readonly ContentEntryRecord[] _records;
    private readonly IComparer<ContentEntryRecord>? _ordering;

    internal ContentRecordQuery(IEnumerable<ContentEntryRecord> records)
        : this(records.ToArray(), ordering: null)
    {
    }

    private ContentRecordQuery(ContentEntryRecord[] records, IComparer<ContentEntryRecord>? ordering)
    {
        _records = records;
        _ordering = ordering;
    }

    /// <summary>
    /// Filters the current materialized view, preserving its order.
    /// </summary>
    /// <param name="predicate">The condition each retained record must satisfy.</param>
    /// <returns>A new materialized query containing matching records.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
    public ContentRecordQuery Where(Func<ContentEntryRecord, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        return new ContentRecordQuery(_records.Where(predicate).ToArray(), _ordering);
    }

    /// <summary>
    /// Sorts the current materialized view using a stable comparer.
    /// </summary>
    /// <param name="comparer">The record comparer.</param>
    /// <returns>A new materialized query in comparer order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
    public ContentRecordQuery OrderBy(IComparer<ContentEntryRecord> comparer)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        ContentEntryRecord[] sorted = _records.OrderBy(record => record, comparer).ToArray();
        return new ContentRecordQuery(sorted, comparer);
    }

    /// <summary>
    /// Adds a stable secondary ordering to a query that already has an explicit ordering.
    /// </summary>
    /// <param name="comparer">The comparer used when records are equal under the current ordering.</param>
    /// <returns>A new materialized query ordered by the current and secondary comparers.</returns>
    /// <exception cref="InvalidOperationException">The query has no preceding <see cref="OrderBy(IComparer{ContentEntryRecord})"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
    public ContentRecordQuery ThenBy(IComparer<ContentEntryRecord> comparer)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        if (_ordering is null)
        {
            throw new InvalidOperationException("ThenBy requires a preceding OrderBy call.");
        }

        var combined = new CombinedComparer(_ordering, comparer);
        ContentEntryRecord[] sorted = _records.OrderBy(record => record, combined).ToArray();
        return new ContentRecordQuery(sorted, combined);
    }

    /// <summary>
    /// Returns the current query as a stable, materialized, read-only list.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> ToReadOnlyList() => Array.AsReadOnly(_records);

    private sealed class CombinedComparer : IComparer<ContentEntryRecord>
    {
        private readonly IComparer<ContentEntryRecord> _primary;
        private readonly IComparer<ContentEntryRecord> _secondary;

        public CombinedComparer(IComparer<ContentEntryRecord> primary, IComparer<ContentEntryRecord> secondary)
        {
            _primary = primary;
            _secondary = secondary;
        }

        public int Compare(ContentEntryRecord? x, ContentEntryRecord? y)
        {
            int primaryResult = _primary.Compare(x!, y!);
            return primaryResult != 0 ? primaryResult : _secondary.Compare(x!, y!);
        }
    }
}
