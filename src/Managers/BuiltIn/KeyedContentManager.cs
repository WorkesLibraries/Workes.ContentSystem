namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides a content workflow for structures that use caller-provided typed IDs.
/// </summary>
/// <typeparam name="TId">The caller-facing ID type.</typeparam>
public sealed class KeyedContentManager<TId> : KeyedContentManagerBase<TId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentManager{TId}"/> class with a keyed structure using the default ID strategy for <typeparamref name="TId"/>.
    /// </summary>
    public KeyedContentManager()
        : this(new KeyedContentStructure<TId>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentManager{TId}"/> class.
    /// </summary>
    /// <param name="idStrategy">The strategy used by the created keyed structure to validate and normalize caller-provided IDs.</param>
    public KeyedContentManager(IContentEntryIdStrategy<TId> idStrategy)
        : this(new KeyedContentStructure<TId>(idStrategy))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedContentManager{TId}"/> class.
    /// </summary>
    /// <param name="structure">The keyed content structure.</param>
    public KeyedContentManager(KeyedContentStructure<TId> structure)
        : base(structure)
    {
    }

    /// <inheritdoc />
    protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
    {
        if (structure is KeyedContentStructure<TId>)
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.StructureUnsupportedOperation("Replacement structure is not the active keyed content structure type.");
        return false;
    }
}
