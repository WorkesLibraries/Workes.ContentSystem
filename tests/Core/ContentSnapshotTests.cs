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

    private sealed class UnsupportedPayload
    {
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
}
