using System;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentFailureTests
{
    [Test]
    public void Constructor_StoresFailureDetails()
    {
        ContentFailure cause = ContentFailure.Create(
            ContentFailureKind.Structure,
            ContentFailureCodes.StructureReadOnly,
            "Structure is read-only.");

        var failure = new ContentFailure(
            ContentFailureKind.Entry,
            ContentFailureCodes.EntryRejected,
            "Entry was rejected.",
            component: "ContentSequenceStructure",
            source: "entry:42",
            cause: cause);

        Assert.That(failure.Kind, Is.EqualTo(ContentFailureKind.Entry));
        Assert.That(failure.Code, Is.EqualTo(ContentFailureCodes.EntryRejected));
        Assert.That(failure.Message, Is.EqualTo("Entry was rejected."));
        Assert.That(failure.Component, Is.EqualTo("ContentSequenceStructure"));
        Assert.That(failure.Source, Is.EqualTo("entry:42"));
        Assert.That(failure.Cause, Is.SameAs(cause));
    }

    [TestCase(null, "message")]
    [TestCase("", "message")]
    [TestCase("   ", "message")]
    [TestCase("code", null)]
    [TestCase("code", "")]
    [TestCase("code", "   ")]
    public void Constructor_InvalidCodeOrMessageThrows(string? code, string? message)
    {
        Assert.Throws<ArgumentException>(() => new ContentFailure(
            ContentFailureKind.Unknown,
            code!,
            message!));
    }

    [Test]
    public void Create_CreatesFailure()
    {
        ContentFailure failure = ContentFailure.Create(
            ContentFailureKind.Validation,
            ContentFailureCodes.ValidationRejected,
            "Rejected.");

        Assert.That(failure.Kind, Is.EqualTo(ContentFailureKind.Validation));
        Assert.That(failure.Code, Is.EqualTo(ContentFailureCodes.ValidationRejected));
        Assert.That(failure.Message, Is.EqualTo("Rejected."));
    }

    [Test]
    public void Wrap_PreservesCauseAndAddsCauseMessage()
    {
        ContentFailure cause = ContentFailure.Create(
            ContentFailureKind.Entry,
            ContentFailureCodes.EntryNotFound,
            "Entry was not found.");

        ContentFailure wrapped = ContentFailure.Wrap(
            ContentFailureKind.Structure,
            ContentFailureCodes.StructureRejected,
            "Structure rejected lookup",
            cause,
            component: "ContentStructure",
            source: "lookup");

        Assert.That(wrapped.Cause, Is.SameAs(cause));
        Assert.That(wrapped.Message, Is.EqualTo("Structure rejected lookup: Entry was not found."));
        Assert.That(wrapped.ToString(), Is.EqualTo("Structure rejected lookup: Entry was not found. Cause: Entry was not found."));
    }

    [Test]
    public void FromException_WhenContentSystemException_ReturnsContainedFailure()
    {
        ContentFailure failure = ContentFailure.Create(
            ContentFailureKind.Structure,
            ContentFailureCodes.StructureRejected,
            "Structure failed.");
        var exception = new ContentOperationException(failure);

        ContentFailure converted = ContentFailure.FromException(exception);

        Assert.That(converted, Is.SameAs(failure));
    }

    [Test]
    public void FromException_WhenStandardException_CreatesExtensionFailure()
    {
        ContentFailure failure = ContentFailure.FromException(new InvalidOperationException("Extension failed."));

        Assert.That(failure.Kind, Is.EqualTo(ContentFailureKind.Extension));
        Assert.That(failure.Code, Is.EqualTo(ContentFailureCodes.ExtensionRejected));
        Assert.That(failure.Message, Is.EqualTo("Extension failed."));
    }

    [Test]
    public void Equality_UsesAllFailureFields()
    {
        ContentFailure first = ContentFailure.Create(
            ContentFailureKind.Bridge,
            ContentFailureCodes.BridgeRejected,
            "Bridge rejected entry.",
            component: "Bridge",
            source: "chat");
        ContentFailure second = ContentFailure.Create(
            ContentFailureKind.Bridge,
            ContentFailureCodes.BridgeRejected,
            "Bridge rejected entry.",
            component: "Bridge",
            source: "chat");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }

    [Test]
    public void ContentSystemException_ExposesFailure()
    {
        ContentFailure failure = ContentFailure.Create(
            ContentFailureKind.Export,
            ContentFailureCodes.ExportRejected,
            "Export failed.");

        var exception = new ContentSystemException(failure);

        Assert.That(exception.Failure, Is.SameAs(failure));
        Assert.That(exception.Message, Is.EqualTo("Export failed."));
    }

    [Test]
    public void ContentOperationException_ExposesFailure()
    {
        ContentFailure failure = ContentFailure.Create(
            ContentFailureKind.Structure,
            ContentFailureCodes.StructureUnsupportedOperation,
            "Structure does not support lookup.");

        var exception = new ContentOperationException(failure);

        Assert.That(exception.Failure, Is.SameAs(failure));
    }
}
