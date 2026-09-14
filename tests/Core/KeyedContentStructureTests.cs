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

        bool accepted = structure.TryAdd("duplicate", Entry("Second"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Kind, Is.EqualTo(ContentFailureKind.Entry));
        Assert.That(failure.Code, Is.EqualTo(ContentFailureCodes.EntryIdDuplicate));
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

        bool accepted = structure.TryAdd(id!, Entry("Entry"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
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
