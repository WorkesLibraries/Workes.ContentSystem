namespace Workes.ContentSystem.Core;

/// <summary>
/// Describes the broad subsystem or reason category for an expected content-system failure.
/// </summary>
public enum ContentFailureKind
{
    /// <summary>
    /// The failure could not be classified more precisely.
    /// </summary>
    Unknown,

    /// <summary>
    /// General validation rejected the request.
    /// </summary>
    Validation,

    /// <summary>
    /// Configuration validation rejected the request.
    /// </summary>
    Configuration,

    /// <summary>
    /// Entry validation, lookup, or mutation rejected the request.
    /// </summary>
    Entry,

    /// <summary>
    /// Structure validation, capability, lookup, or mutation rejected the request.
    /// </summary>
    Structure,

    /// <summary>
    /// Export behavior rejected or failed the request.
    /// </summary>
    Export,

    /// <summary>
    /// Attachment behavior rejected or failed the request.
    /// </summary>
    Attachment,

    /// <summary>
    /// Persistence behavior rejected or failed the request.
    /// </summary>
    Persistence,

    /// <summary>
    /// Bridge behavior rejected or failed the request.
    /// </summary>
    Bridge,

    /// <summary>
    /// Extension-provided code rejected or failed inside an expected path.
    /// </summary>
    Extension
}
