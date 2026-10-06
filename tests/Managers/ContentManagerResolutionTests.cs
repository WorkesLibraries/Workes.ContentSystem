using System;
using System.Collections.Generic;
using NUnit.Framework;
using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests;

public sealed class ContentManagerResolutionTests
{
    [Test]
    public void SequenceStructure_CreatesSequenceManager()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);

        ContentManagerBase manager = structure.CreateManager();

        Assert.That(structure, Is.InstanceOf<ContentSequenceStructureBase<long>>());
        Assert.That(manager, Is.TypeOf<ContentSequenceManager>());
        Assert.That(manager, Is.InstanceOf<ContentSequenceManagerBase>());
        var sequence = (ContentSequenceManager)manager;
        ContentEntryRecord record = sequence.Add(new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
        Assert.That(sequence.Get(1), Is.EqualTo(record));
    }

    [Test]
    public void MapStructure_CreatesMapManager()
    {
        var structure = new ContentMapStructure<string>();

        ContentManagerBase manager = structure.CreateManager();

        Assert.That(structure, Is.InstanceOf<ContentMapStructureBase<string>>());
        Assert.That(manager, Is.TypeOf<ContentMapManager<string>>());
        Assert.That(manager, Is.InstanceOf<ContentMapManagerBase<string>>());
        var map = (ContentMapManager<string>)manager;
        ContentEntryRecord record = map.Add("entry", new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
        Assert.That(map.Get("entry"), Is.EqualTo(record));
    }

    [Test]
    public void ForStructure_ReturnsCreatedManager()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);

        ContentManagerBase manager = ContentManagers.ForStructure(structure);

        Assert.That(manager, Is.TypeOf<ContentSequenceManager>());
    }

    [Test]
    public void ForStructure_Typed_ReturnsExpectedManager()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);

        ContentSequenceManager manager = ContentManagers.ForStructure<ContentSequenceManager>(structure);

        Assert.That(manager, Is.Not.Null);
    }

    [Test]
    public void ForStructure_Typed_ReturnsStructuredFailureWhenManagerDoesNotMatch()
    {
        var structure = new ContentSequenceStructure(ContentOverflowPolicy.None);

        bool resolved = ContentManagers.TryForStructure<ContentMapManager<string>>(
            structure,
            out ContentMapManager<string>? manager,
            out ContentFailure? failure);
        ContentOperationException exception = Assert.Throws<ContentOperationException>(
            () => ContentManagers.ForStructure<ContentMapManager<string>>(structure))!;

        Assert.That(resolved, Is.False);
        Assert.That(manager, Is.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.ManagerMismatch));
        Assert.That(exception.Failure.Code, Is.EqualTo(ContentFailureCodes.ManagerMismatch));
    }

    [Test]
    public void CustomStructure_CanCreateCustomManagerWithoutRegistry()
    {
        var structure = new CustomStructure();

        CustomManager manager = ContentManagers.ForStructure<CustomManager>(structure);

        Assert.That(manager.Structure, Is.SameAs(structure));
    }

    private sealed class CustomManager : ContentManagerBase
    {
        public CustomManager(IContentStructure structure)
            : base(structure)
        {
        }
    }

    private sealed class CustomStructure : IContentStructure
    {
        public IReadOnlyList<ContentEntryRecord> Records => Array.Empty<ContentEntryRecord>();

        public ContentManagerBase CreateManager()
        {
            return new CustomManager(this);
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
}
