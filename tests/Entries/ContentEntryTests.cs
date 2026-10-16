using System;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentEntryTests
{
    [Test]
    public void ContentEntryId_StoresStringValue()
    {
        var id = new ContentEntryId("entry-1");

        Assert.That(id.Value, Is.EqualTo("entry-1"));
        Assert.That((string)id, Is.EqualTo("entry-1"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ContentEntryId_InvalidValueThrows(string? value)
    {
        Assert.Throws<ArgumentException>(() => new ContentEntryId(value!));
    }

    [Test]
    public void ContentEntryId_EqualityIsValueBased()
    {
        var first = new ContentEntryId("entry-1");
        var second = ContentEntryId.FromString("entry-1");
        var different = new ContentEntryId("entry-2");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first == second, Is.True);
        Assert.That(first != different, Is.True);
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }

    [Test]
    public void ContentEntryId_ToStringReturnsValue()
    {
        var id = new ContentEntryId("entry-1");

        Assert.That(id.ToString(), Is.EqualTo("entry-1"));
    }

    [Test]
    public void PlainContentEntry_StoresEntryDetails()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 9, 14, 12, 30, 0, TimeSpan.Zero);

        var entry = new PlainContentEntry(timestamp, "Server started.");

        Assert.That(entry.Timestamp, Is.EqualTo(timestamp));
        Assert.That(ContentTestText.Of(entry), Is.EqualTo("Server started."));
    }

    [Test]
    public void PlainContentEntry_NullPlainTextThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new PlainContentEntry(DateTimeOffset.UtcNow, null!));
    }

    [Test]
    public void ContentEntryRecord_StoresIdAndEntry()
    {
        var id = new ContentEntryId("entry-1");
        DateTimeOffset timestamp = new DateTimeOffset(2026, 9, 14, 12, 30, 0, TimeSpan.Zero);
        var entry = new PlainContentEntry(timestamp, "Server started.");

        var record = new ContentEntryRecord(id, entry);

        Assert.That(record.Id, Is.EqualTo(id));
        Assert.That(record.Entry, Is.SameAs(entry));
        Assert.That(record.Timestamp, Is.EqualTo(timestamp));
        Assert.That(ContentTestText.Of(record), Is.EqualTo("Server started."));
    }

    [Test]
    public void ContentEntryRecord_NullEntryThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentEntryRecord(new ContentEntryId("entry-1"), null!));
    }

    [Test]
    public void CustomEntry_CanImplementIContentEntry()
    {
        IContentEntry entry = new CustomEntry(
            new DateTimeOffset(2026, 9, 14, 13, 0, 0, TimeSpan.Zero),
            "Custom content");

        Assert.That(entry.Timestamp, Is.EqualTo(new DateTimeOffset(2026, 9, 14, 13, 0, 0, TimeSpan.Zero)));
        Assert.That(ContentTestText.Of(entry), Is.EqualTo("Custom content"));
    }

    [Test]
    public void TimestampOnlyEntry_WorksWithBuiltInManagersAndEntrySnapshotRoundTrip()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 10, 16, 9, 30, 0, TimeSpan.Zero);
        IContentEntry entry = new TimestampOnlyEntry(timestamp);

        var sequenceStructure = new ContentSequenceStructure(ContentOverflowPolicy.None);
        ContentSequenceManager sequence = ContentManagers.ForStructure<ContentSequenceManager>(sequenceStructure);
        ContentEntryRecord sequenceRecord = sequence.Add(entry);

        ContentMapManager<string> map = ContentManagers.ForStructure<ContentMapManager<string>>(
            new ContentMapStructure<string>());
        ContentEntryRecord mapRecord = map.Add("timestamp-only", entry);

        ContentEntrySnapshot snapshot = ContentEntrySnapshots.Capture(entry);
        IContentEntry restored = ContentEntrySnapshots.Restore(snapshot, TimestampOnlyEntry.Factory);

        Assert.That(sequenceRecord.Entry, Is.SameAs(entry));
        Assert.That(sequenceRecord.Timestamp, Is.EqualTo(timestamp));
        Assert.That(mapRecord.Entry, Is.SameAs(entry));
        Assert.That(mapRecord.Timestamp, Is.EqualTo(timestamp));
        Assert.That(restored, Is.TypeOf<TimestampOnlyEntry>());
        Assert.That(restored.Timestamp, Is.EqualTo(timestamp));
    }

    private sealed class CustomEntry : ITestPlainTextEntry
    {
        public CustomEntry(DateTimeOffset timestamp, string plainText)
        {
            Timestamp = timestamp;
            PlainText = plainText;
        }

        public DateTimeOffset Timestamp { get; }

        public string PlainText { get; }
    }

    private sealed class TimestampOnlyEntry : IContentEntry, IContentEntrySnapshotSerializable
    {
        public const string SnapshotKind = "test.entry.timestamp-only";
        public static IContentEntrySnapshotFactory Factory { get; } = new TimestampOnlyEntryFactory();

        public TimestampOnlyEntry(DateTimeOffset timestamp) => Timestamp = timestamp;

        public DateTimeOffset Timestamp { get; }

        public bool TryCaptureSnapshot(out ContentEntrySnapshot? snapshot, out ContentFailure? failure)
        {
            snapshot = new ContentEntrySnapshot
            {
                Kind = SnapshotKind,
                DataVersion = 1,
                Data = ContentSnapshotValue.Object(new[]
                {
                    new ContentSnapshotNamedValue
                    {
                        Name = "timestamp",
                        Value = ContentSnapshotCodecs.Encode(Timestamp)
                    }
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

            throw new ContentOperationException(failure ?? SnapshotFailure("Timestamp-only entry snapshot capture failed."));
        }

        private static ContentFailure SnapshotFailure(string message) => ContentFailure.Create(
            ContentFailureKind.Snapshot,
            ContentFailureCodes.SnapshotRejected,
            message);

        private sealed class TimestampOnlyEntryFactory : IContentEntrySnapshotFactory
        {
            public string Kind => SnapshotKind;

            public bool TryRestore(ContentEntrySnapshot snapshot, out IContentEntry? entry, out ContentFailure? failure)
            {
                if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
                entry = null;
                failure = null;
                if (snapshot.Kind != SnapshotKind || snapshot.DataVersion != 1 || snapshot.Data is null)
                {
                    failure = SnapshotFailure("The timestamp-only entry snapshot is invalid.");
                    return false;
                }

                ContentSnapshotNamedValue? timestampValue = snapshot.Data.Properties
                    .FirstOrDefault(value => value.Name == "timestamp");
                if (timestampValue is null
                    || !ContentSnapshotCodecs.TryDecode(timestampValue.Value, out DateTimeOffset timestamp, out failure))
                {
                    failure ??= SnapshotFailure("The timestamp-only entry snapshot has no valid timestamp.");
                    return false;
                }

                entry = new TimestampOnlyEntry(timestamp);
                return true;
            }

            public IContentEntry Restore(ContentEntrySnapshot snapshot)
            {
                if (TryRestore(snapshot, out IContentEntry? entry, out ContentFailure? failure) && entry is not null)
                {
                    return entry;
                }

                throw new ContentOperationException(failure ?? SnapshotFailure("Timestamp-only entry snapshot restore failed."));
            }
        }
    }
}
