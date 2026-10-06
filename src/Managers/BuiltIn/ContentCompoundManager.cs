namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides the public workflow for <see cref="ContentCompoundStructure{TId}"/>.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public class ContentCompoundManager<TId> : ContentCompoundManagerBase<TId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundManager{TId}"/> class.
    /// </summary>
    public ContentCompoundManager(ContentCompoundStructure<TId> structure)
        : base(structure)
    {
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentCompoundStructure<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not the built-in compound content structure for the active ID type.");
        return false;
    }
}

/// <summary>
/// Provides the public long-ID workflow for <see cref="ContentCompoundStructure"/>.
/// </summary>
public sealed class ContentCompoundManager : ContentCompoundManagerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundManager"/> class.
    /// </summary>
    public ContentCompoundManager(ContentCompoundStructure structure)
        : base(structure)
    {
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentCompoundStructure)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not the built-in long-ID compound content structure.");
        return false;
    }
}
