using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the public workflow for <see cref="ContentSequenceStructure"/>.
/// </summary>
public sealed class ContentSequenceManager : ContentSequenceManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceManager"/> class.
    /// </summary>
    /// <param name="structure">The sequence structure.</param>
    public ContentSequenceManager(ContentSequenceStructure structure)
        : base(structure)
    {
    }

    private ContentSequenceStructure ConcreteSequence => (ContentSequenceStructure)Structure;

    /// <summary>
    /// Attempts to change one runtime parameter on the active sequence structure.
    /// </summary>
    public bool TrySetStructureParameter(
        string parameterId,
        object? value,
        out IReadOnlyList<ContentEntryRecord> removedRecords,
        out ContentFailure? failure)
    {
        if (string.IsNullOrWhiteSpace(parameterId))
        {
            removedRecords = Array.Empty<ContentEntryRecord>();
            failure = ContentFailures.Configuration("Structure parameter ID cannot be empty.");
            return false;
        }

        IContentStructure previous = Structure;
        if (!ConcreteSequence.TryCreateWithParameter(parameterId, value, out IContentStructure? replacement, out removedRecords, out failure))
        {
            return false;
        }

        if (replacement is null || ReferenceEquals(replacement, Structure))
        {
            failure = null;
            return true;
        }

        if (!TryAcceptStructureReplacement(replacement, out failure))
        {
            return false;
        }

        ReplaceStructure(replacement);
        OnChanged(new ContentChangedEventArgs(
            removedRecords: removedRecords,
            kind: ContentChangeKind.ConfigurationChanged,
            configurationChanged: new[]
            {
                new ContentConfigurationChanged(
                    ContentConfigurationChangeKind.StructureParameter,
                    parameterId,
                    value,
                    previous,
                    replacement,
                    requiresFullRefresh: true)
            },
            requiresFullRefresh: true));
        failure = null;
        return true;
    }

    /// <summary>
    /// Changes one runtime parameter on the active sequence structure.
    /// </summary>
    public IReadOnlyList<ContentEntryRecord> SetStructureParameter(string parameterId, object? value)
    {
        if (TrySetStructureParameter(parameterId, value, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure))
        {
            return removedRecords;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentSequenceStructure)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not a content sequence structure.");
        return false;
    }
}
