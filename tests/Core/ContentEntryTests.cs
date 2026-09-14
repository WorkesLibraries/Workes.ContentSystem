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
        Assert.That(entry.PlainText, Is.EqualTo("Server started."));
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
        Assert.That(record.PlainText, Is.EqualTo("Server started."));
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
        Assert.That(entry.PlainText, Is.EqualTo("Custom content"));
    }

    private sealed class CustomEntry : IContentEntry
    {
        public CustomEntry(DateTimeOffset timestamp, string plainText)
        {
            Timestamp = timestamp;
            PlainText = plainText;
        }

        public DateTimeOffset Timestamp { get; }

        public string PlainText { get; }
    }
}
