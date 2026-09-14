using System;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentChangedEventArgsTests
{
    [Test]
    public void Constructor_StoresAddedAndRemovedRecords()
    {
        ContentEntryRecord added = Record("added", "Added");
        ContentEntryRecord removed = Record("removed", "Removed");

        var args = new ContentChangedEventArgs(new[] { added }, new[] { removed });

        Assert.That(args.AddedRecords, Is.EqualTo(new[] { added }));
        Assert.That(args.RemovedRecords, Is.EqualTo(new[] { removed }));
    }

    [Test]
    public void Constructor_NullCollectionsBecomeEmptyCollections()
    {
        var args = new ContentChangedEventArgs(null, null);

        Assert.That(args.AddedRecords, Is.Empty);
        Assert.That(args.RemovedRecords, Is.Empty);
    }

    private static ContentEntryRecord Record(string id, string text)
    {
        return new ContentEntryRecord(
            new ContentEntryId(id),
            new PlainContentEntry(DateTimeOffset.UtcNow, text));
    }
}
