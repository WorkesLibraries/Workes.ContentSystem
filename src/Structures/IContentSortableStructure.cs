using System;
using System.Collections.Generic;

namespace Workes.ContentSystem.Core;

/// <summary>Optional structure capability for reordering retained map records.</summary>
public interface IContentSortableStructure
{
    /// <summary>Assesses whether sorting is supported.</summary>
    ContentPreflightResult AssessSort(IComparer<ContentEntryRecord> comparer);
    /// <summary>Attempts to sort records, returning whether their order changed.</summary>
    bool TrySort(IComparer<ContentEntryRecord> comparer, out bool orderChanged, out ContentFailure? failure);
}

/// <summary>Optional structure capability for sorting compound sibling groups.</summary>
public interface IContentCompoundSortableStructure
{
    /// <summary>Assesses whether all sibling groups can be sorted.</summary>
    ContentPreflightResult AssessSortSiblings(IComparer<ContentEntryRecord> comparer);
    /// <summary>Attempts to sort all sibling groups, returning whether any order changed.</summary>
    bool TrySortSiblings(IComparer<ContentEntryRecord> comparer, out bool orderChanged, out ContentFailure? failure);

    /// <summary>Assesses sorting all sibling groups with a comparer selected for each parent ID; null identifies roots.</summary>
    ContentPreflightResult AssessSortSiblings(Func<ContentEntryId?, IComparer<ContentEntryRecord>> comparerSelector);
    /// <summary>Attempts to sort all sibling groups with a comparer selected for each parent ID; null identifies roots.</summary>
    bool TrySortSiblings(Func<ContentEntryId?, IComparer<ContentEntryRecord>> comparerSelector, out bool orderChanged, out ContentFailure? failure);

    /// <summary>Assesses sorting the roots or one parent's children; null identifies roots.</summary>
    ContentPreflightResult AssessSortSiblings(ContentEntryId? parentId, IComparer<ContentEntryRecord> comparer);
    /// <summary>Attempts to sort the roots or one parent's children; null identifies roots.</summary>
    bool TrySortSiblings(ContentEntryId? parentId, IComparer<ContentEntryRecord> comparer, out bool orderChanged, out ContentFailure? failure);
}
