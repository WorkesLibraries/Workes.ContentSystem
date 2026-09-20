namespace Workes.ContentSystem.Core;

/// <summary>
/// Represents a content structure that exposes its retention and overflow policy.
/// </summary>
public interface IContentRetentionPolicyStructure : IContentStructure
{
    /// <summary>
    /// Gets the policy used for retention and overflow.
    /// </summary>
    ContentOverflowPolicy OverflowPolicy { get; }
}
