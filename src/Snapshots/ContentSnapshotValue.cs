using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Concrete serializer-friendly value tree used by content snapshot codecs.
/// </summary>
public sealed class ContentSnapshotValue
{
    /// <summary>
    /// Gets or sets the payload shape.
    /// </summary>
    public ContentSnapshotValueKind Kind { get; set; }

    /// <summary>
    /// Gets or sets a Boolean scalar when <see cref="Kind"/> is <see cref="ContentSnapshotValueKind.Boolean"/>.
    /// </summary>
    public bool BooleanValue { get; set; }

    /// <summary>
    /// Gets or sets a string scalar when <see cref="Kind"/> is <see cref="ContentSnapshotValueKind.String"/>.
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Gets or sets ordered encoded children when <see cref="Kind"/> is <see cref="ContentSnapshotValueKind.List"/>.
    /// </summary>
    public List<ContentSnapshotEncodedValue> Items { get; set; } = new List<ContentSnapshotEncodedValue>();

    /// <summary>
    /// Gets or sets named encoded children when <see cref="Kind"/> is <see cref="ContentSnapshotValueKind.Object"/>.
    /// </summary>
    public List<ContentSnapshotNamedValue> Properties { get; set; } = new List<ContentSnapshotNamedValue>();

    /// <summary>
    /// Creates a null value.
    /// </summary>
    /// <returns>The created value.</returns>
    public static ContentSnapshotValue Null()
    {
        return new ContentSnapshotValue { Kind = ContentSnapshotValueKind.Null };
    }

    /// <summary>
    /// Creates a Boolean value.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    /// <returns>The created value.</returns>
    public static ContentSnapshotValue Boolean(bool value)
    {
        return new ContentSnapshotValue { Kind = ContentSnapshotValueKind.Boolean, BooleanValue = value };
    }

    /// <summary>
    /// Creates a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The created value.</returns>
    public static ContentSnapshotValue String(string value)
    {
        return new ContentSnapshotValue { Kind = ContentSnapshotValueKind.String, StringValue = value };
    }

    /// <summary>
    /// Creates a list value.
    /// </summary>
    /// <param name="items">The encoded child values.</param>
    /// <returns>The created value.</returns>
    public static ContentSnapshotValue List(IEnumerable<ContentSnapshotEncodedValue>? items = null)
    {
        return new ContentSnapshotValue
        {
            Kind = ContentSnapshotValueKind.List,
            Items = items is null
                ? new List<ContentSnapshotEncodedValue>()
                : items.Select(ContentSnapshotCloner.CloneEncoded).ToList()
        };
    }

    /// <summary>
    /// Creates an object value.
    /// </summary>
    /// <param name="properties">The named encoded child values.</param>
    /// <returns>The created value.</returns>
    public static ContentSnapshotValue Object(IEnumerable<ContentSnapshotNamedValue>? properties = null)
    {
        return new ContentSnapshotValue
        {
            Kind = ContentSnapshotValueKind.Object,
            Properties = properties is null
                ? new List<ContentSnapshotNamedValue>()
                : properties.Select(ContentSnapshotCloner.CloneNamed).ToList()
        };
    }
}
