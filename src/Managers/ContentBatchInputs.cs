using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

internal static class ContentBatchInputs
{
    public static IReadOnlyList<IContentEntry> ReadEntries(IEnumerable<IContentEntry> entries, string parameterName)
    {
        if (entries is null) throw new ArgumentNullException(parameterName);
        IContentEntry[] values = entries.ToArray();
        if (values.Any(entry => entry is null)) throw new ArgumentException("Entry batches cannot contain null values.", parameterName);
        return Array.AsReadOnly(values);
    }

    public static IReadOnlyList<KeyValuePair<TId, IContentEntry>> ReadMapEntries<TId>(IEnumerable<KeyValuePair<TId, IContentEntry>> entries, string parameterName)
    {
        if (entries is null) throw new ArgumentNullException(parameterName);
        KeyValuePair<TId, IContentEntry>[] values = entries.ToArray();
        if (values.Any(pair => pair.Value is null)) throw new ArgumentException("Entry batches cannot contain null values.", parameterName);
        return Array.AsReadOnly(values);
    }
}
