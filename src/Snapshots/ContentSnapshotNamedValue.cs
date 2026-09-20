namespace Workes.ContentSystem.Core;

/// <summary>
/// Associates a stable string name with an encoded content snapshot value.
/// </summary>
public sealed class ContentSnapshotNamedValue
{
    /// <summary>
    /// Gets or sets the property or metadata name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encoded value.
    /// </summary>
    public ContentSnapshotEncodedValue Value { get; set; } = new ContentSnapshotEncodedValue();
}
