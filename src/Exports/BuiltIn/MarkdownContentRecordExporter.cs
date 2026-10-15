using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Workes.ContentSystem.Core;

/// <summary>Exports plain content entries as Markdown sections with timestamp headings.</summary>
public sealed class MarkdownContentRecordExporter : IContentRecordExporter
{
    /// <summary>Stable format ID for the Markdown exporter.</summary>
    public const string StableFormatId = ContentFailureCodes.PackagePrefix + "export.markdown";

    /// <inheritdoc />
    public string FormatId => StableFormatId;

    /// <inheritdoc />
    public bool TryExport(IReadOnlyList<ContentEntryRecord> records, out string? text, out ContentFailure? failure)
    {
        if (records is null) throw new System.ArgumentNullException(nameof(records));
        if (!PlainTextContentRecordExporter.TryGetEntries(records, out PlainContentEntry[] entries, out failure))
        {
            text = null;
            return false;
        }

        text = string.Join("\n\n", entries.Select(entry =>
            $"### {entry.Timestamp.ToString("O", CultureInfo.InvariantCulture)}\n\n{EscapePlainText(entry.PlainText)}"));
        failure = null;
        return true;
    }

    private static string EscapePlainText(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (character == '&') builder.Append("&amp;");
            else if (character == '<') builder.Append("&lt;");
            else if (character == '>') builder.Append("&gt;");
            else
            {
                if ("\\`*_{}[]()#+-.!|~".IndexOf(character) >= 0) builder.Append('\\');
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
