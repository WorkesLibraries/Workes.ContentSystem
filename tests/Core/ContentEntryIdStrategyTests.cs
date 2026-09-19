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
}
