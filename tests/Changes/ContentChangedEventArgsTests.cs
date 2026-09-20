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

        var configurationChanged = new ContentConfigurationChanged(
            ContentConfigurationChangeKind.StructureParameter,
            "overflowPolicy",
            ContentOverflowPolicy.DropOldest(1),
            ContentOverflowPolicy.None,
            ContentOverflowPolicy.DropOldest(1),
            requiresFullRefresh: true);

        var args = new ContentChangedEventArgs(
            new[] { added },
            new[] { removed },
            ContentChangeKind.ConfigurationChanged,
            cleared: false,
            new[] { configurationChanged });

        Assert.That(args.AddedRecords, Is.EqualTo(new[] { added }));
        Assert.That(args.RemovedRecords, Is.EqualTo(new[] { removed }));
        Assert.That(args.Kind, Is.EqualTo(ContentChangeKind.ConfigurationChanged));
        Assert.That(args.Cleared, Is.False);
        Assert.That(args.ConfigurationChanged, Is.EqualTo(new[] { configurationChanged }));
        Assert.That(args.RequiresFullRefresh, Is.True);
    }

    [Test]
    public void Constructor_NullCollectionsBecomeEmptyCollections()
    {
        var args = new ContentChangedEventArgs(null, null);

        Assert.That(args.AddedRecords, Is.Empty);
        Assert.That(args.RemovedRecords, Is.Empty);
        Assert.That(args.ConfigurationChanged, Is.Empty);
        Assert.That(args.Kind, Is.EqualTo(ContentChangeKind.Unknown));
        Assert.That(args.Cleared, Is.False);
        Assert.That(args.RequiresFullRefresh, Is.False);
    }

    [Test]
    public void Constructor_ClearedImpliesFullRefresh()
    {
        var args = new ContentChangedEventArgs(cleared: true);

        Assert.That(args.Cleared, Is.True);
        Assert.That(args.RequiresFullRefresh, Is.True);
    }

    [Test]
    public void Constructor_NullConfigurationChangesBecomeEmptyCollection()
    {
        var args = new ContentChangedEventArgs(configurationChanged: null);

        Assert.That(args.ConfigurationChanged, Is.Empty);
    }

    [Test]
    public void ContentConfigurationChanged_StoresValues()
    {
        ContentOverflowPolicy previous = ContentOverflowPolicy.None;
        ContentOverflowPolicy current = ContentOverflowPolicy.DropOldest(5);

        var change = new ContentConfigurationChanged(
            ContentConfigurationChangeKind.StructureParameter,
            "overflowPolicy",
            current,
            previous,
            current,
            requiresFullRefresh: true);

        Assert.That(change.Kind, Is.EqualTo(ContentConfigurationChangeKind.StructureParameter));
        Assert.That(change.ConfigurationId, Is.EqualTo("overflowPolicy"));
        Assert.That(change.Value, Is.SameAs(current));
        Assert.That(change.PreviousComponent, Is.SameAs(previous));
        Assert.That(change.CurrentComponent, Is.SameAs(current));
        Assert.That(change.RequiresFullRefresh, Is.True);
    }

    private static ContentEntryRecord Record(string id, string text)
    {
        return new ContentEntryRecord(
            new ContentEntryId(id),
            new PlainContentEntry(DateTimeOffset.UtcNow, text));
    }
}
