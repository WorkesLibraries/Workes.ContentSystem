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
    /// <param name="kind">The high-level change kind.</param>
    /// <param name="cleared">Whether the mutation cleared the content source.</param>
    /// <param name="configurationChanged">Runtime configuration changes produced by the mutation.</param>
    /// <param name="requiresFullRefresh">Whether observers should rebuild their full view.</param>
    public ContentChangedEventArgs(
        IEnumerable<ContentEntryRecord>? addedRecords = null,
        IEnumerable<ContentEntryRecord>? removedRecords = null,
        ContentChangeKind kind = ContentChangeKind.Unknown,
        bool cleared = false,
        IEnumerable<ContentConfigurationChanged>? configurationChanged = null,
        bool requiresFullRefresh = false)
    {
        AddedRecords = CopyRecords(addedRecords, nameof(addedRecords));
        RemovedRecords = CopyRecords(removedRecords, nameof(removedRecords));
        Kind = kind;
        Cleared = cleared;
        ConfigurationChanged = CopyConfigurationChanges(configurationChanged, nameof(configurationChanged));
        RequiresFullRefresh = requiresFullRefresh || cleared || ConfigurationChanged.Any(change => change.RequiresFullRefresh);
    }

    /// <summary>
    /// Gets the high-level change kind.
    /// </summary>
    public ContentChangeKind Kind { get; }

    /// <summary>
    /// Gets the records added by the committed mutation.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> AddedRecords { get; }

    /// <summary>
    /// Gets the records removed by the committed mutation.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> RemovedRecords { get; }

    /// <summary>
    /// Gets whether the mutation cleared the content source.
    /// </summary>
    public bool Cleared { get; }

    /// <summary>
    /// Gets runtime configuration changes produced by the mutation.
    /// </summary>
    public IReadOnlyList<ContentConfigurationChanged> ConfigurationChanged { get; }

    /// <summary>
    /// Gets whether observers should rebuild their full view.
    /// </summary>
    public bool RequiresFullRefresh { get; }

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

    private static IReadOnlyList<ContentConfigurationChanged> CopyConfigurationChanges(
        IEnumerable<ContentConfigurationChanged>? changes,
        string parameterName)
    {
        if (changes is null)
        {
            return Array.Empty<ContentConfigurationChanged>();
        }

        ContentConfigurationChanged[] copy = changes.ToArray();
        if (copy.Any(change => change is null))
        {
            throw new ArgumentException("Configuration change collections cannot contain null values.", parameterName);
        }

        return Array.AsReadOnly(copy);
    }
}
