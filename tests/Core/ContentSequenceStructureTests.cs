using System;
using System.Collections.Generic;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentSequenceStructureTests
{
    [Test]
    public void ContentSequenceStructure_ImplementsRetentionPolicyContract()
    {
        ContentOverflowPolicy overflowPolicy = ContentOverflowPolicy.DropOldest(3);

        IContentRetentionPolicyStructure structure = new ContentSequenceStructure(overflowPolicy);

        Assert.That(structure.OverflowPolicy, Is.EqualTo(overflowPolicy));
    }

    [Test]
    public void ContentSequenceStructure_ImplementsReadOrderContract()
    {
        IContentReadOrderStructure structure = new ContentSequenceStructure(
            ContentOverflowPolicy.None,
            ContentSequenceReadOrder.NewestFirst);

        Assert.That(structure.ReadOrder, Is.EqualTo(ContentSequenceReadOrder.NewestFirst));
    }

    [Test]
    public void CustomStructure_CanImplementRetentionAndReadOrderContracts()
    {
        ContentOverflowPolicy overflowPolicy = ContentOverflowPolicy.DropOldest(5);
        var structure = new TestConfiguredStructure(
            overflowPolicy,
            ContentSequenceReadOrder.NewestFirst);

        IContentRetentionPolicyStructure retention = structure;
        IContentReadOrderStructure readOrder = structure;

        Assert.That(retention.OverflowPolicy, Is.EqualTo(overflowPolicy));
        Assert.That(readOrder.ReadOrder, Is.EqualTo(ContentSequenceReadOrder.NewestFirst));
    }

    [Test]
    public void CustomStructure_CanImplementMutationContracts()
    {
        var structure = new TestMutableStructure(ContentOverflowPolicy.None);

        Assert.That(structure, Is.InstanceOf<IContentClearableStructure>());
        Assert.That(structure, Is.InstanceOf<IContentRecordRemovalStructure>());
        Assert.That(structure, Is.InstanceOf<IParameterizedContentStructure>());
    }

    [Test]
    public void Constructor_StoresConfiguration()
    {
        ContentOverflowPolicy overflowPolicy = ContentOverflowPolicy.DropOldest(3);

        var structure = new ContentSequenceStructure(
            overflowPolicy,
            ContentSequenceReadOrder.NewestFirst);

        Assert.That(structure.ReadOrder, Is.EqualTo(ContentSequenceReadOrder.NewestFirst));
        Assert.That(structure.OverflowPolicy, Is.EqualTo(overflowPolicy));
        Assert.That(structure.Count, Is.EqualTo(0));
        Assert.That(structure.Records, Is.Empty);
    }

    [Test]
    public void Constructor_NullOverflowPolicyThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentSequenceStructure(null!));
    }

    [Test]
    public void Constructor_InvalidReadOrderThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentSequenceStructure(
            ContentOverflowPolicy.None,
            (ContentSequenceReadOrder)99));
    }

    [Test]
    public void OverflowPolicy_None_RetainsAllRecords()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);

        structure.Add(Entry("First"));
        structure.Add(Entry("Second"));
        structure.Add(Entry("Third"));

        Assert.That(structure.Count, Is.EqualTo(3));
        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "First", "Second", "Third" }));
        Assert.That(structure.OverflowPolicy.Kind, Is.EqualTo(ContentOverflowPolicyKind.None));
        Assert.That(structure.OverflowPolicy.Capacity, Is.Null);
    }

    [Test]
    public void OverflowPolicy_DropOldest_StoresCapacity()
    {
        ContentOverflowPolicy policy = ContentOverflowPolicy.DropOldest(3);

        Assert.That(policy.Kind, Is.EqualTo(ContentOverflowPolicyKind.DropOldest));
        Assert.That(policy.Capacity, Is.EqualTo(3));
        Assert.That(policy.ToString(), Is.EqualTo("DropOldest(3)"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void OverflowPolicy_DropOldest_InvalidCapacityThrows(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ContentOverflowPolicy.DropOldest(capacity));
    }

    [Test]
    public void OverflowPolicy_EqualityIsValueBased()
    {
        ContentOverflowPolicy none = ContentOverflowPolicy.None;
        ContentOverflowPolicy sameNone = ContentOverflowPolicy.None;
        ContentOverflowPolicy bounded = ContentOverflowPolicy.DropOldest(2);
        ContentOverflowPolicy sameBounded = ContentOverflowPolicy.DropOldest(2);
        ContentOverflowPolicy differentBounded = ContentOverflowPolicy.DropOldest(3);

        Assert.That(none, Is.EqualTo(sameNone));
        Assert.That(bounded, Is.EqualTo(sameBounded));
        Assert.That(bounded.GetHashCode(), Is.EqualTo(sameBounded.GetHashCode()));
        Assert.That(bounded, Is.Not.EqualTo(differentBounded));
        Assert.That(bounded, Is.Not.EqualTo(none));
    }

    [Test]
    public void Add_ReturnsRecordWithSequentialStructureAssignedId()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(3));

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
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(3));

        bool accepted = structure.TryAdd(Entry("First"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(record, Is.Not.Null);
        Assert.That(record!.Id, Is.EqualTo(new ContentEntryId("1")));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void Add_EmitsChangedWithAddedRecord()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(3));
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
        Assert.That(changedArgs.Kind, Is.EqualTo(ContentChangeKind.Added));
    }

    [Test]
    public void Add_WhenDropOldestCapacityExceeded_EmitsOneChangedEventWithAddedAndRemovedRecords()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(1));
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
        Assert.That(changedArgs.Kind, Is.EqualTo(ContentChangeKind.Added));
    }

    [Test]
    public void Clear_RemovesAllRecordsEmitsEventAndPreservesNextGeneratedId()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        ContentEntryRecord first = structure.Add(Entry("First"));
        ContentEntryRecord second = structure.Add(Entry("Second"));
        ContentChangedEventArgs? changedArgs = null;
        structure.Changed += (_, args) => changedArgs = args;

        var removed = structure.Clear();

        Assert.That(removed, Is.EqualTo(new[] { first, second }));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.RemovedRecords, Is.EqualTo(new[] { first, second }));
        Assert.That(changedArgs.Kind, Is.EqualTo(ContentChangeKind.Cleared));
        Assert.That(changedArgs.Cleared, Is.True);
        Assert.That(changedArgs.RequiresFullRefresh, Is.True);

        ContentEntryRecord third = structure.Add(Entry("Third"));

        Assert.That(third.Id, Is.EqualTo(new ContentEntryId("3")));
    }

    [Test]
    public void Clear_WhenEmptyEmitsNoEvent()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        var removed = structure.Clear();

        Assert.That(removed, Is.Empty);
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void TryRemove_WithContentEntryId_RemovesRecordAndEmitsEvent()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        ContentEntryRecord first = structure.Add(Entry("First"));
        ContentEntryRecord second = structure.Add(Entry("Second"));
        ContentChangedEventArgs? changedArgs = null;
        structure.Changed += (_, args) => changedArgs = args;

        bool removed = structure.TryRemove(first.Id, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.True);
        Assert.That(removedRecord, Is.SameAs(first));
        Assert.That(failure, Is.Null);
        Assert.That(structure.Records, Is.EqualTo(new[] { second }));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.Kind, Is.EqualTo(ContentChangeKind.Removed));
        Assert.That(changedArgs.RemovedRecords, Is.EqualTo(new[] { first }));
    }

    [Test]
    public void TryRemove_WithNumericId_RemovesRecord()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        ContentEntryRecord first = structure.Add(Entry("First"));
        ContentEntryRecord second = structure.Add(Entry("Second"));

        bool removed = structure.TryRemove(2, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.True);
        Assert.That(removedRecord, Is.SameAs(second));
        Assert.That(failure, Is.Null);
        Assert.That(structure.Records, Is.EqualTo(new[] { first }));
    }

    [Test]
    public void TryRemove_WhenMissingReturnsEntryNotFoundAndEmitsNoEvent()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        bool removed = structure.TryRemove(new ContentEntryId("missing"), out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.False);
        Assert.That(removedRecord, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void Remove_WhenMissingThrowsContentOperationException()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => structure.Remove(new ContentEntryId("missing")));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void TryCreateWithParameter_FromNoneToDropOldest_CreatesReplacementAndTrimsOldestRecords()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        ContentEntryRecord first = structure.Add(Entry("First"));
        ContentEntryRecord second = structure.Add(Entry("Second"));
        ContentEntryRecord third = structure.Add(Entry("Third"));

        bool changed = structure.TryCreateWithParameter(
            ContentSequenceStructure.OverflowPolicyParameterId,
            ContentOverflowPolicy.DropOldest(2),
            out IContentStructure? replacement,
            out var removedRecords,
            out ContentFailure? failure);

        Assert.That(changed, Is.True);
        Assert.That(failure, Is.Null);
        Assert.That(replacement, Is.TypeOf<ContentSequenceStructure>());
        Assert.That(removedRecords, Is.EqualTo(new[] { first }));
        Assert.That(structure.Records, Is.EqualTo(new[] { first, second, third }));
        Assert.That(replacement!.Records, Is.EqualTo(new[] { second, third }));
        Assert.That(((IContentRetentionPolicyStructure)replacement).OverflowPolicy, Is.EqualTo(ContentOverflowPolicy.DropOldest(2)));
    }

    [Test]
    public void TrySetStructureParameter_ToNoneStopsFutureOverflow()
    {
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(1)));
        ContentEntryRecord first = manager.Add(Entry("First"));

        manager.SetStructureParameter(ContentSequenceStructure.OverflowPolicyParameterId, ContentOverflowPolicy.None);
        ContentEntryRecord second = manager.Add(Entry("Second"));

        Assert.That(manager.Records, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void TrySetStructureParameter_SamePolicyEmitsNoEvent()
    {
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2)));
        int eventCount = 0;
        manager.Changed += (_, _) => eventCount++;

        bool changed = manager.TrySetStructureParameter(
            ContentSequenceStructure.OverflowPolicyParameterId,
            ContentOverflowPolicy.DropOldest(2),
            out var removedRecords,
            out ContentFailure? failure);

        Assert.That(changed, Is.True);
        Assert.That(removedRecords, Is.Empty);
        Assert.That(failure, Is.Null);
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void TryCreateWithParameter_NullPolicyReturnsFailureAndDoesNotAlterRecords()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        ContentEntryRecord record = structure.Add(Entry("First"));

        bool changed = structure.TryCreateWithParameter(
            ContentSequenceStructure.OverflowPolicyParameterId,
            null,
            out IContentStructure? replacement,
            out var removedRecords,
            out ContentFailure? failure);

        Assert.That(changed, Is.False);
        Assert.That(replacement, Is.Null);
        Assert.That(removedRecords, Is.Empty);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.ConfigurationRejected));
        Assert.That(structure.Records, Is.EqualTo(new[] { record }));
        Assert.That(structure.OverflowPolicy, Is.EqualTo(ContentOverflowPolicy.None));
    }

    [Test]
    public void Add_NullEntryEmitsNoEventBeforeThrowing()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        Assert.Throws<ArgumentNullException>(() => structure.Add(null!));

        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void Changed_UnsubscribedHandlerIsNotCalled()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));
        int eventCount = 0;
        EventHandler<ContentChangedEventArgs> handler = (_, _) => eventCount++;

        structure.Changed += handler;
        structure.Changed -= handler;
        structure.Add(Entry("First"));

        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void Records_WithOldestFirstReadOrder_AreReadOldestToNewest()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(3), ContentSequenceReadOrder.OldestFirst);

        structure.Add(Entry("First"));
        structure.Add(Entry("Second"));
        structure.Add(Entry("Third"));

        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "First", "Second", "Third" }));
    }

    [Test]
    public void Records_WithNewestFirstReadOrder_AreReadNewestToOldest()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(3), ContentSequenceReadOrder.NewestFirst);

        structure.Add(Entry("First"));
        structure.Add(Entry("Second"));
        structure.Add(Entry("Third"));

        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "Third", "Second", "First" }));
    }

    [Test]
    public void Add_WhenDropOldestCapacityExceeded_DropsChronologicalOldestRetainedRecord()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2), ContentSequenceReadOrder.NewestFirst);

        ContentEntryRecord first = structure.Add(Entry("First"));
        ContentEntryRecord second = structure.Add(Entry("Second"));
        ContentEntryRecord third = structure.Add(Entry("Third"));

        Assert.That(structure.Count, Is.EqualTo(2));
        Assert.That(structure.Records, Is.EqualTo(new[] { third, second }));
        Assert.That(structure.TryGet(first.Id, out _, out ContentFailure? failure), Is.False);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Add_AfterOverflow_ContinuesIncreasingIds()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));

        structure.Add(Entry("First"));
        structure.Add(Entry("Second"));
        ContentEntryRecord third = structure.Add(Entry("Third"));

        Assert.That(third.Id, Is.EqualTo(new ContentEntryId("3")));
    }

    [Test]
    public void TryGet_WhenRecordIsRetained_ReturnsRecord()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));
        ContentEntryRecord record = structure.Add(Entry("First"));

        bool found = structure.TryGet(record.Id, out ContentEntryRecord? foundRecord, out ContentFailure? failure);

        Assert.That(found, Is.True);
        Assert.That(foundRecord, Is.SameAs(record));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void TryGet_WithNumericId_ReturnsRecord()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));
        ContentEntryRecord record = structure.Add(Entry("First"));

        bool found = structure.TryGet(1, out ContentEntryRecord? foundRecord, out ContentFailure? failure);

        Assert.That(found, Is.True);
        Assert.That(foundRecord, Is.SameAs(record));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void TryGet_WhenRecordIsMissing_ReturnsEntryNotFoundFailure()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));

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
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => structure.Get(new ContentEntryId("missing")));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Kind, Is.EqualTo(ContentFailureKind.Entry));
        Assert.That(exception.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Get_WithNumericId_ReturnsRecord()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));
        ContentEntryRecord record = structure.Add(Entry("First"));

        ContentEntryRecord found = structure.Get(1);

        Assert.That(found, Is.SameAs(record));
    }

    [Test]
    public void Add_NullEntryThrows()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));

        Assert.Throws<ArgumentNullException>(() => structure.Add(null!));
    }

    [Test]
    public void TryAdd_NullEntryThrows()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));

        Assert.Throws<ArgumentNullException>(() => structure.TryAdd(null!, out _, out _));
    }

    [Test]
    public void TryGet_DefaultIdThrows()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));

        Assert.Throws<ArgumentException>(() => structure.TryGet(default(ContentEntryId), out _, out _));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void TryGet_InvalidNumericIdThrows(long id)
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(2));

        Assert.Throws<ArgumentOutOfRangeException>(() => structure.TryGet(id, out _, out _));
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }

    private sealed class TestConfiguredStructure : IContentRetentionPolicyStructure, IContentReadOrderStructure
    {
        public TestConfiguredStructure(ContentOverflowPolicy overflowPolicy, ContentSequenceReadOrder readOrder)
        {
            OverflowPolicy = overflowPolicy;
            ReadOrder = readOrder;
        }

        public ContentOverflowPolicy OverflowPolicy { get; }

        public ContentSequenceReadOrder ReadOrder { get; }

        public System.Collections.Generic.IReadOnlyList<ContentEntryRecord> Records => Array.Empty<ContentEntryRecord>();

        public ContentManagerBase CreateManager()
        {
            return new TestManager(this);
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

    private sealed class TestMutableStructure :
        IContentClearableStructure,
        IContentRecordRemovalStructure,
        IParameterizedContentStructure
    {
        public TestMutableStructure(ContentOverflowPolicy overflowPolicy)
        {
            OverflowPolicy = overflowPolicy;
        }

        public ContentOverflowPolicy OverflowPolicy { get; private set; }

        public IReadOnlyCollection<ContentParameterDefinition> Parameters =>
            new[] { new ContentParameterDefinition("overflowPolicy", typeof(ContentOverflowPolicy), "Overflow policy.") };

        public IReadOnlyList<ContentEntryRecord> Records => Array.Empty<ContentEntryRecord>();

        public ContentManagerBase CreateManager()
        {
            return new TestManager(this);
        }

        public bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
        {
            removedRecords = Array.Empty<ContentEntryRecord>();
            failure = null;
            return true;
        }

        public IReadOnlyList<ContentEntryRecord> Clear()
        {
            return Array.Empty<ContentEntryRecord>();
        }

        public bool TryRemove(ContentEntryId id, out ContentEntryRecord? removedRecord, out ContentFailure? failure)
        {
            removedRecord = null;
            failure = ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryNotFound, "Missing.");
            return false;
        }

        public ContentEntryRecord Remove(ContentEntryId id)
        {
            throw new ContentOperationException(ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryNotFound, "Missing."));
        }

        public bool TryCreateWithParameter(
            string parameterId,
            object? value,
            out IContentStructure? structure,
            out IReadOnlyList<ContentEntryRecord> removedRecords,
            out ContentFailure? failure)
        {
            if (parameterId != "overflowPolicy" || value is not ContentOverflowPolicy overflowPolicy)
            {
                structure = null;
                removedRecords = Array.Empty<ContentEntryRecord>();
                failure = ContentFailure.Create(ContentFailureKind.Configuration, ContentFailureCodes.ConfigurationRejected, "Invalid parameter.");
                return false;
            }

            structure = new TestMutableStructure(overflowPolicy);
            removedRecords = Array.Empty<ContentEntryRecord>();
            failure = null;
            return true;
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

    private sealed class TestManager : ContentManagerBase
    {
        public TestManager(IContentStructure structure)
            : base(structure)
        {
        }
    }
}
