namespace Workes.ContentSystem.Core;

/// <summary>
/// Stable package-owned failure codes. Callers should branch on codes or kinds rather than display messages.
/// </summary>
public static class ContentFailureCodes
{
    /// <summary>
    /// Prefix reserved for built-in package failures.
    /// </summary>
    public const string PackagePrefix = "workes.content.";

    /// <summary>
    /// Unknown or unclassified failure.
    /// </summary>
    public const string Unknown = PackagePrefix + "unknown";

    /// <summary>
    /// General validation rejection.
    /// </summary>
    public const string ValidationRejected = PackagePrefix + "validation.rejected";

    /// <summary>
    /// Configuration rejection.
    /// </summary>
    public const string ConfigurationRejected = PackagePrefix + "configuration.rejected";

    /// <summary>
    /// Entry operation rejection.
    /// </summary>
    public const string EntryRejected = PackagePrefix + "entry.rejected";

    /// <summary>
    /// Entry ID could not be found.
    /// </summary>
    public const string EntryNotFound = PackagePrefix + "entry.not_found";

    /// <summary>
    /// Entry ID is invalid for the active structure or ID strategy.
    /// </summary>
    public const string EntryIdInvalid = PackagePrefix + "entry.id.invalid";

    /// <summary>
    /// Entry ID already exists in the active structure.
    /// </summary>
    public const string EntryIdDuplicate = PackagePrefix + "entry.id.duplicate";

    /// <summary>
    /// Structure operation rejection.
    /// </summary>
    public const string StructureRejected = PackagePrefix + "structure.rejected";

    /// <summary>
    /// Structure is read-only for the requested operation.
    /// </summary>
    public const string StructureReadOnly = PackagePrefix + "structure.read_only";

    /// <summary>
    /// Structure does not support the requested operation.
    /// </summary>
    public const string StructureUnsupportedOperation = PackagePrefix + "structure.unsupported_operation";

    /// <summary>
    /// Export operation rejection.
    /// </summary>
    public const string ExportRejected = PackagePrefix + "export.rejected";

    /// <summary>
    /// Attachment operation rejection.
    /// </summary>
    public const string AttachmentRejected = PackagePrefix + "attachment.rejected";

    /// <summary>
    /// Persistence operation rejection.
    /// </summary>
    public const string PersistenceRejected = PackagePrefix + "persistence.rejected";

    /// <summary>
    /// Bridge operation rejection.
    /// </summary>
    public const string BridgeRejected = PackagePrefix + "bridge.rejected";

    /// <summary>
    /// Extension contract rejection.
    /// </summary>
    public const string ExtensionRejected = PackagePrefix + "extension.rejected";
}
