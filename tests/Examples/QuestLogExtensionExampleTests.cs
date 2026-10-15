using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Examples;

public sealed class QuestLogExtensionExampleTests
{
    [Test]
    public void CustomStructure_CanResolveAppendAndPublishCommittedChange()
    {
        QuestLogManager manager = ContentManagers.ForStructure<QuestLogManager>(new QuestLogStructure());
        var events = new List<ContentChangedEventArgs>();
        manager.Changed += (_, args) => events.Add(args);

        ContentPreflightResult assessment = manager.AssessAppend(Entry("The gate is open."));
        Assert.That(assessment.CanCommit, Is.True);

        Assert.That(manager.TryAppend(Entry("The gate is open."), out ContentEntryRecord? record, out ContentFailure? failure), Is.True);
        Assert.That(failure, Is.Null);
        Assert.That(record, Is.Not.Null);
        Assert.That(record!.Id.Value, Is.EqualTo("quest/1"));
        Assert.That(manager.Records, Has.Count.EqualTo(1));
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Kind, Is.EqualTo(ContentChangeKind.Added));
        Assert.That(events[0].AddedRecords, Is.EqualTo(new[] { record }));
    }

    [Test]
    public void ArchivedStructure_ReportsSameFailureAcrossAssessmentTryAndThrowingPaths()
    {
        QuestLogManager manager = ContentManagers.ForStructure<QuestLogManager>(new QuestLogStructure(isArchived: true));
        var events = new List<ContentChangedEventArgs>();
        manager.Changed += (_, args) => events.Add(args);
        ContentEntryRecord[] before = manager.Records.ToArray();

        ContentPreflightResult assessment = manager.AssessAppend(Entry("Should not be retained."));
        Assert.That(assessment.CanCommit, Is.False);
        Assert.That(assessment.Failure!.Kind, Is.EqualTo(ContentFailureKind.Extension));
        Assert.That(assessment.Failure.Code, Is.EqualTo(QuestLogFailureCodes.Archived));

        Assert.That(manager.TryAppend(Entry("Should not be retained."), out ContentEntryRecord? record, out ContentFailure? failure), Is.False);
        Assert.That(record, Is.Null);
        Assert.That(failure, Is.EqualTo(assessment.Failure));

        ContentOperationException exception = Assert.Throws<ContentOperationException>(
            () => manager.Append(Entry("Should not be retained.")))!;
        Assert.That(exception.Failure, Is.EqualTo(failure));
        Assert.That(manager.Records, Is.EqualTo(before));
        Assert.That(events, Is.Empty);
    }

    [Test]
    public void FailureHelpers_PreserveExtensionCodesAndNestedCauses()
    {
        var cause = ContentFailure.Create(
            ContentFailureKind.Validation,
            "example.questlog.content.entry_invalid",
            "The quest update is invalid.");

        ContentFailure wrapped = ContentFailure.Wrap(
            ContentFailureKind.Extension,
            "example.questlog.content.append_rejected",
            "The quest update could not be appended.",
            cause,
            component: nameof(QuestLogStructure));

        Assert.That(wrapped.Kind, Is.EqualTo(ContentFailureKind.Extension));
        Assert.That(wrapped.Code, Is.EqualTo("example.questlog.content.append_rejected"));
        Assert.That(wrapped.Cause, Is.SameAs(cause));

        var operationException = new ContentOperationException(cause);
        Assert.That(ContentFailure.FromException(operationException), Is.SameAs(cause));

        ContentFailure converted = ContentFailure.FromException(
            new InvalidOperationException("An expected callback failed."),
            code: "example.questlog.content.callback_failed");
        Assert.That(converted.Kind, Is.EqualTo(ContentFailureKind.Extension));
        Assert.That(converted.Code, Is.EqualTo("example.questlog.content.callback_failed"));
    }

    [Test]
    public void ProgrammerMisuse_RemainsAStandardArgumentException()
    {
        QuestLogManager manager = ContentManagers.ForStructure<QuestLogManager>(new QuestLogStructure());

        Assert.Throws<ArgumentNullException>(() => manager.AssessAppend(null!));
        Assert.Throws<ArgumentNullException>(() => manager.TryAppend(null!, out _, out _));
    }

    private static PlainContentEntry Entry(string text) => new(DateTimeOffset.UnixEpoch, text);
}

internal static class QuestLogFailureCodes
{
    public const string Archived = "example.questlog.content.archived";
}

internal sealed class QuestLogStructure : IContentStructure, IContentChangeSource
{
    private readonly List<ContentEntryRecord> _records = new();
    private readonly bool _isArchived;
    private long _nextId = 1;

    public QuestLogStructure(bool isArchived = false)
    {
        _isArchived = isArchived;
    }

    public event EventHandler<ContentChangedEventArgs>? Changed;

    public IReadOnlyList<ContentEntryRecord> Records => Array.AsReadOnly(_records.ToArray());

    public ContentManagerBase CreateManager() => new QuestLogManager(this);

    public ContentPreflightResult AssessAppend(IContentEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        return _isArchived
            ? ContentPreflightResult.Rejected(ArchivedFailure())
            : ContentPreflightResult.Success();
    }

    public bool TryAppend(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        if (_isArchived)
        {
            record = null;
            failure = ArchivedFailure();
            return false;
        }

        record = new ContentEntryRecord(
            new ContentEntryId("quest/" + _nextId.ToString(CultureInfo.InvariantCulture)),
            entry);
        _nextId++;
        _records.Add(record);
        failure = null;
        Changed?.Invoke(this, new ContentChangedEventArgs(
            addedRecords: new[] { record },
            kind: ContentChangeKind.Added));
        return true;
    }

    public ContentEntryRecord Append(IContentEntry entry)
    {
        if (TryAppend(entry, out ContentEntryRecord? record, out ContentFailure? failure)) return record!;
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

        failure = ContentFailure.Create(
            ContentFailureKind.Entry,
            ContentFailureCodes.EntryNotFound,
            $"Quest log entry '{id}' was not found.",
            component: nameof(QuestLogStructure));
        return false;
    }

    public ContentEntryRecord Get(ContentEntryId id)
    {
        if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure)) return record!;
        throw new ContentOperationException(failure!);
    }

    private static ContentFailure ArchivedFailure() => ContentFailure.Create(
        ContentFailureKind.Extension,
        QuestLogFailureCodes.Archived,
        "The quest log is archived and cannot accept new entries.",
        component: nameof(QuestLogStructure));
}

internal sealed class QuestLogManager : ContentManagerBase
{
    public QuestLogManager(QuestLogStructure structure) : base(structure) { }

    private QuestLogStructure QuestLog => (QuestLogStructure)Structure;

    public ContentPreflightResult AssessAppend(IContentEntry entry) => QuestLog.AssessAppend(entry);

    public bool TryAppend(IContentEntry entry, out ContentEntryRecord? record, out ContentFailure? failure) =>
        QuestLog.TryAppend(entry, out record, out failure);

    public ContentEntryRecord Append(IContentEntry entry) => QuestLog.Append(entry);
}
