using System;
using System.Collections.Generic;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class KeyedContentManagerTests
{
    [Test]
    public void DefaultStringManager_UsesKeyedStrategy()
    {
        var manager = new KeyedContentManager<string>();

        ContentEntryRecord added = manager.Add("entry-1", Entry("First"));

        Assert.That(added.Id, Is.EqualTo(new ContentEntryId("entry-1")));
        Assert.That(manager.Get("entry-1"), Is.SameAs(added));
    }

    [Test]
    public void DefaultLongManager_UsesKeyedStrategy()
    {
        var manager = new KeyedContentManager<long>();

        ContentEntryRecord added = manager.Add(8, Entry("Eighth"));

        Assert.That(added.Id, Is.EqualTo(new ContentEntryId("8")));
        Assert.That(manager.Get(8), Is.SameAs(added));
    }

    [Test]
    public void Constructor_AcceptsCustomKeyedStructure()
    {
        var structure = new TestKeyedContentStructure();
        var manager = new KeyedContentManager<CustomId>(structure);

        ContentEntryRecord added = manager.Add(new CustomId("custom"), Entry("Custom"));

        Assert.That(manager.Structure, Is.SameAs(structure));
        Assert.That(manager.Records, Is.EqualTo(new[] { added }));
    }

    [Test]
    public void Constructor_NullStrategyThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new KeyedContentManager<string>((IContentEntryIdStrategy<string>)null!));
    }

    [Test]
    public void Constructor_UnsupportedDefaultStrategyThrows()
    {
        Assert.Throws<NotSupportedException>(() => new KeyedContentManager<Guid>());
    }

    [Test]
    public void Constructor_NullStructureThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new KeyedContentManager<string>((IKeyedContentStructure<string>)null!));
    }

    [Test]
    public void Changed_ForwardsKeyedStructureEventsWithManagerSender()
    {
        var manager = new KeyedContentManager<string>();
        ContentChangedEventArgs? changedArgs = null;
        object? sender = null;
        manager.Changed += (eventSender, args) =>
        {
            sender = eventSender;
            changedArgs = args;
        };

        ContentEntryRecord added = manager.Add("entry", Entry("Stored"));

        Assert.That(sender, Is.SameAs(manager));
        Assert.That(changedArgs, Is.Not.Null);
        Assert.That(changedArgs!.AddedRecords, Is.EqualTo(new[] { added }));
    }

    [Test]
    public void TryAdd_DuplicateIdReturnsFailure()
    {
        var manager = new KeyedContentManager<string>();
        manager.Add("duplicate", Entry("First"));

        bool accepted = manager.TryAdd("duplicate", Entry("Second"), out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdDuplicate));
    }

    [Test]
    public void Add_InvalidIdThrowsContentOperationException()
    {
        var manager = new KeyedContentManager<string>();

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Add("   ", Entry("Invalid")));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void TryGet_MissingIdReturnsEntryNotFoundFailure()
    {
        var manager = new KeyedContentManager<string>();

        bool found = manager.TryGet("missing", out ContentEntryRecord? record, out ContentFailure? failure);

        Assert.That(found, Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Get_MissingIdThrowsContentOperationException()
    {
        var manager = new KeyedContentManager<string>();

        ContentOperationException? exception = Assert.Throws<ContentOperationException>(() => manager.Get("missing"));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Failure.Code, Is.EqualTo(ContentFailureCodes.EntryNotFound));
    }

    [Test]
    public void Add_NullEntryThrows()
    {
        var manager = new KeyedContentManager<string>();

        Assert.Throws<ArgumentNullException>(() => manager.Add("entry", null!));
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

    private sealed class TestKeyedContentStructure : IKeyedContentStructure<CustomId>
    {
        private readonly Dictionary<ContentEntryId, ContentEntryRecord> _recordsById = new Dictionary<ContentEntryId, ContentEntryRecord>();
        private readonly List<ContentEntryRecord> _records = new List<ContentEntryRecord>();

        public IReadOnlyList<ContentEntryRecord> Records => _records.ToArray();

        public bool TryAdd(CustomId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            ContentEntryId normalizedId = new ContentEntryId(id.Value);
            record = new ContentEntryRecord(normalizedId, entry);
            _recordsById.Add(normalizedId, record);
            _records.Add(record);
            failure = null;
            return true;
        }

        public ContentEntryRecord Add(CustomId id, IContentEntry entry)
        {
            if (TryAdd(id, entry, out ContentEntryRecord? record, out ContentFailure? failure))
            {
                return record!;
            }

            throw new ContentOperationException(failure!);
        }

        public bool TryGet(CustomId id, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            return TryGet(new ContentEntryId(id.Value), out record, out failure);
        }

        public ContentEntryRecord Get(CustomId id)
        {
            if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure))
            {
                return record!;
            }

            throw new ContentOperationException(failure!);
        }

        public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            if (_recordsById.TryGetValue(id, out record))
            {
                failure = null;
                return true;
            }

            failure = ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryNotFound, "Missing.");
            return false;
        }

        public ContentEntryRecord Get(ContentEntryId id)
        {
            if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure))
            {
                return record!;
            }

            throw new ContentOperationException(failure!);
        }
    }
}
