namespace Workes.ContentSystem.Core;

/// <summary>
/// Accepts non-empty string content entry IDs without changing their value.
/// </summary>
public sealed class StringContentEntryIdStrategy : IContentEntryIdStrategy<string>
{
    /// <inheritdoc />
    public bool TryNormalize(string id, out ContentEntryId normalizedId, out ContentFailure? failure)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            normalizedId = default;
            failure = ContentFailures.EntryIdInvalid("Entry ID cannot be empty.");
            return false;
        }

        normalizedId = new ContentEntryId(id);
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
