using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides shared read and lookup behavior for content managers.
/// </summary>
public abstract class ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentManagerBase"/> class.
    /// </summary>
    /// <param name="structure">The active content structure.</param>
    protected ContentManagerBase(IContentStructure structure)
    {
        Structure = structure ?? throw new ArgumentNullException(nameof(structure));

        if (Structure is IContentChangeSource changeSource)
        {
            changeSource.Changed += HandleStructureChanged;
        }
    }

    /// <summary>
    /// Gets the active content structure.
    /// </summary>
    public IContentStructure Structure { get; }

    /// <summary>
    /// Gets retained records in the active structure's read order.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> Records => Structure.Records;

    /// <summary>
    /// Occurs after the active structure commits a content mutation.
    /// </summary>
    public event EventHandler<ContentChangedEventArgs>? Changed;

    /// <summary>
    /// Attempts to get a retained record by ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <param name="record">The retained record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is found.</returns>
    public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return Structure.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found.</exception>
    public ContentEntryRecord Get(ContentEntryId id)
    {
        return Structure.Get(id);
    }

    private void HandleStructureChanged(object? sender, ContentChangedEventArgs args)
    {
        Changed?.Invoke(this, args);
    }
}
