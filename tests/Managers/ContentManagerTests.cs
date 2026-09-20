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
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(200)));

        ContentEntryRecord first = manager.Add(Entry("First"));
        ContentEntryRecord second = manager.Add(Entry("Second"));

        Assert.That(first.Id, Is.EqualTo(new ContentEntryId("1")));
        Assert.That(second.Id, Is.EqualTo(new ContentEntryId("2")));
        Assert.That(manager.Records, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void Constructor_AcceptsSequenceStructure()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(1));
        var manager = new ContentSequenceManager(structure);

        ContentEntryRecord record = manager.Add(Entry("Entry"));

        Assert.That(manager.Structure, Is.SameAs(structure));
        Assert.That(manager.Records, Is.EqualTo(new[] { record }));
    }

    [Test]
    public void Constructor_NullStructureThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentSequenceManager(null!));
    }

    [Test]
    public void TryAdd_NullEntryThrows()
    {
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(200)));

        Assert.Throws<ArgumentNullException>(() => manager.TryAdd(null!, out _, out _));
    }

    [Test]
    public void BaseManager_ExposesRecordsAndDelegatesLookup()
    {
        ContentManagerBase manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(200)));
        ContentEntryRecord added = ((ContentSequenceManager)manager).Add(Entry("Stored"));

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
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(200)));
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
    public void SequenceManager_TryClear_DelegatesToSequenceStructure()
    {
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord added = manager.Add(Entry("Stored"));

        bool cleared = manager.TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

        Assert.That(cleared, Is.True);
        Assert.That(removedRecords, Is.EqualTo(new[] { added }));
        Assert.That(failure, Is.Null);
        Assert.That(manager.Records, Is.Empty);
    }

    [Test]
    public void SequenceManager_TryRemove_DelegatesToSequenceStructure()
    {
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord added = manager.Add(Entry("Stored"));

        bool removed = manager.TryRemove(1, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.True);
        Assert.That(removedRecord, Is.SameAs(added));
        Assert.That(failure, Is.Null);
        Assert.That(manager.Records, Is.Empty);
    }

    [Test]
    public void SequenceManagerBase_DelegatesSharedSequenceOperations()
    {
        ContentSequenceManagerBase manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));

        ContentEntryRecord added = manager.Add(Entry("Stored"));
        bool found = manager.TryGet(1, out ContentEntryRecord? foundRecord, out ContentFailure? getFailure);
        bool removed = manager.TryRemove(1, out ContentEntryRecord? removedRecord, out ContentFailure? removeFailure);
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
    public void ContentManagersForStructure_ReturnsSequenceManager()
    {
        var manager = ContentManagers.ForStructure<ContentSequenceManager>(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord added = manager.Add(Entry("Stored"));

        ContentEntryRecord found = manager.Get(1);
        ContentEntryRecord removed = manager.Remove(1);

        Assert.That(found, Is.SameAs(added));
        Assert.That(removed, Is.SameAs(added));
        Assert.That(manager.Records, Is.Empty);
    }

    [Test]
    public void ContentSequenceManager_CanBeConstructedExplicitly()
    {
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord added = manager.Add(Entry("Stored"));

        Assert.That(manager.Get(1), Is.SameAs(added));
    }

    [Test]
    public void SequenceManager_TrySetStructureParameter_DelegatesToParameterizedStructure()
    {
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        manager.Add(Entry("First"));
        ContentEntryRecord second = manager.Add(Entry("Second"));

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
    public void SequenceManager_SetStructureParameter_EmitsConfigurationChangedEvent()
    {
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord first = manager.Add(Entry("First"));
        manager.Add(Entry("Second"));
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

    [Test]
    public void BaseManager_CapturesActiveStructureSnapshot()
    {
        ContentManagerBase manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ((ContentSequenceManager)manager).Add(Entry("Stored"));

        bool captured = manager.TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure);

        Assert.That(captured, Is.True);
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.Kind, Is.EqualTo(ContentSequenceStructure.SnapshotKind));
        Assert.That(snapshot.Records, Has.Count.EqualTo(1));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void BaseManager_RestoreSnapshot_ReplacesStructureAtomicallyAndEmitsFullRefresh()
    {
        var source = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(5)));
        source.Add(Entry("One"));
        source.Add(Entry("Two"));
        ContentStructureSnapshot snapshot = source.CaptureSnapshot();

        var target = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord oldRecord = target.Add(Entry("Old"));
        var changes = new List<ContentChangedEventArgs>();
        object? sender = null;
        target.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            changes.Add(args);
        };

        bool restored = target.TryRestoreSnapshot(snapshot, out ContentFailure? failure);
        ContentEntryRecord next = target.Add(Entry("Three"));

        Assert.That(restored, Is.True);
        Assert.That(failure, Is.Null);
        Assert.That(target.Structure, Is.TypeOf<ContentSequenceStructure>());
        Assert.That(target.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "One", "Two", "Three" }));
        Assert.That(next.Id.Value, Is.EqualTo("3"));
        Assert.That(sender, Is.SameAs(target));
        ContentChangedEventArgs changedArgs = changes[0];
        Assert.That(changedArgs!.Kind, Is.EqualTo(ContentChangeKind.SnapshotRestored));
        Assert.That(changedArgs.RequiresFullRefresh, Is.True);
        Assert.That(changedArgs.RemovedRecords, Is.EqualTo(new[] { oldRecord }));
        Assert.That(changedArgs.AddedRecords.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "One", "Two" }));
    }

    [Test]
    public void BaseManager_RestoreSnapshot_RejectsIncompatibleStructureWithoutChangingState()
    {
        var keyed = new KeyedContentStructure<string>();
        keyed.Add("entry-1", Entry("Keyed"));
        ContentStructureSnapshot keyedSnapshot = keyed.CaptureSnapshot();

        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord original = manager.Add(Entry("Original"));
        int eventCount = 0;
        manager.Changed += (_, _) => eventCount++;

        bool restored = manager.TryRestoreSnapshot(
            keyedSnapshot,
            KeyedContentStructure<string>.CreateSnapshotFactory(),
            out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
        Assert.That(manager.Structure, Is.TypeOf<ContentSequenceStructure>());
        Assert.That(manager.Records, Is.EqualTo(new[] { original }));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void BaseManager_RestoreSnapshot_RejectedRestoreEmitsNoEvent()
    {
        var manager = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None));
        ContentEntryRecord original = manager.Add(Entry("Original"));
        int eventCount = 0;
        manager.Changed += (_, _) => eventCount++;

        var snapshot = new ContentStructureSnapshot
        {
            Kind = ContentSequenceStructure.SnapshotKind,
            DataVersion = 99,
            Data = ContentSnapshotValue.Object()
        };

        bool restored = manager.TryRestoreSnapshot(snapshot, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedVersion));
        Assert.That(manager.Records, Is.EqualTo(new[] { original }));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void KeyedContentManager_RestoreSnapshot_PreservesTypedWorkflow()
    {
        var source = new KeyedContentManager<string>();
        source.Add("entry-1", Entry("One"));
        ContentStructureSnapshot snapshot = source.CaptureSnapshot();

        var target = new KeyedContentManager<string>();
        target.RestoreSnapshot(snapshot);
        target.Add("entry-2", Entry("Two"));

        Assert.That(target.Get("entry-1").Entry.PlainText, Is.EqualTo("One"));
        Assert.That(target.Get("entry-2").Entry.PlainText, Is.EqualTo("Two"));
    }

    [Test]
    public void KeyedContentManager_RestoreSnapshot_InvalidStoredIdLeavesStateUnchangedAndEmitsNoEvent()
    {
        var source = new KeyedContentManager<long>();
        source.Add(1, Entry("One"));
        ContentStructureSnapshot snapshot = source.CaptureSnapshot();
        snapshot.Records[0].EntryId = "abc";

        var target = new KeyedContentManager<long>();
        ContentEntryRecord original = target.Add(2, Entry("Original"));
        int eventCount = 0;
        target.Changed += (_, _) => eventCount++;

        bool restored = target.TryRestoreSnapshot(snapshot, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
        Assert.That(target.Records, Is.EqualTo(new[] { original }));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void BaseManager_RestoreSnapshot_WithoutFactoryRejectsUnsupportedActiveStructure()
    {
        ContentManagerBase manager = new UnsupportedSnapshotManager(new UnsupportedStructure());
        var snapshot = new ContentStructureSnapshot
        {
            Kind = ContentSequenceStructure.SnapshotKind,
            DataVersion = ContentSequenceStructure.SnapshotDataVersion,
            Data = ContentSnapshotValue.Object()
        };

        bool restored = manager.TryRestoreSnapshot(snapshot, out ContentFailure? failure);
        ContentOperationException exception = Assert.Throws<ContentOperationException>(() => manager.RestoreSnapshot(snapshot))!;

        Assert.That(restored, Is.False);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedStructure));
        Assert.That(exception.Failure.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedStructure));
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }

    private sealed class UnsupportedSnapshotManager : ContentManagerBase
    {
        public UnsupportedSnapshotManager(IContentStructure structure)
            : base(structure)
        {
        }
    }

    private sealed class UnsupportedStructure : IContentStructure
    {
        public IReadOnlyList<ContentEntryRecord> Records => Array.Empty<ContentEntryRecord>();

        public ContentManagerBase CreateManager()
        {
            return new UnsupportedSnapshotManager(this);
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
