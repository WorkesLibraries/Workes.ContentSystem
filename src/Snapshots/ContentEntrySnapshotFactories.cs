using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Provides package-wide lookup for entry snapshot factories used during restore workflows.
/// </summary>
public static class ContentEntrySnapshotFactories
{
    private static readonly object s_gate = new object();
    private static readonly Dictionary<string, IContentEntrySnapshotFactory> s_factories =
        new Dictionary<string, IContentEntrySnapshotFactory>(StringComparer.Ordinal);

    static ContentEntrySnapshotFactories()
    {
        Register(PlainContentEntry.Factory);
    }

    /// <summary>
    /// Attempts to register an entry snapshot factory.
    /// </summary>
    /// <param name="factory">The factory to register.</param>
    /// <param name="failure">The structured failure when registration is rejected.</param>
    /// <returns><see langword="true"/> when the factory is registered or was already registered.</returns>
    public static bool TryRegister(IContentEntrySnapshotFactory factory, out ContentFailure? failure)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        if (string.IsNullOrWhiteSpace(factory.Kind))
        {
            throw new ArgumentException("Entry snapshot factory kind cannot be empty.", nameof(factory));
        }

        lock (s_gate)
        {
            if (s_factories.TryGetValue(factory.Kind, out IContentEntrySnapshotFactory? existing))
            {
                if (ReferenceEquals(existing, factory))
                {
                    failure = null;
                    return true;
                }

                failure = ContentFailures.SnapshotFactoryDuplicate(
                    $"Entry snapshot factory kind '{factory.Kind}' is already registered.",
                    factory.Kind);
                return false;
            }

            s_factories.Add(factory.Kind, factory);
            failure = null;
            return true;
        }
    }

    /// <summary>
    /// Registers an entry snapshot factory.
    /// </summary>
    /// <param name="factory">The factory to register.</param>
    /// <exception cref="ContentOperationException">Thrown when another factory already owns the same kind.</exception>
    public static void Register(IContentEntrySnapshotFactory factory)
    {
        if (!TryRegister(factory, out ContentFailure? failure))
        {
            throw new ContentOperationException(failure!);
        }
    }

    /// <summary>
    /// Attempts to get the factory registered for a snapshot kind.
    /// </summary>
    /// <param name="kind">The snapshot kind.</param>
    /// <param name="factory">The matching factory.</param>
    /// <returns><see langword="true"/> when a matching factory is registered.</returns>
    public static bool TryGet(string kind, out IContentEntrySnapshotFactory? factory)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            factory = null;
            return false;
        }

        lock (s_gate)
        {
            return s_factories.TryGetValue(kind, out factory);
        }
    }
}
