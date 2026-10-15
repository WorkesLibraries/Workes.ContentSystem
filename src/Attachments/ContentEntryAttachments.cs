using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>Read helpers for optional entry attachment references.</summary>
public static class ContentEntryAttachments
{
    /// <summary>Returns a stable read-only copy of the entry's references, or an empty list when unsupported.</summary>
    /// <param name="entry">The entry to inspect.</param>
    public static IReadOnlyList<ContentAttachmentReference> GetReferences(IContentEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        if (entry is not IContentEntryAttachmentSource source) return Array.Empty<ContentAttachmentReference>();
        IReadOnlyList<ContentAttachmentReference> references = source.Attachments
            ?? throw new InvalidOperationException("An attachment source returned a null reference list.");
        var copy = new ContentAttachmentReference[references.Count];
        for (int i = 0; i < references.Count; i++)
        {
            copy[i] = references[i]
                ?? throw new InvalidOperationException("An attachment source returned a null reference.");
        }

        return Array.AsReadOnly(copy);
    }
}
