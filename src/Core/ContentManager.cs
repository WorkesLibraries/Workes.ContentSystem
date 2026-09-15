using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the default content workflow for structures that assign entry IDs.
/// </summary>
public sealed class ContentManager : ContentManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentManager"/> class.
    /// </summary>
    /// <param name="structure">The structure that assigns IDs when entries are added.</param>
    public ContentManager(IStructureAssignedIdContentStructure structure)
        : base(structure)
    {
        if (structure is null)
        {
            throw new ArgumentNullException(nameof(structure));
        }
    }

    private IStructureAssignedIdContentStructure AssignedIdStructure => (IStructureAssignedIdContentStructure)Structure;

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
    /// Creates a typed manager for a structure-assigned-ID structure with a natural retained-record ID type.
    /// </summary>
    /// <typeparam name="TId">The natural retained-record ID type.</typeparam>
    /// <param name="structure">The structure that assigns IDs and exposes natural typed lookup.</param>
    /// <returns>A typed content manager for the structure.</returns>
    public static ContentManager<TId> For<TId>(IStructureAssignedIdContentStructure<TId> structure)
    {
        return new ContentManager<TId>(structure);
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is IStructureAssignedIdContentStructure)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure does not support structure-assigned IDs.");
        return false;
    }
}
