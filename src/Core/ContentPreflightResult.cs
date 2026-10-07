using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a side-effect-free assessment of whether an operation can commit.
/// </summary>
public sealed class ContentPreflightResult
{
    private static readonly ContentPreflightResult s_success = new ContentPreflightResult(true, null);

    private ContentPreflightResult(bool canCommit, ContentFailure? failure)
    {
        CanCommit = canCommit;
        Failure = failure;
    }

    /// <summary>
    /// Gets a value indicating whether the assessed operation can currently commit.
    /// </summary>
    public bool CanCommit { get; }

    /// <summary>
    /// Gets the structured failure when the operation is rejected.
    /// </summary>
    public ContentFailure? Failure { get; }

    /// <summary>
    /// Creates a successful preflight result.
    /// </summary>
    public static ContentPreflightResult Success()
    {
        return s_success;
    }

    /// <summary>
    /// Creates a rejected preflight result.
    /// </summary>
    /// <param name="failure">The structured failure that would reject the operation.</param>
    public static ContentPreflightResult Rejected(ContentFailure failure)
    {
        return new ContentPreflightResult(false, failure ?? throw new ArgumentNullException(nameof(failure)));
    }
}
