namespace Workes.ContentSystem.Core;

/// <summary>
/// Validates and normalizes caller-provided content entry IDs for a structure.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public interface IContentEntryIdStrategy<TId>
{
    /// <summary>
    /// Attempts to normalize a caller-provided entry ID.
    /// </summary>
    /// <param name="id">The caller-provided entry ID.</param>
    /// <param name="normalizedId">The normalized ID when accepted.</param>
    /// <param name="failure">The structured failure when the ID is rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the ID is accepted.</returns>
    bool TryNormalize(TId id, out ContentEntryId normalizedId, out ContentFailure? failure);

    /// <summary>
    /// Attempts to validate a normalized stored ID, such as one restored from a snapshot.
    /// </summary>
    /// <param name="id">The normalized stored ID.</param>
    /// <param name="failure">The structured failure when the ID is rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the normalized ID is accepted.</returns>
    bool TryValidateNormalized(ContentEntryId id, out ContentFailure? failure);
}
