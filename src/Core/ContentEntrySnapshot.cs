namespace Workes.ContentSystem.Core;

/// <summary>
/// Portable representation of one content entry payload.
/// </summary>
public sealed class ContentEntrySnapshot
{
    /// <summary>
    /// Gets or sets the stable entry snapshot kind identifier.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entry snapshot data format version.
    /// </summary>
    public int DataVersion { get; set; }

    /// <summary>
    /// Gets or sets the entry-owned snapshot value tree.
    /// </summary>
    public ContentSnapshotValue Data { get; set; } = ContentSnapshotValue.Null();
}
