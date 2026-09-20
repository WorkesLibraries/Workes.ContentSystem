using System;
using System.Collections.Generic;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentSnapshotTests
{
    [TestCase("hello")]
    [TestCase("")]
    public void ContentSnapshotCodecs_RoundTripString(string value)
    {
        ContentSnapshotEncodedValue encoded = ContentSnapshotCodecs.Encode(value);

        Assert.That(encoded.CodecId, Is.EqualTo(ContentSnapshotCodecs.StringCodecId));
        Assert.That(ContentSnapshotCodecs.Decode<string>(encoded), Is.EqualTo(value));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ContentSnapshotCodecs_RoundTripBoolean(bool value)
    {
        ContentSnapshotEncodedValue encoded = ContentSnapshotCodecs.Encode(value);

        Assert.That(encoded.CodecId, Is.EqualTo(ContentSnapshotCodecs.BooleanCodecId));
        Assert.That(ContentSnapshotCodecs.Decode<bool>(encoded), Is.EqualTo(value));
    }

    [Test]
    public void ContentSnapshotCodecs_RoundTripNumbersAndTimestamp()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 9, 18, 10, 15, 30, TimeSpan.FromHours(2));

        Assert.That(ContentSnapshotCodecs.Decode<int>(ContentSnapshotCodecs.Encode(42)), Is.EqualTo(42));
        Assert.That(ContentSnapshotCodecs.Decode<long>(ContentSnapshotCodecs.Encode(1234567890123L)), Is.EqualTo(1234567890123L));
        Assert.That(ContentSnapshotCodecs.Decode<DateTimeOffset>(ContentSnapshotCodecs.Encode(timestamp)), Is.EqualTo(timestamp));
    }

    [Test]
    public void ContentSnapshotCodecs_RoundTripNull()
    {
        ContentSnapshotEncodedValue encoded = ContentSnapshotCodecs.Encode<string?>(null);

        Assert.That(encoded.CodecId, Is.EqualTo(ContentSnapshotCodecs.NullCodecId));
        Assert.That(ContentSnapshotCodecs.Decode<string?>(encoded), Is.Null);
    }

    [Test]
    public void ContentSnapshotCodecs_RejectUnsupportedEncodeType()
    {
        bool encoded = ContentSnapshotCodecs.TryEncode(
            new UnsupportedPayload(),
            out ContentSnapshotEncodedValue? value,
            out ContentFailure? failure);

        Assert.That(encoded, Is.False);
        Assert.That(value, Is.Null);
        Assert.That(failure?.Kind, Is.EqualTo(ContentFailureKind.Snapshot));
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotCodecRejected));
    }

    [Test]
    public void ContentSnapshotCodecs_RejectUnsupportedCodecVersion()
    {
        var encoded = ContentSnapshotCodecs.Encode("hello");
        encoded.CodecVersion = 99;

        bool decoded = ContentSnapshotCodecs.TryDecode(encoded, out string value, out ContentFailure? failure);

        Assert.That(decoded, Is.False);
        Assert.That(value, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedVersion));
    }

    [Test]
    public void ContentSnapshotCodecs_RejectMalformedValueShape()
    {
        var encoded = new ContentSnapshotEncodedValue
        {
            CodecId = ContentSnapshotCodecs.Int32CodecId,
            CodecVersion = 1,
            Data = ContentSnapshotValue.Boolean(true)
        };

        bool decoded = ContentSnapshotCodecs.TryDecode(encoded, out int value, out ContentFailure? failure);

        Assert.That(decoded, Is.False);
        Assert.That(value, Is.EqualTo(0));
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void ContentSnapshotCodecs_RejectWrongCodecForExpectedType()
    {
        ContentSnapshotEncodedValue encoded = ContentSnapshotCodecs.Encode("42");

        bool decoded = ContentSnapshotCodecs.TryDecode(encoded, out int value, out ContentFailure? failure);

        Assert.That(decoded, Is.False);
        Assert.That(value, Is.EqualTo(0));
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotCodecRejected));
    }

    [Test]
    public void ContentSnapshotValues_DetachListAndObjectInputs()
    {
        var child = ContentSnapshotCodecs.Encode("before");
        var items = new List<ContentSnapshotEncodedValue> { child };
        ContentSnapshotValue list = ContentSnapshotValue.List(items);

        items.Clear();
        child.Data.StringValue = "after";

        Assert.That(list.Items, Has.Count.EqualTo(1));
        Assert.That(ContentSnapshotCodecs.Decode<string>(list.Items[0]), Is.EqualTo("before"));

        var named = new ContentSnapshotNamedValue
        {
            Name = "name",
            Value = ContentSnapshotCodecs.Encode("value")
        };
        var properties = new List<ContentSnapshotNamedValue> { named };
        ContentSnapshotValue obj = ContentSnapshotValue.Object(properties);

        properties.Clear();
        named.Name = "changed";
        named.Value.Data.StringValue = "changed";

        Assert.That(obj.Properties, Has.Count.EqualTo(1));
        Assert.That(obj.Properties[0].Name, Is.EqualTo("name"));
        Assert.That(ContentSnapshotCodecs.Decode<string>(obj.Properties[0].Value), Is.EqualTo("value"));
    }

    [Test]
    public void ContentRecordSnapshot_DefaultValuesAreSerializerFriendly()
    {
        var snapshot = new ContentRecordSnapshot();

        Assert.That(snapshot.EntryId, Is.EqualTo(string.Empty));
        Assert.That(snapshot.Entry, Is.Not.Null);
        Assert.That(snapshot.Entry.Kind, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ContentRecordSnapshot_StoresEntryIdAndEntrySnapshot()
    {
        ContentEntrySnapshot entry = new PlainContentEntry(
            new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero),
            "Stored").CaptureSnapshot();

        var snapshot = new ContentRecordSnapshot
        {
            EntryId = "record-1",
            Entry = entry
        };

        Assert.That(snapshot.EntryId, Is.EqualTo("record-1"));
        Assert.That(snapshot.Entry, Is.SameAs(entry));
    }

    [Test]
    public void ContentStructureSnapshot_DefaultValuesAreSerializerFriendly()
    {
        var snapshot = new ContentStructureSnapshot();

        Assert.That(snapshot.Kind, Is.EqualTo(string.Empty));
        Assert.That(snapshot.DataVersion, Is.EqualTo(0));
        Assert.That(snapshot.Records, Is.Not.Null);
        Assert.That(snapshot.Records, Is.Empty);
        Assert.That(snapshot.Data.Kind, Is.EqualTo(ContentSnapshotValueKind.Null));
    }

    [Test]
    public void ContentStructureSnapshot_StoresKindVersionRecordsAndData()
    {
        var record = new ContentRecordSnapshot
        {
            EntryId = "1",
            Entry = new PlainContentEntry(
                new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero),
                "Stored").CaptureSnapshot()
        };
        ContentSnapshotValue data = ContentSnapshotValue.Object(new[]
        {
            new ContentSnapshotNamedValue
            {
                Name = "nextId",
                Value = ContentSnapshotCodecs.Encode(2L)
            }
        });

        var snapshot = new ContentStructureSnapshot
        {
            Kind = "workes.content.structure.sequence",
            DataVersion = 1,
            Records = new List<ContentRecordSnapshot> { record },
            Data = data
        };

        Assert.That(snapshot.Kind, Is.EqualTo("workes.content.structure.sequence"));
        Assert.That(snapshot.DataVersion, Is.EqualTo(1));
        Assert.That(snapshot.Records, Is.EquivalentTo(new[] { record }));
        Assert.That(snapshot.Data, Is.SameAs(data));
    }

    [Test]
    public void SnapshotDtos_AreMutableSerializerFriendlyClasses()
    {
        var record = new ContentRecordSnapshot();
        record.EntryId = "changed";
        record.Entry = new ContentEntrySnapshot { Kind = "entry-kind", DataVersion = 3 };

        var structure = new ContentStructureSnapshot();
        structure.Kind = "structure-kind";
        structure.DataVersion = 4;
        structure.Records.Add(record);
        structure.Data = ContentSnapshotValue.String("data");

        Assert.That(record.EntryId, Is.EqualTo("changed"));
        Assert.That(record.Entry.Kind, Is.EqualTo("entry-kind"));
        Assert.That(structure.Kind, Is.EqualTo("structure-kind"));
        Assert.That(structure.DataVersion, Is.EqualTo(4));
        Assert.That(structure.Records, Has.Count.EqualTo(1));
        Assert.That(structure.Data.StringValue, Is.EqualTo("data"));
    }

    [Test]
    public void PlainContentEntry_CapturesSnapshot()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 9, 18, 10, 15, 30, TimeSpan.Zero);
        var entry = new PlainContentEntry(timestamp, "Server started.");

        ContentEntrySnapshot snapshot = entry.CaptureSnapshot();

        Assert.That(snapshot.Kind, Is.EqualTo(PlainContentEntry.SnapshotKind));
        Assert.That(snapshot.DataVersion, Is.EqualTo(PlainContentEntry.SnapshotDataVersion));
        Assert.That(snapshot.Data.Kind, Is.EqualTo(ContentSnapshotValueKind.Object));
        Assert.That(snapshot.Data.Properties, Has.Count.EqualTo(2));
    }

    [Test]
    public void PlainContentEntry_FactoryRestoresEquivalentEntry()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 9, 18, 10, 15, 30, TimeSpan.Zero);
        var entry = new PlainContentEntry(timestamp, "Server started.");
        ContentEntrySnapshot snapshot = entry.CaptureSnapshot();

        IContentEntry restored = ContentEntrySnapshots.Restore(snapshot, PlainContentEntry.Factory);

        Assert.That(restored, Is.TypeOf<PlainContentEntry>());
        Assert.That(restored.Timestamp, Is.EqualTo(timestamp));
        Assert.That(restored.PlainText, Is.EqualTo("Server started."));
    }

    [Test]
    public void ContentEntrySnapshots_CaptureSupportsSerializableEntries()
    {
        var entry = new PlainContentEntry(DateTimeOffset.UtcNow, "Entry");

        bool captured = ContentEntrySnapshots.TryCapture(entry, out ContentEntrySnapshot? snapshot, out ContentFailure? failure);

        Assert.That(captured, Is.True);
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void ContentEntrySnapshots_CaptureRejectsUnsupportedCustomEntry()
    {
        bool captured = ContentEntrySnapshots.TryCapture(
            new CustomEntry(),
            out ContentEntrySnapshot? snapshot,
            out ContentFailure? failure);

        Assert.That(captured, Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedEntry));
    }

    [Test]
    public void ContentEntrySnapshots_RestoreDelegatesToFactory()
    {
        var snapshot = new ContentEntrySnapshot
        {
            Kind = "custom",
            DataVersion = 1,
            Data = ContentSnapshotValue.Null()
        };
        var factory = new CustomFactory();

        bool restored = ContentEntrySnapshots.TryRestore(snapshot, factory, out IContentEntry? entry, out ContentFailure? failure);

        Assert.That(restored, Is.True);
        Assert.That(entry?.PlainText, Is.EqualTo("Restored"));
        Assert.That(factory.RestoreCalls, Is.EqualTo(1));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void ContentEntrySnapshots_RestoreRejectsWrongKind()
    {
        ContentEntrySnapshot snapshot = new PlainContentEntry(DateTimeOffset.UtcNow, "Entry").CaptureSnapshot();
        snapshot.Kind = "wrong";

        bool restored = ContentEntrySnapshots.TryRestore(
            snapshot,
            PlainContentEntry.Factory,
            out IContentEntry? entry,
            out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(entry, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void PlainContentEntry_FactoryRejectsUnsupportedVersion()
    {
        ContentEntrySnapshot snapshot = new PlainContentEntry(DateTimeOffset.UtcNow, "Entry").CaptureSnapshot();
        snapshot.DataVersion = 99;

        bool restored = PlainContentEntry.Factory.TryRestore(snapshot, out IContentEntry? entry, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(entry, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedVersion));
    }

    [Test]
    public void PlainContentEntry_FactoryRejectsMissingFields()
    {
        var snapshot = new ContentEntrySnapshot
        {
            Kind = PlainContentEntry.SnapshotKind,
            DataVersion = PlainContentEntry.SnapshotDataVersion,
            Data = ContentSnapshotValue.Object()
        };

        bool restored = PlainContentEntry.Factory.TryRestore(snapshot, out IContentEntry? entry, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(entry, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void PlainContentEntry_FactoryRejectsMalformedFields()
    {
        ContentEntrySnapshot snapshot = new PlainContentEntry(DateTimeOffset.UtcNow, "Entry").CaptureSnapshot();
        snapshot.Data.Properties[0].Value.Data = ContentSnapshotValue.Boolean(true);

        bool restored = PlainContentEntry.Factory.TryRestore(snapshot, out IContentEntry? entry, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(entry, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void ContentEntrySnapshots_ThrowingApisCarryStructuredFailure()
    {
        ContentOperationException captureException = Assert.Throws<ContentOperationException>(
            () => ContentEntrySnapshots.Capture(new CustomEntry()))!;

        Assert.That(captureException.Failure.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedEntry));

        ContentEntrySnapshot snapshot = new PlainContentEntry(DateTimeOffset.UtcNow, "Entry").CaptureSnapshot();
        snapshot.DataVersion = 99;

        ContentOperationException restoreException = Assert.Throws<ContentOperationException>(
            () => ContentEntrySnapshots.Restore(snapshot, PlainContentEntry.Factory))!;

        Assert.That(restoreException.Failure.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedVersion));
    }

    [Test]
    public void ContentStructureSnapshots_CaptureRejectsUnsupportedCustomStructure()
    {
        bool captured = ContentStructureSnapshots.TryCapture(
            new UnsupportedStructure(),
            out ContentStructureSnapshot? snapshot,
            out ContentFailure? failure);

        Assert.That(captured, Is.False);
        Assert.That(snapshot, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedStructure));
    }

    [Test]
    public void EntryFactoryRegistry_ContainsPlainEntryFactoryByDefault()
    {
        ContentEntrySnapshot snapshot = new PlainContentEntry(DateTimeOffset.UtcNow, "Entry").CaptureSnapshot();

        bool found = ContentEntrySnapshotFactories.TryGet(snapshot.Kind, out IContentEntrySnapshotFactory? factory);

        Assert.That(found, Is.True);
        Assert.That(factory, Is.SameAs(PlainContentEntry.Factory));
    }

    [Test]
    public void EntryFactoryRegistry_RepeatedSameFactoryRegistrationIsSafe()
    {
        bool registered = ContentEntrySnapshotFactories.TryRegister(PlainContentEntry.Factory, out ContentFailure? failure);

        Assert.That(registered, Is.True);
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void EntryFactoryRegistry_ConflictingFactoryRegistrationReturnsFailure()
    {
        var conflictingFactory = new ConflictingPlainEntryFactory();

        bool registered = ContentEntrySnapshotFactories.TryRegister(conflictingFactory, out ContentFailure? failure);

        Assert.That(registered, Is.False);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.SnapshotFactoryDuplicate));
    }

    [Test]
    public void ContentSequenceStructure_ExposesSnapshotFactory()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);

        Assert.That(structure.SnapshotFactory, Is.SameAs(ContentSequenceStructure.Factory));
    }

    [Test]
    public void KeyedContentStructure_ExposesSnapshotFactoryForConfiguredStrategy()
    {
        var structure = new KeyedContentStructure<CustomSnapshotId>(new PrefixIdStrategy());
        structure.Add(new CustomSnapshotId("good"), Entry("Good"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();

        var restored = (KeyedContentStructure<CustomSnapshotId>)structure.SnapshotFactory.Restore(snapshot);

        Assert.That(restored.Get(new CustomSnapshotId("good")).Entry.PlainText, Is.EqualTo("Good"));
    }

    [Test]
    public void ContentSequenceStructure_CapturesAndRestoresExactState()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(3), ContentSequenceReadOrder.NewestFirst);
        structure.Add(Entry("One"));
        ContentEntryRecord second = structure.Add(Entry("Two"));
        ContentEntryRecord third = structure.Add(Entry("Three"));

        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        var restored = (ContentSequenceStructure)ContentStructureSnapshots.Restore(snapshot, ContentSequenceStructure.Factory);
        ContentEntryRecord next = restored.Add(Entry("Four"));

        Assert.That(snapshot.Kind, Is.EqualTo(ContentSequenceStructure.SnapshotKind));
        Assert.That(snapshot.DataVersion, Is.EqualTo(ContentSequenceStructure.SnapshotDataVersion));
        Assert.That(restored.OverflowPolicy, Is.EqualTo(ContentOverflowPolicy.DropOldest(3)));
        Assert.That(restored.ReadOrder, Is.EqualTo(ContentSequenceReadOrder.NewestFirst));
        Assert.That(restored.Records.Select(record => record.Id.Value), Is.EqualTo(new[] { "4", "3", "2" }));
        Assert.That(restored.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "Four", "Three", "Two" }));
        Assert.That(next.Id.Value, Is.EqualTo("4"));
        Assert.That(restored.TryGet(second.Id, out ContentEntryRecord? restoredSecond, out _), Is.True);
        Assert.That(restoredSecond!.Entry.PlainText, Is.EqualTo(second.Entry.PlainText));
        Assert.That(restored.TryGet(third.Id, out ContentEntryRecord? restoredThird, out _), Is.True);
        Assert.That(restoredThird!.Entry.PlainText, Is.EqualTo(third.Entry.PlainText));
    }

    [Test]
    public void KeyedContentStructure_CapturesAndRestoresStringState()
    {
        var structure = new KeyedContentStructure<string>();
        structure.Add("entry-1", Entry("One"));
        structure.Add("entry-2", Entry("Two"));

        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        var restored = (KeyedContentStructure<string>)ContentStructureSnapshots.Restore(
            snapshot,
            KeyedContentStructure<string>.CreateSnapshotFactory());

        restored.Add("entry-3", Entry("Three"));

        Assert.That(snapshot.Kind, Is.EqualTo(KeyedContentStructure<string>.SnapshotKind));
        Assert.That(restored.Records.Select(record => record.Id.Value), Is.EqualTo(new[] { "entry-1", "entry-2", "entry-3" }));
        Assert.That(restored.Get("entry-1").Entry.PlainText, Is.EqualTo("One"));
        Assert.Throws<ContentOperationException>(() => restored.Add("entry-1", Entry("Duplicate")));
    }

    [Test]
    public void KeyedContentStructure_CapturesAndRestoresLongState()
    {
        var structure = new KeyedContentStructure<long>();
        structure.Add(10, Entry("Ten"));
        structure.Add(20, Entry("Twenty"));

        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        var restored = (KeyedContentStructure<long>)ContentStructureSnapshots.Restore(
            snapshot,
            KeyedContentStructure<long>.CreateSnapshotFactory());

        restored.Add(30, Entry("Thirty"));

        Assert.That(restored.Records.Select(record => record.Id.Value), Is.EqualTo(new[] { "10", "20", "30" }));
        Assert.That(restored.Get(20).Entry.PlainText, Is.EqualTo("Twenty"));
    }

    [Test]
    public void StructureRestore_ReturnsMissingFactoryFailureForCustomEntry()
    {
        var structure = new KeyedContentStructure<string>();
        structure.Add("custom", new UnregisteredSerializableCustomEntry("Custom"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();

        bool restored = ContentStructureSnapshots.TryRestore(
            snapshot,
            KeyedContentStructure<string>.CreateSnapshotFactory(),
            out IContentStructure? restoredStructure,
            out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(restoredStructure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotFactoryMissing));
    }

    [Test]
    public void StructureRestore_UsesCustomEntryFactoryFromRegistry()
    {
        var structure = new KeyedContentStructure<string>();
        structure.Add("custom", new SerializableCustomEntry("Custom"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        ContentEntrySnapshotFactories.Register(SerializableCustomEntry.Factory);

        var restored = (KeyedContentStructure<string>)ContentStructureSnapshots.Restore(
            snapshot,
            KeyedContentStructure<string>.CreateSnapshotFactory());

        Assert.That(restored.Get("custom").Entry, Is.TypeOf<SerializableCustomEntry>());
        Assert.That(restored.Get("custom").Entry.PlainText, Is.EqualTo("Custom"));
    }

    [Test]
    public void KeyedRestore_RejectsLongSnapshotIdOutsideConfiguredStrategy()
    {
        var structure = new KeyedContentStructure<long>();
        structure.Add(1, Entry("One"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        snapshot.Records[0].EntryId = "abc";

        bool restored = ContentStructureSnapshots.TryRestore(
            snapshot,
            KeyedContentStructure<long>.CreateSnapshotFactory(),
            out IContentStructure? restoredStructure,
            out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(restoredStructure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void KeyedRestore_UsesCustomStrategyNormalizedValidation()
    {
        var structure = new KeyedContentStructure<CustomSnapshotId>(new PrefixIdStrategy());
        structure.Add(new CustomSnapshotId("good"), Entry("Good"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        snapshot.Records[0].EntryId = "bad";

        bool restored = ContentStructureSnapshots.TryRestore(
            snapshot,
            KeyedContentStructure<CustomSnapshotId>.CreateSnapshotFactory(new PrefixIdStrategy()),
            out IContentStructure? restoredStructure,
            out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(restoredStructure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void StructureRestore_RejectsMalformedSequenceData()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        structure.Add(Entry("Entry"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        snapshot.DataVersion = 99;

        bool restored = ContentStructureSnapshots.TryRestore(
            snapshot,
            ContentSequenceStructure.Factory,
            out IContentStructure? restoredStructure,
            out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(restoredStructure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedVersion));
    }

    [Test]
    public void StructureRestore_RejectsDuplicateRecordIds()
    {
        var structure = new KeyedContentStructure<string>();
        structure.Add("entry-1", Entry("One"));
        structure.Add("entry-2", Entry("Two"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        snapshot.Records[1].EntryId = "entry-1";

        bool restored = ContentStructureSnapshots.TryRestore(
            snapshot,
            KeyedContentStructure<string>.CreateSnapshotFactory(),
            out IContentStructure? restoredStructure,
            out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(restoredStructure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void StructureRestore_RejectsInvalidSequenceIds()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        structure.Add(Entry("Entry"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        snapshot.Records[0].EntryId = "not-numeric";

        bool restored = ContentStructureSnapshots.TryRestore(
            snapshot,
            ContentSequenceStructure.Factory,
            out IContentStructure? restoredStructure,
            out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(restoredStructure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void StructureRestore_ThrowingApisCarryStructuredFailure()
    {
        var snapshot = new ContentStructureSnapshot
        {
            Kind = ContentSequenceStructure.SnapshotKind,
            DataVersion = 99,
            Data = ContentSnapshotValue.Object()
        };

        ContentOperationException exception = Assert.Throws<ContentOperationException>(
            () => ContentStructureSnapshots.Restore(snapshot, ContentSequenceStructure.Factory))!;

        Assert.That(exception.Failure.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedVersion));
    }

    [Test]
    public void ContentSnapshotProperties_DecodesRequiredScalarProperties()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 9, 19, 12, 30, 0, TimeSpan.Zero);
        ContentSnapshotValue data = ContentSnapshotValue.Object(new[]
        {
            ContentSnapshotProperties.Named("text", ContentSnapshotCodecs.Encode("hello")),
            ContentSnapshotProperties.Named("enabled", ContentSnapshotCodecs.Encode(true)),
            ContentSnapshotProperties.Named("count", ContentSnapshotCodecs.Encode(3)),
            ContentSnapshotProperties.Named("nextId", ContentSnapshotCodecs.Encode(9L)),
            ContentSnapshotProperties.Named("timestamp", ContentSnapshotCodecs.Encode(timestamp))
        });

        Assert.That(ContentSnapshotProperties.TryDecodeRequiredString(data, "text", out string text, out ContentFailure? failure), Is.True);
        Assert.That(text, Is.EqualTo("hello"));
        Assert.That(ContentSnapshotProperties.TryDecodeRequiredBoolean(data, "enabled", out bool enabled, out failure), Is.True);
        Assert.That(enabled, Is.True);
        Assert.That(ContentSnapshotProperties.TryDecodeRequiredInt32(data, "count", out int count, out failure), Is.True);
        Assert.That(count, Is.EqualTo(3));
        Assert.That(ContentSnapshotProperties.TryDecodeRequiredInt64(data, "nextId", out long nextId, out failure), Is.True);
        Assert.That(nextId, Is.EqualTo(9L));
        Assert.That(ContentSnapshotProperties.TryDecodeRequiredDateTimeOffset(data, "timestamp", out DateTimeOffset decodedTimestamp, out failure), Is.True);
        Assert.That(decodedTimestamp, Is.EqualTo(timestamp));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void ContentSnapshotProperties_ReturnsSnapshotFailuresForMissingOrWrongShape()
    {
        ContentSnapshotValue data = ContentSnapshotValue.Object();

        bool found = ContentSnapshotProperties.TryGetRequired(data, "missing", out ContentSnapshotEncodedValue? value, out ContentFailure? failure);

        Assert.That(found, Is.False);
        Assert.That(value, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));

        bool decoded = ContentSnapshotProperties.TryDecodeRequiredString(
            ContentSnapshotValue.String("not-object"),
            "missing",
            out string text,
            out failure);

        Assert.That(decoded, Is.False);
        Assert.That(text, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void ContentSnapshotRecords_CapturesAndRestoresRecordsThroughRegisteredFactories()
    {
        var records = new[]
        {
            new ContentEntryRecord(new ContentEntryId("1"), Entry("One")),
            new ContentEntryRecord(new ContentEntryId("2"), Entry("Two"))
        };

        bool captured = ContentSnapshotRecords.TryCapture(records, out List<ContentRecordSnapshot>? snapshots, out ContentFailure? failure);
        bool restored = ContentSnapshotRecords.TryRestore(snapshots, out ContentEntryRecord[] restoredRecords, out failure);

        Assert.That(captured, Is.True);
        Assert.That(restored, Is.True);
        Assert.That(restoredRecords.Select(record => record.Id.Value), Is.EqualTo(new[] { "1", "2" }));
        Assert.That(restoredRecords.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "One", "Two" }));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void ContentSnapshotRecords_RejectsDuplicateIdsAndMissingFactories()
    {
        var duplicateSnapshots = new List<ContentRecordSnapshot>
        {
            new ContentRecordSnapshot { EntryId = "same", Entry = Entry("One").CaptureSnapshot() },
            new ContentRecordSnapshot { EntryId = "same", Entry = Entry("Two").CaptureSnapshot() }
        };

        bool duplicateRestored = ContentSnapshotRecords.TryRestore(
            duplicateSnapshots,
            out ContentEntryRecord[] restoredRecords,
            out ContentFailure? failure);

        Assert.That(duplicateRestored, Is.False);
        Assert.That(restoredRecords, Is.Empty);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));

        var missingFactorySnapshots = new List<ContentRecordSnapshot>
        {
            new ContentRecordSnapshot
            {
                EntryId = "custom",
                Entry = new ContentEntrySnapshot { Kind = "test.entry.missing", DataVersion = 1 }
            }
        };

        bool missingFactoryRestored = ContentSnapshotRecords.TryRestore(
            missingFactorySnapshots,
            out restoredRecords,
            out failure);

        Assert.That(missingFactoryRestored, Is.False);
        Assert.That(restoredRecords, Is.Empty);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotFactoryMissing));
    }

    [Test]
    public void ContentStructureSnapshotFactoryBase_ValidatesHeaderAndThrowingRestore()
    {
        var factory = new ExampleAssignedStructure.Factory();
        var wrongKind = new ContentStructureSnapshot
        {
            Kind = "test.structure.other",
            DataVersion = ExampleAssignedStructure.SnapshotDataVersion,
            Data = ContentSnapshotValue.Object()
        };

        bool restored = factory.TryRestore(wrongKind, out IContentStructure? structure, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(structure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));

        wrongKind.Kind = ExampleAssignedStructure.SnapshotKind;
        wrongKind.DataVersion = 99;
        ContentOperationException exception = Assert.Throws<ContentOperationException>(() => factory.Restore(wrongKind))!;

        Assert.That(exception.Failure.Code, Is.EqualTo(ContentFailureCodes.SnapshotUnsupportedVersion));
        Assert.Throws<ArgumentNullException>(() => factory.TryRestore(null!, out _, out _));
    }

    [Test]
    public void ContentStructureSnapshotFactoryBase_RejectsNullRestoredStructure()
    {
        var factory = new NullRestoringStructureFactory();
        var snapshot = new ContentStructureSnapshot
        {
            Kind = NullRestoringStructureFactory.SnapshotKind,
            DataVersion = NullRestoringStructureFactory.SnapshotDataVersion,
            Data = ContentSnapshotValue.Object()
        };

        bool restored = factory.TryRestore(snapshot, out IContentStructure? structure, out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(structure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.SnapshotMalformed));
    }

    [Test]
    public void CustomAssignedStructure_UsesAuthoringHelpersForSnapshotRoundTrip()
    {
        var structure = new ExampleAssignedStructure("quest-log");
        structure.Add(Entry("Accepted quest"));
        structure.Add(Entry("Completed quest"));

        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        var manager = new ExampleAssignedManager(new ExampleAssignedStructure("empty"));
        manager.RestoreSnapshot(snapshot);
        ContentEntryRecord next = manager.Add(Entry("Claimed reward"));
        var restored = (ExampleAssignedStructure)manager.Structure;

        Assert.That(restored.Label, Is.EqualTo("quest-log"));
        Assert.That(restored.Records.Select(record => record.Id.Value), Is.EqualTo(new[] { "1", "2", "3" }));
        Assert.That(restored.Records.Select(record => record.Entry.PlainText), Is.EqualTo(new[] { "Accepted quest", "Completed quest", "Claimed reward" }));
        Assert.That(next.Id.Value, Is.EqualTo("3"));
    }

    [Test]
    public void CustomKeyedStructure_UsesStrategyValidationDuringSnapshotRestore()
    {
        var structure = new ExampleKeyedStructure(new PrefixIdStrategy());
        structure.Add(new CustomSnapshotId("good"), Entry("Good"));
        ContentStructureSnapshot snapshot = structure.CaptureSnapshot();
        snapshot.Records[0].EntryId = "bad";

        bool restored = ContentStructureSnapshots.TryRestore(
            snapshot,
            new ExampleKeyedStructure.Factory(new PrefixIdStrategy()),
            out IContentStructure? restoredStructure,
            out ContentFailure? failure);

        Assert.That(restored, Is.False);
        Assert.That(restoredStructure, Is.Null);
        Assert.That(failure?.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void CustomKeyedStructure_RestoresThroughManagerWithoutExplicitFactory()
    {
        var sourceStructure = new ExampleKeyedStructure(new PrefixIdStrategy());
        var source = new ExampleKeyedManager(sourceStructure);
        source.Add(new CustomSnapshotId("quest"), Entry("Quest"));
        ContentStructureSnapshot snapshot = source.CaptureSnapshot();

        var target = new ExampleKeyedManager(new ExampleKeyedStructure(new PrefixIdStrategy()));

        target.RestoreSnapshot(snapshot);

        Assert.That(target.Get(new CustomSnapshotId("quest")).Entry.PlainText, Is.EqualTo("Quest"));
    }

    private sealed class UnsupportedPayload
    {
    }

    private static PlainContentEntry Entry(string text)
    {
        return new PlainContentEntry(DateTimeOffset.UtcNow, text);
    }

    private sealed class CustomEntry : IContentEntry
    {
        public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;

        public string PlainText => "Custom";
    }

    private sealed class CustomFactory : IContentEntrySnapshotFactory
    {
        public int RestoreCalls { get; private set; }

        public string Kind => "custom";

        public bool TryRestore(ContentEntrySnapshot snapshot, out IContentEntry? entry, out ContentFailure? failure)
        {
            RestoreCalls++;
            entry = new PlainContentEntry(DateTimeOffset.UtcNow, "Restored");
            failure = null;
            return true;
        }

        public IContentEntry Restore(ContentEntrySnapshot snapshot)
        {
            if (TryRestore(snapshot, out IContentEntry? entry, out ContentFailure? failure) && entry is not null)
            {
                return entry;
            }

            throw new ContentOperationException(
                failure ?? ContentFailure.Create(
                    ContentFailureKind.Snapshot,
                    ContentFailureCodes.SnapshotRejected,
                    "Snapshot restore failed."));
        }
    }

    private sealed class ConflictingPlainEntryFactory : IContentEntrySnapshotFactory
    {
        public string Kind => PlainContentEntry.SnapshotKind;

        public bool TryRestore(ContentEntrySnapshot snapshot, out IContentEntry? entry, out ContentFailure? failure)
        {
            entry = null;
            failure = ContentFailure.Create(ContentFailureKind.Snapshot, ContentFailureCodes.SnapshotRejected, "Conflicting factory.");
            return false;
        }

        public IContentEntry Restore(ContentEntrySnapshot snapshot)
        {
            throw new ContentOperationException(
                ContentFailure.Create(ContentFailureKind.Snapshot, ContentFailureCodes.SnapshotRejected, "Conflicting factory."));
        }
    }

    private sealed class UnsupportedStructure : IContentStructure
    {
        public IReadOnlyList<ContentEntryRecord> Records => Array.Empty<ContentEntryRecord>();

        public ContentManagerBase CreateManager()
        {
            return new UnsupportedManager(this);
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

    private sealed class UnsupportedManager : ContentManagerBase
    {
        public UnsupportedManager(IContentStructure structure)
            : base(structure)
        {
        }
    }

    private sealed class SerializableCustomEntry : IContentEntry, IContentEntrySnapshotSerializable
    {
        public const string SnapshotKind = "test.entry.custom";

        public static IContentEntrySnapshotFactory Factory { get; } = new SerializableCustomEntryFactory();

        public SerializableCustomEntry(string plainText)
        {
            PlainText = plainText;
        }

        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch;

        public string PlainText { get; }

        public bool TryCaptureSnapshot(out ContentEntrySnapshot? snapshot, out ContentFailure? failure)
        {
            snapshot = new ContentEntrySnapshot
            {
                Kind = SnapshotKind,
                DataVersion = 1,
                Data = ContentSnapshotValue.Object(new[]
                {
                    Named("plainText", ContentSnapshotCodecs.Encode(PlainText))
                })
            };
            failure = null;
            return true;
        }

        public ContentEntrySnapshot CaptureSnapshot()
        {
            return new ContentEntrySnapshot
            {
                Kind = SnapshotKind,
                DataVersion = 1,
                Data = ContentSnapshotValue.Object(new[]
                {
                    Named("plainText", ContentSnapshotCodecs.Encode(PlainText))
                })
            };
        }
    }

    private sealed class SerializableCustomEntryFactory : IContentEntrySnapshotFactory
    {
        public string Kind => SerializableCustomEntry.SnapshotKind;

        public bool TryRestore(ContentEntrySnapshot snapshot, out IContentEntry? entry, out ContentFailure? failure)
        {
            entry = null;
            failure = null;
            if (!TryGetRequired(snapshot.Data, "plainText", out ContentSnapshotEncodedValue? plainTextValue, out failure))
            {
                return false;
            }

            if (!ContentSnapshotCodecs.TryDecode(plainTextValue!, out string plainText, out failure))
            {
                return false;
            }

            entry = new SerializableCustomEntry(plainText);
            return true;
        }

        public IContentEntry Restore(ContentEntrySnapshot snapshot)
        {
            if (TryRestore(snapshot, out IContentEntry? entry, out ContentFailure? failure) && entry is not null)
            {
                return entry;
            }

            throw new ContentOperationException(
                failure ?? ContentFailure.Create(
                    ContentFailureKind.Snapshot,
                    ContentFailureCodes.SnapshotRejected,
                    "Snapshot restore failed."));
        }
    }

    private sealed class UnregisteredSerializableCustomEntry : IContentEntry, IContentEntrySnapshotSerializable
    {
        public const string SnapshotKind = "test.entry.unregistered";

        public UnregisteredSerializableCustomEntry(string plainText)
        {
            PlainText = plainText;
        }

        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch;

        public string PlainText { get; }

        public bool TryCaptureSnapshot(out ContentEntrySnapshot? snapshot, out ContentFailure? failure)
        {
            snapshot = new ContentEntrySnapshot
            {
                Kind = SnapshotKind,
                DataVersion = 1,
                Data = ContentSnapshotValue.Object(new[]
                {
                    Named("plainText", ContentSnapshotCodecs.Encode(PlainText))
                })
            };
            failure = null;
            return true;
        }

        public ContentEntrySnapshot CaptureSnapshot()
        {
            if (TryCaptureSnapshot(out ContentEntrySnapshot? snapshot, out ContentFailure? failure) && snapshot is not null)
            {
                return snapshot;
            }

            throw new ContentOperationException(
                failure ?? ContentFailure.Create(
                    ContentFailureKind.Snapshot,
                    ContentFailureCodes.SnapshotRejected,
                    "Snapshot capture failed."));
        }
    }

    private readonly struct CustomSnapshotId
    {
        public CustomSnapshotId(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    private sealed class PrefixIdStrategy : IContentEntryIdStrategy<CustomSnapshotId>
    {
        public bool TryNormalize(CustomSnapshotId id, out ContentEntryId normalizedId, out ContentFailure? failure)
        {
            if (string.IsNullOrWhiteSpace(id.Value))
            {
                normalizedId = default;
                failure = ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryIdInvalid, "Invalid ID.");
                return false;
            }

            normalizedId = new ContentEntryId("custom:" + id.Value);
            failure = null;
            return true;
        }

        public bool TryValidateNormalized(ContentEntryId id, out ContentFailure? failure)
        {
            if (!id.Value.StartsWith("custom:", StringComparison.Ordinal))
            {
                failure = ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryIdInvalid, "Invalid restored ID.");
                return false;
            }

            failure = null;
            return true;
        }
    }

    private sealed class NullRestoringStructureFactory : ContentStructureSnapshotFactoryBase<UnsupportedStructure>
    {
        public const string SnapshotKind = "test.structure.null";

        public const int SnapshotDataVersion = 1;

        public NullRestoringStructureFactory()
            : base(SnapshotKind, SnapshotDataVersion)
        {
        }

        protected override bool TryRestoreValidatedSnapshot(
            ContentStructureSnapshot snapshot,
            out UnsupportedStructure? structure,
            out ContentFailure? failure)
        {
            structure = null;
            failure = null;
            return true;
        }
    }

    private sealed class ExampleAssignedStructure : IStructureAssignedIdContentStructure<long>, IContentStructureSnapshotRoundTrippable
    {
        public const string SnapshotKind = "test.structure.assigned";

        public const int SnapshotDataVersion = 1;

        public static IContentStructureSnapshotFactory SnapshotFactory { get; } = new Factory();

        private readonly List<ContentEntryRecord> _records;
        private long _nextId;

        public ExampleAssignedStructure(string label)
            : this(label, new List<ContentEntryRecord>(), 1)
        {
        }

        private ExampleAssignedStructure(string label, IEnumerable<ContentEntryRecord> records, long nextId)
        {
            Label = label;
            _records = records.ToList();
            _nextId = nextId;
        }

        public string Label { get; }

        public IReadOnlyList<ContentEntryRecord> Records => _records.ToArray();

        IContentStructureSnapshotFactory IContentStructureSnapshotRoundTrippable.SnapshotFactory => SnapshotFactory;

        public ContentManagerBase CreateManager()
        {
            return new ExampleAssignedManager(this);
        }

        public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            record = _records.FirstOrDefault(candidate => candidate.Id.Equals(id));
            if (record is not null)
            {
                failure = null;
                return true;
            }

            failure = ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryNotFound, "Missing.");
            return false;
        }

        public bool TryGet(long id, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            return TryGet(new ContentEntryId(id.ToString(System.Globalization.CultureInfo.InvariantCulture)), out record, out failure);
        }

        public ContentEntryRecord Get(long id)
        {
            return Get(new ContentEntryId(id.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        public ContentEntryRecord Get(ContentEntryId id)
        {
            if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure) && record is not null)
            {
                return record;
            }

            throw new ContentOperationException(failure!);
        }

        public bool TryAdd(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            record = new ContentEntryRecord(new ContentEntryId((_nextId++).ToString(System.Globalization.CultureInfo.InvariantCulture)), entry);
            _records.Add(record);
            failure = null;
            return true;
        }

        public ContentEntryRecord Add(IContentEntry entry)
        {
            TryAdd(entry, out ContentEntryRecord? record, out _);
            return record!;
        }

        public bool TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure)
        {
            if (!ContentSnapshotRecords.TryCapture(_records, out List<ContentRecordSnapshot>? records, out failure))
            {
                snapshot = null;
                return false;
            }

            snapshot = new ContentStructureSnapshot
            {
                Kind = SnapshotKind,
                DataVersion = SnapshotDataVersion,
                Records = records!,
                Data = ContentSnapshotValue.Object(new[]
                {
                    ContentSnapshotProperties.Named("label", ContentSnapshotCodecs.Encode(Label)),
                    ContentSnapshotProperties.Named("nextId", ContentSnapshotCodecs.Encode(_nextId))
                })
            };
            failure = null;
            return true;
        }

        public ContentStructureSnapshot CaptureSnapshot()
        {
            if (TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure) && snapshot is not null)
            {
                return snapshot;
            }

            throw new ContentOperationException(failure ?? ContentFailure.Create(ContentFailureKind.Snapshot, ContentFailureCodes.SnapshotRejected, "Capture failed."));
        }

        public sealed class Factory : ContentStructureSnapshotFactoryBase<ExampleAssignedStructure>
        {
            public Factory()
                : base(SnapshotKind, SnapshotDataVersion)
            {
            }

            protected override bool TryRestoreValidatedSnapshot(
                ContentStructureSnapshot snapshot,
                out ExampleAssignedStructure? structure,
                out ContentFailure? failure)
            {
                structure = null;

                if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure)
                    || !ContentSnapshotRecords.TryGetMaximumPositiveNumericId(records, out long maximumId, out failure)
                    || !ContentSnapshotProperties.TryDecodeRequiredString(snapshot.Data, "label", out string label, out failure)
                    || !ContentSnapshotProperties.TryDecodeRequiredInt64(snapshot.Data, "nextId", out long nextId, out failure))
                {
                    return false;
                }

                if (nextId <= maximumId)
                {
                    failure = ContentFailure.Create(ContentFailureKind.Snapshot, ContentFailureCodes.SnapshotMalformed, "Next ID must be greater than retained IDs.");
                    return false;
                }

                structure = new ExampleAssignedStructure(label, records, nextId);
                return true;
            }
        }
    }

    private sealed class ExampleAssignedManager : ContentManagerBase
    {
        public ExampleAssignedManager(ExampleAssignedStructure structure)
            : base(structure)
        {
        }

        private ExampleAssignedStructure Assigned => (ExampleAssignedStructure)Structure;

        public ContentEntryRecord Add(IContentEntry entry)
        {
            return Assigned.Add(entry);
        }

        public ContentEntryRecord Get(long id)
        {
            return Assigned.Get(id);
        }

        protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
        {
            if (structure is ExampleAssignedStructure)
            {
                failure = null;
                return true;
            }

            failure = ContentFailure.Create(ContentFailureKind.Structure, ContentFailureCodes.StructureUnsupportedOperation, "Replacement structure is not an example assigned structure.");
            return false;
        }
    }

    private sealed class ExampleKeyedStructure : IKeyedContentStructure<CustomSnapshotId>, IContentStructureSnapshotRoundTrippable
    {
        public const string SnapshotKind = "test.structure.keyed";

        public const int SnapshotDataVersion = 1;

        private readonly PrefixIdStrategy _strategy;
        private readonly List<ContentEntryRecord> _records;

        public ExampleKeyedStructure(PrefixIdStrategy strategy)
            : this(strategy, Enumerable.Empty<ContentEntryRecord>())
        {
        }

        private ExampleKeyedStructure(PrefixIdStrategy strategy, IEnumerable<ContentEntryRecord> records)
        {
            _strategy = strategy;
            _records = records.ToList();
        }

        public IReadOnlyList<ContentEntryRecord> Records => _records.ToArray();

        public IContentStructureSnapshotFactory SnapshotFactory => new Factory(_strategy);

        public ContentManagerBase CreateManager()
        {
            return new ExampleKeyedManager(this);
        }

        public bool TryAdd(CustomSnapshotId id, IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            record = null;
            if (!_strategy.TryNormalize(id, out ContentEntryId normalizedId, out failure))
            {
                return false;
            }

            if (_records.Any(candidate => candidate.Id.Equals(normalizedId)))
            {
                failure = ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryIdDuplicate, "Duplicate.");
                return false;
            }

            record = new ContentEntryRecord(normalizedId, entry);
            _records.Add(record);
            failure = null;
            return true;
        }

        public ContentEntryRecord Add(CustomSnapshotId id, IContentEntry entry)
        {
            if (TryAdd(id, entry, out ContentEntryRecord? record, out ContentFailure? failure) && record is not null)
            {
                return record;
            }

            throw new ContentOperationException(failure!);
        }

        public bool TryGet(CustomSnapshotId id, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            record = null;
            if (!_strategy.TryNormalize(id, out ContentEntryId normalizedId, out failure))
            {
                return false;
            }

            return TryGet(normalizedId, out record, out failure);
        }

        public ContentEntryRecord Get(CustomSnapshotId id)
        {
            if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure) && record is not null)
            {
                return record;
            }

            throw new ContentOperationException(failure!);
        }

        public bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
        {
            record = _records.FirstOrDefault(candidate => candidate.Id.Equals(id));
            if (record is not null)
            {
                failure = null;
                return true;
            }

            failure = ContentFailure.Create(ContentFailureKind.Entry, ContentFailureCodes.EntryNotFound, "Missing.");
            return false;
        }

        public ContentEntryRecord Get(ContentEntryId id)
        {
            if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure) && record is not null)
            {
                return record;
            }

            throw new ContentOperationException(failure!);
        }

        public bool TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure)
        {
            if (!ContentSnapshotRecords.TryCapture(_records, out List<ContentRecordSnapshot>? records, out failure))
            {
                snapshot = null;
                return false;
            }

            snapshot = new ContentStructureSnapshot
            {
                Kind = SnapshotKind,
                DataVersion = SnapshotDataVersion,
                Records = records!,
                Data = ContentSnapshotValue.Object()
            };
            failure = null;
            return true;
        }

        public ContentStructureSnapshot CaptureSnapshot()
        {
            if (TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure) && snapshot is not null)
            {
                return snapshot;
            }

            throw new ContentOperationException(failure ?? ContentFailure.Create(ContentFailureKind.Snapshot, ContentFailureCodes.SnapshotRejected, "Capture failed."));
        }

        public sealed class Factory : ContentStructureSnapshotFactoryBase<ExampleKeyedStructure>
        {
            private readonly PrefixIdStrategy _strategy;

            public Factory(PrefixIdStrategy strategy)
                : base(SnapshotKind, SnapshotDataVersion)
            {
                _strategy = strategy;
            }

            protected override bool TryRestoreValidatedSnapshot(
                ContentStructureSnapshot snapshot,
                out ExampleKeyedStructure? structure,
                out ContentFailure? failure)
            {
                structure = null;
                if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure))
                {
                    return false;
                }

                foreach (ContentEntryRecord record in records)
                {
                    if (!_strategy.TryValidateNormalized(record.Id, out failure))
                    {
                        return false;
                    }
                }

                structure = new ExampleKeyedStructure(_strategy, records);
                failure = null;
                return true;
            }
        }
    }

    private sealed class ExampleKeyedManager : ContentManagerBase
    {
        public ExampleKeyedManager(ExampleKeyedStructure structure)
            : base(structure)
        {
        }

        private ExampleKeyedStructure Keyed => (ExampleKeyedStructure)Structure;

        public ContentEntryRecord Add(CustomSnapshotId id, IContentEntry entry)
        {
            return Keyed.Add(id, entry);
        }

        public ContentEntryRecord Get(CustomSnapshotId id)
        {
            return Keyed.Get(id);
        }

        protected override bool TryAcceptStructureReplacement(IContentStructure structure, out ContentFailure? failure)
        {
            if (structure is ExampleKeyedStructure)
            {
                failure = null;
                return true;
            }

            failure = ContentFailure.Create(ContentFailureKind.Structure, ContentFailureCodes.StructureUnsupportedOperation, "Replacement structure is not an example keyed structure.");
            return false;
        }
    }

    private static ContentSnapshotNamedValue Named(string name, ContentSnapshotEncodedValue value)
    {
        return ContentSnapshotProperties.Named(name, value);
    }

    private static bool TryGetRequired(
        ContentSnapshotValue data,
        string name,
        out ContentSnapshotEncodedValue? value,
        out ContentFailure? failure)
    {
        return ContentSnapshotProperties.TryGetRequired(data, name, out value, out failure);
    }
}
