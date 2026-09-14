using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Base exception for expected-success content-system wrappers that fail due to domain rejection.
/// </summary>
public class ContentSystemException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSystemException"/> class.
    /// </summary>
    /// <param name="failure">The structured failure.</param>
    public ContentSystemException(ContentFailure failure)
        : base((failure ?? throw new ArgumentNullException(nameof(failure))).Message)
    {
        Failure = failure;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentSystemException"/> class.
    /// </summary>
    /// <param name="failure">The structured failure.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public ContentSystemException(ContentFailure failure, Exception? innerException)
        : base((failure ?? throw new ArgumentNullException(nameof(failure))).Message, innerException)
    {
        Failure = failure;
    }

    /// <summary>
    /// Gets the structured failure.
    /// </summary>
    public ContentFailure Failure { get; }
}
