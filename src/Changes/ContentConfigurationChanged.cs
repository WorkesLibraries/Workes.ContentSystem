using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Describes a runtime content configuration change.
/// </summary>
public sealed class ContentConfigurationChanged
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentConfigurationChanged"/> class.
    /// </summary>
    /// <param name="kind">The kind of configuration that changed.</param>
    /// <param name="configurationId">The stable configuration or parameter ID.</param>
    /// <param name="value">The committed parameter value.</param>
    /// <param name="previousComponent">The component before the change.</param>
    /// <param name="currentComponent">The component after the change.</param>
    /// <param name="requiresFullRefresh">Whether observers should rebuild their full view.</param>
    public ContentConfigurationChanged(
        ContentConfigurationChangeKind kind,
        string configurationId,
        object? value,
        object previousComponent,
        object currentComponent,
        bool requiresFullRefresh)
    {
        if (string.IsNullOrWhiteSpace(configurationId))
        {
            throw new ArgumentException("Configuration ID cannot be null or empty.", nameof(configurationId));
        }

        Kind = kind;
        ConfigurationId = configurationId;
        Value = value;
        PreviousComponent = previousComponent ?? throw new ArgumentNullException(nameof(previousComponent));
        CurrentComponent = currentComponent ?? throw new ArgumentNullException(nameof(currentComponent));
        RequiresFullRefresh = requiresFullRefresh;
    }

    /// <summary>
    /// Gets the kind of configuration that changed.
    /// </summary>
    public ContentConfigurationChangeKind Kind { get; }

    /// <summary>
    /// Gets the stable configuration or parameter ID.
    /// </summary>
    public string ConfigurationId { get; }

    /// <summary>
    /// Gets the committed parameter value.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the component before the change.
    /// </summary>
    public object PreviousComponent { get; }

    /// <summary>
    /// Gets the component after the change.
    /// </summary>
    public object CurrentComponent { get; }

    /// <summary>
    /// Gets whether observers should rebuild their full view.
    /// </summary>
    public bool RequiresFullRefresh { get; }
}
