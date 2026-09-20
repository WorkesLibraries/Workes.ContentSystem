using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Exposes synchronous notifications for committed content changes.
/// </summary>
public interface IContentChangeSource
{
    /// <summary>
    /// Occurs after a content mutation has been committed.
    /// </summary>
    event EventHandler<ContentChangedEventArgs>? Changed;
}
