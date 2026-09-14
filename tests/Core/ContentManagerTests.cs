using System;
using System.Collections.Generic;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentManagerTests
{
    [Test]
    public void Constructor_UsesProvidedFifoBehavior()
    {
        var manager = new ContentManager(new BoundedFifoContentStructure());

        ContentEntryRecord first = manager.Add(Entry("First"));
        ContentEntryRecord second = manager.Add(Entry("Second"));

        Assert.That(first.Id, Is.EqualTo(new ContentEntryId("1")));
        Assert.That(second.Id, Is.EqualTo(new ContentEntryId("2")));
        Assert.That(manager.Records, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void Constructor_AcceptsStructureAssignedIdStructure()
    {
        var structure = new BoundedFifoContentStructure(capacity: 1);
        var manager = new ContentManager(structure);

        ContentEntryRecord record = manager.Add(Entry("Entry"));

        Assert.That(manager.Structure, Is.SameAs(structure));
        Assert.That(manager.Records, Is.EqualTo(new[] { record }));
    }

    [Test]
    public void Constructor_NullStructureThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentManager(null!));
    }

    [Test]
    public void TryAdd_DelegatesToStructure()
    {
        var structure = new TestStructureAssignedIdContentStructure();
        var manager = new ContentManager(structure);

        bool accepted = manager.TryAdd(Entry("Accepted"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(record, Is.SameAs(structure.LastRecord));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void Add_WhenStructureRejects_ThrowsContentOperationException()
    {
        var manager = new ContentManager(new RejectingStructureAssignedIdContentStructure());

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Add(Entry("Rejected")));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.StructureRejected));
    }

    [Test]
    public void TryAdd_NullEntryThrows()
    {
        var manager = new ContentManager(new BoundedFifoContentStructure());

        Assert.Throws<ArgumentNullException>(() => manager.TryAdd(null!, out _, out _));
    }

    [Test]
    public void BaseManager_ExposesRecordsAndDelegatesLookup()
    {
        ContentManagerBase manager = new ContentManager(new BoundedFifoContentStructure());
        ContentEntryRecord added = ((ContentManager)manager).Add(Entry("Stored"));

        bool found = manager.TryGet(added.Id, out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(manager.Records, Is.EqualTo(new[] { added }));
        Assert.That(found, Is.True);
        Assert.That(record, Is.SameAs(added));
        Assert.That(failure, Is.Null);
        Assert.That(manager.Get(added.Id), Is.SameAs(added));
    }

    [Test]
    public void Changed_ForwardsStructureEventsWithManagerSender()
    {
        var manager = new ContentManager(new BoundedFifoContentStructure());
        ContentChangedEventArgs? changedArgs = null;
        object? sender = null;
        manager.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            changedArgs = args;
        };

        ContentEntryRecord added = manager.Add(Entry("Stored"));

        Assert.That(sender, Is.SameAs(manager));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.AddedRecords, Is.EqualTo(new[] { added }));
    }

    [Test]
    public void Changed_WithNonHookStructureEmitsNoEvents()
    {
        var manager = new ContentManager(new TestStructureAssignedIdContentStructure());
        int eventCount = 0;
        manager.Changed += (_, _) => eventCount++;

        manager.Add(Entry("Stored"));

        Assert.That(eventCount, Is.EqualTo(0));
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }

    private sealed class TestStructureAssignedIdContentStructure : IStructureAssignedIdContentStructure
    {
        private readonly List<ContentEntryRecord> _records = new List<ContentEntryRecord>();

        public ContentEntryRecord? LastRecord { get; private set; }

        public IReadOnlyList<ContentEntryRecord> Records => _records.ToArray();

        public bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            record = new ContentEntryRecord(new ContentEntryId("custom-" + (_records.Count + 1)), entry);
            _records.Add(record);
            LastRecord = record;
            failure = null;
            return true;
        }

        public ContentEntryRecord Add(IContentEntry entry)
        {
            if (TryAdd(entry, out ContentEntryRecord? record, out ContentFailure? failure))
            {
                return record!;
            }

            throw new ContentOperationException(failure!);
        }

        public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            foreach (ContentEntryRecord candidate in _records)
            {
                if (candidate.Id == id)
                {
                    record = candidate;
                    failure = null;
                    return true;
                }
            }

            record = null;
            failure = ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryNotFound, "Missing.");
            return false;
        }

        public ContentEntryRecord Get(ContentEntryId id)
        {
            if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure))
            {
                return record!;
            }

            throw new ContentOperationException(failure!);
        }
    }

    private sealed class RejectingStructureAssignedIdContentStructure : IStructureAssignedIdContentStructure
    {
        public IReadOnlyList<ContentEntryRecord> Records => Array.Empty<ContentEntryRecord>();

        public bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            record = null;
            failure = ContentFailure.Create(ContentFailureKind.Structure, ContentFailureCodes.StructureRejected, "Rejected.");
            return false;
        }

        public ContentEntryRecord Add(IContentEntry entry)
        {
            if (TryAdd(entry, out ContentEntryRecord? record, out ContentFailure? failure))
            {
                return record!;
            }

            throw new ContentOperationException(failure!);
        }

        public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            record = null;
            failure = ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryNotFound, "Missing.");
            return false;
        }

        public ContentEntryRecord Get(ContentEntryId id)
        {
            throw new ContentOperationException(ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryNotFound, "Missing."));
        }
    }
}
