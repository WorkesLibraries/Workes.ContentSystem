using System;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class KeyedContentStructureTests
{
    [Test]
    public void Constructor_NullStrategyThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new KeyedContentStructure<string>(null!));
    }

    [Test]
    public void Constructor_DefaultStringStrategyWorks()
    {
        var structure = new KeyedContentStructure<string>();

        ContentEntryRecord record = structure.Add("thread-main", Entry("First"));

        Assert.That(record.Id, Is.EqualTo(new ContentEntryId("thread-main")));
    }

    [Test]
    public void Constructor_DefaultLongStrategyWorks()
    {
        var structure = new KeyedContentStructure<long>();

        ContentEntryRecord record = structure.Add(8, Entry("Eighth"));

        Assert.That(record.Id, Is.EqualTo(new ContentEntryId("8")));
    }

    [Test]
    public void Constructor_UnsupportedDefaultStrategyThrows()
    {
        Assert.Throws<NotSupportedException>(() => new KeyedContentStructure<Guid>());
    }

    [Test]
    public void TryAdd_WithStringStrategy_AddsRecord()
    {
        var structure = new KeyedContentStructure<string>();
        var entry = Entry("First");

        bool accepted = structure.TryAdd("thread-main", entry, out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(record, Is.Not.Null);
        Assert.That(record!.Id, Is.EqualTo(new ContentEntryId("thread-main")));
        Assert.That(record.Entry, Is.SameAs(entry));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void Add_EmitsChangedWithAddedRecord()
    {
        var structure = new KeyedContentStructure<string>();
        ContentChangedEventArgs? changedArgs = null;
        object? sender = null;
        structure.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            changedArgs = args;
        };

        ContentEntryRecord record = structure.Add("entry", Entry("First"));

        Assert.That(sender, Is.SameAs(structure));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.AddedRecords, Is.EqualTo(new[] { record }));
        Assert.That(changedArgs.RemovedRecords, Is.Empty);
        Assert.That(changedArgs.Kind, Is.EqualTo(ContentChangeKind.Added));
    }

    [Test]
    public void Records_AreReadInInsertionOrder()
    {
        var structure = new KeyedContentStructure<string>();

        structure.Add("first", Entry("First"));
        structure.Add("second", Entry("Second"));
        structure.Add("third", Entry("Third"));

        Assert.That(structure.Records.Select(record => record.PlainText), Is.EqualTo(new[] { "First", "Second", "Third" }));
    }

    [Test]
    public void Get_WithStringStrategy_ReturnsRecordByStringId()
    {
        var structure = new KeyedContentStructure<string>();
        ContentEntryRecord added = structure.Add("thread-main", Entry("First"));

        ContentEntryRecord found = structure.Get("thread-main");

        Assert.That(found, Is.SameAs(added));
    }

    [Test]
    public void Add_WithIntegerStrategy_AddsRecordByLongId()
    {
        var structure = new KeyedContentStructure<long>();

        ContentEntryRecord added = structure.Add(8, Entry("Eighth"));
        ContentEntryRecord found = structure.Get(8);

        Assert.That(added.Id, Is.EqualTo(new ContentEntryId("8")));
        Assert.That(found, Is.SameAs(added));
    }

    [Test]
    public void ExplicitCustomStrategy_CanSupportCustomIdType()
    {
        var structure = new KeyedContentStructure<CustomId>(new CustomIdStrategy());

        ContentEntryRecord added = structure.Add(new CustomId("quest-main"), Entry("Quest"));

        Assert.That(added.Id, Is.EqualTo(new ContentEntryId("quest-main")));
        Assert.That(structure.Get(new CustomId("quest-main")), Is.SameAs(added));
    }

    [Test]
    public void TryAdd_DuplicateIdReturnsFailure()
    {
        var structure = new KeyedContentStructure<string>();
        structure.Add("duplicate", Entry("First"));
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        bool accepted = structure.TryAdd("duplicate", Entry("Second"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Kind, Is.EqualTo(ContentFailureKind.Entry));
        Assert.That(failure.Code, Is.EqualTo(ContentFailureCodes.EntryIdDuplicate));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void Add_DuplicateIdThrowsContentOperationException()
    {
        var structure = new KeyedContentStructure<string>();
        structure.Add("duplicate", Entry("First"));

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => structure.Add("duplicate", Entry("Second")));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryIdDuplicate));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void TryAdd_InvalidStringIdReturnsFailure(string? id)
    {
        var structure = new KeyedContentStructure<string>();
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        bool accepted = structure.TryAdd(id!, Entry("Entry"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void TryAdd_IntegerStrategyRejectsInvalidNumericId()
    {
        var structure = new KeyedContentStructure<long>();

        bool accepted = structure.TryAdd(0, Entry("Entry"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void TryGet_MissingIdReturnsEntryNotFoundFailure()
    {
        var structure = new KeyedContentStructure<string>();

        bool found = structure.TryGet("missing", out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(found, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void TryRemove_WithStringId_RemovesRecordAndEmitsEvent()
    {
        var structure = new KeyedContentStructure<string>();
        ContentEntryRecord first = structure.Add("first", Entry("First"));
        ContentEntryRecord second = structure.Add("second", Entry("Second"));
        ContentChangedEventArgs? changedArgs = null;
        structure.Changed += (_, args) => changedArgs = args;

        bool removed = structure.TryRemove("first", out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.True);
        Assert.That(removedRecord, Is.SameAs(first));
        Assert.That(failure, Is.Null);
        Assert.That(structure.Records, Is.EqualTo(new[] { second }));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.Kind, Is.EqualTo(ContentChangeKind.Removed));
        Assert.That(changedArgs.RemovedRecords, Is.EqualTo(new[] { first }));
    }

    [Test]
    public void TryRemove_WithLongId_RemovesRecord()
    {
        var structure = new KeyedContentStructure<long>();
        ContentEntryRecord record = structure.Add(8, Entry("Eighth"));

        bool removed = structure.TryRemove(8, out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.True);
        Assert.That(removedRecord, Is.SameAs(record));
        Assert.That(failure, Is.Null);
        Assert.That(structure.Records, Is.Empty);
    }

    [Test]
    public void TryRemove_InvalidIdReturnsFailureAndEmitsNoEvent()
    {
        var structure = new KeyedContentStructure<string>();
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        bool removed = structure.TryRemove("   ", out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.False);
        Assert.That(removedRecord, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void TryRemove_MissingIdReturnsEntryNotFoundAndEmitsNoEvent()
    {
        var structure = new KeyedContentStructure<string>();
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        bool removed = structure.TryRemove("missing", out ContentEntryRecord? removedRecord, out ContentFailure? failure);

        Assert.That(removed, Is.False);
        Assert.That(removedRecord, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void Remove_MissingIdThrowsContentOperationException()
    {
        var structure = new KeyedContentStructure<string>();

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => structure.Remove("missing"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Clear_RemovesAllRecordsAndEmitsEvent()
    {
        var structure = new KeyedContentStructure<string>();
        ContentEntryRecord first = structure.Add("first", Entry("First"));
        ContentEntryRecord second = structure.Add("second", Entry("Second"));
        ContentChangedEventArgs? changedArgs = null;
        structure.Changed += (_, args) => changedArgs = args;

        var removed = structure.Clear();

        Assert.That(removed, Is.EqualTo(new[] { first, second }));
        Assert.That(structure.Records, Is.Empty);
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.Kind, Is.EqualTo(ContentChangeKind.Cleared));
        Assert.That(changedArgs.Cleared, Is.True);
        Assert.That(changedArgs.RemovedRecords, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void Clear_WhenEmptyEmitsNoEvent()
    {
        var structure = new KeyedContentStructure<string>();
        int eventCount = 0;
        structure.Changed += (_, _) => eventCount++;

        var removed = structure.Clear();

        Assert.That(removed, Is.Empty);
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void Add_NullEntryThrows()
    {
        var structure = new KeyedContentStructure<string>();

        Assert.Throws<ArgumentNullException>(() => structure.Add("entry", null!));
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }

    private readonly struct CustomId
    {
        public CustomId(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    private sealed class CustomIdStrategy : IContentEntryIdStrategy<CustomId>
    {
        public bool TryNormalize(CustomId id, out ContentEntryId normalizedId, out ContentFailure? failure)
        {
            if (string.IsNullOrWhiteSpace(id.Value))
            {
                normalizedId = default;
                failure = ContentFailure.Create(
                    ContentFailureKind.Entry,
                    ContentFailureCodes.EntryIdInvalid,
                    "Custom ID cannot be empty.");
                return false;
            }

            normalizedId = new ContentEntryId(id.Value);
            failure = null;
            return true;
        }
    }
}
