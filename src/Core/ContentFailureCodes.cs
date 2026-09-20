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
    /// Structure resolved to a manager that does not match the requested manager type.
    /// </summary>
    public const string ManagerMismatch = PackagePrefix + "manager.mismatch";

    /// <summary>
    /// Snapshot operation rejection.
    /// </summary>
    public const string SnapshotRejected = PackagePrefix + "snapshot.rejected";

    /// <summary>
    /// Snapshot capture is not supported by the active entry or component.
    /// </summary>
    public const string SnapshotUnsupportedEntry = PackagePrefix + "snapshot.entry.unsupported";

    /// <summary>
    /// Snapshot capture is not supported by the active structure or component.
    /// </summary>
    public const string SnapshotUnsupportedStructure = PackagePrefix + "snapshot.structure.unsupported";

    /// <summary>
    /// Snapshot restore could not find a required factory.
    /// </summary>
    public const string SnapshotFactoryMissing = PackagePrefix + "snapshot.factory.missing";

    /// <summary>
    /// Snapshot restore factory registration conflicts with an existing factory.
    /// </summary>
    public const string SnapshotFactoryDuplicate = PackagePrefix + "snapshot.factory.duplicate";

    /// <summary>
    /// Snapshot data is missing, malformed, or inconsistent.
    /// </summary>
    public const string SnapshotMalformed = PackagePrefix + "snapshot.malformed";

    /// <summary>
    /// Snapshot data uses an unsupported format or data version.
    /// </summary>
    public const string SnapshotUnsupportedVersion = PackagePrefix + "snapshot.unsupported_version";

    /// <summary>
    /// Snapshot codec rejected the encoded value.
    /// </summary>
    public const string SnapshotCodecRejected = PackagePrefix + "snapshot.codec.rejected";

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
