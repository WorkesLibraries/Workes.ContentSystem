using System;
using System.Globalization;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Encodes and decodes package-supported portable content snapshot scalar values.
/// </summary>
public static class ContentSnapshotCodecs
{
    /// <summary>
    /// Prefix reserved for built-in content snapshot codecs.
    /// </summary>
    public const string ReservedPrefix = ContentFailureCodes.PackagePrefix + "value.";

    /// <summary>
    /// Codec ID for null values.
    /// </summary>
    public const string NullCodecId = ReservedPrefix + "null";

    /// <summary>
    /// Codec ID for string values.
    /// </summary>
    public const string StringCodecId = ReservedPrefix + "string";

    /// <summary>
    /// Codec ID for Boolean values.
    /// </summary>
    public const string BooleanCodecId = ReservedPrefix + "boolean";

    /// <summary>
    /// Codec ID for 32-bit integer values.
    /// </summary>
    public const string Int32CodecId = ReservedPrefix + "int32";

    /// <summary>
    /// Codec ID for 64-bit integer values.
    /// </summary>
    public const string Int64CodecId = ReservedPrefix + "int64";

    /// <summary>
    /// Codec ID for <see cref="DateTimeOffset"/> values.
    /// </summary>
    public const string DateTimeOffsetCodecId = ReservedPrefix + "date_time_offset";

    private const int CodecVersion = 1;

    /// <summary>
    /// Attempts to encode a supported scalar value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoded">The encoded value.</param>
    /// <param name="failure">The structured failure when encoding is rejected.</param>
    /// <returns><see langword="true"/> when the value was encoded.</returns>
    public static bool TryEncode<T>(
        T value,
        out ContentSnapshotEncodedValue? encoded,
        out ContentFailure? failure)
    {
        return TryEncodeObject(value, out encoded, out failure);
    }

    /// <summary>
    /// Encodes a supported scalar value or throws when the value cannot be encoded.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value.</returns>
    public static ContentSnapshotEncodedValue Encode<T>(T value)
    {
        if (TryEncode(value, out ContentSnapshotEncodedValue? encoded, out ContentFailure? failure) && encoded is not null)
        {
            return encoded;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    /// <summary>
    /// Attempts to decode a supported scalar value.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="encoded">The encoded value.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="failure">The structured failure when decoding is rejected.</param>
    /// <returns><see langword="true"/> when the value was decoded.</returns>
    public static bool TryDecode<T>(
        ContentSnapshotEncodedValue encoded,
        out T value,
        out ContentFailure? failure)
    {
        if (encoded is null)
        {
            throw new ArgumentNullException(nameof(encoded));
        }

        value = default!;
        if (!TryDecodeObject(encoded, typeof(T), out object? decoded, out failure))
        {
            return false;
        }

        if (decoded is null)
        {
            if (default(T) is not null)
            {
                failure = ContentFailures.SnapshotCodecRejected(
                    $"Snapshot null cannot be decoded as non-nullable type '{typeof(T).FullName}'.");
                return false;
            }

            value = default!;
            failure = null;
            return true;
        }

        if (decoded is not T typed)
        {
            failure = ContentFailures.SnapshotCodecRejected("Snapshot codec returned an incompatible value.");
            return false;
        }

        value = typed;
        return true;
    }

    /// <summary>
    /// Decodes a supported scalar value or throws when the value cannot be decoded.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="encoded">The encoded value.</param>
    /// <returns>The decoded value.</returns>
    public static T Decode<T>(ContentSnapshotEncodedValue encoded)
    {
        if (TryDecode(encoded, out T value, out ContentFailure? failure))
        {
            return value;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    private static bool TryEncodeObject(
        object? value,
        out ContentSnapshotEncodedValue? encoded,
        out ContentFailure? failure)
    {
        encoded = null;
        failure = null;

        if (value is null)
        {
            encoded = CreateEncoded(NullCodecId, ContentSnapshotValue.Null());
            return true;
        }

        Type valueType = value.GetType();
        if (valueType == typeof(string))
        {
            encoded = CreateEncoded(StringCodecId, ContentSnapshotValue.String((string)value));
            return true;
        }

        if (valueType == typeof(bool))
        {
            encoded = CreateEncoded(BooleanCodecId, ContentSnapshotValue.Boolean((bool)value));
            return true;
        }

        if (valueType == typeof(int))
        {
            encoded = CreateEncoded(
                Int32CodecId,
                ContentSnapshotValue.String(((int)value).ToString(CultureInfo.InvariantCulture)));
            return true;
        }

        if (valueType == typeof(long))
        {
            encoded = CreateEncoded(
                Int64CodecId,
                ContentSnapshotValue.String(((long)value).ToString(CultureInfo.InvariantCulture)));
            return true;
        }

        if (valueType == typeof(DateTimeOffset))
        {
            encoded = CreateEncoded(
                DateTimeOffsetCodecId,
                ContentSnapshotValue.String(((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture)));
            return true;
        }

        failure = ContentFailures.SnapshotCodecRejected(
            $"No content snapshot codec is registered for type '{valueType.FullName}'.");
        return false;
    }

    private static bool TryDecodeObject(
        ContentSnapshotEncodedValue encoded,
        Type expectedType,
        out object? value,
        out ContentFailure? failure)
    {
        value = null;
        failure = null;

        if (string.IsNullOrWhiteSpace(encoded.CodecId))
        {
            failure = ContentFailures.SnapshotMalformed("Snapshot encoded value is missing a codec ID.");
            return false;
        }

        if (encoded.CodecVersion != CodecVersion)
        {
            failure = ContentFailures.SnapshotUnsupportedVersion(
                $"Snapshot codec '{encoded.CodecId}' version {encoded.CodecVersion} is not supported.");
            return false;
        }

        if (encoded.Data is null)
        {
            failure = ContentFailures.SnapshotMalformed("Snapshot encoded value is missing data.");
            return false;
        }

        if (encoded.CodecId == NullCodecId)
        {
            if (encoded.Data.Kind != ContentSnapshotValueKind.Null)
            {
                failure = ContentFailures.SnapshotMalformed("Snapshot null codec must contain null data.");
                return false;
            }

            return true;
        }

        if (!CodecMatchesExpectedType(encoded.CodecId, expectedType))
        {
            failure = ContentFailures.SnapshotCodecRejected(
                $"Snapshot codec '{encoded.CodecId}' does not match expected type '{expectedType.FullName}'.");
            return false;
        }

        if (encoded.CodecId == StringCodecId)
        {
            return TryDecodeString(encoded.Data, out value, out failure);
        }

        if (encoded.CodecId == BooleanCodecId)
        {
            if (encoded.Data.Kind != ContentSnapshotValueKind.Boolean)
            {
                failure = ContentFailures.SnapshotMalformed("Snapshot Boolean codec must contain Boolean data.");
                return false;
            }

            value = encoded.Data.BooleanValue;
            return true;
        }

        if (encoded.CodecId == Int32CodecId)
        {
            if (!TryDecodeString(encoded.Data, out object? text, out failure))
            {
                return false;
            }

            if (!int.TryParse((string)text!, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                failure = ContentFailures.SnapshotMalformed("Snapshot Int32 codec contains malformed data.");
                return false;
            }

            value = parsed;
            return true;
        }

        if (encoded.CodecId == Int64CodecId)
        {
            if (!TryDecodeString(encoded.Data, out object? text, out failure))
            {
                return false;
            }

            if (!long.TryParse((string)text!, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
            {
                failure = ContentFailures.SnapshotMalformed("Snapshot Int64 codec contains malformed data.");
                return false;
            }

            value = parsed;
            return true;
        }

        if (encoded.CodecId == DateTimeOffsetCodecId)
        {
            if (!TryDecodeString(encoded.Data, out object? text, out failure))
            {
                return false;
            }

            if (!DateTimeOffset.TryParseExact(
                    (string)text!,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset parsed))
            {
                failure = ContentFailures.SnapshotMalformed("Snapshot DateTimeOffset codec contains malformed data.");
                return false;
            }

            value = parsed;
            return true;
        }

        failure = ContentFailures.SnapshotCodecRejected($"Snapshot codec '{encoded.CodecId}' is not supported.");
        return false;
    }

    private static bool TryDecodeString(
        ContentSnapshotValue data,
        out object? value,
        out ContentFailure? failure)
    {
        value = null;
        failure = null;
        if (data.Kind != ContentSnapshotValueKind.String || data.StringValue is null)
        {
            failure = ContentFailures.SnapshotMalformed("Snapshot string codec must contain string data.");
            return false;
        }

        value = data.StringValue;
        return true;
    }

    private static bool CodecMatchesExpectedType(string codecId, Type expectedType)
    {
        if (expectedType == typeof(string))
        {
            return codecId == StringCodecId;
        }

        if (expectedType == typeof(bool))
        {
            return codecId == BooleanCodecId;
        }

        if (expectedType == typeof(int))
        {
            return codecId == Int32CodecId;
        }

        if (expectedType == typeof(long))
        {
            return codecId == Int64CodecId;
        }

        if (expectedType == typeof(DateTimeOffset))
        {
            return codecId == DateTimeOffsetCodecId;
        }

        return false;
    }

    private static ContentSnapshotEncodedValue CreateEncoded(string codecId, ContentSnapshotValue data)
    {
        return new ContentSnapshotEncodedValue
        {
            CodecId = codecId,
            CodecVersion = CodecVersion,
            Data = data
        };
    }
}
