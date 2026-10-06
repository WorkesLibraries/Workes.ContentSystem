namespace Workes.ContentSystem.Core;

/// <summary>
/// Defines how compound structures handle removal of nodes that have children.
/// </summary>
public enum ContentCompoundChildRemovalPolicy
{
    /// <summary>
    /// Reject removal when the target node has children.
    /// </summary>
    Reject = 0,

    /// <summary>
    /// Remove the target node and its descendants.
    /// </summary>
    RemoveSubtree = 1
}
