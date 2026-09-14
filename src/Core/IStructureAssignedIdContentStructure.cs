namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content structure that assigns entry IDs when entries are added.
/// </summary>
public interface IStructureAssignedIdContentStructure : IContentStructure
{
    /// <summary>
    /// Attempts to add an entry and return the retained record created for it.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <param name="record">The retained record when accepted; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the entry is added.</returns>
    bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Adds an entry and returns the retained record created for it.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the entry cannot be added.</exception>
    ContentEntryRecord Add(IContentEntry entry);
}
