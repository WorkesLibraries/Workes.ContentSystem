using System;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class BoundedFifoContentStructureTests
{
    [Test]
    public void Constructor_UsesDefaultCapacity()
    {
        var structure = new BoundedFifoContentStructure();

        Assert.That(structure.Capacity, Is.EqualTo(200));
        Assert.That(structure.Count, Is.EqualTo(0));
        Assert.That(structure.Records, Is.Empty);
    }

    [Test]
    public void Constructor_StoresCustomCapacity()
    {
        var structure = new BoundedFifoContentStructure(3);

        Assert.That(structure.Capacity, Is.EqualTo(3));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_InvalidCapacityThrows(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedFifoContentStructure(capacity));
    }

    [Test]
    public void Add_ReturnsRecordWithSequentialStructureAssignedId()
    {
        var structure = new BoundedFifoContentStructure(3);

        ContentEntryRecord first = structure.Add(Entry("First"));
        ContentEntryRecord second = structure.Add(Entry("Second"));

        Assert.That(first.Id, Is.EqualTo(new ContentEntryId("1")));
        Assert.That(second.Id, Is.EqualTo(new ContentEntryId("2")));
        Assert.That(first.PlainText, Is.EqualTo("First"));
        Assert.That(second.PlainText, Is.EqualTo("Second"));
    }

    [Test]
    public void TryAdd_ReturnsRecordWithSequentialStructureAssignedId()
    {
        var structure = new BoundedFifoContentStructure(3);

        bool accepted = structure.TryAdd(Entry("First"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(record, Is.Not.Null);
        Assert.That(record!.Id, Is.EqualTo(new ContentEntryId("1")));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void Add_EmitsChangedWithAddedRecord()
    {
        var structure = new BoundedFifoContentStructure(3);
        ContentChangedEventArgs? changedArgs = null;
        object? sender = null;
        structure.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            changedArgs = args;
        };

        ContentEntryRecord record = structure.Add(Entry("First"));

        Assert.That(sender, Is.SameAs(structure));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.AddedRecords, Is.EqualTo(new[] { record }));
        Assert.That(changedArgs.RemovedRecords, Is.Empty);
    }

    [Test]
    public void Add_WhenCapacityExceeded_EmitsOneChangedEventWithAddedAndRemovedRecords()
    {
        var structure = new BoundedFifoContentStructure(1);
        ContentEntryRecord removed = structure.Add(Entry("First"));
        int eventCount = 0;
        ContentChangedEventArgs? changedArgs = null;
        structure.Changed += (_, args) =>
        {
            eventCount++;
            changedArgs = args;
        };

        ContentEntryRecord added = structure.Add(Entry("Second"));

        Assert.That(eventCount, Is.EqualTo(1));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.AddedRecords, Is.EqualTo(new[] { added }));
        Assert.That(changedArgs.RemovedRecords, Is.EqualTo(new[] { removed }));
    }

    [Test]
    public void Add_NullEntryEmitsNoEventBeforeThrowing()
    {
        var structure = new BoundedFifoContentStructure(2);
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        Assert.Throws<ArgumentNullException>(() => structure.Add(null!));

        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void Changed_UnsubscribedHandlerIsNotCalled()
    {
        var structure = new BoundedFifoContentStructure(2);
        int eventCount = 0;
        EventHandler<ContentChangedEventArgs> handler = (_, _) => eventCount++;

        structure.Changed += handler;
        structure.Changed -= handler;
        structure.Add(Entry("First"));

        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void Records_AreReadOldestToNewest()
    {
        var structure = new BoundedFifoContentStructure(3);

        structure.Add(Entry("First"));
        structure.Add(Entry("Second"));
        structure.Add(Entry("Third"));

        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "First", "Second", "Third" }));
    }

    [Test]
    public void Add_WhenCapacityExceeded_DropsOldestRetainedRecord()
    {
        var structure = new BoundedFifoContentStructure(2);

        ContentEntryRecord first = structure.Add(Entry("First"));
        ContentEntryRecord second = structure.Add(Entry("Second"));
        ContentEntryRecord third = structure.Add(Entry("Third"));

        Assert.That(structure.Count, Is.EqualTo(2));
        Assert.That(structure.Records, Is.EqualTo(new[] { second, third }));
        Assert.That(structure.TryGet(first.Id, out _, out ContentFailure? failure), Is.False);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Add_AfterOverflow_ContinuesIncreasingIds()
    {
        var structure = new BoundedFifoContentStructure(2);

        structure.Add(Entry("First"));
        structure.Add(Entry("Second"));
        ContentEntryRecord third = structure.Add(Entry("Third"));

        Assert.That(third.Id, Is.EqualTo(new ContentEntryId("3")));
    }

    [Test]
    public void TryGet_WhenRecordIsRetained_ReturnsRecord()
    {
        var structure = new BoundedFifoContentStructure(2);
        ContentEntryRecord record = structure.Add(Entry("First"));

        bool found = structure.TryGet(record.Id, out ContentEntryRecord? foundRecord, out ContentFailure? failure);

        Assert.That(found, Is.True);
        Assert.That(foundRecord, Is.SameAs(record));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void TryGet_WithNumericId_ReturnsRecord()
    {
        var structure = new BoundedFifoContentStructure(2);
        ContentEntryRecord record = structure.Add(Entry("First"));

        bool found = structure.TryGet(1, out ContentEntryRecord? foundRecord, out ContentFailure? failure);

        Assert.That(found, Is.True);
        Assert.That(foundRecord, Is.SameAs(record));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void TryGet_WhenRecordIsMissing_ReturnsEntryNotFoundFailure()
    {
        var structure = new BoundedFifoContentStructure(2);

        bool found = structure.TryGet(new ContentEntryId("missing"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(found, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Kind, Is.EqualTo(ContentFailureKind.Entry));
        Assert.That(failure.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Get_WhenRecordIsMissing_ThrowsContentOperationException()
    {
        var structure = new BoundedFifoContentStructure(2);

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => structure.Get(new ContentEntryId("missing")));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Kind, Is.EqualTo(ContentFailureKind.Entry));
        Assert.That(exception.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Get_WithNumericId_ReturnsRecord()
    {
        var structure = new BoundedFifoContentStructure(2);
        ContentEntryRecord record = structure.Add(Entry("First"));

        ContentEntryRecord found = structure.Get(1);

        Assert.That(found, Is.SameAs(record));
    }

    [Test]
    public void Add_NullEntryThrows()
    {
        var structure = new BoundedFifoContentStructure(2);

        Assert.Throws<ArgumentNullException>(() => structure.Add(null!));
    }

    [Test]
    public void TryAdd_NullEntryThrows()
    {
        var structure = new BoundedFifoContentStructure(2);

        Assert.Throws<ArgumentNullException>(() => structure.TryAdd(null!, out _, out _));
    }

    [Test]
    public void TryGet_DefaultIdThrows()
    {
        var structure = new BoundedFifoContentStructure(2);

        Assert.Throws<ArgumentException>(() => structure.TryGet(default(ContentEntryId), out _, out _));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void TryGet_InvalidNumericIdThrows(long id)
    {
        var structure = new BoundedFifoContentStructure(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => structure.TryGet(id, out _, out _));
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }
}
