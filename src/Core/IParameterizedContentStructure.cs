using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content structure that can create a configured replacement with one runtime parameter changed.
/// </summary>
public interface IParameterizedContentStructure : IContentStructure
{
    /// <summary>
    /// Gets the runtime parameters supported by this structure.
    /// </summary>
    IReadOnlyCollection<ContentParameterDefinition> Parameters { get; }

    /// <summary>
    /// Attempts to create a replacement structure with one parameter changed.
    /// </summary>
    /// <param name="parameterId">The stable parameter ID.</param>
    /// <param name="value">The proposed parameter value.</param>
    /// <param name="structure">The replacement structure when creation succeeds; otherwise <see langword="null"/>.</param>
    /// <param name="removedRecords">Records removed while creating the replacement state.</param>
    /// <param name="failure">The structured failure when rejected; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a replacement structure was created or no change was needed.</returns>
    bool TryCreateWithParameter(
        string parameterId,
        object? value,
        out IContentStructure? structure,
        out IReadOnlyList<ContentEntryRecord> removedRecords,
        out ContentFailure? failure);
}
