using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Optional capability for generated ID sources to reserve a range atomically.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
/// <remarks>A failed range request must leave source state unchanged. Successful results must be unique and valid under <see cref="IContentGeneratedIdSource{TId}.IdStrategy"/>.</remarks>
public interface IContentGeneratedIdRangeSource<TId>
{
    /// <summary>Assesses whether the requested number of IDs can be generated without advancing source state.</summary>
    /// <param name="count">The number of IDs to reserve.</param>
    /// <param name="failure">The structured rejection reason, when the range cannot be reserved.</param>
    /// <returns><see langword="true"/> when the requested range can be created.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    bool CanCreateRange(int count, out ContentFailure? failure);

    /// <summary>Creates a range of unique IDs atomically.</summary>
    /// <param name="count">The number of IDs to reserve.</param>
    /// <param name="ids">The generated IDs in creation order, or an empty list when rejected.</param>
    /// <param name="failure">The structured rejection reason, when the range cannot be reserved.</param>
    /// <returns><see langword="true"/> when the entire range was reserved.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    bool TryCreateRange(int count, out IReadOnlyList<TId> ids, out ContentFailure? failure);
}
