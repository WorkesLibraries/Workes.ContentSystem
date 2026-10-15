using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Converts a materialized set of content records into a text representation.
/// </summary>
public interface IContentRecordExporter
{
    /// <summary>Gets the stable identifier for this output format.</summary>
    string FormatId { get; }

    /// <summary>Attempts to export the supplied read-only record snapshot.</summary>
    /// <param name="records">Records in the manager's captured read order.</param>
    /// <param name="text">The exported text when successful.</param>
    /// <param name="failure">The expected rejection when unsuccessful.</param>
    /// <returns><see langword="true"/> when the export succeeds.</returns>
    bool TryExport(IReadOnlyList<ContentEntryRecord> records, out string? text, out ContentFailure? failure);
}
