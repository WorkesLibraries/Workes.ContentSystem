using System;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentStackStructureTests
{
    [Test]
    public void PushPeekAndPop_UseStackOrder()
    {
        var structure = new ContentStackStructure(ContentOverflowPolicy.None);
        ContentEntryRecord first = structure.Push(Entry("First"));
        ContentEntryRecord second = structure.Push(Entry("Second"));

        ContentEntryRecord peeked = structure.Peek();
        ContentEntryRecord popped = structure.Pop();

        Assert.That(peeked, Is.SameAs(second));
        Assert.That(popped, Is.SameAs(second));
        Assert.That(structure.Peek(), Is.SameAs(first));
    }

    [Test]
    public void Records_DefaultReadOrderIsTopFirst()
    {
        var structure = new ContentStackStructure(ContentOverflowPolicy.None);

        structure.Push(Entry("Bottom"));
        structure.Push(Entry("Top"));

        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "Top", "Bottom" }));
    }

    [Test]
    public void Records_CanReadBottomFirst()
    {
        var structure = new ContentStackStructure(ContentOverflowPolicy.None, ContentStackReadOrder.BottomFirst);

        structure.Push(Entry("Bottom"));
        structure.Push(Entry("Top"));

        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "Bottom", "Top" }));
    }

    [Test]
    public void Push_WhenRejectCapacityReached_ReturnsCapacityFailureAndEmitsNoEvent()
    {
        var structure = new ContentStackStructure(ContentOverflowPolicy.Reject(1));
        structure.Push(Entry("First"));
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        bool accepted = structure.TryPush(Entry("Second"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.StructureCapacityReached));
        Assert.That(eventCount, Is.EqualTo(0));
        Assert.That(structure.Records.Select(item => item.PlainText), Is.EqualTo(new[] { "First" }));
    }

    [Test]
    public void Constructor_DropOldestPolicyThrows()
    {
        Assert.Throws<ArgumentException>(() => new ContentStackStructure(ContentOverflowPolicy.DropOldest(1)));
    }

    [Test]
    public void Push_WithExplicitId_StoresRecord()
    {
        var structure = new ContentStackStructure(ContentOverflowPolicy.None);

        ContentEntryRecord record = structure.Push(10, Entry("Manual"));

        Assert.That(record.Id, Is.EqualTo(new ContentEntryId("10")));
        Assert.That(structure.Get(10), Is.SameAs(record));
    }

    [Test]
    public void GenericStack_CanUseGuidGeneratedIdSource()
    {
        var structure = new ContentStackStructure<Guid>(
            new GuidContentGeneratedIdSource(),
            ContentOverflowPolicy.None);
        Guid explicitId = Guid.Parse("9fd3efda-747d-4a60-81c1-c38ed2d60774");

        ContentEntryRecord generated = structure.Push(Entry("Generated"));
        ContentEntryRecord explicitRecord = structure.Push(explicitId, Entry("Explicit"));

        Assert.That(Guid.Parse(generated.Id.Value), Is.Not.EqualTo(Guid.Empty));
        Assert.That(explicitRecord.Id, Is.EqualTo(new ContentEntryId("9fd3efda-747d-4a60-81c1-c38ed2d60774")));
        Assert.That(structure.Get(explicitId), Is.SameAs(explicitRecord));
    }

    [Test]
    public void CaptureAndRestore_PreservesOrderPolicyAndGeneratedIdState()
    {
        var structure = new ContentStackStructure(ContentOverflowPolicy.Reject(3), ContentStackReadOrder.BottomFirst);
        structure.Push(Entry("First"));
        structure.Push(Entry("Second"));

        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        var restored = (ContentStackStructure)ContentStackStructure.Factory.Restore(snapshot);
        ContentEntryRecord next = restored.Push(Entry("Third"));

        Assert.That(restored.OverflowPolicy, Is.EqualTo(ContentOverflowPolicy.Reject(3)));
        Assert.That(restored.ReadOrder, Is.EqualTo(ContentStackReadOrder.BottomFirst));
        Assert.That(restored.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "First", "Second", "Third" }));
        Assert.That(next.Id, Is.EqualTo(new ContentEntryId("3")));
    }

    [Test]
    public void CreateManager_ReturnsContentStackManager()
    {
        var structure = new ContentStackStructure(ContentOverflowPolicy.None);

        ContentManagerBase manager = structure.CreateManager();

        Assert.That(manager, Is.TypeOf<ContentStackManager>());
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }
}
