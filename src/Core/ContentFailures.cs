namespace Workes.ContentSystem.Core;

internal static class ContentFailures
{
    public static ContentFailure Unknown(string? message = null)
    {
        return Create(ContentFailureKind.Unknown, ContentFailureCodes.Unknown, message);
    }

    public static ContentFailure Validation(string? message = null)
    {
        return Create(ContentFailureKind.Validation, ContentFailureCodes.ValidationRejected, message);
    }

    public static ContentFailure Configuration(string? message = null)
    {
        return Create(ContentFailureKind.Configuration, ContentFailureCodes.ConfigurationRejected, message);
    }

    public static ContentFailure Entry(string? message = null)
    {
        return Create(ContentFailureKind.Entry, ContentFailureCodes.EntryRejected, message);
    }

    public static ContentFailure EntryNotFound(string? message = null, string? source = null)
    {
        return Create(ContentFailureKind.Entry, ContentFailureCodes.EntryNotFound, message, source: source);
    }

    public static ContentFailure Structure(string? message = null)
    {
        return Create(ContentFailureKind.Structure, ContentFailureCodes.StructureRejected, message);
    }

    public static ContentFailure StructureReadOnly(string? message = null)
    {
        return Create(ContentFailureKind.Structure, ContentFailureCodes.StructureReadOnly, message);
    }

    public static ContentFailure StructureUnsupportedOperation(string? message = null)
    {
        return Create(ContentFailureKind.Structure, ContentFailureCodes.StructureUnsupportedOperation, message);
    }

    public static ContentFailure Export(string? message = null)
    {
        return Create(ContentFailureKind.Export, ContentFailureCodes.ExportRejected, message);
    }

    public static ContentFailure Attachment(string? message = null)
    {
        return Create(ContentFailureKind.Attachment, ContentFailureCodes.AttachmentRejected, message);
    }

    public static ContentFailure Persistence(string? message = null)
    {
        return Create(ContentFailureKind.Persistence, ContentFailureCodes.PersistenceRejected, message);
    }

    public static ContentFailure Bridge(string? message = null)
    {
        return Create(ContentFailureKind.Bridge, ContentFailureCodes.BridgeRejected, message);
    }

    public static ContentFailure Extension(string? message = null)
    {
        return Create(ContentFailureKind.Extension, ContentFailureCodes.ExtensionRejected, message);
    }

    private static ContentFailure Create(
        ContentFailureKind kind,
        string code,
        string? message,
        string? component = null,
        string? source = null,
        ContentFailure? cause = null)
    {
        return ContentFailure.Create(
            kind,
            code,
            string.IsNullOrWhiteSpace(message) ? "Content operation failed." : message!,
            component,
            source,
            cause);
    }
}
