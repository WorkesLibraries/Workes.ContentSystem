using System.Linq;

namespace Workes.ContentSystem.Core;

internal static class ContentSnapshotProperties
{
    public static ContentSnapshotNamedValue Named(string name, ContentSnapshotEncodedValue value)
    {
        return new ContentSnapshotNamedValue { Name = name, Value = value };
    }

    public static bool TryGetRequired(
        ContentSnapshotValue data,
        string name,
        out ContentSnapshotEncodedValue? value,
        out ContentFailure? failure)
    {
        value = null;
        failure = null;

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
}
