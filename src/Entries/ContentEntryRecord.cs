using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content entry stored in a structure with a structure-assigned ID.
/// </summary>
public sealed class ContentEntryRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentEntryRecord"/> class.
    /// </summary>
    /// <param name="id">The structure-assigned entry ID.</param>
    /// <param name="entry">The stored content entry.</param>
    public ContentEntryRecord(ContentEntryId id, IContentEntry entry)
    {
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        Id = id;
    }

    /// <summary>
    /// Gets the structure-assigned entry ID.
    /// </summary>
    public ContentEntryId Id { get; }

    /// <summary>
    /// Gets the stored content entry.
    /// </summary>
    public IContentEntry Entry { get; }

    /// <summary>
    /// Gets the time associated with the stored entry.
    /// </summary>
    public DateTimeOffset Timestamp => Entry.Timestamp;

    /// <summary>
    /// Gets plain text for rendering, searching, export, or diagnostics.
    /// </summary>
    public string PlainText => Entry.PlainText;
}
