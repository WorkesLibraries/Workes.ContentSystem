using System;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests;

internal interface ITestPlainTextEntry : IContentEntry
{
    string PlainText { get; }
}

internal static class ContentTestText
{
    public static string Of(IContentEntry entry)
    {
        if (entry is PlainContentEntry plain)
        {
            return plain.PlainText;
        }

        if (entry is ITestPlainTextEntry custom)
        {
            return custom.PlainText;
        }

        throw new InvalidOperationException($"Test entry '{entry.GetType().Name}' has no test text.");
    }

    public static string Of(ContentEntryRecord record) => Of(record.Entry);
}
