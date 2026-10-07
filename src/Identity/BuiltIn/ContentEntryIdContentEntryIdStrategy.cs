namespace Workes.ContentSystem.Core;

/// <summary>
/// Accepts already-normalized <see cref="ContentEntryId"/> values without changing them.
/// </summary>
public sealed class ContentEntryIdContentEntryIdStrategy : IContentEntryIdStrategy<ContentEntryId>
{
    /// <inheritdoc />
    public bool TryNormalize(ContentEntryId id, out ContentEntryId normalizedId, out ContentFailure? failure)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            normalizedId = default;
            failure = ContentFailures.EntryIdInvalid("Entry ID cannot be empty.");
            return false;
        }

        normalizedId = id;
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public bool TryValidateNormalized(ContentEntryId id, out ContentFailure? failure)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            failure = ContentFailures.EntryIdInvalid("Entry ID cannot be empty.");
            return false;
        }

        failure = null;
        return true;
    }
}
