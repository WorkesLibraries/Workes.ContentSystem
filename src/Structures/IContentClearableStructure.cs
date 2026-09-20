using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content structure that can clear retained records.
/// </summary>
public interface IContentClearableStructure : IContentStructure
{
    /// <summary>
    /// Attempts to clear retained records.
    /// </summary>
    /// <param name="removedRecords">The records removed by the clear operation.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the clear operation is accepted.</returns>
    bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

    /// <summary>
    /// Clears retained records.
    /// </summary>
    /// <returns>The records removed by the clear operation.</returns>
    /// <exception cref="ContentOperationException">Thrown when the clear operation is rejected.</exception>
    IReadOnlyList<ContentEntryRecord> Clear();
}
