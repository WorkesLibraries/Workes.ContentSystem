using System;
using System.Collections.Generic;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentCompoundStructureTests
{
    [Test]
    public void AddRoot_WithGeneratedId_CreatesRootNode()
    {
        var structure = new ContentCompoundStructure();

        ContentCompoundNode node = structure.AddRoot(Entry("Root"));

        Assert.That(node.Record.Id, Is.EqualTo(new ContentEntryId("1")));
        Assert.That(node.ParentId, Is.Null);
        Assert.That(structure.GetRoots().Select(root => root.Record.PlainText), Is.EqualTo(new[] { "Root" }));
    }

    [Test]
    public void AddRoot_WithExplicitId_CreatesRootNodeAndAdvancesGeneratedIds()
    {
        var structure = new ContentCompoundStructure();

        ContentCompoundNode manual = structure.AddRoot(10, Entry("Manual"));
        ContentCompoundNode generated = structure.AddRoot(Entry("Generated"));

        Assert.That(manual.Record.Id, Is.EqualTo(new ContentEntryId("10")));
        Assert.That(generated.Record.Id, Is.EqualTo(new ContentEntryId("11")));
    }

    [Test]
    public void AddChild_WithGeneratedAndExplicitIds_CreatesChildren()
    {
        var structure = new ContentCompoundStructure();
        ContentCompoundNode root = structure.AddRoot(Entry("Root"));

        ContentCompoundNode generated = structure.AddChild(1, Entry("Generated child"));
        ContentCompoundNode manual = structure.AddChild(1, 10, Entry("Manual child"));

        Assert.That(generated.ParentId, Is.EqualTo(root.Record.Id));
        Assert.That(manual.ParentId, Is.EqualTo(root.Record.Id));
        Assert.That(structure.GetChildren(1).Select(child => child.Record.PlainText), Is.EqualTo(new[] { "Generated child", "Manual child" }));
    }

    [Test]
    public void Records_FlattensDepthFirstOldestFirst()
    {
        var structure = new ContentCompoundStructure();
        structure.AddRoot(Entry("A"));
        structure.AddRoot(Entry("B"));
        structure.AddChild(1, Entry("A1"));
        structure.AddChild(1, Entry("A2"));
        structure.AddChild(3, Entry("A1a"));

        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "A", "A1", "A1a", "A2", "B" }));
    }

    [Test]
    public void Records_CanFlattenNewestFirst()
    {
        var structure = new ContentCompoundStructure(
            ContentCompoundChildRemovalPolicy.Reject,
            ContentCompoundSiblingReadOrder.NewestFirst);
        structure.AddRoot(Entry("A"));
        structure.AddRoot(Entry("B"));
        structure.AddChild(1, Entry("A1"));
        structure.AddChild(1, Entry("A2"));

        Assert.That(structure.GetRoots().Select(root => root.Record.PlainText), Is.EqualTo(new[] { "B", "A" }));
        Assert.That(structure.GetChildren(1).Select(child => child.Record.PlainText), Is.EqualTo(new[] { "A2", "A1" }));
        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "B", "A", "A2", "A1" }));
    }

    [Test]
    public void AddRoot_WithDuplicateId_ReturnsDuplicateFailure()
    {
        var structure = new ContentCompoundStructure();
        structure.AddRoot(1, Entry("First"));

        bool accepted = structure.TryAddRoot(1, Entry("Second"), out ContentCompoundNode? node, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(node, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryIdDuplicate));
        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "First" }));
    }

    [Test]
    public void AddRoot_WithInvalidId_ReturnsInvalidFailure()
    {
        var structure = new ContentCompoundStructure();

        bool accepted = structure.TryAddRoot(0, Entry("Invalid"), out ContentCompoundNode? node, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(node, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void AddChild_WithMissingParent_ReturnsEntryNotFoundFailure()
    {
        var structure = new ContentCompoundStructure();

        bool accepted = structure.TryAddChild(1, Entry("Child"), out ContentCompoundNode? node, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(node, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Remove_WithRejectPolicyAndChildren_ReturnsStructureFailureAndEmitsNoEvent()
    {
        var structure = new ContentCompoundStructure(ContentCompoundChildRemovalPolicy.Reject);
        structure.AddRoot(Entry("Root"));
        structure.AddChild(1, Entry("Child"));
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        bool removed = structure.TryRemove(1, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure);

        Assert.That(removed, Is.False);
        Assert.That(removedRecords, Is.Empty);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.StructureRejected));
        Assert.That(eventCount, Is.EqualTo(0));
        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "Root", "Child" }));
    }

    [Test]
    public void Remove_WithRemoveSubtreePolicy_RemovesParentAndDescendants()
    {
        var structure = new ContentCompoundStructure(ContentCompoundChildRemovalPolicy.RemoveSubtree);
        structure.AddRoot(Entry("Root"));
        structure.AddChild(1, Entry("Child"));
        structure.AddChild(2, Entry("Grandchild"));
        ContentChangedEventArgs? changedArgs = null;
        structure.Changed += (_, args) => changedArgs = args;

        IReadOnlyList<ContentEntryRecord> removed = structure.Remove(1);

        Assert.That(removed.Select(record => record.PlainText), Is.EqualTo(new[] { "Root", "Child", "Grandchild" }));
        Assert.That(structure.Records, Is.Empty);
        Assert.That(changedArgs?.Kind, Is.EqualTo(ContentChangeKind.Removed));
        Assert.That(changedArgs?.RequiresFullRefresh, Is.True);
    }

    [Test]
    public void Clear_RemovesAllRecordsAndKeepsGeneratedIdsCoherent()
    {
        var structure = new ContentCompoundStructure();
        structure.AddRoot(Entry("First"));
        structure.AddRoot(10, Entry("Manual"));

        IReadOnlyList<ContentEntryRecord> removed = structure.Clear();
        ContentCompoundNode next = structure.AddRoot(Entry("Next"));

        Assert.That(removed.Select(record => record.PlainText), Is.EqualTo(new[] { "First", "Manual" }));
        Assert.That(next.Record.Id, Is.EqualTo(new ContentEntryId("11")));
    }

    [Test]
    public void CreateManager_ReturnsContentCompoundManager()
    {
        var structure = new ContentCompoundStructure();

        ContentManagerBase manager = structure.CreateManager();

        Assert.That(manager, Is.TypeOf<ContentCompoundManager>());
    }

    [Test]
    public void Manager_ForStructure_ResolvesCompoundManagerAndForwardsEventsWithManagerSender()
    {
        var manager = ContentManagers.ForStructure<ContentCompoundManager>(new ContentCompoundStructure());
        object? sender = null;
        manager.Changed += (eventSender, _) => sender = eventSender;

        manager.AddRoot(Entry("Root"));

        Assert.That(sender, Is.SameAs(manager));
    }

    [Test]
    public void GenericCompoundManager_UsesCustomGeneratedStringIds()
    {
        var structure = new ContentCompoundStructure<string>(new TestStringGeneratedIdSource());
        var manager = ContentManagers.ForStructure<ContentCompoundManager<string>>(structure);

        ContentCompoundNode root = manager.AddRoot(Entry("Root"));
        ContentCompoundNode child = manager.AddChild("node-1", "manual", Entry("Child"));
        ContentCompoundNode next = manager.AddRoot(Entry("Next"));

        Assert.That(root.Record.Id, Is.EqualTo(new ContentEntryId("node-1")));
        Assert.That(child.ParentId, Is.EqualTo(root.Record.Id));
        Assert.That(next.Record.Id, Is.EqualTo(new ContentEntryId("node-2")));
    }

    [Test]
    public void GenericCompound_CanUseGuidGeneratedIdSource()
    {
        var structure = new ContentCompoundStructure<Guid>(new GuidContentGeneratedIdSource());
        Guid childId = Guid.Parse("9fd3efda-747d-4a60-81c1-c38ed2d60774");

        ContentCompoundNode root = structure.AddRoot(Entry("Root"));
        ContentCompoundNode child = structure.AddChild(Guid.Parse(root.Record.Id.Value), childId, Entry("Child"));

        Assert.That(Guid.Parse(root.Record.Id.Value), Is.Not.EqualTo(Guid.Empty));
        Assert.That(child.Record.Id, Is.EqualTo(new ContentEntryId("9fd3efda-747d-4a60-81c1-c38ed2d60774")));
        Assert.That(child.ParentId, Is.EqualTo(root.Record.Id));
    }

    [Test]
    public void TypedManagerMismatch_StillReturnsManagerMismatch()
    {
        bool resolved = ContentManagers.TryForStructure<ContentSequenceManager>(
            new ContentCompoundStructure(),
            out ContentSequenceManager? manager,
            out ContentFailure? failure);

        Assert.That(resolved, Is.False);
        Assert.That(manager, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.ManagerMismatch));
    }

    [Test]
    public void CaptureAndRestore_LongCompound_PreservesHierarchyPoliciesAndFutureGeneratedIds()
    {
        var structure = new ContentCompoundStructure(
            ContentCompoundChildRemovalPolicy.RemoveSubtree,
            ContentCompoundSiblingReadOrder.NewestFirst);
        structure.AddRoot(Entry("A"));
        structure.AddRoot(Entry("B"));
        structure.AddChild(1, Entry("A1"));
        structure.AddChild(1, Entry("A2"));
        structure.AddChild(3, Entry("A1a"));

        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        var restored = (ContentCompoundStructure)ContentCompoundStructure.Factory.Restore(snapshot);

        Assert.That(snapshot.Kind, Is.EqualTo(ContentCompoundStructure.SnapshotKind));
        Assert.That(restored.ChildRemovalPolicy, Is.EqualTo(ContentCompoundChildRemovalPolicy.RemoveSubtree));
        Assert.That(restored.SiblingReadOrder, Is.EqualTo(ContentCompoundSiblingReadOrder.NewestFirst));
        Assert.That(restored.GetRoots().Select(root => root.Record.PlainText), Is.EqualTo(new[] { "B", "A" }));
        Assert.That(restored.GetChildren(1).Select(child => child.Record.PlainText), Is.EqualTo(new[] { "A2", "A1" }));
        Assert.That(restored.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "B", "A", "A2", "A1", "A1a" }));

        ContentCompoundNode next = restored.AddRoot(Entry("Next"));

        Assert.That(next.Record.Id, Is.EqualTo(new ContentEntryId("6")));
    }

    [Test]
    public void CaptureAndRestore_CustomIdCompound_PreservesSourceState()
    {
        var structure = new ContentCompoundStructure<string>(
            new TestStringGeneratedIdSource(),
            ContentCompoundChildRemovalPolicy.RemoveSubtree);
        structure.AddRoot(Entry("Root"));
        structure.AddChild("node-1", "node-10", Entry("Manual child"));

        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        var restored = (ContentCompoundStructure<string>)ContentCompoundStructure<string>
            .CreateSnapshotFactory(TestStringGeneratedIdSource.Factory)
            .Restore(snapshot);
        ContentCompoundNode next = restored.AddRoot(Entry("Next"));

        Assert.That(restored.GetChildren("node-1").Select(child => child.Record.Id.Value), Is.EqualTo(new[] { "node-10" }));
        Assert.That(next.Record.Id.Value, Is.EqualTo("node-11"));
    }

    [Test]
    public void Manager_RestoreSnapshot_UsesCompoundFactoryAndEmitsSnapshotRestoredEvent()
    {
        var source = new ContentCompoundStructure(ContentCompoundChildRemovalPolicy.RemoveSubtree);
        source.AddRoot(Entry("Root"));
        source.AddChild(1, Entry("Child"));
        ContentStructureSnapshot snapshot = source.CaptureSnapshot();
        var manager = ContentManagers.ForStructure<ContentCompoundManager>(new ContentCompoundStructure());
        ContentChangedEventArgs? changedArgs = null;
        object? sender = null;
        manager.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            changedArgs = args;
        };

        manager.RestoreSnapshot(snapshot);

        Assert.That(sender, Is.SameAs(manager));
        Assert.That(changedArgs?.Kind, Is.EqualTo(ContentChangeKind.SnapshotRestored));
        Assert.That(changedArgs?.RequiresFullRefresh, Is.True);
        Assert.That(manager.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "Root", "Child" }));
    }

    [Test]
    public void Manager_RestoreSnapshot_WhenRejected_PreservesStateAndEmitsNoEvent()
    {
        var source = new ContentCompoundStructure();
        source.AddRoot(Entry("Snapshot root"));
        ContentStructureSnapshot snapshot = source.CaptureSnapshot();
        SetDataString(snapshot, "idSourceKind", "wrong.kind");
        var manager = ContentManagers.ForStructure<ContentCompoundManager>(new ContentCompoundStructure());
        manager.AddRoot(Entry("Existing"));
        int eventCount = 0;
        manager.Changed += (_, _) => eventCount++;

        bool restored = manager.TryRestoreSnapshot(snapshot, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(failure?.Kind, Is.EqualTo(ContentFailureKind.Snapshot));
        Assert.That(eventCount, Is.EqualTo(0));
        Assert.That(manager.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "Existing" }));
    }

    [Test]
    public void RestoreSnapshot_WithMissingRelationship_ReturnsMalformedFailure()
    {
        ContentStructureSnapshot snapshot = CreateCompoundSnapshot();
        Relationships(snapshot).RemoveAt(1);

        bool restored = ContentCompoundStructure.Factory.TryRestore(snapshot, out IContentStructure? structure, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(structure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void RestoreSnapshot_WithDuplicateRelationship_ReturnsMalformedFailure()
    {
        ContentStructureSnapshot snapshot = CreateCompoundSnapshot();
        List<ContentSnapshotEncodedValue> relationships = Relationships(snapshot);
        relationships[1] = relationships[0];

        bool restored = ContentCompoundStructure.Factory.TryRestore(snapshot, out IContentStructure? structure, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(structure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void RestoreSnapshot_WithUnknownParent_ReturnsMalformedFailure()
    {
        ContentStructureSnapshot snapshot = CreateCompoundSnapshot();
        SetRelationshipParent(snapshot, relationshipIndex: 1, "999");

        bool restored = ContentCompoundStructure.Factory.TryRestore(snapshot, out IContentStructure? structure, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(structure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void RestoreSnapshot_WithInvalidRestoredId_ReturnsInvalidIdFailure()
    {
        ContentStructureSnapshot snapshot = CreateCompoundSnapshot();
        snapshot.Records[1].EntryId = "not-a-number";
        SetRelationshipId(snapshot, relationshipIndex: 1, "not-a-number");

        bool restored = ContentCompoundStructure.Factory.TryRestore(snapshot, out IContentStructure? structure, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(structure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void RestoreSnapshot_WithInvalidEnum_ReturnsMalformedFailure()
    {
        ContentStructureSnapshot snapshot = CreateCompoundSnapshot();
        SetDataString(snapshot, "childRemovalPolicy", "Explode");

        bool restored = ContentCompoundStructure.Factory.TryRestore(snapshot, out IContentStructure? structure, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(structure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void RestoreSnapshot_WithUnsupportedVersion_ReturnsUnsupportedVersionFailure()
    {
        ContentStructureSnapshot snapshot = CreateCompoundSnapshot();
        snapshot.DataVersion = 999;

        bool restored = ContentCompoundStructure.Factory.TryRestore(snapshot, out IContentStructure? structure, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(structure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedVersion));
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }

    private static ContentStructureSnapshot CreateCompoundSnapshot()
    {
        var structure = new ContentCompoundStructure(ContentCompoundChildRemovalPolicy.RemoveSubtree);
        structure.AddRoot(Entry("Root"));
        structure.AddChild(1, Entry("Child"));
        return structure.CaptureSnapshot();
    }

    private static List<ContentSnapshotEncodedValue> Relationships(ContentStructureSnapshot snapshot)
    {
        return snapshot.Data.Properties.Single(property => property.Name == "relationships").Value.Data.Items;
    }

    private static void SetDataString(ContentStructureSnapshot snapshot, string propertyName, string value)
    {
        ContentSnapshotNamedValue property = snapshot.Data.Properties.Single(candidate => candidate.Name == propertyName);
        property.Value = ContentSnapshotCodecs.Encode(value);
    }

    private static void SetRelationshipId(ContentStructureSnapshot snapshot, int relationshipIndex, string id)
    {
        ContentSnapshotValue data = Relationships(snapshot)[relationshipIndex].Data;
        ContentSnapshotNamedValue property = data.Properties.Single(candidate => candidate.Name == "id");
        property.Value = ContentSnapshotCodecs.Encode(id);
    }

    private static void SetRelationshipParent(ContentStructureSnapshot snapshot, int relationshipIndex, string parentId)
    {
        ContentSnapshotValue data = Relationships(snapshot)[relationshipIndex].Data;
        data.Properties.Single(candidate => candidate.Name == "hasParent").Value = ContentSnapshotCodecs.Encode(true);
        data.Properties.Single(candidate => candidate.Name == "parentId").Value = ContentSnapshotCodecs.Encode(parentId);
    }

    private sealed class TestStringGeneratedIdSource : IContentGeneratedIdSource<string>
    {
        private int _next = 1;

        public static IContentGeneratedIdSourceFactory<string> Factory { get; } = new TestStringGeneratedIdSourceFactory();

        public TestStringGeneratedIdSource()
        {
        }

        private TestStringGeneratedIdSource(int next)
        {
            _next = next;
        }

        public IContentEntryIdStrategy<string> IdStrategy { get; } = new StringContentEntryIdStrategy();

        public IContentGeneratedIdSourceFactory<string> SnapshotFactory => Factory;

        public bool TryCreateNext(out string id, out ContentFailure? failure)
        {
            id = "node-" + _next.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _next++;
            failure = null;
            return true;
        }

        public bool TryObserve(string id, out ContentFailure? failure)
        {
            if (!IdStrategy.TryNormalize(id, out ContentEntryId normalizedId, out failure))
            {
                return false;
            }

            return TryObserveNormalized(normalizedId, out failure);
        }

        public bool TryObserveNormalized(ContentEntryId id, out ContentFailure? failure)
        {
            if (!IdStrategy.TryValidateNormalized(id, out failure))
            {
                return false;
            }

            const string prefix = "node-";
            if (id.Value.StartsWith(prefix, StringComparison.Ordinal)
                && int.TryParse(id.Value.Substring(prefix.Length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int parsed)
                && parsed >= _next)
            {
                _next = parsed + 1;
            }

            failure = null;
            return true;
        }

        public bool TryCaptureSnapshot(out ContentSnapshotValue? snapshot, out ContentFailure? failure)
        {
            snapshot = ContentSnapshotValue.Object(new[]
            {
                ContentSnapshotProperties.Named("next", ContentSnapshotCodecs.Encode(_next))
            });
            failure = null;
            return true;
        }

        public static TestStringGeneratedIdSource Restore(ContentSnapshotValue snapshot)
        {
            if (!ContentSnapshotProperties.TryDecodeRequiredInt32(snapshot, "next", out int next, out ContentFailure? failure))
            {
                throw new ContentOperationException(failure!);
            }

            return new TestStringGeneratedIdSource(next);
        }
    }

    private sealed class TestStringGeneratedIdSourceFactory : IContentGeneratedIdSourceFactory<string>
    {
        public string Kind => "test.string";

        public bool TryRestore(ContentSnapshotValue snapshot, out IContentGeneratedIdSource<string>? source, out ContentFailure? failure)
        {
            source = TestStringGeneratedIdSource.Restore(snapshot);
            failure = null;
            return true;
        }

        public IContentGeneratedIdSource<string> Restore(ContentSnapshotValue snapshot)
        {
            return TestStringGeneratedIdSource.Restore(snapshot);
        }
    }
}
