using System;

namespace Workes.ContentSystem.Core;

/// <summary>Contains the text produced by a content record exporter.</summary>
public sealed class ContentExportResult
{
    internal ContentExportResult(string formatId, string text)
    {
        FormatId = formatId ?? throw new ArgumentNullException(nameof(formatId));
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets the stable identifier of the format used for this export.</summary>
    public string FormatId { get; }

    /// <summary>Gets the exported text.</summary>
    public string Text { get; }
}
