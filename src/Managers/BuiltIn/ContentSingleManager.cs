namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the public workflow for <see cref="ContentSingleStructure{TId}"/>.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public class ContentSingleManager<TId> : ContentSingleManagerBase<TId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSingleManager{TId}"/> class.
    /// </summary>
    public ContentSingleManager(ContentSingleStructure<TId> structure)
        : base(structure)
    {
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentSingleStructure<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not the active content single structure type.");
        return false;
    }
}

/// <summary>
/// Provides the public long-ID workflow for <see cref="ContentSingleStructure"/>.
/// </summary>
public sealed class ContentSingleManager : ContentSingleManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSingleManager"/> class.
    /// </summary>
    public ContentSingleManager(ContentSingleStructure structure)
        : base(structure)
    {
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentSingleStructure)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not a content single structure.");
        return false;
    }
}
