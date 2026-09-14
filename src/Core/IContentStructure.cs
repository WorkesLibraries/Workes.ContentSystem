using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents content storage that can expose retained records and look them up by ID.
/// </summary>
public interface IContentStructure
{
    /// <summary>
    /// Gets retained records in the structure's read order.
    /// </summary>
    IReadOnlyList<ContentEntryRecord> Records { get; }

    /// <summary>
    /// Attempts to get a retained record by ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <param name="record">The retained record when found; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when the record cannot be found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a retained record is found.</returns>
    bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure);

    /// <summary>
    /// Gets a retained record by ID.
    /// </summary>
    /// <param name="id">The entry ID to look up.</param>
    /// <returns>The retained record.</returns>
    /// <exception cref="ContentOperationException">Thrown when the record cannot be found.</exception>
    ContentEntryRecord Get(ContentEntryId id);
}
