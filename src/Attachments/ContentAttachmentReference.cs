using System;

namespace Workes.ContentSystem.Core;

/// <summary>Identifies an attachment owned and resolved by a host application.</summary>
/// <remarks>The locator is opaque to ContentSystem. This value does not own, open, transfer, or persist attachment bytes.</remarks>
public sealed class ContentAttachmentReference
{
    /// <summary>Creates an immutable attachment reference.</summary>
    /// <param name="locator">A stable host-defined locator; ContentSystem does not interpret it as a URI or path.</param>
    /// <param name="mediaType">Optional media type such as <c>image/png</c>.</param>
    /// <param name="fileName">Optional suggested file name.</param>
    /// <param name="displayName">Optional user-facing name.</param>
    public ContentAttachmentReference(string locator, string? mediaType = null, string? fileName = null, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(locator)) throw new ArgumentException("Attachment locator cannot be empty.", nameof(locator));
        Locator = locator;
        MediaType = mediaType;
        FileName = fileName;
        DisplayName = displayName;
    }

    /// <summary>Gets the opaque stable host-defined locator.</summary>
    public string Locator { get; }

    /// <summary>Gets the optional media type.</summary>
    public string? MediaType { get; }

    /// <summary>Gets the optional suggested file name.</summary>
    public string? FileName { get; }

    /// <summary>Gets the optional user-facing name.</summary>
    public string? DisplayName { get; }
}
