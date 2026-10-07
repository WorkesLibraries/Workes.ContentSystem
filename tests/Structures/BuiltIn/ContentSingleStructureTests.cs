using System;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentSingleStructureTests
{
    [Test]
    public void Set_WithGeneratedId_StoresCurrentRecord()
    {
        var structure = new ContentSingleStructure();

        ContentEntryRecord record = structure.Set(Entry("Ready"));

        Assert.That(record.Id, Is.EqualTo(new ContentEntryId("1")));
        Assert.That(structure.GetCurrent(), Is.SameAs(record));
        Assert.That(structure.Records, Is.EqualTo(new[] { record }));
    }

    [Test]
    public void Set_WithExplicitId_StoresCurrentRecord()
    {
        var structure = new ContentSingleStructure();

        ContentEntryRecord record = structure.Set(42, Entry("Manual"));

        Assert.That(record.Id, Is.EqualTo(new ContentEntryId("42")));
        Assert.That(structure.Get(42), Is.SameAs(record));
    }

    [Test]
    public void GenericSingle_CanUseGuidGeneratedIdSource()
    {
        var structure = new ContentSingleStructure<Guid>(
            new GuidContentGeneratedIdSource(),
            ContentSingleReplacementPolicy.Replace);
        Guid explicitId = Guid.Parse("9fd3efda-747d-4a60-81c1-c38ed2d60774");

        ContentEntryRecord generated = structure.Set(Entry("Generated"));
        ContentEntryRecord explicitRecord = structure.Set(explicitId, Entry("Explicit"));

        Assert.That(Guid.Parse(generated.Id.Value), Is.Not.EqualTo(Guid.Empty));
        Assert.That(explicitRecord.Id, Is.EqualTo(new ContentEntryId("9fd3efda-747d-4a60-81c1-c38ed2d60774")));
        Assert.That(structure.Get(explicitId), Is.SameAs(explicitRecord));
    }

    [Test]
    public void Set_WhenPolicyIsReplace_ReplacesCurrentRecordAndEmitsReplacedEvent()
    {
        var structure = new ContentSingleStructure(ContentSingleReplacementPolicy.Replace);
        ContentEntryRecord original = structure.Set(Entry("First"));
        ContentChangedEventArgs? changedArgs = null;
        structure.Changed += (_, args) => changedArgs = args;

        ContentEntryRecord replacement = structure.Set(Entry("Second"));

        Assert.That(structure.GetCurrent(), Is.SameAs(replacement));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.Kind, Is.EqualTo(ContentChangeKind.Replaced));
        Assert.That(changedArgs.AddedRecords, Is.EqualTo(new[] { replacement }));
        Assert.That(changedArgs.RemovedRecords, Is.EqualTo(new[] { original }));
    }

    [Test]
    public void Set_WhenPolicyIsRejectAndOccupied_ReturnsCapacityFailureAndEmitsNoEvent()
    {
        var structure = new ContentSingleStructure(ContentSingleReplacementPolicy.Reject);
        structure.Set(Entry("First"));
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        bool accepted = structure.TrySet(Entry("Second"), out ContentEntryRecord? record, out ContentEntryRecord? replaced, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(replaced, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.StructureCapacityReached));
        Assert.That(eventCount, Is.EqualTo(0));
        Assert.That(structure.GetCurrent().PlainText, Is.EqualTo("First"));
    }

    [Test]
    public void Clear_RemovesCurrentRecord()
    {
        var structure = new ContentSingleStructure();
        ContentEntryRecord record = structure.Set(Entry("Ready"));

        var removed = structure.Clear();

        Assert.That(removed, Is.EqualTo(new[] { record }));
        Assert.That(structure.Records, Is.Empty);
    }

    [Test]
    public void CaptureAndRestore_PreservesPolicyRecordAndGeneratedIdState()
    {
        var structure = new ContentSingleStructure(ContentSingleReplacementPolicy.Replace);
        structure.Set(Entry("First"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();

        var restored = (ContentSingleStructure)ContentSingleStructure.Factory.Restore(snapshot);
        ContentEntryRecord next = restored.Set(Entry("Second"));

        Assert.That(restored.ReplacementPolicy, Is.EqualTo(ContentSingleReplacementPolicy.Replace));
        Assert.That(next.Id, Is.EqualTo(new ContentEntryId("2")));
    }

    [Test]
    public void CreateManager_ReturnsContentSingleManager()
    {
        var structure = new ContentSingleStructure();

        ContentManagerBase manager = structure.CreateManager();

        Assert.That(manager, Is.TypeOf<ContentSingleManager>());
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }
}
