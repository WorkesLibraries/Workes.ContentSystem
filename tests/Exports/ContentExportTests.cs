using System;
using System.Collections.Generic;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Exports;

public sealed class ContentExportTests
{
    [Test]
    public void BuiltInPlainTextExporter_PreservesRecordOrderAndDoesNotMutateManager()
    {
        var manager = CreateManager();
        manager.Add(new PlainContentEntry(DateTimeOffset.UnixEpoch, "first"));
        manager.Add(new PlainContentEntry(DateTimeOffset.UnixEpoch.AddSeconds(1), "second"));
        int changes = 0;
        manager.Changed += (_, _) => changes++;

        ContentExportResult result = manager.Export(new PlainTextContentRecordExporter());

        Assert.That(result.FormatId, Is.EqualTo(PlainTextContentRecordExporter.StableFormatId));
        Assert.That(result.Text, Is.EqualTo("first\nsecond"));
        Assert.That(manager.Count, Is.EqualTo(2));
        Assert.That(changes, Is.Zero);
    }

    [Test]
    public void BuiltInMarkdownExporter_FormatsTimestampAndTextInRecordOrder()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 10, 15, 10, 30, 0, TimeSpan.Zero);
        var manager = CreateManager();
        manager.Add(new PlainContentEntry(timestamp, "hello"));

        ContentExportResult result = manager.Export(new MarkdownContentRecordExporter());

        Assert.That(result.FormatId, Is.EqualTo(MarkdownContentRecordExporter.StableFormatId));
        Assert.That(result.Text, Is.EqualTo("### 2026-10-15T10:30:00.0000000+00:00\n\nhello"));
    }

    [Test]
    public void BuiltInMarkdownExporter_EscapesPlainTextMarkup()
    {
        var manager = CreateManager();
        manager.Add(new PlainContentEntry(DateTimeOffset.UnixEpoch, "# <hero> & *friend*"));

        ContentExportResult result = manager.Export(new MarkdownContentRecordExporter());

        Assert.That(result.Text, Does.EndWith("\\# &lt;hero&gt; &amp; \\*friend\\*"));
    }

    [Test]
    public void BuiltInExporters_ExportEmptyManagerToEmptyText()
    {
        var manager = CreateManager();

        Assert.That(manager.Export(new PlainTextContentRecordExporter()).Text, Is.Empty);
        Assert.That(manager.Export(new MarkdownContentRecordExporter()).Text, Is.Empty);
    }

    [Test]
    public void BuiltInExporter_RejectsUnsupportedEntryWithStructuredFailureAndNoMutation()
    {
        var manager = CreateManager();
        manager.Add(new TestEntry("custom"));
        int changes = 0;
        manager.Changed += (_, _) => changes++;

        bool exported = manager.TryExport(new PlainTextContentRecordExporter(), out ContentExportResult? result, out ContentFailure? failure);

        Assert.That(exported, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(failure!.Kind, Is.EqualTo(ContentFailureKind.Export));
        Assert.That(failure.Code, Is.EqualTo(ContentFailureCodes.ExportRejected));
        Assert.That(manager.Count, Is.EqualTo(1));
        Assert.That(changes, Is.Zero);
        ContentOperationException thrown = Assert.Throws<ContentOperationException>(
            () => manager.Export(new PlainTextContentRecordExporter()))!;
        Assert.That(thrown.Failure.Code, Is.EqualTo(failure.Code));
    }

    [Test]
    public void CustomExporter_CanSupportCustomEntryAndReceivesReadOnlyRecordSnapshot()
    {
        var manager = CreateManager();
        manager.Add(new TestEntry("custom"));
        var exporter = new CustomExporter();

        ContentExportResult result = manager.Export(exporter);

        Assert.That(result.FormatId, Is.EqualTo("sample.custom"));
        Assert.That(result.Text, Is.EqualTo("1:custom"));
        Assert.That(exporter.ReceivedReadOnlyRecords, Is.True);
    }

    [Test]
    public void CustomExporterRejection_IsWrappedAsExportFailureAndPreservesCause()
    {
        var manager = CreateManager();
        ContentFailure cause = ContentFailure.Create(
            ContentFailureKind.Validation,
            ContentFailureCodes.ValidationRejected,
            "Entry failed validation.");

        bool exported = manager.TryExport(
            new RejectingExporter(cause),
            out ContentExportResult? result,
            out ContentFailure? failure);

        Assert.That(exported, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(failure!.Kind, Is.EqualTo(ContentFailureKind.Export));
        Assert.That(failure.Code, Is.EqualTo(ContentFailureCodes.ExportRejected));
        Assert.That(failure.Source, Is.EqualTo("sample.rejecting"));
        Assert.That(failure.Cause, Is.EqualTo(cause));
    }

    [Test]
    public void AttachmentReferences_AreImmutableAndUnsupportedEntriesReturnEmptyView()
    {
        var reference = new ContentAttachmentReference("asset:quest/portrait", "image/png", "hero.png", "Hero portrait");
        var entry = new AttachmentEntry(reference);

        IReadOnlyList<ContentAttachmentReference> references = ContentEntryAttachments.GetReferences(entry);
        Assert.That(references, Has.Count.EqualTo(1));
        Assert.That(references[0].Locator, Is.EqualTo("asset:quest/portrait"));
        Assert.That(references[0].MediaType, Is.EqualTo("image/png"));
        Assert.That(references[0].FileName, Is.EqualTo("hero.png"));
        Assert.That(references[0].DisplayName, Is.EqualTo("Hero portrait"));
        Assert.Throws<NotSupportedException>(() => ((IList<ContentAttachmentReference>)references).Add(reference));
        Assert.That(ContentEntryAttachments.GetReferences(new TestEntry("plain")), Is.Empty);
        Assert.Throws<ArgumentException>(() => new ContentAttachmentReference(" "));
    }

    [Test]
    public void AttachmentEntrySnapshot_RoundTripsItsOwnAttachmentMetadata()
    {
        var entry = new AttachmentEntry(new ContentAttachmentReference(
            "asset:quest/portrait", "image/png", "hero.png", "Hero portrait"));

        ContentEntrySnapshot snapshot = entry.CaptureSnapshot();
        var restored = (AttachmentEntry)AttachmentEntry.Factory.Restore(snapshot);
        ContentAttachmentReference reference = ContentEntryAttachments.GetReferences(restored).Single();

        Assert.That(reference.Locator, Is.EqualTo("asset:quest/portrait"));
        Assert.That(reference.MediaType, Is.EqualTo("image/png"));
        Assert.That(reference.FileName, Is.EqualTo("hero.png"));
        Assert.That(reference.DisplayName, Is.EqualTo("Hero portrait"));
    }

    private sealed class TestEntry : ITestPlainTextEntry
    {
        public TestEntry(string text) => PlainText = text;
        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch;
        public string PlainText { get; }
    }

    private static ContentSequenceManager CreateManager() => ContentManagers.ForStructure<ContentSequenceManager>(
        new ContentSequenceStructure(ContentOverflowPolicy.None));

    private sealed class CustomExporter : IContentRecordExporter
    {
        public string FormatId => "sample.custom";
        public bool ReceivedReadOnlyRecords { get; private set; }

        public bool TryExport(IReadOnlyList<ContentEntryRecord> records, out string? text, out ContentFailure? failure)
        {
            ReceivedReadOnlyRecords = records is not IList<ContentEntryRecord> list || list.IsReadOnly;
            text = string.Join(";", records.Select(record => $"{record.Id}:{ContentTestText.Of(record)}"));
            failure = null;
            return true;
        }
    }

    private sealed class RejectingExporter : IContentRecordExporter
    {
        private readonly ContentFailure _failure;
        public RejectingExporter(ContentFailure failure) => _failure = failure;
        public string FormatId => "sample.rejecting";
        public bool TryExport(IReadOnlyList<ContentEntryRecord> records, out string? text, out ContentFailure? failure)
        {
            text = null;
            failure = _failure;
            return false;
        }
    }

    private sealed class AttachmentEntry : IContentEntry, IContentEntryAttachmentSource, IContentEntrySnapshotSerializable
    {
        public const string SnapshotKind = "sample.entry.attachment";
        public static IContentEntrySnapshotFactory Factory { get; } = new AttachmentEntryFactory();

        public AttachmentEntry(ContentAttachmentReference reference)
        {
            Attachments = Array.AsReadOnly(new[] { reference });
        }

        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch;
        public IReadOnlyList<ContentAttachmentReference> Attachments { get; }

        public bool TryCaptureSnapshot(out ContentEntrySnapshot? snapshot, out ContentFailure? failure)
        {
            ContentAttachmentReference reference = Attachments[0];
            snapshot = new ContentEntrySnapshot
            {
                Kind = SnapshotKind,
                DataVersion = 1,
                Data = ContentSnapshotValue.Object(new[]
                {
                    Named("locator", reference.Locator),
                    Named("mediaType", reference.MediaType ?? string.Empty),
                    Named("fileName", reference.FileName ?? string.Empty),
                    Named("displayName", reference.DisplayName ?? string.Empty)
                })
            };
            failure = null;
            return true;
        }

        public ContentEntrySnapshot CaptureSnapshot()
        {
            if (TryCaptureSnapshot(out ContentEntrySnapshot? snapshot, out ContentFailure? failure)) return snapshot!;
            throw new ContentOperationException(failure!);
        }

        private static ContentSnapshotNamedValue Named(string name, string value) => new()
        {
            Name = name,
            Value = ContentSnapshotCodecs.Encode(value)
        };

        private sealed class AttachmentEntryFactory : IContentEntrySnapshotFactory
        {
            public string Kind => SnapshotKind;

            public bool TryRestore(ContentEntrySnapshot snapshot, out IContentEntry? entry, out ContentFailure? failure)
            {
                entry = null;
                if (snapshot.Kind != Kind || snapshot.DataVersion != 1 || snapshot.Data.Kind != ContentSnapshotValueKind.Object)
                {
                    failure = ContentFailure.Create(
                        ContentFailureKind.Snapshot,
                        ContentFailureCodes.SnapshotMalformed,
                        "Invalid attachment entry snapshot.");
                    return false;
                }

                string Read(string name)
                {
                    ContentSnapshotNamedValue? named = snapshot.Data.Properties.FirstOrDefault(value => value.Name == name);
                    if (named is null || !ContentSnapshotCodecs.TryDecode(named.Value, out string decoded, out _))
                        throw new InvalidOperationException($"Missing attachment property '{name}'.");
                    return decoded;
                }

                string mediaType = Read("mediaType");
                string fileName = Read("fileName");
                string displayName = Read("displayName");
                entry = new AttachmentEntry(new ContentAttachmentReference(
                    Read("locator"),
                    mediaType.Length == 0 ? null : mediaType,
                    fileName.Length == 0 ? null : fileName,
                    displayName.Length == 0 ? null : displayName));
                failure = null;
                return true;
            }

            public IContentEntry Restore(ContentEntrySnapshot snapshot)
            {
                if (TryRestore(snapshot, out IContentEntry? entry, out ContentFailure? failure)) return entry!;
                throw new ContentOperationException(failure!);
            }
        }
    }
}
