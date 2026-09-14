using System;
using System.Collections.Generic;
using System.Globalization;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Stores content records in chronological FIFO order with a fixed retained capacity.
/// </summary>
public sealed class BoundedFifoContentStructure : IStructureAssignedIdContentStructure, IContentChangeSource
{
    /// <summary>
    /// The default number of records retained by a bounded FIFO structure.
    /// </summary>
    public const int DefaultCapacity = 200;

    private readonly List<ContentEntryRecord> _records;
    private long _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedFifoContentStructure"/> class.
    /// </summary>
    public BoundedFifoContentStructure()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedFifoContentStructure"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of records to retain.</param>
    public BoundedFifoContentStructure(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity values must be greater than zero.");
        }

        Capacity = capacity;
        _records = new List<ContentEntryRecord>(capacity);
    }

    /// <summary>
    /// Gets the maximum number of records retained by the structure.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the number of records currently retained by the structure.
    /// </summary>
    public int Count => _records.Count;

    /// <inheritdoc />
    public IReadOnlyList<ContentEntryRecord> Records => _records.ToArray();

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
        if (_records.Count == Capacity)
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
    /// Attempts to get a retained record by its FIFO-assigned numeric ID.
    /// </summary>
    /// <param name="id">The FIFO-assigned numeric entry ID.</param>
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
    /// Gets a retained record by its FIFO-assigned numeric ID.
    /// </summary>
    /// <param name="id">The FIFO-assigned numeric entry ID.</param>
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
            throw new ArgumentOutOfRangeException(nameof(id), id, "FIFO entry IDs must be greater than zero.");
        }

        return new ContentEntryId(id.ToString(CultureInfo.InvariantCulture));
    }

    private void OnChanged(ContentChangedEventArgs args)
    {
        Changed?.Invoke(this, args);
    }
}
