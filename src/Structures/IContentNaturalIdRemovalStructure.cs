namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content structure that can remove retained records by natural ID.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public interface IContentNaturalIdRemovalStructure<TId> : IContentNaturalIdStructure<TId>, IContentRecordRemovalStructure
{
    /// <summary>
    /// Attempts to remove a retained record by natural ID.
    /// </summary>
    /// <param name="id">The natural retained-record ID.</param>
    /// <param name="removedRecord">The removed record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be removed; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is removed.</returns>
    bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

    /// <summary>
    /// Removes a retained record by natural ID.
    /// </summary>
    /// <param name="id">The natural retained-record ID.</param>
    /// <returns>The removed record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be removed.</exception>
    ContentEntryRecord Remove(TId id);
}
