namespace Workes.ContentSystem.Core;

/// <summary>
/// Exception thrown by expected-success content operations when validation rejects the operation.
/// </summary>
public sealed class ContentOperationException : ContentSystemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentOperationException"/> class.
    /// </summary>
    /// <param name="failure">The structured operation failure.</param>
    public ContentOperationException(ContentFailure failure)
        : base(failure)
    {
    }
}
