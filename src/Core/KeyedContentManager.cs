using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides a content workflow for structures that use caller-provided typed IDs.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public sealed class KeyedContentManager<TId> : ContentManagerBase
{
    private readonly IKeyedContentStructure<TId> _structure;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentManager{TId}"/> class with the default keyed structure for <typeparamref name="TId"/>.
    /// </summary>
    public KeyedContentManager()
        : this(new KeyedContentStructure<TId>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentManager{TId}"/> class.
    /// </summary>
    /// <param name="structure">The keyed content structure.</param>
    public KeyedContentManager(IKeyedContentStructure<TId> structure)
        : base(structure)
    {
        _structure = structure ?? throw new ArgumentNullException(nameof(structure));
    }

    /// <summary>
    /// Attempts to add an entry with a caller-provided ID.
    /// </summary>
    /// <param name="id">The caller-provided entry ID.</param>
    /// <param name="entry">The entry to add.</param>
    /// <param name="record">The retained record when accepted; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the entry is added.</returns>
    public bool TryAdd(TId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return _structure.TryAdd(id, entry, out record, out failure);
    }

    /// <summary>
    /// Adds an entry with a caller-provided ID.
    /// </summary>
    /// <param name="id">The caller-provided entry ID.</param>
    /// <param name="entry">The entry to add.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the ID is rejected.</exception>
    public ContentEntryRecord Add(TId id, IContentEntry entry)
    {
        return _structure.Add(id, entry);
    }

    /// <summary>
    /// Attempts to get a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <param name="record">The retained record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is found.</returns>
    public bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return _structure.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found or the ID is rejected.</exception>
    public ContentEntryRecord Get(TId id)
    {
        return _structure.Get(id);
    }
}
