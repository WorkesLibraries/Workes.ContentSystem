using System;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Resolves content structures to their normal managers.
/// </summary>
public static class ContentManagers
{
    /// <summary>
    /// Attempts to create the manager owned by a structure.
    /// </summary>
    /// <param name="structure">The content structure.</param>
    /// <param name="manager">The created manager when accepted; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when manager creation is rejected.</param>
    /// <returns><see langword="true"/> when a manager is created.</returns>
    public static bool TryForStructure(
        IContentStructure structure,
        out ContentManagerBase? manager,
        out ContentFailure? failure)
    {
        if (structure is null)
        {
            throw new ArgumentNullException(nameof(structure));
        }

        manager = structure.CreateManager();
        if (manager is null)
        {
            failure = ContentFailures.StructureUnsupportedOperation(
                $"Structure type '{structure.GetType().FullName}' did not create a content manager.");
            return false;
        }

        failure = null;
        return true;
    }

    /// <summary>
    /// Creates the manager owned by a structure.
    /// </summary>
    /// <param name="structure">The content structure.</param>
    /// <returns>The created manager.</returns>
    /// <exception cref="ContentOperationException">Thrown when manager creation is rejected.</exception>
    public static ContentManagerBase ForStructure(IContentStructure structure)
    {
        if (TryForStructure(structure, out ContentManagerBase? manager, out ContentFailure? failure))
        {
            return manager!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <summary>
    /// Attempts to create the expected manager type owned by a structure.
    /// </summary>
    /// <typeparam name="TManager">The expected manager type.</typeparam>
    /// <param name="structure">The content structure.</param>
    /// <param name="manager">The created manager when accepted and assignable; otherwise <see langword="null"/>.</param>
    /// <param name="failure">The structured failure when manager creation is rejected or the manager type does not match.</param>
    /// <returns><see langword="true"/> when the expected manager is created.</returns>
    public static bool TryForStructure<TManager>(
        IContentStructure structure,
        out TManager? manager,
        out ContentFailure? failure)
        where TManager : ContentManagerBase
    {
        if (!TryForStructure(structure, out ContentManagerBase? resolved, out failure))
        {
            manager = null;
            return false;
        }

        if (resolved is TManager typed)
        {
            manager = typed;
            failure = null;
            return true;
        }

        manager = null;
        failure = ContentFailures.ManagerMismatch(
            $"Resolved manager type '{resolved!.GetType().FullName}' is not assignable to expected manager type '{typeof(TManager).FullName}'.",
            structure.GetType().FullName);
        return false;
    }

    /// <summary>
    /// Creates the expected manager type owned by a structure.
    /// </summary>
    /// <typeparam name="TManager">The expected manager type.</typeparam>
    /// <param name="structure">The content structure.</param>
    /// <returns>The created manager.</returns>
    /// <exception cref="ContentOperationException">Thrown when manager creation is rejected or the manager type does not match.</exception>
    public static TManager ForStructure<TManager>(IContentStructure structure)
        where TManager : ContentManagerBase
    {
        if (TryForStructure(structure, out TManager? manager, out ContentFailure? failure))
        {
            return manager!;
        }

        throw new ContentOperationException(failure!);
    }
}
