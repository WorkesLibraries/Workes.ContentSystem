using System;
using System.Collections.Generic;
using System.Globalization;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Stores content records as an ordered sequence with configurable retention behavior.
/// </summary>
public sealed class ContentSequenceStructure : IStructureAssignedIdContentStructure, IContentChangeSource
{
    private readonly List<ContentEntryRecord> _records = new List<ContentEntryRecord>();
    private long _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceStructure"/> class.
    /// </summary>
    /// <param name="overflowPolicy">The policy used for retention and overflow.</param>
    /// <param name="readOrder">The order used when reading retained records.</param>
    public ContentSequenceStructure(
        ContentOverflowPolicy overflowPolicy,
        ContentSequenceReadOrder readOrder = ContentSequenceReadOrder.OldestFirst)
    {
        OverflowPolicy = overflowPolicy ?? throw new ArgumentNullException(nameof(overflowPolicy));

        if (!Enum.IsDefined(typeof(ContentSequenceReadOrder), readOrder))
        {
            throw new ArgumentOutOfRangeException(nameof(readOrder), readOrder, "Content sequence read order is not supported.");
        }

        ReadOrder = readOrder;
    }

    /// <summary>
    /// Gets the order used when reading retained records.
    /// </summary>
    public ContentSequenceReadOrder ReadOrder { get; }

    /// <summary>
    /// Gets the policy used for retention and overflow.
    /// </summary>
    public ContentOverflowPolicy OverflowPolicy { get; }

    /// <summary>
    /// Gets the number of records currently retained by the structure.
    /// </summary>
    public int Count => _records.Count;

    /// <inheritdoc />
    public IReadOnlyList<ContentEntryRecord> Records => CreateReadSnapshot();

    /// <inheritdoc />
    public event EventHandler<ContentChangedEventArgs>? Changed;

    /// <summary>
    /// Adds an entry and returns the retained record created for it.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <returns>The retained record.</returns>
    public ContentEntryRecord Add(IContentEntry entry)
    {
        if (TryAdd(entry, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        ContentEntryRecord? removedRecord = null;
        if (OverflowPolicy.Kind == ContentOverflowPolicyKind.DropOldest && _records.Count == OverflowPolicy.Capacity)
        {
            removedRecord = _records[0];
            _records.RemoveAt(0);
        }

        ContentEntryId id = new ContentEntryId(_nextId.ToString(CultureInfo.InvariantCulture));
        _nextId++;

        record = new ContentEntryRecord(id, entry);
        _records.Add(record);
        failure = null;
        OnChanged(new ContentChangedEventArgs(
            new[] { record },
            removedRecord is null ? null : new[] { removedRecord }));
        return true;
    }

    /// <inheritdoc />
    public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        EnsureValidId(id);

        for (int i = 0; i < _records.Count; i++)
        {
            ContentEntryRecord candidate = _records[i];
            if (candidate.Id == id)
            {
                record = candidate;
                failure = null;
                return true;
            }
        }

        record = null;
        failure = ContentFailures.EntryNotFound($"Entry '{id}' was not found.", id.ToString());
        return false;
    }

    /// <summary>
    /// Attempts to get a retained record by its structure-assigned numeric ID.
    /// </summary>
    /// <param name="id">The structure-assigned numeric entry ID.</param>
    /// <param name="record">The retained record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is found.</returns>
    public bool TryGet(long id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return TryGet(CreateNumericId(id), out record, out failure);
    }

    /// <inheritdoc />
    public ContentEntryRecord Get(ContentEntryId id)
    {
        if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Gets a retained record by its structure-assigned numeric ID.
    /// </summary>
    /// <param name="id">The structure-assigned numeric entry ID.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found.</exception>
    public ContentEntryRecord Get(long id)
    {
        return Get(CreateNumericId(id));
    }

    private static void EnsureValidId(ContentEntryId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException("Content entry ID cannot be empty.", nameof(id));
        }
    }

    private static ContentEntryId CreateNumericId(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Content sequence entry IDs must be greater than zero.");
        }

        return new ContentEntryId(id.ToString(CultureInfo.InvariantCulture));
    }

    private IReadOnlyList<ContentEntryRecord> CreateReadSnapshot()
    {
        ContentEntryRecord[] snapshot = _records.ToArray();
        if (ReadOrder == ContentSequenceReadOrder.NewestFirst)
        {
            Array.Reverse(snapshot);
        }

        return snapshot;
    }

    private void OnChanged(ContentChangedEventArgs args)
    {
        Changed?.Invoke(this, args);
    }
}
