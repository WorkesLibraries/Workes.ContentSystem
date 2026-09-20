namespace Workes.ContentSystem.Core;

/// <summary>
/// Portable representation of one retained content record.
/// </summary>
public sealed class ContentRecordSnapshot
{
    /// <summary>
    /// Gets or sets the stored record ID as a serializer-friendly string.
    /// </summary>
    public string EntryId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the captured entry payload.
    /// </summary>
    public ContentEntrySnapshot Entry { get; set; } = new ContentEntrySnapshot();
}
