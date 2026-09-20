namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content structure that can remove retained records by normalized ID.
/// </summary>
public interface IContentRecordRemovalStructure : IContentStructure
{
    /// <summary>
    /// Attempts to remove a retained record by ID.
    /// </summary>
    /// <param name="id">The retained record ID.</param>
    /// <param name="removedRecord">The removed record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is removed.</returns>
    bool TryRemove(ContentEntryId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

    /// <summary>
    /// Removes a retained record by ID.
    /// </summary>
    /// <param name="id">The retained record ID.</param>
    /// <returns>The removed record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be removed.</exception>
    ContentEntryRecord Remove(ContentEntryId id);
}
