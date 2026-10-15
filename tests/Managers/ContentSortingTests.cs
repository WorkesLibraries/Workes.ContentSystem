using System;
using System.Collections.Generic;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentSortingTests
{
    [Test]
    public void GetSortedRecords_IsStableMaterializedAndAvailableToEveryManagerFamily()
    {
        ContentManagerBase[] managers =
        {
            new ContentSequenceManager(new ContentSequenceStructure(ContentOverflowPolicy.None)),
            new ContentMapManager<string>(),
            new ContentSingleManager(new ContentSingleStructure()),
            new ContentStackManager(new ContentStackStructure(ContentOverflowPolicy.None)),
            new ContentCompoundManager(new ContentCompoundStructure())
        };

        foreach (ContentManagerBase manager in managers)
        {
            switch (manager)
            {
                case ContentSequenceManager sequence: sequence.Add(Entry("b")); sequence.Add(Entry("a")); sequence.Add(Entry("a")); break;
                case ContentMapManager<string> map: map.Add("b", Entry("b")); map.Add("a1", Entry("a")); map.Add("a2", Entry("a")); break;
                case ContentSingleManager single: single.Set(Entry("b")); break;
                case ContentStackManager stack: stack.Push(Entry("b")); stack.Push(Entry("a")); break;
                case ContentCompoundManager compound: compound.AddRoot(Entry("b")); compound.AddRoot(Entry("a")); break;
            }
            var before = manager.Records.ToArray();
            ContentStructureSnapshot snapshotBefore = manager.CaptureSnapshot();
            int events = 0;
            manager.Changed += (_, _) => events++;
            IReadOnlyList<ContentEntryRecord> view = manager.GetSortedRecords(Comparer());
            Assert.That(view.Select(record => record.Entry.PlainText), Is.Ordered);
            Assert.That(manager.Records, Is.EqualTo(before));
            Assert.That(manager.CaptureSnapshot().Records.Select(record => record.EntryId), Is.EqualTo(snapshotBefore.Records.Select(record => record.EntryId)));
            Assert.That(events, Is.Zero);
            Assert.Throws<NotSupportedException>(() => ((IList<ContentEntryRecord>)view).Clear());
        }
    }

    [Test]
    public void MapSort_IsStableAndEmitsOneFullRefreshOnlyForReorder()
    {
        var manager = new ContentMapManager<string>();
        ContentEntryRecord b = manager.Add("b", Entry("b"));
        ContentEntryRecord a1 = manager.Add("a1", Entry("a"));
        ContentEntryRecord a2 = manager.Add("a2", Entry("a"));
        ContentChangedEventArgs? args = null;
        int events = 0;
        manager.Changed += (_, change) => { args = change; events++; };

        Assert.That(manager.AssessSort(Comparer()).CanCommit, Is.True);
        Assert.That(manager.Sort(Comparer()), Is.True);
        Assert.That(manager.Records, Is.EqualTo(new[] { a1, a2, b }));
        Assert.That(manager.Get("b"), Is.SameAs(b));
        Assert.That(args?.Kind, Is.EqualTo(ContentChangeKind.Sorted));
        Assert.That(args?.RequiresFullRefresh, Is.True);
        Assert.That(manager.Sort(Comparer()), Is.False);
        Assert.That(events, Is.EqualTo(1));
        Assert.That(manager.Remove("a1"), Is.SameAs(a1));
    }

    [Test]
    public void MapSort_ComparerExceptionLeavesStateAndEventsUntouchedAndSnapshotPreservesOrder()
    {
        var manager = new ContentMapManager<string>();
        manager.Add("b", Entry("b"));
        manager.Add("a", Entry("a"));
        ContentEntryRecord[] before = manager.Records.ToArray();
        int events = 0;
        manager.Changed += (_, _) => events++;

        Assert.Throws<InvalidOperationException>(() => manager.Sort(new ThrowingComparer()));
        Assert.That(manager.Records, Is.EqualTo(before));
        Assert.That(events, Is.Zero);
        Assert.That(manager.Sort(Comparer()), Is.True);
        ContentStructureSnapshot snapshot = manager.CaptureSnapshot();
        var restored = new ContentMapManager<string>();
        restored.RestoreSnapshot(snapshot);
        Assert.That(restored.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void CompoundSortSortsEveryGroupAndSnapshotPreservesTopologyAndOrder()
    {
        var manager = new ContentCompoundManager(new ContentCompoundStructure());
        ContentCompoundNode rootZ = manager.AddRoot(Entry("z"));
        ContentCompoundNode rootA = manager.AddRoot(Entry("a"));
        ContentCompoundNode childZ = manager.AddChild(rootZ, Entry("z-child"));
        ContentCompoundNode childA = manager.AddChild(rootZ, Entry("a-child"));
        ContentCompoundNode grandchild = manager.AddChild(childZ, Entry("grandchild"));
        int events = 0;
        ContentChangedEventArgs? args = null;
        manager.Changed += (_, change) => { events++; args = change; };

        Assert.That(manager.AssessSortSiblings(Comparer()).CanCommit, Is.True);
        Assert.That(manager.SortSiblings(Comparer()), Is.True);
        Assert.That(manager.GetRoots().Select(node => node.Record), Is.EqualTo(new[] { rootA.Record, rootZ.Record }));
        Assert.That(manager.GetChildren(rootZ).Select(node => node.Record), Is.EqualTo(new[] { childA.Record, childZ.Record }));
        Assert.That(manager.GetParent(grandchild).Record, Is.SameAs(childZ.Record));
        Assert.That(manager.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "a", "z", "a-child", "z-child", "grandchild" }));
        Assert.That(args?.Kind, Is.EqualTo(ContentChangeKind.Sorted));
        Assert.That(args?.RequiresFullRefresh, Is.True);
        Assert.That(manager.SortSiblings(Comparer()), Is.False);
        Assert.That(events, Is.EqualTo(1));

        ContentStructureSnapshot snapshot = manager.CaptureSnapshot();
        var restored = new ContentCompoundManager(new ContentCompoundStructure());
        restored.RestoreSnapshot(snapshot);
        Assert.That(restored.Records.Select(record => record.Entry.PlainText), Is.EqualTo(manager.Records.Select(record => record.Entry.PlainText)));
        Assert.That(restored.GetChildren(1).Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "a-child", "z-child" }));
    }

    [Test]
    public void CompoundSort_ComparerExceptionIsAtomic()
    {
        var manager = new ContentCompoundManager(new ContentCompoundStructure());
        manager.AddRoot(Entry("a"));
        manager.AddRoot(Entry("z"));
        manager.AddChild(1, Entry("a-child"));
        manager.AddChild(1, Entry("z-child"));
        ContentEntryRecord[] before = manager.Records.ToArray();
        int events = 0;
        manager.Changed += (_, _) => events++;

        Assert.Throws<InvalidOperationException>(() => manager.SortSiblings(new ThrowAfterOneComparisonComparer()));
        Assert.That(manager.Records, Is.EqualTo(before));
        Assert.That(events, Is.Zero);
    }

    [Test]
    public void CompoundSort_NewestFirstSortsTheVisibleOrderAndRoundTrips()
    {
        var manager = new ContentCompoundManager(new ContentCompoundStructure(
            ContentCompoundChildRemovalPolicy.Reject,
            ContentCompoundSiblingReadOrder.NewestFirst));
        manager.AddRoot(Entry("a"));
        manager.AddRoot(Entry("z"));
        manager.AddChild(1, Entry("a-child"));
        manager.AddChild(1, Entry("z-child"));

        Assert.That(manager.SortSiblings(Comparer()), Is.True);
        Assert.That(manager.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "a", "a-child", "z-child", "z" }));

        var restored = new ContentCompoundManager(new ContentCompoundStructure(
            ContentCompoundChildRemovalPolicy.Reject,
            ContentCompoundSiblingReadOrder.OldestFirst));
        restored.RestoreSnapshot(manager.CaptureSnapshot());
        Assert.That(restored.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "a", "a-child", "z-child", "z" }));
    }

    [Test]
    public void CompoundSort_ComparerSelectorCanUseDifferentPoliciesPerGroupAndRoundTrips()
    {
        var manager = new ContentCompoundManager(new ContentCompoundStructure());
        ContentCompoundNode rootA = manager.AddRoot(Entry("a"));
        ContentCompoundNode rootZ = manager.AddRoot(Entry("z"));
        manager.AddChild(rootA, Entry("b"));
        manager.AddChild(rootA, Entry("a"));
        manager.AddChild(rootZ, Entry("1"));
        manager.AddChild(rootZ, Entry("2"));
        var requestedGroups = new List<ContentEntryId?>();
        Func<ContentEntryId?, IComparer<ContentEntryRecord>> selector = parentId =>
        {
            requestedGroups.Add(parentId);
            return parentId is null || parentId.Value == rootZ.Record.Id
                ? PlainTextComparer(descending: true)
                : PlainTextComparer(descending: false);
        };
        int events = 0;
        manager.Changed += (_, _) => events++;

        Assert.That(manager.AssessSortSiblings(selector).CanCommit, Is.True);
        Assert.That(requestedGroups, Is.Empty, "assessment must not invoke caller code");
        Assert.That(manager.SortSiblings(selector), Is.True);
        Assert.That(requestedGroups, Is.EqualTo(new ContentEntryId?[] { null, rootA.Record.Id, rootZ.Record.Id }));
        Assert.That(manager.GetRoots().Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "z", "a" }));
        Assert.That(manager.GetChildren(rootA).Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(manager.GetChildren(rootZ).Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "2", "1" }));
        Assert.That(events, Is.EqualTo(1));

        var restored = new ContentCompoundManager(new ContentCompoundStructure());
        restored.RestoreSnapshot(manager.CaptureSnapshot());
        Assert.That(restored.Records.Select(record => record.Entry.PlainText), Is.EqualTo(manager.Records.Select(record => record.Entry.PlainText)));
    }

    [Test]
    public void CompoundSort_CanTargetRootsOrOneParentGroupAndRejectUnknownParents()
    {
        var manager = new ContentCompoundManager(new ContentCompoundStructure());
        ContentCompoundNode rootZ = manager.AddRoot(Entry("z"));
        ContentCompoundNode rootA = manager.AddRoot(Entry("a"));
        manager.AddChild(rootZ, Entry("child-z"));
        manager.AddChild(rootZ, Entry("child-a"));
        manager.AddChild(rootA, Entry("other-z"));
        manager.AddChild(rootA, Entry("other-a"));
        int events = 0;
        manager.Changed += (_, _) => events++;

        Assert.That(manager.AssessSortSiblings(parentId: null, PlainTextComparer(false)).CanCommit, Is.True);
        Assert.That(manager.SortSiblings(parentId: null, PlainTextComparer(false)), Is.True);
        Assert.That(manager.GetRoots().Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "a", "z" }));
        Assert.That(manager.SortSiblings(rootZ.Record.Id, PlainTextComparer(false)), Is.True);
        Assert.That(manager.GetChildren(rootZ).Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "child-a", "child-z" }));
        Assert.That(manager.GetChildren(rootA).Select(node => node.Record.Entry.PlainText), Is.EqualTo(new[] { "other-z", "other-a" }));
        Assert.That(manager.SortSiblings(rootZ.Record.Id, PlainTextComparer(false)), Is.False);
        Assert.That(events, Is.EqualTo(2));

        var unknownParent = new ContentEntryId("999");
        ContentPreflightResult assessment = manager.AssessSortSiblings(unknownParent, PlainTextComparer(false));
        bool succeeded = manager.TrySortSiblings(unknownParent, PlainTextComparer(false), out bool changed, out ContentFailure? failure);
        Assert.That(assessment.Failure?.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
        Assert.That(succeeded, Is.False);
        Assert.That(changed, Is.False);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
        Assert.Throws<ContentOperationException>(() => manager.SortSiblings(unknownParent, PlainTextComparer(false)));
        Assert.That(events, Is.EqualTo(2));
    }

    [Test]
    public void CompoundSort_SelectorExceptionLeavesEveryGroupAndEventsUntouched()
    {
        var manager = new ContentCompoundManager(new ContentCompoundStructure());
        manager.AddRoot(Entry("a"));
        manager.AddRoot(Entry("z"));
        manager.AddChild(1, Entry("child-a"));
        manager.AddChild(1, Entry("child-z"));
        ContentEntryRecord[] before = manager.Records.ToArray();
        int selectorCalls = 0;
        int events = 0;
        manager.Changed += (_, _) => events++;

        Assert.Throws<InvalidOperationException>(() => manager.SortSiblings(parentId =>
        {
            if (selectorCalls++ > 0) throw new InvalidOperationException("Policy selection failed.");
            return PlainTextComparer(descending: true);
        }));
        Assert.That(selectorCalls, Is.EqualTo(2));
        Assert.That(manager.Records, Is.EqualTo(before));
        Assert.That(events, Is.Zero);
    }

    [Test]
    public void CompoundSort_UnsupportedStructureReturnsStructuredFailure()
    {
        var manager = new UnsupportedCompoundManager(new UnsupportedCompoundStructure());
        ContentFailure? failure;

        Assert.That(manager.AssessSortSiblings(Comparer()).Failure?.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
        Assert.That(manager.TrySortSiblings(Comparer(), out bool changed, out failure), Is.False);
        Assert.That(changed, Is.False);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
        Assert.That(manager.TrySortSiblings(_ => Comparer(), out changed, out failure), Is.False);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
        Assert.That(manager.TrySortSiblings(parentId: null, Comparer(), out changed, out failure), Is.False);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.StructureUnsupportedOperation));
        Assert.Throws<ContentOperationException>(() => manager.SortSiblings(Comparer()));
        Assert.Throws<ContentOperationException>(() => manager.SortSiblings(_ => Comparer()));
        Assert.Throws<ContentOperationException>(() => manager.SortSiblings(parentId: null, Comparer()));
    }

    private static Comparer<ContentEntryRecord> Comparer() => Comparer<ContentEntryRecord>.Create(
        (left, right) => StringComparer.Ordinal.Compare(left.Entry.PlainText, right.Entry.PlainText));
    private static Comparer<ContentEntryRecord> PlainTextComparer(bool descending) => Comparer<ContentEntryRecord>.Create(
        (left, right) => descending
            ? StringComparer.Ordinal.Compare(right.Entry.PlainText, left.Entry.PlainText)
            : StringComparer.Ordinal.Compare(left.Entry.PlainText, right.Entry.PlainText));
    private static PlainContentEntry Entry(string text) => new(DateTimeOffset.UnixEpoch, text);

    private sealed class ThrowingComparer : IComparer<ContentEntryRecord>
    {
        public int Compare(ContentEntryRecord? x, ContentEntryRecord? y) => throw new InvalidOperationException("Comparison failed.");
    }

    private sealed class ThrowAfterOneComparisonComparer : IComparer<ContentEntryRecord>
    {
        private int _comparisons;
        public int Compare(ContentEntryRecord? x, ContentEntryRecord? y)
        {
            if (_comparisons++ > 0) throw new InvalidOperationException("Comparison failed after preparing the first sibling group.");
            return StringComparer.Ordinal.Compare(x!.Entry.PlainText, y!.Entry.PlainText);
        }
    }

    private sealed class UnsupportedCompoundManager : ContentCompoundManagerBase<long>
    {
        public UnsupportedCompoundManager(ContentCompoundStructureBase<long> structure) : base(structure) { }
    }

    private sealed class UnsupportedCompoundStructure : ContentCompoundStructureBase<long>
    {
        private static NotSupportedException Unsupported() => new NotSupportedException();
        public override IReadOnlyList<ContentEntryRecord> Records => Array.Empty<ContentEntryRecord>();
        public override ContentManagerBase CreateManager() => new UnsupportedCompoundManager(this);
        public override ContentPreflightResult AssessAddRoot(IContentEntry entry) => throw Unsupported();
        public override bool TryAddRoot(IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => throw Unsupported();
        public override ContentCompoundNode AddRoot(IContentEntry entry) => throw Unsupported();
        public override bool TryAddRoot(long id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => throw Unsupported();
        public override ContentPreflightResult AssessAddRoot(long id, IContentEntry entry) => throw Unsupported();
        public override ContentCompoundNode AddRoot(long id, IContentEntry entry) => throw Unsupported();
        public override bool TryAddChild(long parentId, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => throw Unsupported();
        public override ContentPreflightResult AssessAddChild(long parentId, IContentEntry entry) => throw Unsupported();
        public override ContentCompoundNode AddChild(long parentId, IContentEntry entry) => throw Unsupported();
        public override bool TryAddChild(ContentEntryId parentId, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => throw Unsupported();
        public override ContentPreflightResult AssessAddChild(ContentEntryId parentId, IContentEntry entry) => throw Unsupported();
        public override ContentCompoundNode AddChild(ContentEntryId parentId, IContentEntry entry) => throw Unsupported();
        public override bool TryAddChild(long parentId, long id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => throw Unsupported();
        public override ContentPreflightResult AssessAddChild(long parentId, long id, IContentEntry entry) => throw Unsupported();
        public override ContentCompoundNode AddChild(long parentId, long id, IContentEntry entry) => throw Unsupported();
        public override bool TryAddChild(ContentEntryId parentId, long id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure) => throw Unsupported();
        public override ContentPreflightResult AssessAddChild(ContentEntryId parentId, long id, IContentEntry entry) => throw Unsupported();
        public override ContentCompoundNode AddChild(ContentEntryId parentId, long id, IContentEntry entry) => throw Unsupported();
        public override bool TryGetNode(ContentEntryId id, out ContentCompoundNode? node, out ContentFailure? failure) => throw Unsupported();
        public override ContentCompoundNode GetNode(ContentEntryId id) => throw Unsupported();
        public override bool TryGetNode(long id, out ContentCompoundNode? node, out ContentFailure? failure) => throw Unsupported();
        public override ContentCompoundNode GetNode(long id) => throw Unsupported();
        public override IReadOnlyList<ContentCompoundNode> GetRoots() => throw Unsupported();
        public override IReadOnlyList<ContentCompoundNode> GetChildren(long parentId) => throw Unsupported();
        public override IReadOnlyList<ContentCompoundNode> GetChildren(ContentEntryId parentId) => throw Unsupported();
        public override IReadOnlyList<ContentCompoundRecordView> GetRecordViews() => throw Unsupported();
        public override bool TryRemove(long id, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure) => throw Unsupported();
        public override ContentPreflightResult AssessRemove(long id) => throw Unsupported();
        public override IReadOnlyList<ContentEntryRecord> Remove(long id) => throw Unsupported();
        public override bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure) => throw Unsupported();
        public override ContentEntryRecord Get(ContentEntryId id) => throw Unsupported();
        public override bool TryGet(long id, out ContentEntryRecord? record, out ContentFailure? failure) => throw Unsupported();
        public override ContentEntryRecord Get(long id) => throw Unsupported();
        public override bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure) => throw Unsupported();
        public override ContentPreflightResult AssessClear() => throw Unsupported();
        public override IReadOnlyList<ContentEntryRecord> Clear() => throw Unsupported();
    }
}
