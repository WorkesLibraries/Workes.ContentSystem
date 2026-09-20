using System;

namespace Workes.ContentSystem.Core;

internal static class ContentEntryIdStrategies
{
    public static IContentEntryIdStrategy<TId> GetDefault<TId>()
    {
        if (typeof(TId) == typeof(string))
        {
            return (IContentEntryIdStrategy<TId>)(object)new StringContentEntryIdStrategy();
        }

        if (typeof(TId) == typeof(long))
        {
            return (IContentEntryIdStrategy<TId>)(object)new IntegerContentEntryIdStrategy();
        }

        throw new NotSupportedException($"No default content entry ID strategy is registered for '{typeof(TId).FullName}'. Provide a custom strategy.");
    }
}
