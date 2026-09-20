using System;
using System.Collections.Generic;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class KeyedContentManagerTests
{
    [Test]
    public void DefaultStringManager_UsesKeyedStrategy()
    {
        var manager = new KeyedContentManager<string>();

        ContentEntryRecord added = manager.Add("entry-1", Entry("First"));

        Assert.That(added.Id, Is.EqualTo(new ContentEntryId("entry-1")));
        Assert.That(manager.Get("entry-1"), Is.SameAs(added));
    }

    [Test]
    public void DefaultLongManager_UsesKeyedStrategy()
    {
        var manager = new KeyedContentManager<long>();

        ContentEntryRecord added = manager.Add(8, Entry("Eighth"));

        Assert.That(added.Id, Is.EqualTo(new ContentEntryId("8")));
        Assert.That(manager.Get(8), Is.SameAs(added));
    }

    [Test]
    public void Constructor_NullStrategyThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new KeyedContentManager<string>((IContentEntryIdStrategy<string>)null!));
    }

    [Test]
    public void Constructor_UnsupportedDefaultStrategyThrows()
    {
        Assert.Throws<NotSupportedException>(() => new KeyedContentManager<Guid>());
    }

    [Test]
    public void Constructor_NullStructureThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new KeyedContentManager<string>((KeyedContentStructure<string>)null!));
    }

    [Test]
    public void Changed_ForwardsKeyedStructureEventsWithManagerSender()
    {
        var manager = new KeyedContentManager<string>();
        ContentChangedEventArgs? changedArgs = null;
        object? sender = null;
        manager.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            changedArgs = args;
        };

        ContentEntryRecord added = manager.Add("entry", Entry("Stored"));

        Assert.That(sender, Is.SameAs(manager));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.AddedRecords, Is.EqualTo(new[] { added }));
    }

    [Test]
    public void KeyedManagerBase_DelegatesSharedKeyedOperations()
    {
        KeyedContentManagerBase<string> manager = new KeyedContentManager<string>();

        ContentEntryRecord added = manager.Add("entry", Entry("Stored"));
        bool found = manager.TryGet("entry", out ContentEntryRecord? foundRecord, out ContentFailure? getFailure);
        bool removed = manager.TryRemove("entry", out ContentEntryRecord? removedRecord, out ContentFailure? removeFailure);
        bool cleared = manager.TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? clearFailure);

        Assert.That(found, Is.True);
        Assert.That(foundRecord, Is.SameAs(added));
        Assert.That(getFailure, Is.Null);
        Assert.That(removed, Is.True);
        Assert.That(removedRecord, Is.SameAs(added));
        Assert.That(removeFailure, Is.Null);
        Assert.That(cleared, Is.True);
        Assert.That(removedRecords, Is.Empty);
        Assert.That(clearFailure, Is.Null);
    }

    [Test]
    public void TryAdd_DuplicateIdReturnsFailure()
    {
        var manager = new KeyedContentManager<string>();
        manager.Add("duplicate", Entry("First"));

        bool accepted = manager.TryAdd("duplicate", Entry("Second"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdDuplicate));
    }

    [Test]
    public void Add_InvalidIdThrowsContentOperationException()
    {
        var manager = new KeyedContentManager<string>();

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Add("   ", Entry("Invalid")));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void TryGet_MissingIdReturnsEntryNotFoundFailure()
    {
        var manager = new KeyedContentManager<string>();

        bool found = manager.TryGet("missing", out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(found, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void TryRemove_WithTypedId_RemovesRecord()
    {
        var manager = new KeyedContentManager<string>();
        ContentEntryRecord added = manager.Add("entry", Entry("Stored"));

        bool removed = manager.TryRemove("entry", out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.True);
        Assert.That(removedRecord, Is.SameAs(added));
        Assert.That(failure, Is.Null);
        Assert.That(manager.Records, Is.Empty);
    }

    [Test]
    public void TryRemove_DuplicateOrMissingSemanticsArePreserved()
    {
        var manager = new KeyedContentManager<string>();

        bool removed = manager.TryRemove("missing", out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.False);
        Assert.That(removedRecord, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Remove_InvalidIdThrowsContentOperationException()
    {
        var manager = new KeyedContentManager<string>();

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Remove("   "));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void Clear_UsesBaseManagerMutation()
    {
        var manager = new KeyedContentManager<string>();
        ContentEntryRecord first = manager.Add("first", Entry("First"));
        ContentEntryRecord second = manager.Add("second", Entry("Second"));

        var removed = manager.Clear();

        Assert.That(removed, Is.EqualTo(new[] { first, second }));
        Assert.That(manager.Records, Is.Empty);
    }

    [Test]
    public void Changed_ForwardsKeyedRemovalEventWithManagerSender()
    {
        var manager = new KeyedContentManager<string>();
        manager.Add("entry", Entry("Stored"));
        ContentChangedEventArgs? changedArgs = null;
        object? sender = null;
        manager.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            changedArgs = args;
        };

        manager.Remove("entry");

        Assert.That(sender, Is.SameAs(manager));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.Kind, Is.EqualTo(ContentChangeKind.Removed));
    }

    [Test]
    public void Get_MissingIdThrowsContentOperationException()
    {
        var manager = new KeyedContentManager<string>();

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Get("missing"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Add_NullEntryThrows()
    {
        var manager = new KeyedContentManager<string>();

        Assert.Throws<ArgumentNullException>(() => manager.Add("entry", null!));
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }

}
