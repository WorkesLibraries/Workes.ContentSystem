using Workes.ContentSystem.Core;

namespace Workes.ContentSystem.Tests.Core;

public sealed class ContentEntryIdStrategyTests
{
    [Test]
    public void StringStrategy_AcceptsValidStringId()
    {
        var strategy = new StringContentEntryIdStrategy();

        bool accepted = strategy.TryNormalize("thread-main", out ContentEntryId normalizedId, out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(normalizedId, Is.EqualTo(new ContentEntryId("thread-main")));
        Assert.That(failure, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void StringStrategy_RejectsInvalidStringId(string? value)
    {
        var strategy = new StringContentEntryIdStrategy();

        bool accepted = strategy.TryNormalize(value!, out ContentEntryId normalizedId, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(normalizedId, Is.EqualTo(default(ContentEntryId)));
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void StringStrategy_ValidatesNormalizedStringId()
    {
        var strategy = new StringContentEntryIdStrategy();

        bool accepted = strategy.TryValidateNormalized(new ContentEntryId("thread-main"), out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void StringStrategy_RejectsInvalidNormalizedStringId()
    {
        var strategy = new StringContentEntryIdStrategy();

        bool accepted = strategy.TryValidateNormalized(default, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void IntegerStrategy_AcceptsPositiveIntegerId()
    {
        var strategy = new IntegerContentEntryIdStrategy();

        bool accepted = strategy.TryNormalize(8, out ContentEntryId normalizedId, out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(normalizedId, Is.EqualTo(new ContentEntryId("8")));
        Assert.That(failure, Is.Null);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void IntegerStrategy_RejectsInvalidId(long value)
    {
        var strategy = new IntegerContentEntryIdStrategy();

        bool accepted = strategy.TryNormalize(value, out ContentEntryId normalizedId, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(normalizedId, Is.EqualTo(default(ContentEntryId)));
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Kind, Is.EqualTo(ContentFailureKind.Entry));
        Assert.That(failure.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [TestCase("1")]
    [TestCase("42")]
    public void IntegerStrategy_ValidatesNormalizedIntegerId(string value)
    {
        var strategy = new IntegerContentEntryIdStrategy();

        bool accepted = strategy.TryValidateNormalized(new ContentEntryId(value), out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(failure, Is.Null);
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("abc")]
    [TestCase("01")]
    public void IntegerStrategy_RejectsInvalidNormalizedIntegerId(string value)
    {
        var strategy = new IntegerContentEntryIdStrategy();

        bool accepted = strategy.TryValidateNormalized(new ContentEntryId(value), out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void GuidStrategy_AcceptsNonEmptyGuidId()
    {
        var strategy = new GuidContentEntryIdStrategy();
        Guid id = Guid.Parse("9fd3efda-747d-4a60-81c1-c38ed2d60774");

        bool accepted = strategy.TryNormalize(id, out ContentEntryId normalizedId, out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(normalizedId, Is.EqualTo(new ContentEntryId("9fd3efda-747d-4a60-81c1-c38ed2d60774")));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void GuidStrategy_RejectsEmptyGuidId()
    {
        var strategy = new GuidContentEntryIdStrategy();

        bool accepted = strategy.TryNormalize(Guid.Empty, out ContentEntryId normalizedId, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(normalizedId, Is.EqualTo(default(ContentEntryId)));
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [TestCase("9fd3efda-747d-4a60-81c1-c38ed2d60774")]
    public void GuidStrategy_ValidatesCanonicalNormalizedGuidId(string value)
    {
        var strategy = new GuidContentEntryIdStrategy();

        bool accepted = strategy.TryValidateNormalized(new ContentEntryId(value), out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(failure, Is.Null);
    }

    [TestCase("00000000-0000-0000-0000-000000000000")]
    [TestCase("9FD3EFDA-747D-4A60-81C1-C38ED2D60774")]
    [TestCase("9fd3efda747d4a6081c1c38ed2d60774")]
    [TestCase("not-a-guid")]
    public void GuidStrategy_RejectsInvalidNormalizedGuidId(string value)
    {
        var strategy = new GuidContentEntryIdStrategy();

        bool accepted = strategy.TryValidateNormalized(new ContentEntryId(value), out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }

    [Test]
    public void ContentEntryIdStrategy_AcceptsValidContentEntryId()
    {
        var strategy = new ContentEntryIdContentEntryIdStrategy();
        var id = new ContentEntryId("entry-1");

        bool accepted = strategy.TryNormalize(id, out ContentEntryId normalizedId, out ContentFailure? failure);

        Assert.That(accepted, Is.True);
        Assert.That(normalizedId, Is.EqualTo(id));
        Assert.That(failure, Is.Null);
    }

    [Test]
    public void ContentEntryIdStrategy_RejectsDefaultContentEntryId()
    {
        var strategy = new ContentEntryIdContentEntryIdStrategy();

        bool accepted = strategy.TryNormalize(default, out ContentEntryId normalizedId, out ContentFailure? failure);

        Assert.That(accepted, Is.False);
        Assert.That(normalizedId, Is.EqualTo(default(ContentEntryId)));
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure!.Code, Is.EqualTo(ContentFailureCodes.EntryIdInvalid));
    }
}
