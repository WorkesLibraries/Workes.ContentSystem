namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides a content workflow for structures that use caller-provided typed IDs.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public sealed class ContentMapManager<TId> : ContentMapManagerBase<TId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentMapManager{TId}"/> class with a map structure using the default ID strategy for <typeparamref name="TId"/>.
    /// </summary>
    public ContentMapManager()
        : this(new ContentMapStructure<TId>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentMapManager{TId}"/> class.
    /// </summary>
    /// <param name="idStrategy">The strategy used by the created map structure to validate and normalize caller-provided IDs.</param>
    public ContentMapManager(IContentEntryIdStrategy<TId> idStrategy)
        : this(new ContentMapStructure<TId>(idStrategy))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentMapManager{TId}"/> class.
    /// </summary>
    /// <param name="structure">The map content structure.</param>
    public ContentMapManager(ContentMapStructure<TId> structure)
        : base(structure)
    {
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is ContentMapStructure<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not the active map content structure type.");
        return false;
    }
}
