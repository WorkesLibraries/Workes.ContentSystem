namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the public workflow for <see cref="ContentStackStructure{TId}"/>.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public class ContentStackManager<TId> : ContentStackManagerBase<TId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStackManager{TId}"/> class.
    /// </summary>
    public ContentStackManager(ContentStackStructure<TId> structure)
        : base(structure)
    {
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentStackStructure<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not the active content stack structure type.");
        return false;
    }
}

/// <summary>
/// Provides the public long-ID workflow for <see cref="ContentStackStructure"/>.
/// </summary>
public sealed class ContentStackManager : ContentStackManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStackManager"/> class.
    /// </summary>
    public ContentStackManager(ContentStackStructure structure)
        : base(structure)
    {
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentStackStructure)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not a content stack structure.");
        return false;
    }
}
