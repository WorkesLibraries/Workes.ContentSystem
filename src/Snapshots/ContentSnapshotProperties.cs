using System;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Helper methods for working with structure-owned snapshot object data.
/// </summary>
public static class ContentSnapshotProperties
{
    /// <summary>
    /// Creates a named encoded snapshot property.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The encoded property value.</param>
    /// <returns>The named property.</returns>
    public static ContentSnapshotNamedValue Named(string name, ContentSnapshotEncodedValue value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Snapshot property name cannot be empty.", nameof(name));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new ContentSnapshotNamedValue { Name = name, Value = value };
    }

    /// <summary>
    /// Attempts to read a required encoded property from object-shaped snapshot data.
    /// </summary>
    /// <param name="data">The object-shaped snapshot data.</param>
    /// <param name="name">The required property name.</param>
    /// <param name="value">The encoded property value.</param>
    /// <param name="failure">The structured failure when the property is missing or malformed.</param>
    /// <returns><see langword="true"/> when the property was found.</returns>
    public static bool TryGetRequired(
        ContentSnapshotValue data,
        string name,
        out ContentSnapshotEncodedValue? value,
        out ContentFailure? failure)
    {
        value = null;
        failure = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Snapshot property name cannot be empty.", nameof(name));
        }

        if (data is null || data.Kind != ContentSnapshotValueKind.Object)
        {
            failure = ContentFailures.SnapshotMalformed("Snapshot data must be an object.");
            return false;
        }

        ContentSnapshotNamedValue? property = data.Properties.FirstOrDefault(candidate => candidate.Name == name);
        if (property is null)
        {
            failure = ContentFailures.SnapshotMalformed($"Snapshot data is missing '{name}'.");
            return false;
        }

        value = property.Value;
        return true;
    }

    /// <summary>
    /// Attempts to read and decode a required string property.
    /// </summary>
    public static bool TryDecodeRequiredString(
        ContentSnapshotValue data,
        string name,
        out string value,
        out ContentFailure? failure)
    {
        return TryDecodeRequired(data, name, out value, out failure);
    }

    /// <summary>
    /// Attempts to read and decode a required Boolean property.
    /// </summary>
    public static bool TryDecodeRequiredBoolean(
        ContentSnapshotValue data,
        string name,
        out bool value,
        out ContentFailure? failure)
    {
        return TryDecodeRequired(data, name, out value, out failure);
    }

    /// <summary>
    /// Attempts to read and decode a required 32-bit integer property.
    /// </summary>
    public static bool TryDecodeRequiredInt32(
        ContentSnapshotValue data,
        string name,
        out int value,
        out ContentFailure? failure)
    {
        return TryDecodeRequired(data, name, out value, out failure);
    }

    /// <summary>
    /// Attempts to read and decode a required 64-bit integer property.
    /// </summary>
    public static bool TryDecodeRequiredInt64(
        ContentSnapshotValue data,
        string name,
        out long value,
        out ContentFailure? failure)
    {
        return TryDecodeRequired(data, name, out value, out failure);
    }

    /// <summary>
    /// Attempts to read and decode a required timestamp property.
    /// </summary>
    public static bool TryDecodeRequiredDateTimeOffset(
        ContentSnapshotValue data,
        string name,
        out DateTimeOffset value,
        out ContentFailure? failure)
    {
        return TryDecodeRequired(data, name, out value, out failure);
    }

    /// <summary>
    /// Attempts to read and decode a required scalar property.
    /// </summary>
    /// <typeparam name="T">The expected scalar type.</typeparam>
    /// <param name="data">The object-shaped snapshot data.</param>
    /// <param name="name">The required property name.</param>
    /// <param name="value">The decoded property value.</param>
    /// <param name="failure">The structured failure when the property is missing or malformed.</param>
    /// <returns><see langword="true"/> when the property was decoded.</returns>
    public static bool TryDecodeRequired<T>(
        ContentSnapshotValue data,
        string name,
        out T value,
        out ContentFailure? failure)
    {
        value = default!;

        if (!TryGetRequired(data, name, out ContentSnapshotEncodedValue? encoded, out failure))
        {
            return false;
        }

        return ContentSnapshotCodecs.TryDecode(encoded!, out value, out failure);
    }
}
