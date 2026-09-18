using System.Linq;

namespace Workes.ContentSystem.Core;

internal static class ContentSnapshotCloner
{
    public static ContentSnapshotEncodedValue CloneEncoded(ContentSnapshotEncodedValue value)
    {
        return new ContentSnapshotEncodedValue
        {
            CodecId = value.CodecId,
            CodecVersion = value.CodecVersion,
            Data = CloneValue(value.Data)
        };
    }

    public static ContentSnapshotNamedValue CloneNamed(ContentSnapshotNamedValue value)
    {
        return new ContentSnapshotNamedValue
        {
            Name = value.Name,
            Value = CloneEncoded(value.Value)
        };
    }

    public static ContentSnapshotValue CloneValue(ContentSnapshotValue value)
    {
        return new ContentSnapshotValue
        {
            Kind = value.Kind,
            BooleanValue = value.BooleanValue,
            StringValue = value.StringValue,
            Items = value.Items.Select(CloneEncoded).ToList(),
            Properties = value.Properties.Select(CloneNamed).ToList()
        };
    }
}
