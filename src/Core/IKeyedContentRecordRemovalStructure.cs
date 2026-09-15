namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a keyed content structure that can remove retained records by caller-facing ID.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public interface IKeyedContentRecordRemovalStructure<TId> : IKeyedContentStructure<TId>, IContentRecordRemovalStructure
{
    /// <summary>
    /// Attempts to remove a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to remove.</param>
    /// <param name="removedRecord">The removed record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be removed; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is removed.</returns>
    bool TryRemove(TId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

    /// <summary>
    /// Removes a retained record by caller-facing ID.
    /// </summary>
    /// <param name="id">The entry ID to remove.</param>
    /// <returns>The removed record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be removed or the ID is rejected.</exception>
    ContentEntryRecord Remove(TId id);
}
