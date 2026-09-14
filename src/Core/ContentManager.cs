using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the default content workflow for structures that assign entry IDs.
/// </summary>
public sealed class ContentManager : ContentManagerBase
{
    private readonly IStructureAssignedIdContentStructure _structure;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentManager"/> class with the default bounded FIFO structure.
    /// </summary>
    public ContentManager()
        : this(new BoundedFifoContentStructure())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentManager"/> class.
    /// </summary>
    /// <param name="structure">The structure that assigns IDs when entries are added.</param>
    public ContentManager(IStructureAssignedIdContentStructure structure)
        : base(structure)
    {
        _structure = structure ?? throw new ArgumentNullException(nameof(structure));
    }

    /// <summary>
    /// Attempts to add an entry and return the retained record created for it.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <param name="record">The retained record when accepted; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the entry is added.</returns>
    public bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return _structure.TryAdd(entry, out record, out failure);
    }

    /// <summary>
    /// Adds an entry and returns the retained record created for it.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the entry cannot be added.</exception>
    public ContentEntryRecord Add(IContentEntry entry)
    {
        return _structure.Add(entry);
    }
}
