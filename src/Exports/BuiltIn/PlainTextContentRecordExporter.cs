using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>Exports plain content entries as newline-separated plain text.</summary>
public sealed class PlainTextContentRecordExporter : IContentRecordExporter
{
    /// <summary>Stable format ID for the plain-text exporter.</summary>
    public const string StableFormatId = ContentFailureCodes.PackagePrefix + "export.plain_text";

    /// <inheritdoc />
    public string FormatId => StableFormatId;

    /// <inheritdoc />
    public bool TryExport(IReadOnlyList<ContentEntryRecord> records, out string? text, out ContentFailure? failure)
    {
        if (records is null) throw new System.ArgumentNullException(nameof(records));
        if (!TryGetEntries(records, out PlainContentEntry[] entries, out failure))
        {
            text = null;
            return false;
        }

        text = string.Join("\n", entries.Select(entry => entry.PlainText));
        failure = null;
        return true;
    }

    internal static bool TryGetEntries(
        IReadOnlyList<ContentEntryRecord> records,
        out PlainContentEntry[] entries,
        out ContentFailure? failure)
    {
        entries = new PlainContentEntry[records.Count];
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].Entry is not PlainContentEntry plainEntry)
            {
                failure = ContentFailure.Create(
                    ContentFailureKind.Export,
                    ContentFailureCodes.ExportRejected,
                    $"The built-in plain-text exporter does not support entry type '{records[i].Entry.GetType().FullName}'.",
                    source: records[i].Id.ToString());
                entries = System.Array.Empty<PlainContentEntry>();
                return false;
            }

            entries[i] = plainEntry;
        }

        failure = null;
        return true;
    }
}
