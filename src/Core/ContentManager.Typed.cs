using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides a content workflow for structures that assign entry IDs and expose a natural retained-record ID type.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public sealed class ContentManager<TId> : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentManager{TId}"/> class.
    /// </summary>
    /// <param name="structure">The structure that assigns IDs and exposes natural typed lookup.</param>
    public ContentManager(IStructureAssignedIdContentStructure<TId> structure)
        : base(structure)
    {
        if (structure is null)
        {
            throw new ArgumentNullException(nameof(structure));
        }
    }

    private IStructureAssignedIdContentStructure<TId> AssignedIdStructure => (IStructureAssignedIdContentStructure<TId>)Structure;

    /// <summary>
    /// Attempts to add an entry and return the retained record created for it.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <param name="record">The retained record when accepted; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the entry is added.</returns>
    public bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return AssignedIdStructure.TryAdd(entry, out record, out failure);
    }

    /// <summary>
    /// Adds an entry and returns the retained record created for it.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the entry cannot be added.</exception>
    public ContentEntryRecord Add(IContentEntry entry)
    {
        return AssignedIdStructure.Add(entry);
    }

    /// <summary>
    /// Attempts to get a retained record by natural ID.
    /// </summary>
    /// <param name="id">The natural retained-record ID.</param>
    /// <param name="record">The retained record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is found.</returns>
    public bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        return AssignedIdStructure.TryGet(id, out record, out failure);
    }

    /// <summary>
    /// Gets a retained record by natural ID.
    /// </summary>
    /// <param name="id">The natural retained-record ID.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found.</exception>
    public ContentEntryRecord Get(TId id)
    {
        return AssignedIdStructure.Get(id);
    }

    /// <summary>
    /// Attempts to remove a retained record by natural ID.
    /// </summary>
    /// <param name="id">The natural retained-record ID.</param>
    /// <param name="removedRecord">The removed record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is removed.</returns>
    public bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
    {
        if (Structure is IContentNaturalIdRemovalStructure<TId> removable)
        {
            return removable.TryRemove(id, out removedRecord, out failure);
        }

        removedRecord = null;
        failure = ContentFailures.StructureUnsupportedOperation("The active content structure does not support natural-ID record removal.");
        return false;
    }

    /// <summary>
    /// Removes a retained record by natural ID.
    /// </summary>
    /// <param name="id">The natural retained-record ID.</param>
    /// <returns>The removed record.</returns>
    /// <exception cref="ContentOperationException">Thrown when removal is rejected or unsupported.</exception>
    public ContentEntryRecord Remove(TId id)
    {
        if (TryRemove(id, out ContentEntryRecord? removedRecord, out ContentFailure? failure))
        {
            return removedRecord!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is IStructureAssignedIdContentStructure<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure does not support the active natural ID workflow.");
        return false;
    }
}
