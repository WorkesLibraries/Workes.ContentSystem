namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content structure with a natural caller-facing retained-record ID type.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public interface IContentNaturalIdStructure<TId> : IContentStructure
{
    /// <summary>
    /// Attempts to get a retained record by natural ID.
    /// </summary>
    /// <param name="id">The natural retained-record ID.</param>
    /// <param name="record">The retained record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is found.</returns>
    bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Gets a retained record by natural ID.
    /// </summary>
    /// <param name="id">The natural retained-record ID.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found.</exception>
    ContentEntryRecord Get(TId id);
}
