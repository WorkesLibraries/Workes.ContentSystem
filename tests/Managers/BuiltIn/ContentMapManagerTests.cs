using System;
using System.Collections.Generic;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentMapManagerTests
{
    [Test]
    public void DefaultStringManager_UsesMapStrategy()
    {
        var manager = new ContentMapManager<string>();

        ContentEntryRecord added = manager.Add("entry-1", Entry("First"));

        Assert.That(added.Id, Is.EqualTo(new ContentEntryId("entry-1")));
        Assert.That(manager.Get("entry-1"), Is.SameAs(added));
    }

    [Test]
    public void DefaultLongManager_UsesMapStrategy()
    {
        var manager = new ContentMapManager<long>();

        ContentEntryRecord added = manager.Add(8, Entry("Eighth"));

        Assert.That(added.Id, Is.EqualTo(new ContentEntryId("8")));
        Assert.That(manager.Get(8), Is.SameAs(added));
    }

    [Test]
    public void Constructor_NullStrategyThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentMapManager<string>((IContentEntryIdStrategy<string>)null!));
    }

    [Test]
    public void DefaultGuidManager_UsesMapStrategy()
    {
        var manager = new ContentMapManager<Guid>();
        Guid id = Guid.Parse("9fd3efda-747d-4a60-81c1-c38ed2d60774");

        ContentEntryRecord added = manager.Add(id, Entry("Guid"));

        Assert.That(added.Id, Is.EqualTo(new ContentEntryId("9fd3efda-747d-4a60-81c1-c38ed2d60774")));
        Assert.That(manager.Get(id), Is.SameAs(added));
    }

    [Test]
    public void DefaultContentEntryIdManager_UsesMapStrategy()
    {
        var manager = new ContentMapManager<ContentEntryId>();
        var id = new ContentEntryId("stored");

        ContentEntryRecord added = manager.Add(id, Entry("Stored"));

        Assert.That(added.Id, Is.EqualTo(id));
        Assert.That(manager.Get(id), Is.SameAs(added));
    }

    [Test]
    public void Constructor_NullStructureThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentMapManager<string>((ContentMapStructure<string>)null!));
    }

    [Test]
    public void Changed_ForwardsMapStructureEventsWithManagerSender()
    {
        var manager = new ContentMapManager<string>();
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
    public void MapManagerBase_DelegatesSharedMapOperations()
    {
        ContentMapManagerBase<string> manager = new ContentMapManager<string>();

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
    public void MapManagerBase_ContainsUsesTypedId()
    {
        ContentMapManagerBase<string> manager = new ContentMapManager<string>();
        manager.Add("entry", Entry("Stored"));

        Assert.That(manager.Contains("entry"), Is.True);
        Assert.That(manager.Contains("missing"), Is.False);
    }

    [Test]
    public void GetOrSet_ExistingRecordDoesNotInvokeFactoryOrEmitEvent()
    {
        var manager = new ContentMapManager<string>();
        ContentEntryRecord existing = manager.Add("entry", Entry("Existing"));
        int factoryCalls = 0;
        int eventCount = 0;
        manager.Changed += (_, _) => eventCount++;

        bool accepted = manager.TryGetOrSet(
            "entry",
            () =>
            {
                factoryCalls++;
                return Entry("Created");
            },
            out ContentEntryRecord? record,
            out bool added,
            out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(record, Is.SameAs(existing));
        Assert.That(added, Is.False);
        Assert.That(failure, Is.Null);
        Assert.That(factoryCalls, Is.EqualTo(0));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void GetOrSet_MissingRecordInvokesFactoryAndAddsRecord()
    {
        var manager = new ContentMapManager<string>();
        int factoryCalls = 0;
        ContentChangedEventArgs? changedArgs = null;
        manager.Changed += (_, args) => changedArgs = args;

        ContentEntryRecord record = manager.GetOrSet(
            "entry",
            () =>
            {
                factoryCalls++;
                return Entry("Created");
            });

        Assert.That(record.Id, Is.EqualTo(new ContentEntryId("entry")));
        Assert.That(record.PlainText, Is.EqualTo("Created"));
        Assert.That(factoryCalls, Is.EqualTo(1));
        Assert.That(changedArgs?.Kind, Is.EqualTo(ContentChangeKind.Added));
        Assert.That(changedArgs?.AddedRecords, Is.EqualTo(new[] { record }));
    }

    [Test]
    public void TryGetOrSet_InvalidIdReturnsFailureWithoutInvokingFactory()
    {
        var manager = new ContentMapManager<string>();
        int factoryCalls = 0;

        bool accepted = manager.TryGetOrSet(
            "   ",
            () =>
            {
                factoryCalls++;
                return Entry("Created");
            },
            out ContentEntryRecord? record,
            out bool added,
            out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(added, Is.False);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
        Assert.That(factoryCalls, Is.EqualTo(0));
    }

    [Test]
    public void TryGetOrSet_NullFactoryThrows()
    {
        var manager = new ContentMapManager<string>();

        Assert.Throws<ArgumentNullException>(() => manager.TryGetOrSet("entry", null!, out _, out _, out _));
    }

    [Test]
    public void TryAdd_DuplicateIdReturnsFailure()
    {
        var manager = new ContentMapManager<string>();
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
        var manager = new ContentMapManager<string>();

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Add("   ", Entry("Invalid")));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void TryGet_MissingIdReturnsEntryNotFoundFailure()
    {
        var manager = new ContentMapManager<string>();

        bool found = manager.TryGet("missing", out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(found, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void TryRemove_WithTypedId_RemovesRecord()
    {
        var manager = new ContentMapManager<string>();
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
        var manager = new ContentMapManager<string>();

        bool removed = manager.TryRemove("missing", out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.False);
        Assert.That(removedRecord, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Remove_InvalidIdThrowsContentOperationException()
    {
        var manager = new ContentMapManager<string>();

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Remove("   "));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void Clear_UsesBaseManagerMutation()
    {
        var manager = new ContentMapManager<string>();
        ContentEntryRecord first = manager.Add("first", Entry("First"));
        ContentEntryRecord second = manager.Add("second", Entry("Second"));

        var removed = manager.Clear();

        Assert.That(removed, Is.EqualTo(new[] { first, second }));
        Assert.That(manager.Records, Is.Empty);
    }

    [Test]
    public void Changed_ForwardsMapRemovalEventWithManagerSender()
    {
        var manager = new ContentMapManager<string>();
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
        var manager = new ContentMapManager<string>();

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Get("missing"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Add_NullEntryThrows()
    {
        var manager = new ContentMapManager<string>();

        Assert.Throws<ArgumentNullException>(() => manager.Add("entry", null!));
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }

}
