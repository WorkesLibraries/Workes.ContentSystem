using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Accepts non-empty GUID content entry IDs and normalizes them as canonical lowercase D-format strings.
/// </summary>
public sealed class GuidContentEntryIdStrategy : IContentEntryIdStrategy<Guid>
{
    /// <inheritdoc />
    public bool TryNormalize(Guid id, out ContentEntryId normalizedId, out ContentFailure? failure)
    {
        if (id == Guid.Empty)
        {
            normalizedId = default;
            failure = ContentFailures.EntryIdInvalid("Entry ID cannot be an empty GUID.");
            return false;
        }

        normalizedId = new ContentEntryId(id.ToString("D"));
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public bool TryValidateNormalized(ContentEntryId id, out ContentFailure? failure)
    {
        if (!Guid.TryParseExact(id.Value, "D", out Guid parsed) || parsed == Guid.Empty)
        {
            failure = ContentFailures.EntryIdInvalid($"Entry ID '{id}' must be a non-empty canonical GUID.", id.ToString());
            return false;
        }

        if (!string.Equals(parsed.ToString("D"), id.Value, StringComparison.Ordinal))
        {
            failure = ContentFailures.EntryIdInvalid($"Entry ID '{id}' must use canonical lowercase GUID format.", id.ToString());
            return false;
        }

        failure = null;
        return true;
    }
}
