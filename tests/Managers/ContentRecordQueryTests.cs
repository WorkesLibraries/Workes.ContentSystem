using System;
using System.Collections.Generic;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentRecordQueryTests
{
    [Test]
    public void FilteredAndQueryViews_WorkForEveryBuiltInManagerAndPreserveReadOrder()
    {
        ContentManagerBase[] managers = CreatePopulatedManagers();

        foreach (ContentManagerBase manager in managers)
        {
            ContentEntryRecord[] before = manager.Records.ToArray();
            ContentStructureSnapshot? snapshotBefore = manager.TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out _)
                ? snapshot
                : null;
            int events = 0;
            manager.Changed += (_, _) => events++;

            Func<ContentEntryRecord, bool> predicate = record =>
                record.Entry.PlainText is "first" or "second";
            IReadOnlyList<ContentEntryRecord> expected = before.Where(predicate).ToArray();
            IReadOnlyList<ContentEntryRecord> filtered = manager.GetFilteredRecords(predicate);
            IReadOnlyList<ContentEntryRecord> composed = manager.QueryRecords()
                .Where(predicate)
                .ToReadOnlyList();

            Assert.That(filtered, Is.EqualTo(expected));
            Assert.That(composed, Is.EqualTo(expected));
            Assert.That(manager.Records, Is.EqualTo(before));
            Assert.That(events, Is.Zero);
            AssertReadOnly(filtered);
            AssertReadOnly(composed);
            if (snapshotBefore is not null)
            {
                Assert.That(manager.CaptureSnapshot().Records.Select(record => record.EntryId),
                    Is.EqualTo(snapshotBefore.Records.Select(record => record.EntryId)));
            }
        }
    }

    [Test]
    public void Query_IsAnEagerSnapshotAndComposesFilterPrimaryAndSecondaryOrder()
    {
        var manager = new ContentMapManager<string>();
        ContentEntryRecord b2 = manager.Add("b2", Entry("b-2"));
        ContentEntryRecord a2 = manager.Add("a2", Entry("a-2"));
        ContentEntryRecord b1 = manager.Add("b1", Entry("b-1"));
        ContentEntryRecord a1 = manager.Add("a1", Entry("a-1"));
        ContentEntryRecord a1Duplicate = manager.Add("a1-copy", Entry("a-1"));
        ContentEntryRecord[] before = manager.Records.ToArray();
        ContentStructureSnapshot snapshotBefore = manager.CaptureSnapshot();
        int events = 0;
        manager.Changed += (_, _) => events++;

        ContentRecordQuery query = manager.QueryRecords()
            .Where(record => record.Entry.PlainText.StartsWith("a-", StringComparison.Ordinal)
                || record.Entry.PlainText.StartsWith("b-", StringComparison.Ordinal))
            .OrderBy(Comparer<ContentEntryRecord>.Create((left, right) =>
                StringComparer.Ordinal.Compare(left.Entry.PlainText.Substring(0, 1), right.Entry.PlainText.Substring(0, 1))))
            .ThenBy(Comparer<ContentEntryRecord>.Create((left, right) =>
                StringComparer.Ordinal.Compare(left.Entry.PlainText, right.Entry.PlainText)));

        manager.Add("later", Entry("a-0"));
        IReadOnlyList<ContentEntryRecord> result = query.ToReadOnlyList();

        Assert.That(result, Is.EqualTo(new[] { a1, a1Duplicate, a2, b1, b2 }));
        AssertReadOnly(result);
        Assert.That(before.Select(record => record.Id),
            Is.EqualTo(snapshotBefore.Records.Select(record => new ContentEntryId(record.EntryId))));
        Assert.That(manager.Records, Has.Count.EqualTo(6));
        Assert.That(events, Is.EqualTo(1), "only the explicit add raises an event");
    }

    [Test]
    public void Query_OrderByMatchesGetSortedRecordsAndPreservesStableTies()
    {
        var manager = new ContentMapManager<string>();
        ContentEntryRecord b = manager.Add("b", Entry("same"));
        ContentEntryRecord a = manager.Add("a", Entry("same"));

        IReadOnlyList<ContentEntryRecord> fromQuery = manager.QueryRecords()
            .OrderBy(Comparer<ContentEntryRecord>.Create((_, _) => 0))
            .ToReadOnlyList();
        IReadOnlyList<ContentEntryRecord> fromManager = manager.GetSortedRecords(
            Comparer<ContentEntryRecord>.Create((_, _) => 0));

        Assert.That(fromQuery, Is.EqualTo(new[] { b, a }));
        Assert.That(fromManager, Is.EqualTo(fromQuery));
    }

    [Test]
    public void Query_RejectsInvalidCompositionAndPropagatesCallerExceptionsWithoutMutation()
    {
        var manager = new ContentMapManager<string>();
        manager.Add("one", Entry("one"));
        manager.Add("two", Entry("two"));
        ContentEntryRecord[] before = manager.Records.ToArray();
        int events = 0;
        manager.Changed += (_, _) => events++;

        Assert.Throws<ArgumentNullException>(() => manager.GetFilteredRecords(null!));
        Assert.Throws<ArgumentNullException>(() => manager.QueryRecords().Where(null!));
        Assert.Throws<ArgumentNullException>(() => manager.QueryRecords().OrderBy(null!));
        Assert.Throws<ArgumentNullException>(() => manager.QueryRecords().ThenBy(null!));
        Assert.Throws<InvalidOperationException>(() => manager.QueryRecords().ThenBy(Comparer<ContentEntryRecord>.Default));
        Assert.Throws<InvalidOperationException>(() => manager.GetFilteredRecords(_ => throw new InvalidOperationException("Predicate failed.")));
        Assert.Throws<InvalidOperationException>(() => manager.QueryRecords()
            .OrderBy(Comparer<ContentEntryRecord>.Create((_, _) => throw new InvalidOperationException("Comparer failed."))));

        Assert.That(manager.Records, Is.EqualTo(before));
        Assert.That(events, Is.Zero);
    }

    private static ContentManagerBase[] CreatePopulatedManagers()
    {
        var sequence = new ContentSequenceManager(new ContentSequenceStructure(
            ContentOverflowPolicy.None,
            ContentSequenceReadOrder.NewestFirst));
        sequence.Add(Entry("first"));
        sequence.Add(Entry("second"));

        var map = new ContentMapManager<string>();
        map.Add("first", Entry("first"));
        map.Add("second", Entry("second"));

        var single = new ContentSingleManager(new ContentSingleStructure());
        single.Set(Entry("first"));

        var stack = new ContentStackManager(new ContentStackStructure(
            ContentOverflowPolicy.None,
            ContentStackReadOrder.BottomFirst));
        stack.Push(Entry("first"));
        stack.Push(Entry("second"));

        var compound = new ContentCompoundManager(new ContentCompoundStructure(
            siblingReadOrder: ContentCompoundSiblingReadOrder.NewestFirst));
        ContentCompoundNode root = compound.AddRoot(Entry("first"));
        compound.AddChild(root, Entry("second"));

        return new ContentManagerBase[] { sequence, map, single, stack, compound };
    }

    private static void AssertReadOnly(IReadOnlyList<ContentEntryRecord> records)
    {
        Assert.Throws<NotSupportedException>(() => ((IList<ContentEntryRecord>)records).Clear());
    }

    private static PlainContentEntry Entry(string text) => new PlainContentEntry(DateTimeOffset.UnixEpoch, text);
}
