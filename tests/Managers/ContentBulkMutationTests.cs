using System;
using System.Collections.Generic;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentBulkMutationTests
{
    [Test]
    public void SequenceAddRange_IsAtomicOrderedAndEmitsOneEvent()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        var manager = new ContentSequenceManager(structure);
        var events = new List<ContentChangedEventArgs>();
        manager.Changed += (_, args) => events.Add(args);

        IReadOnlyList<ContentEntryRecord> records = manager.AddRange(new[] { Entry("one"), Entry("two") });

        Assert.That(records.Select(record => record.Id.Value), Is.EqualTo(new[] { "1", "2" }));
        Assert.That(manager.Records, Is.EqualTo(records));
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Kind, Is.EqualTo(ContentChangeKind.Added));
        Assert.That(events[0].AddedRecords, Is.EqualTo(records));
        Assert.That(events[0].RequiresFullRefresh, Is.True);
        ContentStructureSnapshot sequenceSnapshot = manager.CaptureSnapshot();
        manager.RestoreSnapshot(sequenceSnapshot);
        Assert.That(manager.Add(Entry("three")).Id.Value, Is.EqualTo("3"));

        var rejecting = new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.Reject(2)));
        rejecting.Add(Entry("kept"));
        ContentStructureSnapshot before = rejecting.CaptureSnapshot();
        int eventCount = 0;
        rejecting.Changed += (_, _) => eventCount++;
        Assert.That(rejecting.TryAddRange(new[] { Entry("a"), Entry("b") }, out _, out ContentFailure? failure), Is.False);
        Assert.That(failure!.Kind, Is.EqualTo(ContentFailureKind.Structure));
        Assert.That(rejecting.CaptureSnapshot().Records.Select(record => record.EntryId), Is.EqualTo(before.Records.Select(record => record.EntryId)));
        Assert.That(eventCount, Is.Zero);
        Assert.That(rejecting.Add(Entry("after rejection")).Id.Value, Is.EqualTo("2"));

        var stack = new ContentStackManager(new ContentStackStructure(ContentOverflowPolicy.Reject(2)));
        stack.Push(Entry("already present"));
        Assert.That(stack.TryPushRange(new[] { Entry("a"), Entry("b") }, out _, out _), Is.False);
        Assert.That(stack.Push(Entry("after rejection")).Id.Value, Is.EqualTo("2"));
    }

    [Test]
    public void MapRanges_AddAtomicallyAndSetPreservesExistingOrder()
    {
        var manager = new ContentMapManager<string>(new ContentMapStructure<string>());
        ContentEntryRecord original = manager.Add("first", Entry("old"));
        manager.Add("second", Entry("two"));
        ContentEntryRecord[] order = manager.Records.ToArray();
        ContentStructureSnapshot beforeAssessment = manager.CaptureSnapshot();
        var events = new List<ContentChangedEventArgs>();
        manager.Changed += (_, args) => events.Add(args);

        Assert.That(manager.AssessAddRange(new[] { new KeyValuePair<string, IContentEntry>("third", Entry("three")) }).CanCommit, Is.True);
        Assert.That(manager.Records, Is.EqualTo(order));
        Assert.That(events, Is.Empty);
        Assert.That(manager.CaptureSnapshot().Records.Select(record => record.EntryId), Is.EqualTo(beforeAssessment.Records.Select(record => record.EntryId)));

        Assert.That(manager.TryAddRange(new[]
        {
            new KeyValuePair<string, IContentEntry>("third", Entry("three")),
            new KeyValuePair<string, IContentEntry>("first", Entry("duplicate"))
        }, out _, out ContentFailure? failure), Is.False);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdDuplicate));
        Assert.That(manager.Records, Is.EqualTo(order));
        Assert.That(events, Is.Empty);

        IReadOnlyList<ContentEntryRecord> set = manager.SetRange(new[]
        {
            new KeyValuePair<string, IContentEntry>("first", Entry("new")),
            new KeyValuePair<string, IContentEntry>("third", Entry("three"))
        });
        Assert.That(manager.Records.Select(record => record.Id.Value), Is.EqualTo(new[] { "first", "second", "third" }));
        Assert.That(manager.Get("first"), Is.SameAs(set[0]));
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Kind, Is.EqualTo(ContentChangeKind.Replaced));
        Assert.That(events[0].AddedRecords, Is.EqualTo(set));
        Assert.That(events[0].RemovedRecords, Is.EqualTo(new[] { original }));
        Assert.That(events[0].RequiresFullRefresh, Is.True);
        ContentStructureSnapshot mapSnapshot = manager.CaptureSnapshot();
        manager.RestoreSnapshot(mapSnapshot);
        Assert.That(manager.Records.Select(record => record.Id.Value), Is.EqualTo(new[] { "first", "second", "third" }));
    }

    [Test]
    public void MapAddRange_SuccessEmitsOneRefreshEvent()
    {
        var manager = new ContentMapManager<string>(new ContentMapStructure<string>());
        ContentChangedEventArgs? changed = null;
        manager.Changed += (_, args) => changed = args;
        IReadOnlyList<ContentEntryRecord> records = manager.AddRange(new[]
        {
            new KeyValuePair<string, IContentEntry>("a", Entry("a")),
            new KeyValuePair<string, IContentEntry>("b", Entry("b"))
        });
        Assert.That(records, Is.EqualTo(manager.Records));
        Assert.That(changed!.AddedRecords, Is.EqualTo(records));
        Assert.That(changed.RequiresFullRefresh, Is.True);
    }

    [Test]
    public void StackRangePushesInInputOrderAndPredicateRemovalUsesSnapshot()
    {
        var manager = new ContentStackManager(new ContentStackStructure(ContentOverflowPolicy.None));
        ContentChangedEventArgs? changed = null;
        manager.Changed += (_, args) => changed = args;
        IReadOnlyList<ContentEntryRecord> pushed = manager.PushRange(new[] { Entry("bottom"), Entry("top") });
        Assert.That(pushed.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "bottom", "top" }));
        Assert.That(manager.Peek(), Is.SameAs(pushed[1]));
        Assert.That(changed!.AddedRecords, Is.EqualTo(pushed));
        Assert.That(changed.RequiresFullRefresh, Is.True);
        ContentStructureSnapshot stackSnapshot = manager.CaptureSnapshot();
        manager.RestoreSnapshot(stackSnapshot);
        Assert.That(manager.Peek().Entry.PlainText, Is.EqualTo("top"));

        ContentEntryRecord[] before = manager.Records.ToArray();
        int events = 0;
        manager.Changed += (_, _) => events++;
        Assert.Throws<InvalidOperationException>(() => manager.RemoveWhere(_ => throw new InvalidOperationException("predicate")));
        Assert.That(manager.Records, Is.EqualTo(before));
        Assert.That(events, Is.Zero);
        IReadOnlyList<ContentEntryRecord> removed = manager.RemoveWhere(record => record.Entry.PlainText == "bottom");
        Assert.That(removed.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "bottom" }));
        Assert.That(manager.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "top" }));
        Assert.That(events, Is.EqualTo(1));
        int afterRemoval = events;
        Assert.That(manager.RemoveWhere(_ => false), Is.Empty);
        Assert.That(events, Is.EqualTo(afterRemoval));

        var map = new ContentMapManager<string>(new ContentMapStructure<string>());
        map.Add("a", Entry("remove")); map.Add("b", Entry("keep"));
        Assert.That(map.RemoveWhere(record => record.Entry.PlainText == "remove").Select(record => record.Id.Value), Is.EqualTo(new[] { "a" }));
    }

    [Test]
    public void CompoundBatchesPreserveTopologySnapshotsAndRemovalPolicies()
    {
        var manager = new ContentCompoundManager(new ContentCompoundStructure(ContentCompoundChildRemovalPolicy.Reject));
        IReadOnlyList<ContentCompoundNode> roots = manager.AddRoots(new[] { Entry("root-a"), Entry("root-b") });
        long rootId = long.Parse(roots[0].Record.Id.Value, System.Globalization.CultureInfo.InvariantCulture);
        IReadOnlyList<ContentCompoundNode> children = manager.AddChildren(rootId, new[] { Entry("child-a"), Entry("child-b") });
        Assert.That(manager.GetRoots().Select(node => node.Record), Is.EqualTo(roots.Select(node => node.Record)));
        Assert.That(manager.GetChildren(rootId).Select(node => node.Record), Is.EqualTo(children.Select(node => node.Record)));
        Assert.That(manager.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "root-a", "child-a", "child-b", "root-b" }));

        var newestFirst = new ContentCompoundManager(new ContentCompoundStructure(
            ContentCompoundChildRemovalPolicy.RemoveSubtree,
            ContentCompoundSiblingReadOrder.NewestFirst));
        ContentCompoundNode[] newestRoots = newestFirst.AddRoots(new[] { Entry("old root"), Entry("new root") }).ToArray();
        Assert.That(newestFirst.GetRoots().Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "new root", "old root" }));
        Assert.That(newestRoots.Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "old root", "new root" }));

        var rejectEvents = 0;
        manager.Changed += (_, _) => rejectEvents++;
        Assert.That(manager.TryRemoveWhere(record => record.Entry.PlainText.StartsWith("root-a", StringComparison.Ordinal), out _, out ContentFailure? failure), Is.False);
        Assert.That(failure!.Kind, Is.EqualTo(ContentFailureKind.Structure));
        Assert.That(rejectEvents, Is.Zero);

        var invalidParent = new ContentCompoundManager(new ContentCompoundStructure());
        Assert.That(invalidParent.TryAddChildren(999, new[] { Entry("orphan") }, out _, out ContentFailure? missing), Is.False);
        Assert.That(missing!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
        Assert.That(invalidParent.AddRoot(Entry("first valid root")).Record.Id.Value, Is.EqualTo("1"));

        ContentStructureSnapshot snapshot = manager.CaptureSnapshot();
        manager.RestoreSnapshot(snapshot);
        Assert.That(manager.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "root-a", "child-a", "child-b", "root-b" }));
        Assert.That(manager.GetChildren(rootId).Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "child-a", "child-b" }));

        var subtreeManager = new ContentCompoundManager(new ContentCompoundStructure(ContentCompoundChildRemovalPolicy.RemoveSubtree));
        ContentCompoundNode root = subtreeManager.AddRoot(Entry("remove root"));
        subtreeManager.AddChild(long.Parse(root.Record.Id.Value, System.Globalization.CultureInfo.InvariantCulture), Entry("keep?"));
        IReadOnlyList<ContentEntryRecord> removed = subtreeManager.RemoveWhere(record => record.Entry.PlainText == "remove root");
        Assert.That(removed.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "remove root", "keep?" }));
        Assert.That(subtreeManager.Records, Is.Empty);
    }

    [Test]
    public void SingleManagerSupportsPredicateRemovalButNoBatchWrite()
    {
        var manager = new ContentSingleManager(new ContentSingleStructure(ContentSingleReplacementPolicy.Replace));
        ContentEntryRecord current = manager.Set(Entry("current"));
        Assert.That(manager.RemoveWhere(record => ReferenceEquals(record, current)), Is.EqualTo(new[] { current }));
        Assert.That(manager.HasCurrent, Is.False);
    }

    [Test]
    public void GeneratedIdBatchRequiresTheOptionalRangeSourceContract()
    {
        var manager = new ContentSequenceManager<long>(new ContentSequenceStructure<long>(new NonRangeIdSource(), ContentOverflowPolicy.None));
        ContentPreflightResult assessment = manager.AssessAddRange(new[] { Entry("one") });
        Assert.That(assessment.CanCommit, Is.False);
        Assert.That(assessment.Failure!.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
        Assert.That(manager.TryAddRange(new[] { Entry("one") }, out _, out ContentFailure? failure), Is.False);
        Assert.That(failure, Is.EqualTo(assessment.Failure));
        ContentOperationException exception = Assert.Throws<ContentOperationException>(() => manager.AddRange(new[] { Entry("one") }))!;
        Assert.That(exception.Failure, Is.EqualTo(assessment.Failure));
        Assert.That(manager.Records, Is.Empty);
    }

    private static PlainContentEntry Entry(string text) => new PlainContentEntry(DateTimeOffset.UtcNow, text);

    private sealed class NonRangeIdSource : IContentGeneratedIdSource<long>
    {
        private readonly LongContentGeneratedIdSource _inner = new LongContentGeneratedIdSource();
        public IContentEntryIdStrategy<long> IdStrategy => _inner.IdStrategy;
        public IContentGeneratedIdSourceFactory<long> SnapshotFactory => LongContentGeneratedIdSource.Factory;
        public bool TryCreateNext(out long id, out ContentFailure? failure) => _inner.TryCreateNext(out id, out failure);
        public bool CanCreateNext(out ContentFailure? failure) => _inner.CanCreateNext(out failure);
        public bool TryObserve(long id, out ContentFailure? failure) => _inner.TryObserve(id, out failure);
        public bool CanObserve(long id, out ContentFailure? failure) => _inner.CanObserve(id, out failure);
        public bool TryObserveNormalized(ContentEntryId id, out ContentFailure? failure) => _inner.TryObserveNormalized(id, out failure);
        public bool CanObserveNormalized(ContentEntryId id, out ContentFailure? failure) => _inner.CanObserveNormalized(id, out failure);
        public bool TryCaptureSnapshot(out ContentSnapshotValue? snapshot, out ContentFailure? failure) => _inner.TryCaptureSnapshot(out snapshot, out failure);
    }
}
