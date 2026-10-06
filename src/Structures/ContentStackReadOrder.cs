namespace Workes.ContentSystem.Core;

/// <summary>
/// Defines how retained stack records are exposed for reading.
/// </summary>
public enum ContentStackReadOrder
{
    /// <summary>
    /// Expose records from the top of the stack to the bottom.
    /// </summary>
    TopFirst = 0,

    /// <summary>
    /// Expose records from the bottom of the stack to the top.
    /// </summary>
    BottomFirst
}
