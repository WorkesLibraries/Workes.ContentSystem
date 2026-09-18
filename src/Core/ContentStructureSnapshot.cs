using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Portable representation of retained records and structure-owned snapshot state.
/// </summary>
public sealed class ContentStructureSnapshot
{
    /// <summary>
    /// Gets or sets the stable structure snapshot kind identifier.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the structure snapshot data format version.
    /// </summary>
    public int DataVersion { get; set; }

    /// <summary>
    /// Gets or sets retained records in the structure snapshot.
    /// </summary>
    public List<ContentRecordSnapshot> Records { get; set; } = new List<ContentRecordSnapshot>();

    /// <summary>
    /// Gets or sets the structure-owned snapshot value tree.
    /// </summary>
    public ContentSnapshotValue Data { get; set; } = ContentSnapshotValue.Null();
}
