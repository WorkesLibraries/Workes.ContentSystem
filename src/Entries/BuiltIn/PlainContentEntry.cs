using System;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a simple plain-text content entry.
/// </summary>
public sealed class PlainContentEntry : IContentEntry, IContentEntrySnapshotSerializable
{
    /// <summary>
    /// Stable entry snapshot kind for <see cref="PlainContentEntry"/>.
    /// </summary>
    public const string SnapshotKind = ContentFailureCodes.PackagePrefix + "entry.plain";

    /// <summary>
    /// Current plain entry snapshot data version.
    /// </summary>
    public const int SnapshotDataVersion = 1;

    /// <summary>
    /// Gets the snapshot factory for <see cref="PlainContentEntry"/>.
    /// </summary>
    public static IContentEntrySnapshotFactory Factory { get; } = new PlainContentEntrySnapshotFactory();

    /// <summary>
    /// Initializes a new instance of the <see cref="PlainContentEntry"/> class.
    /// </summary>
    /// <param name="timestamp">The time associated with the entry.</param>
    /// <param name="plainText">The plain text content.</param>
    public PlainContentEntry(DateTimeOffset timestamp, string plainText)
    {
        PlainText = plainText ?? throw new ArgumentNullException(nameof(plainText));
        Timestamp = timestamp;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <inheritdoc />
    public string PlainText { get; }

    /// <inheritdoc />
    public bool TryCaptureSnapshot(out ContentEntrySnapshot? snapshot, out ContentFailure? failure)
    {
        snapshot = new ContentEntrySnapshot
        {
            Kind = SnapshotKind,
            DataVersion = SnapshotDataVersion,
            Data = ContentSnapshotValue.Object(new[]
            {
                new ContentSnapshotNamedValue
                {
                    Name = "timestamp",
                    Value = ContentSnapshotCodecs.Encode(Timestamp)
                },
                new ContentSnapshotNamedValue
                {
                    Name = "plainText",
                    Value = ContentSnapshotCodecs.Encode(PlainText)
                }
            })
        };
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public ContentEntrySnapshot CaptureSnapshot()
    {
        if (TryCaptureSnapshot(out ContentEntrySnapshot? snapshot, out ContentFailure? failure) && snapshot is not null)
        {
            return snapshot;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    private sealed class PlainContentEntrySnapshotFactory : IContentEntrySnapshotFactory
    {
        public string Kind => SnapshotKind;

        public bool TryRestore(
            ContentEntrySnapshot snapshot,
            out IContentEntry? entry,
            out ContentFailure? failure)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            entry = null;
            failure = null;

            if (snapshot.Kind != SnapshotKind)
            {
                failure = ContentFailures.SnapshotMalformed(
                    $"Plain entry snapshot kind must be '{SnapshotKind}'.");
                return false;
            }

            if (snapshot.DataVersion != SnapshotDataVersion)
            {
                failure = ContentFailures.SnapshotUnsupportedVersion(
                    $"Plain entry snapshot version {snapshot.DataVersion} is not supported.");
                return false;
            }

            if (snapshot.Data is null || snapshot.Data.Kind != ContentSnapshotValueKind.Object)
            {
                failure = ContentFailures.SnapshotMalformed("Plain entry snapshot data must be an object.");
                return false;
            }

            if (!TryGetRequiredProperty(snapshot, "timestamp", out ContentSnapshotEncodedValue? timestampValue, out failure))
            {
                return false;
            }

            if (!TryGetRequiredProperty(snapshot, "plainText", out ContentSnapshotEncodedValue? plainTextValue, out failure))
            {
                return false;
            }

            if (!ContentSnapshotCodecs.TryDecode(timestampValue!, out DateTimeOffset timestamp, out failure))
            {
                return false;
            }

            if (!ContentSnapshotCodecs.TryDecode(plainTextValue!, out string plainText, out failure))
            {
                return false;
            }

            entry = new PlainContentEntry(timestamp, plainText);
            return true;
        }

        public IContentEntry Restore(ContentEntrySnapshot snapshot)
        {
            if (TryRestore(snapshot, out IContentEntry? entry, out ContentFailure? failure) && entry is not null)
            {
                return entry;
            }

            throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
        }

        private static bool TryGetRequiredProperty(
            ContentEntrySnapshot snapshot,
            string name,
            out ContentSnapshotEncodedValue? value,
            out ContentFailure? failure)
        {
            ContentSnapshotNamedValue? property = snapshot.Data.Properties
                .FirstOrDefault(candidate => candidate.Name == name);

            if (property is null)
            {
                value = null;
                failure = ContentFailures.SnapshotMalformed(
                    $"Plain entry snapshot is missing '{name}'.");
                return false;
            }

            value = property.Value;
            failure = null;
            return true;
        }
    }
}
