using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>Optional entry capability for exposing host-owned attachment references.</summary>
public interface IContentEntryAttachmentSource
{
    /// <summary>Gets this entry's attachment references.</summary>
    IReadOnlyList<ContentAttachmentReference> Attachments { get; }
}
