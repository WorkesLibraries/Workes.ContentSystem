using System.Globalization;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Accepts positive integer content entry IDs and normalizes them as invariant decimal strings.
/// </summary>
public sealed class IntegerContentEntryIdStrategy : IContentEntryIdStrategy<long>
{
    /// <inheritdoc />
    public bool TryNormalize(long id, out ContentEntryId normalizedId, out ContentFailure? failure)
    {
        if (id <= 0)
        {
            normalizedId = default;
            failure = ContentFailures.EntryIdInvalid($"Entry ID '{id.ToString(CultureInfo.InvariantCulture)}' must be a positive integer.", id.ToString(CultureInfo.InvariantCulture));
            return false;
        }

        normalizedId = new ContentEntryId(id.ToString(CultureInfo.InvariantCulture));
        failure = null;
        return true;
    }
}
