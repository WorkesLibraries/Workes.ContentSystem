using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides records added to and removed from a content source by a committed mutation.
/// </summary>
public sealed class ContentChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentChangedEventArgs"/> class.
    /// </summary>
    /// <param name="addedRecords">Records added by the committed mutation.</param>
    /// <param name="removedRecords">Records removed by the committed mutation.</param>
    public ContentChangedEventArgs(
        IEnumerable<ContentEntryRecord>? addedRecords = null,
        IEnumerable<ContentEntryRecord>? removedRecords = null)
    {
        AddedRecords = CopyRecords(addedRecords, nameof(addedRecords));
        RemovedRecords = CopyRecords(removedRecords, nameof(removedRecords));
    }

    /// <summary>
    /// Gets the records added by the committed mutation.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> AddedRecords { get; }

    /// <summary>
    /// Gets the records removed by the committed mutation.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> RemovedRecords { get; }

    private static IReadOnlyList<ContentEntryRecord> CopyRecords(
        IEnumerable<ContentEntryRecord>? records,
        string parameterName)
    {
        if (records is null)
        {
            return Array.Empty<ContentEntryRecord>();
        }

        ContentEntryRecord[] copy = records.ToArray();
        if (copy.Any(record => record is null))
        {
            throw new ArgumentException("Change record collections cannot contain null values.", parameterName);
        }

        return Array.AsReadOnly(copy);
    }
}
