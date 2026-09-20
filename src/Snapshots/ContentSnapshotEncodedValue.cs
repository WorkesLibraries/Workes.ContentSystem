namespace Workes.ContentSystem.Core;

/// <summary>
/// Associates a value payload with the codec required to decode it.
/// </summary>
public sealed class ContentSnapshotEncodedValue
{
    /// <summary>
    /// Gets or sets the stable codec format identifier.
    /// </summary>
    public string CodecId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the codec data version.
    /// </summary>
    public int CodecVersion { get; set; }

    /// <summary>
    /// Gets or sets the encoded value payload.
    /// </summary>
    public ContentSnapshotValue Data { get; set; } = ContentSnapshotValue.Null();
}
