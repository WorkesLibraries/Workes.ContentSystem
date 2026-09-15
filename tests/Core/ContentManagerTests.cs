using System;
using System.Collections.Generic;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentManagerTests
{
    [Test]
    public void Constructor_UsesProvidedBoundedBehavior()
    {
        var manager = new ContentManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(200)));

        ContentEntryRecord first = manager.Add(Entry("First"));
        ContentEntryRecord second = manager.Add(Entry("Second"));

        Assert.That(first.Id, Is.EqualTo(new ContentEntryId("1")));
        Assert.That(second.Id, Is.EqualTo(new ContentEntryId("2")));
        Assert.That(manager.Records, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void Constructor_AcceptsStructureAssignedIdStructure()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(1));
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
        var manager = new ContentManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(200)));

        Assert.Throws<ArgumentNullException>(() => manager.TryAdd(null!, out _, out _));
    }

    [Test]
    public void BaseManager_ExposesRecordsAndDelegatesLookup()
    {
        ContentManagerBase manager = new ContentManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(200)));
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
        var manager = new ContentManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(200)));
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

    [Test]
    public void BaseManager_TryClear_DelegatesToClearableStructure()
    {
        ContentManagerBase manager = new ContentManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord added = ((ContentManager)manager).Add(Entry("Stored"));

        bool cleared = manager.TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

        Assert.That(cleared, Is.True);
        Assert.That(removedRecords, Is.EqualTo(new[] { added }));
        Assert.That(failure, Is.Null);
        Assert.That(manager.Records, Is.Empty);
    }

    [Test]
    public void BaseManager_TryRemove_DelegatesToRemovalStructure()
    {
        ContentManagerBase manager = new ContentManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord added = ((ContentManager)manager).Add(Entry("Stored"));

        bool removed = manager.TryRemove(added.Id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.True);
        Assert.That(removedRecord, Is.SameAs(added));
        Assert.That(failure, Is.Null);
        Assert.That(manager.Records, Is.Empty);
    }

    [Test]
    public void ContentManagerFor_InfersNaturalIdManagerForSequenceStructure()
    {
        var manager = ContentManager.For(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord added = manager.Add(Entry("Stored"));

        ContentEntryRecord found = manager.Get(1);
        ContentEntryRecord removed = manager.Remove(1);

        Assert.That(found, Is.SameAs(added));
        Assert.That(removed, Is.SameAs(added));
        Assert.That(manager.Records, Is.Empty);
    }

    [Test]
    public void GenericContentManager_CanBeConstructedExplicitly()
    {
        var manager = new ContentManager<long>(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord added = manager.Add(Entry("Stored"));

        Assert.That(manager.Get(1), Is.SameAs(added));
    }

    [Test]
    public void BaseManager_TrySetStructureParameter_DelegatesToParameterizedStructure()
    {
        ContentManagerBase manager = new ContentManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ((ContentManager)manager).Add(Entry("First"));
        ContentEntryRecord second = ((ContentManager)manager).Add(Entry("Second"));

        bool changed = manager.TrySetStructureParameter(
            ContentSequenceStructure.OverflowPolicyParameterId,
            ContentOverflowPolicy.DropOldest(1),
            out IReadOnlyList<ContentEntryRecord> removedRecords,
            out ContentFailure? failure);

        Assert.That(changed, Is.True);
        Assert.That(removedRecords.Count, Is.EqualTo(1));
        Assert.That(failure, Is.Null);
        Assert.That(manager.Records, Is.EqualTo(new[] { second }));
        Assert.That(((IContentRetentionPolicyStructure)manager.Structure).OverflowPolicy, Is.EqualTo(ContentOverflowPolicy.DropOldest(1)));
    }

    [Test]
    public void BaseManager_UnsupportedMutationsReturnStructuredFailures()
    {
        ContentManagerBase manager = new ContentManager(new TestStructureAssignedIdContentStructure());

        bool cleared = manager.TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? clearFailure);
        bool removed = manager.TryRemove(new ContentEntryId("missing"), out ContentEntryRecord? removedRecord, out ContentFailure? removeFailure);
        bool changed = manager.TrySetStructureParameter(ContentSequenceStructure.OverflowPolicyParameterId, ContentOverflowPolicy.DropOldest(1), out IReadOnlyList<ContentEntryRecord> policyRemovedRecords, out ContentFailure? policyFailure);

        Assert.That(cleared, Is.False);
        Assert.That(removedRecords, Is.Empty);
        Assert.That(clearFailure, Is.Not.Null);
        Assert.That(clearFailure!.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
        Assert.That(removed, Is.False);
        Assert.That(removedRecord, Is.Null);
        Assert.That(removeFailure, Is.Not.Null);
        Assert.That(removeFailure!.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
        Assert.That(changed, Is.False);
        Assert.That(policyRemovedRecords, Is.Empty);
        Assert.That(policyFailure, Is.Not.Null);
        Assert.That(policyFailure!.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
    }

    [Test]
    public void BaseManager_UnsupportedThrowingMutationThrowsContentOperationException()
    {
        ContentManagerBase manager = new ContentManager(new TestStructureAssignedIdContentStructure());

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Clear());

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
    }

    [Test]
    public void BaseManager_SetStructureParameter_EmitsConfigurationChangedEvent()
    {
        ContentManagerBase manager = new ContentManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord first = ((ContentManager)manager).Add(Entry("First"));
        ((ContentManager)manager).Add(Entry("Second"));
        ContentChangedEventArgs? changedArgs = null;
        manager.Changed += (_, args) => changedArgs = args;

        var removed = manager.SetStructureParameter(
            ContentSequenceStructure.OverflowPolicyParameterId,
            ContentOverflowPolicy.DropOldest(1));

        Assert.That(removed, Is.EqualTo(new[] { first }));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.Kind, Is.EqualTo(ContentChangeKind.ConfigurationChanged));
        Assert.That(changedArgs.RemovedRecords, Is.EqualTo(new[] { first }));
        Assert.That(changedArgs.ConfigurationChanged.Single().Kind, Is.EqualTo(ContentConfigurationChangeKind.StructureParameter));
        Assert.That(changedArgs.ConfigurationChanged.Single().ConfigurationId, Is.EqualTo(ContentSequenceStructure.OverflowPolicyParameterId));
        Assert.That(changedArgs.ConfigurationChanged.Single().Value, Is.EqualTo(ContentOverflowPolicy.DropOldest(1)));
        Assert.That(changedArgs.ConfigurationChanged.Single().PreviousComponent, Is.TypeOf<ContentSequenceStructure>());
        Assert.That(changedArgs.ConfigurationChanged.Single().CurrentComponent, Is.TypeOf<ContentSequenceStructure>());
        Assert.That(changedArgs.RequiresFullRefresh, Is.True);
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
