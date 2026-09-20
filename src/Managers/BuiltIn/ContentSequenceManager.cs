using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the public workflow for <see cref="ContentSequenceStructure{TId}"/>.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public class ContentSequenceManager<TId> : ContentSequenceManagerBase<TId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceManager{TId}"/> class.
    /// </summary>
    /// <param name="structure">The sequence structure to manage.</param>
    public ContentSequenceManager(ContentSequenceStructure<TId> structure)
        : base(structure)
    {
    }

    /// <summary>
    /// Gets the active concrete sequence structure.
    /// </summary>
    protected ContentSequenceStructure<TId> ConcreteSequence => (ContentSequenceStructure<TId>)Structure;

    /// <summary>
    /// Attempts to change a sequence structure parameter by replacing the active structure atomically.
    /// </summary>
    /// <param name="parameterId">The stable parameter ID to change.</param>
    /// <param name="value">The committed parameter value.</param>
    /// <param name="removedRecords">Records removed by the parameter change.</param>
    /// <param name="failure">The structured failure when the parameter change is rejected.</param>
    /// <returns><c>true</c> when the parameter change is accepted; otherwise, <c>false</c>.</returns>
    public bool TrySetStructureParameter(
        string parameterId,
        object? value,
        out IReadOnlyList<ContentEntryRecord> removedRecords,
        out ContentFailure? failure)
    {
        if (string.IsNullOrWhiteSpace(parameterId))
        {
            removedRecords = System.Array.Empty<ContentEntryRecord>();
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
    /// Changes a sequence structure parameter by replacing the active structure atomically.
    /// </summary>
    /// <param name="parameterId">The stable parameter ID to change.</param>
    /// <param name="value">The committed parameter value.</param>
    /// <returns>Records removed by the parameter change.</returns>
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
        if (structure is ContentSequenceStructure<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not the active content sequence structure type.");
        return false;
    }
}

/// <summary>
/// Provides the public long-ID workflow for <see cref="ContentSequenceStructure"/>.
/// </summary>
public sealed class ContentSequenceManager : ContentSequenceManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSequenceManager"/> class.
    /// </summary>
    /// <param name="structure">The long-ID sequence structure to manage.</param>
    public ContentSequenceManager(ContentSequenceStructure structure)
        : base(structure)
    {
    }

    private ContentSequenceStructure ConcreteSequence => (ContentSequenceStructure)Structure;

    /// <summary>
    /// Attempts to change a sequence structure parameter by replacing the active structure atomically.
    /// </summary>
    /// <param name="parameterId">The stable parameter ID to change.</param>
    /// <param name="value">The committed parameter value.</param>
    /// <param name="removedRecords">Records removed by the parameter change.</param>
    /// <param name="failure">The structured failure when the parameter change is rejected.</param>
    /// <returns><c>true</c> when the parameter change is accepted; otherwise, <c>false</c>.</returns>
    public bool TrySetStructureParameter(
        string parameterId,
        object? value,
        out IReadOnlyList<ContentEntryRecord> removedRecords,
        out ContentFailure? failure)
    {
        if (string.IsNullOrWhiteSpace(parameterId))
        {
            removedRecords = System.Array.Empty<ContentEntryRecord>();
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
    /// Changes a sequence structure parameter by replacing the active structure atomically.
    /// </summary>
    /// <param name="parameterId">The stable parameter ID to change.</param>
    /// <param name="value">The committed parameter value.</param>
    /// <returns>Records removed by the parameter change.</returns>
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
