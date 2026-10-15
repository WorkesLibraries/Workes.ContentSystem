using System;
using System.Collections.Generic;
using System.Linq;

namespace Workes.ContentSystem.Core;

/// <summary>
/// Stores content records as an owned tree of compound nodes.
/// </summary>
/// <typeparam name="TId">The natural retained-record ID type.</typeparam>
public class ContentCompoundStructure<TId> :
    ContentCompoundStructureBase<TId>,
    IContentCompoundSortableStructure,
    IContentCompoundBatchStructure<TId>,
    IContentPredicateRemovalStructure,
    IContentStructureSnapshotRoundTrippable,
    IContentChangeSource
{
    /// <summary>
    /// Stable structure snapshot kind for compound content structures.
    /// </summary>
    public const string SnapshotKind = ContentFailureCodes.PackagePrefix + "structure.compound";

    /// <summary>
    /// Current structure snapshot data version for compound content structures.
    /// </summary>
    public const int SnapshotDataVersion = 1;

    private readonly Dictionary<ContentEntryId, CompoundNodeState> _nodes = new Dictionary<ContentEntryId, CompoundNodeState>();
    private readonly List<ContentEntryId> _rootIds = new List<ContentEntryId>();
    private readonly List<ContentEntryId> _nodeIds = new List<ContentEntryId>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundStructure{TId}"/> class.
    /// </summary>
    public ContentCompoundStructure(
        IContentGeneratedIdSource<TId> idSource,
        ContentCompoundChildRemovalPolicy childRemovalPolicy = ContentCompoundChildRemovalPolicy.Reject,
        ContentCompoundSiblingReadOrder siblingReadOrder = ContentCompoundSiblingReadOrder.OldestFirst)
    {
        IdSource = idSource ?? throw new ArgumentNullException(nameof(idSource));
        if (!Enum.IsDefined(typeof(ContentCompoundChildRemovalPolicy), childRemovalPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(childRemovalPolicy), childRemovalPolicy, "Compound child removal policy is not supported.");
        }

        if (!Enum.IsDefined(typeof(ContentCompoundSiblingReadOrder), siblingReadOrder))
        {
            throw new ArgumentOutOfRangeException(nameof(siblingReadOrder), siblingReadOrder, "Compound sibling read order is not supported.");
        }

        ChildRemovalPolicy = childRemovalPolicy;
        SiblingReadOrder = siblingReadOrder;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundStructure{TId}"/> class with retained records.
    /// </summary>
    protected ContentCompoundStructure(
        IContentGeneratedIdSource<TId> idSource,
        ContentCompoundChildRemovalPolicy childRemovalPolicy,
        ContentCompoundSiblingReadOrder siblingReadOrder,
        IEnumerable<ContentEntryRecord> records,
        IReadOnlyDictionary<ContentEntryId, ContentEntryId?> parentIds)
        : this(idSource, childRemovalPolicy, siblingReadOrder)
    {
        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        if (parentIds is null)
        {
            throw new ArgumentNullException(nameof(parentIds));
        }

        RestoreNodes(records, parentIds);
    }

    /// <summary>
    /// Creates a snapshot factory for generic compound structures using the supplied generated-ID source factory.
    /// </summary>
    /// <param name="idSourceFactory">The generated-ID source factory used to restore source state.</param>
    /// <returns>A structure snapshot factory for the configured ID source.</returns>
    public static IContentStructureSnapshotFactory CreateSnapshotFactory(IContentGeneratedIdSourceFactory<TId> idSourceFactory)
    {
        return new ContentCompoundStructureSnapshotFactory(idSourceFactory);
    }

    /// <summary>
    /// Gets the generated ID source used by id-less node creation.
    /// </summary>
    public IContentGeneratedIdSource<TId> IdSource { get; }

    /// <summary>
    /// Gets the policy used when removing a node with children.
    /// </summary>
    public ContentCompoundChildRemovalPolicy ChildRemovalPolicy { get; }

    /// <summary>
    /// Gets the sibling read order.
    /// </summary>
    public ContentCompoundSiblingReadOrder SiblingReadOrder { get; }

    /// <summary>
    /// Gets the number of retained nodes.
    /// </summary>
    public int Count => _nodes.Count;

    /// <inheritdoc />
    public virtual IContentStructureSnapshotFactory SnapshotFactory => CreateSnapshotFactory(IdSource.SnapshotFactory);

    /// <inheritdoc />
    public override IReadOnlyList<ContentEntryRecord> Records => FlattenRecords();

    /// <inheritdoc />
    public ContentPreflightResult AssessSortSiblings(IComparer<ContentEntryRecord> comparer)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public bool TrySortSiblings(IComparer<ContentEntryRecord> comparer, out bool orderChanged, out ContentFailure? failure)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        return TrySortSiblings(_ => comparer, out orderChanged, out failure);
    }

    /// <inheritdoc />
    public ContentPreflightResult AssessSortSiblings(Func<ContentEntryId?, IComparer<ContentEntryRecord>> comparerSelector)
    {
        if (comparerSelector is null) throw new ArgumentNullException(nameof(comparerSelector));
        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public bool TrySortSiblings(Func<ContentEntryId?, IComparer<ContentEntryRecord>> comparerSelector, out bool orderChanged, out ContentFailure? failure)
    {
        if (comparerSelector is null) throw new ArgumentNullException(nameof(comparerSelector));

        // Resolve and sort every applicable sibling group before committing any group.
        var candidates = new Dictionary<ContentEntryId, List<ContentEntryId>>();
        List<ContentEntryId> roots = _rootIds;
        bool rootsChanged = false;
        if (_rootIds.Count > 1)
        {
            IComparer<ContentEntryRecord> comparer = comparerSelector(null)
                ?? throw new InvalidOperationException("The sibling comparer selector returned null for the root group.");
            roots = SortVisibleGroup(_rootIds, comparer, out rootsChanged);
        }
        bool anyChanged = rootsChanged;
        foreach (KeyValuePair<ContentEntryId, CompoundNodeState> pair in _nodes)
        {
            if (pair.Value.ChildIds.Count > 1)
            {
                IComparer<ContentEntryRecord> comparer = comparerSelector(pair.Key)
                    ?? throw new InvalidOperationException($"The sibling comparer selector returned null for parent '{pair.Key}'.");
                List<ContentEntryId> sorted = SortVisibleGroup(pair.Value.ChildIds, comparer, out bool changed);
                if (changed) candidates.Add(pair.Key, sorted);
                anyChanged |= changed;
            }
        }

        orderChanged = anyChanged;
        if (anyChanged)
        {
            if (rootsChanged) ReplaceOrder(_rootIds, roots);
            foreach (KeyValuePair<ContentEntryId, List<ContentEntryId>> pair in candidates)
            {
                ReplaceOrder(_nodes[pair.Key].ChildIds, pair.Value);
            }
            CompleteSortCommit();
        }
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public ContentPreflightResult AssessSortSiblings(ContentEntryId? parentId, IComparer<ContentEntryRecord> comparer)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        if (parentId is not null && !_nodes.ContainsKey(parentId.Value))
        {
            return ContentPreflightResult.Rejected(ContentFailures.EntryNotFound(
                $"Compound parent node '{parentId.Value}' was not found.", parentId.Value.ToString()));
        }
        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public bool TrySortSiblings(ContentEntryId? parentId, IComparer<ContentEntryRecord> comparer, out bool orderChanged, out ContentFailure? failure)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));
        ContentPreflightResult assessment = AssessSortSiblings(parentId, comparer);
        if (!assessment.CanCommit)
        {
            orderChanged = false;
            failure = assessment.Failure;
            return false;
        }

        List<ContentEntryId> group = parentId is null ? _rootIds : _nodes[parentId.Value].ChildIds;
        if (group.Count < 2)
        {
            orderChanged = false;
            failure = null;
            return true;
        }

        List<ContentEntryId> sorted = SortVisibleGroup(group, comparer, out orderChanged);
        if (orderChanged)
        {
            ReplaceOrder(group, sorted);
            CompleteSortCommit();
        }
        failure = null;
        return true;
    }

    private void CompleteSortCommit()
    {
        // Snapshot record order determines each restored sibling group's order.
        _nodeIds.Clear();
        foreach (ContentEntryId root in _rootIds) AddInternalPreorder(root, _nodeIds);
        OnChanged(new ContentChangedEventArgs(kind: ContentChangeKind.Sorted, requiresFullRefresh: true));
    }

    private static void ReplaceOrder(List<ContentEntryId> target, IReadOnlyList<ContentEntryId> replacement)
    {
        target.Clear();
        target.AddRange(replacement);
    }

    private List<ContentEntryId> SortVisibleGroup(IReadOnlyList<ContentEntryId> internalIds, IComparer<ContentEntryRecord> comparer, out bool changed)
    {
        ContentEntryId[] visible = ApplySiblingOrder(internalIds).ToArray();
        ContentEntryId[] sortedVisible = visible.OrderBy(id => _nodes[id].Record, comparer).ToArray();
        changed = !visible.SequenceEqual(sortedVisible);
        if (SiblingReadOrder == ContentCompoundSiblingReadOrder.NewestFirst) Array.Reverse(sortedVisible);
        return sortedVisible.ToList();
    }

    private void AddInternalPreorder(ContentEntryId id, List<ContentEntryId> target)
    {
        target.Add(id);
        foreach (ContentEntryId childId in _nodes[id].ChildIds) AddInternalPreorder(childId, target);
    }

    /// <inheritdoc />
    public event EventHandler<ContentChangedEventArgs>? Changed;

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentCompoundManager<TId>(this);
    }

    /// <inheritdoc />
    public override bool TryAddRoot(IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        node = null;
        if (!IdSource.TryCreateNext(out TId id, out failure))
        {
            return false;
        }

        return TryAddAcceptedNode(id, entry, parentId: null, observeId: false, out node, out failure);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessAddRoot(IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        return IdSource.CanCreateNext(out ContentFailure? failure)
            ? ContentPreflightResult.Success()
            : ContentPreflightResult.Rejected(failure!);
    }

    /// <inheritdoc />
    public override ContentCompoundNode AddRoot(IContentEntry entry)
    {
        if (TryAddRoot(entry, out ContentCompoundNode? node, out ContentFailure? failure))
        {
            return node!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryAddRoot(TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        return TryAddAcceptedNode(id, entry, parentId: null, observeId: true, out node, out failure);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessAddRoot(TId id, IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        return AssessAcceptedNode(id, parentId: null, observeId: true);
    }

    /// <inheritdoc />
    public override ContentCompoundNode AddRoot(TId id, IContentEntry entry)
    {
        if (TryAddRoot(id, entry, out ContentCompoundNode? node, out ContentFailure? failure))
        {
            return node!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public ContentPreflightResult AssessAddRoots(IReadOnlyList<IContentEntry> entries)
    {
        ValidateBatchEntries(entries, nameof(entries));
        if (entries.Count == 0) return ContentPreflightResult.Success();
        if (IdSource is not IContentGeneratedIdRangeSource<TId> rangeSource)
            return ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The generated ID source does not support atomic ID ranges."));
        return rangeSource.CanCreateRange(entries.Count, out ContentFailure? failure) ? ContentPreflightResult.Success() : ContentPreflightResult.Rejected(failure!);
    }

    /// <inheritdoc />
    public bool TryAddRoots(IReadOnlyList<IContentEntry> entries, out IReadOnlyList<ContentCompoundNode> nodes, out ContentFailure? failure)
    {
        ValidateBatchEntries(entries, nameof(entries));
        nodes = Array.Empty<ContentCompoundNode>();
        if (entries.Count == 0) { failure = null; return true; }
        ContentPreflightResult assessment = AssessAddRoots(entries);
        if (!assessment.CanCommit) { failure = assessment.Failure; return false; }
        if (!((IContentGeneratedIdRangeSource<TId>)IdSource).TryCreateRange(entries.Count, out IReadOnlyList<TId> ids, out failure)) return false;
        if (!TryCreateBatchNodes(ids, entries, null, out ContentCompoundNode[] created, out failure)) return false;
        foreach (ContentCompoundNode node in created)
        {
            CompoundNodeState state = new CompoundNodeState(node.Record, parentId: null);
            _nodes.Add(node.Record.Id, state); _nodeIds.Add(node.Record.Id); _rootIds.Add(node.Record.Id);
        }
        nodes = Array.AsReadOnly(created);
        OnChanged(new ContentChangedEventArgs(created.Select(node => node.Record), kind: ContentChangeKind.Added, requiresFullRefresh: true));
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public ContentPreflightResult AssessAddChildren(TId parentId, IReadOnlyList<IContentEntry> entries)
    {
        ValidateBatchEntries(entries, nameof(entries));
        if (!TryNormalize(parentId, out ContentEntryId normalizedParentId, out ContentFailure? failure)
            || !TryValidateExistingParent(normalizedParentId, out failure)) return ContentPreflightResult.Rejected(failure!);
        if (entries.Count == 0) return ContentPreflightResult.Success();
        if (IdSource is not IContentGeneratedIdRangeSource<TId> rangeSource)
            return ContentPreflightResult.Rejected(ContentFailures.StructureUnsupportedOperation("The generated ID source does not support atomic ID ranges."));
        return rangeSource.CanCreateRange(entries.Count, out failure) ? ContentPreflightResult.Success() : ContentPreflightResult.Rejected(failure!);
    }

    /// <inheritdoc />
    public bool TryAddChildren(TId parentId, IReadOnlyList<IContentEntry> entries, out IReadOnlyList<ContentCompoundNode> nodes, out ContentFailure? failure)
    {
        ValidateBatchEntries(entries, nameof(entries));
        nodes = Array.Empty<ContentCompoundNode>();
        if (!TryNormalize(parentId, out ContentEntryId normalizedParentId, out failure)
            || !TryValidateExistingParent(normalizedParentId, out failure)) return false;
        if (entries.Count == 0) { failure = null; return true; }
        ContentPreflightResult assessment = AssessAddChildren(parentId, entries);
        if (!assessment.CanCommit) { failure = assessment.Failure; return false; }
        if (!((IContentGeneratedIdRangeSource<TId>)IdSource).TryCreateRange(entries.Count, out IReadOnlyList<TId> ids, out failure)) return false;
        if (!TryCreateBatchNodes(ids, entries, normalizedParentId, out ContentCompoundNode[] created, out failure)) return false;
        CompoundNodeState parent = _nodes[normalizedParentId];
        foreach (ContentCompoundNode node in created)
        {
            var state = new CompoundNodeState(node.Record, normalizedParentId);
            _nodes.Add(node.Record.Id, state); _nodeIds.Add(node.Record.Id); parent.ChildIds.Add(node.Record.Id);
        }
        nodes = Array.AsReadOnly(created);
        OnChanged(new ContentChangedEventArgs(created.Select(node => node.Record), kind: ContentChangeKind.Added, requiresFullRefresh: true));
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public ContentPreflightResult AssessRemoveWhere(Func<ContentEntryRecord, bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        ContentEntryRecord[] records = Records.ToArray();
        ContentEntryRecord[] selected = records.Where(predicate).ToArray();
        return AssessCompoundRemoval(selected);
    }

    /// <inheritdoc />
    public bool TryRemoveWhere(Func<ContentEntryRecord, bool> predicate, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        ContentEntryRecord[] records = Records.ToArray();
        ContentEntryRecord[] matched = records.Where(predicate).ToArray();
        ContentPreflightResult assessment = AssessCompoundRemoval(matched);
        if (!assessment.CanCommit) { removedRecords = Array.Empty<ContentEntryRecord>(); failure = assessment.Failure; return false; }
        HashSet<ContentEntryId> removalIds = new HashSet<ContentEntryId>(matched.Select(record => record.Id));
        foreach (ContentEntryId id in removalIds.ToArray())
        {
            if (ChildRemovalPolicy == ContentCompoundChildRemovalPolicy.RemoveSubtree)
                foreach (ContentEntryId descendant in CollectSubtree(id)) removalIds.Add(descendant);
        }
        ContentEntryRecord[] removed = records.Where(record => removalIds.Contains(record.Id)).ToArray();
        ContentEntryId[] topmost = removalIds.Where(id => _nodes[id].ParentId is null || !removalIds.Contains(_nodes[id].ParentId!.Value)).ToArray();
        foreach (ContentEntryId id in topmost) RemoveSubtree(id);
        removedRecords = Array.AsReadOnly(removed);
        failure = null;
        if (removed.Length > 0) OnChanged(new ContentChangedEventArgs(removedRecords: removedRecords, kind: ContentChangeKind.Removed, requiresFullRefresh: true));
        return true;
    }

    /// <inheritdoc />
    public override bool TryAddChild(TId parentId, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        node = null;
        if (!TryNormalize(parentId, out ContentEntryId normalizedParentId, out failure)
            || !TryValidateExistingParent(normalizedParentId, out failure)
            || !IdSource.TryCreateNext(out TId id, out failure))
        {
            return false;
        }

        return TryAddAcceptedNode(id, entry, normalizedParentId, observeId: false, out node, out failure);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessAddChild(TId parentId, IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (!TryNormalize(parentId, out ContentEntryId normalizedParentId, out ContentFailure? failure)
            || !TryValidateExistingParent(normalizedParentId, out failure)
            || !IdSource.CanCreateNext(out failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public override ContentCompoundNode AddChild(TId parentId, IContentEntry entry)
    {
        if (TryAddChild(parentId, entry, out ContentCompoundNode? node, out ContentFailure? failure))
        {
            return node!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryAddChild(ContentEntryId parentId, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        EnsureValidId(parentId);
        node = null;
        if (!TryValidateExistingParent(parentId, out failure)
            || !IdSource.TryCreateNext(out TId id, out failure))
        {
            return false;
        }

        return TryAddAcceptedNode(id, entry, parentId, observeId: false, out node, out failure);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessAddChild(ContentEntryId parentId, IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        EnsureValidId(parentId);
        if (!TryValidateExistingParent(parentId, out ContentFailure? failure)
            || !IdSource.CanCreateNext(out failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public override ContentCompoundNode AddChild(ContentEntryId parentId, IContentEntry entry)
    {
        if (TryAddChild(parentId, entry, out ContentCompoundNode? node, out ContentFailure? failure))
        {
            return node!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryAddChild(TId parentId, TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        node = null;
        if (!TryNormalize(parentId, out ContentEntryId normalizedParentId, out failure)
            || !TryValidateExistingParent(normalizedParentId, out failure))
        {
            return false;
        }

        return TryAddAcceptedNode(id, entry, normalizedParentId, observeId: true, out node, out failure);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessAddChild(TId parentId, TId id, IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (!TryNormalize(parentId, out ContentEntryId normalizedParentId, out ContentFailure? failure)
            || !TryValidateExistingParent(normalizedParentId, out failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        return AssessAcceptedNode(id, normalizedParentId, observeId: true);
    }

    /// <inheritdoc />
    public override ContentCompoundNode AddChild(TId parentId, TId id, IContentEntry entry)
    {
        if (TryAddChild(parentId, id, entry, out ContentCompoundNode? node, out ContentFailure? failure))
        {
            return node!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryAddChild(ContentEntryId parentId, TId id, IContentEntry entry, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        EnsureValidId(parentId);
        node = null;
        if (!TryValidateExistingParent(parentId, out failure))
        {
            return false;
        }

        return TryAddAcceptedNode(id, entry, parentId, observeId: true, out node, out failure);
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessAddChild(ContentEntryId parentId, TId id, IContentEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        EnsureValidId(parentId);
        if (!TryValidateExistingParent(parentId, out ContentFailure? failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        return AssessAcceptedNode(id, parentId, observeId: true);
    }

    /// <inheritdoc />
    public override ContentCompoundNode AddChild(ContentEntryId parentId, TId id, IContentEntry entry)
    {
        if (TryAddChild(parentId, id, entry, out ContentCompoundNode? node, out ContentFailure? failure))
        {
            return node!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryGetNode(ContentEntryId id, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        EnsureValidId(id);
        if (_nodes.TryGetValue(id, out CompoundNodeState? state))
        {
            node = CreateNode(state);
            failure = null;
            return true;
        }

        node = null;
        failure = ContentFailures.EntryNotFound($"Compound node '{id}' was not found.", id.ToString());
        return false;
    }

    /// <inheritdoc />
    public override ContentCompoundNode GetNode(ContentEntryId id)
    {
        if (TryGetNode(id, out ContentCompoundNode? node, out ContentFailure? failure))
        {
            return node!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryGetNode(TId id, out ContentCompoundNode? node, out ContentFailure? failure)
    {
        if (!TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            node = null;
            return false;
        }

        return TryGetNode(normalizedId, out node, out failure);
    }

    /// <inheritdoc />
    public override ContentCompoundNode GetNode(TId id)
    {
        if (TryGetNode(id, out ContentCompoundNode? node, out ContentFailure? failure))
        {
            return node!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override IReadOnlyList<ContentCompoundNode> GetRoots()
    {
        return ApplySiblingOrder(_rootIds).Select(id => CreateNode(_nodes[id])).ToArray();
    }

    /// <inheritdoc />
    public override IReadOnlyList<ContentCompoundNode> GetChildren(TId parentId)
    {
        if (!TryNormalize(parentId, out ContentEntryId normalizedParentId, out ContentFailure? failure)
            || !TryGetNode(normalizedParentId, out ContentCompoundNode? parent, out failure))
        {
            throw new ContentOperationException(failure!);
        }

        return parent!.ChildIds.Select(id => CreateNode(_nodes[id])).ToArray();
    }

    /// <inheritdoc />
    public override IReadOnlyList<ContentCompoundNode> GetChildren(ContentEntryId parentId)
    {
        if (!TryGetNode(parentId, out ContentCompoundNode? parent, out ContentFailure? failure))
        {
            throw new ContentOperationException(failure!);
        }

        return parent!.ChildIds.Select(id => CreateNode(_nodes[id])).ToArray();
    }

    /// <inheritdoc />
    public override IReadOnlyList<ContentCompoundRecordView> GetRecordViews()
    {
        var views = new List<ContentCompoundRecordView>();
        foreach (ContentEntryId rootId in ApplySiblingOrder(_rootIds))
        {
            AddRecordViews(rootId, depth: 0, views);
        }

        return views;
    }

    /// <inheritdoc />
    public override bool TryGet(ContentEntryId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (TryGetNode(id, out ContentCompoundNode? node, out failure))
        {
            record = node!.Record;
            return true;
        }

        record = null;
        return false;
    }

    /// <inheritdoc />
    public override ContentEntryRecord Get(ContentEntryId id)
    {
        if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryGet(TId id, out ContentEntryRecord? record, out ContentFailure? failure)
    {
        if (!TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            record = null;
            return false;
        }

        return TryGet(normalizedId, out record, out failure);
    }

    /// <inheritdoc />
    public override ContentEntryRecord Get(TId id)
    {
        if (TryGet(id, out ContentEntryRecord? record, out ContentFailure? failure))
        {
            return record!;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryRemove(TId id, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        removedRecords = Array.Empty<ContentEntryRecord>();
        if (!TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            return false;
        }

        if (!_nodes.TryGetValue(normalizedId, out CompoundNodeState? state))
        {
            failure = ContentFailures.EntryNotFound($"Compound node '{normalizedId}' was not found.", normalizedId.ToString());
            return false;
        }

        if (state.ChildIds.Count > 0 && ChildRemovalPolicy == ContentCompoundChildRemovalPolicy.Reject)
        {
            failure = ContentFailures.Structure("Compound node has children and the child removal policy rejects subtree removal.");
            return false;
        }

        ContentEntryRecord[] removed = CollectSubtree(normalizedId).Select(removeId => _nodes[removeId].Record).ToArray();
        RemoveSubtree(normalizedId);
        removedRecords = removed;
        failure = null;
        OnChanged(new ContentChangedEventArgs(
            removedRecords: removedRecords,
            kind: ContentChangeKind.Removed,
            requiresFullRefresh: removedRecords.Count > 1));
        return true;
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessRemove(TId id)
    {
        if (!TryNormalize(id, out ContentEntryId normalizedId, out ContentFailure? failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        if (!_nodes.TryGetValue(normalizedId, out CompoundNodeState? state))
        {
            return ContentPreflightResult.Rejected(ContentFailures.EntryNotFound($"Compound node '{normalizedId}' was not found.", normalizedId.ToString()));
        }

        if (state.ChildIds.Count > 0 && ChildRemovalPolicy == ContentCompoundChildRemovalPolicy.Reject)
        {
            return ContentPreflightResult.Rejected(ContentFailures.Structure("Compound node has children and the child removal policy rejects subtree removal."));
        }

        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public override IReadOnlyList<ContentEntryRecord> Remove(TId id)
    {
        if (TryRemove(id, out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure))
        {
            return removedRecords;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public override bool TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure)
    {
        removedRecords = Records;
        failure = null;
        if (_nodes.Count == 0)
        {
            return true;
        }

        _nodes.Clear();
        _rootIds.Clear();
        _nodeIds.Clear();
        OnChanged(new ContentChangedEventArgs(
            removedRecords: removedRecords,
            kind: ContentChangeKind.Cleared,
            cleared: true,
            requiresFullRefresh: true));
        return true;
    }

    /// <inheritdoc />
    public override ContentPreflightResult AssessClear()
    {
        return ContentPreflightResult.Success();
    }

    /// <inheritdoc />
    public override IReadOnlyList<ContentEntryRecord> Clear()
    {
        if (TryClear(out IReadOnlyList<ContentEntryRecord> removedRecords, out ContentFailure? failure))
        {
            return removedRecords;
        }

        throw new ContentOperationException(failure!);
    }

    /// <inheritdoc />
    public bool TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure)
    {
        snapshot = null;
        ContentEntryRecord[] records = _nodeIds.Select(id => _nodes[id].Record).ToArray();

        if (!ContentSnapshotRecords.TryCapture(records, out List<ContentRecordSnapshot>? recordSnapshots, out failure)
            || !IdSource.TryCaptureSnapshot(out ContentSnapshotValue? sourceSnapshot, out failure))
        {
            return false;
        }

        snapshot = new ContentStructureSnapshot
        {
            Kind = SnapshotKind,
            DataVersion = SnapshotDataVersion,
            Records = recordSnapshots!,
            Data = ContentSnapshotValue.Object(new[]
            {
                ContentSnapshotProperties.Named("idSourceKind", ContentSnapshotCodecs.Encode(IdSource.SnapshotFactory.Kind)),
                ContentSnapshotProperties.Named("idSourceData", new ContentSnapshotEncodedValue { Data = sourceSnapshot! }),
                ContentSnapshotProperties.Named("childRemovalPolicy", ContentSnapshotCodecs.Encode(ChildRemovalPolicy.ToString())),
                ContentSnapshotProperties.Named("siblingReadOrder", ContentSnapshotCodecs.Encode(SiblingReadOrder.ToString())),
                ContentSnapshotProperties.Named("relationships", new ContentSnapshotEncodedValue
                {
                    Data = ContentSnapshotValue.List(_nodeIds.Select(CreateRelationshipSnapshot))
                })
            })
        };
        failure = null;
        return true;
    }

    /// <inheritdoc />
    public ContentStructureSnapshot CaptureSnapshot()
    {
        if (TryCaptureSnapshot(out ContentStructureSnapshot? snapshot, out ContentFailure? failure) && snapshot is not null)
        {
            return snapshot;
        }

        throw new ContentOperationException(failure ?? ContentFailures.Snapshot());
    }

    private bool TryAddAcceptedNode(
        TId id,
        IContentEntry entry,
        ContentEntryId? parentId,
        bool observeId,
        out ContentCompoundNode? node,
        out ContentFailure? failure)
    {
        node = null;
        if (!TryNormalize(id, out ContentEntryId normalizedId, out failure))
        {
            return false;
        }

        if (_nodes.ContainsKey(normalizedId))
        {
            failure = ContentFailures.EntryIdDuplicate($"Entry ID '{normalizedId}' already exists.", normalizedId.ToString());
            return false;
        }

        if (observeId && !IdSource.TryObserve(id, out failure))
        {
            return false;
        }

        var record = new ContentEntryRecord(normalizedId, entry);
        var state = new CompoundNodeState(record, parentId);
        _nodes.Add(normalizedId, state);
        _nodeIds.Add(normalizedId);
        if (parentId is null)
        {
            _rootIds.Add(normalizedId);
        }
        else
        {
            _nodes[parentId.Value].ChildIds.Add(normalizedId);
        }

        node = CreateNode(state);
        failure = null;
        OnChanged(new ContentChangedEventArgs(new[] { record }, kind: ContentChangeKind.Added));
        return true;
    }

    private bool TryCreateBatchNodes(
        IReadOnlyList<TId> ids,
        IReadOnlyList<IContentEntry> entries,
        ContentEntryId? parentId,
        out ContentCompoundNode[] nodes,
        out ContentFailure? failure)
    {
        if (ids.Count != entries.Count) throw new InvalidOperationException("The generated ID range returned a different number of IDs than requested.");
        nodes = new ContentCompoundNode[entries.Count];
        var seen = new HashSet<ContentEntryId>(_nodes.Keys);
        for (int i = 0; i < entries.Count; i++)
        {
            if (!TryNormalize(ids[i], out ContentEntryId normalized, out failure)) return false;
            if (!seen.Add(normalized))
            {
                failure = ContentFailures.EntryIdDuplicate($"Entry ID '{normalized}' is duplicated in the compound add range.", normalized.ToString());
                return false;
            }
            nodes[i] = new ContentCompoundNode(new ContentEntryRecord(normalized, entries[i]), parentId, Array.Empty<ContentEntryId>());
        }
        failure = null;
        return true;
    }

    private ContentPreflightResult AssessCompoundRemoval(IReadOnlyList<ContentEntryRecord> selected)
    {
        if (selected.Count == 0 || ChildRemovalPolicy == ContentCompoundChildRemovalPolicy.RemoveSubtree)
            return ContentPreflightResult.Success();
        var selectedIds = new HashSet<ContentEntryId>(selected.Select(record => record.Id));
        foreach (ContentEntryId id in selectedIds)
        {
            if (_nodes[id].ChildIds.Any(childId => !selectedIds.Contains(childId)))
                return ContentPreflightResult.Rejected(ContentFailures.Structure(
                    $"Compound node '{id}' has children that do not match the predicate, and the child removal policy rejects removing the node."));
        }
        return ContentPreflightResult.Success();
    }

    private static void ValidateBatchEntries(IReadOnlyList<IContentEntry> entries, string parameterName)
    {
        if (entries is null) throw new ArgumentNullException(parameterName);
        for (int i = 0; i < entries.Count; i++) if (entries[i] is null) throw new ArgumentException("Entry batches cannot contain null values.", parameterName);
    }

    private ContentPreflightResult AssessAcceptedNode(TId id, ContentEntryId? parentId, bool observeId)
    {
        if (!TryNormalize(id, out ContentEntryId normalizedId, out ContentFailure? failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        if (_nodes.ContainsKey(normalizedId))
        {
            return ContentPreflightResult.Rejected(ContentFailures.EntryIdDuplicate($"Entry ID '{normalizedId}' already exists.", normalizedId.ToString()));
        }

        if (parentId is not null && !TryValidateExistingParent(parentId.Value, out failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        if (observeId && !IdSource.CanObserve(id, out failure))
        {
            return ContentPreflightResult.Rejected(failure!);
        }

        return ContentPreflightResult.Success();
    }

    private bool TryNormalize(TId id, out ContentEntryId normalizedId, out ContentFailure? failure)
    {
        return IdSource.IdStrategy.TryNormalize(id, out normalizedId, out failure);
    }

    private bool TryValidateExistingParent(ContentEntryId parentId, out ContentFailure? failure)
    {
        if (_nodes.ContainsKey(parentId))
        {
            failure = null;
            return true;
        }

        failure = ContentFailures.EntryNotFound($"Parent compound node '{parentId}' was not found.", parentId.ToString());
        return false;
    }

    private IReadOnlyList<ContentEntryRecord> FlattenRecords()
    {
        var records = new List<ContentEntryRecord>();
        foreach (ContentEntryId rootId in ApplySiblingOrder(_rootIds))
        {
            AddFlattenedRecords(rootId, records);
        }

        return records;
    }

    private void AddFlattenedRecords(ContentEntryId id, List<ContentEntryRecord> records)
    {
        CompoundNodeState state = _nodes[id];
        records.Add(state.Record);
        foreach (ContentEntryId childId in ApplySiblingOrder(state.ChildIds))
        {
            AddFlattenedRecords(childId, records);
        }
    }

    private void AddRecordViews(ContentEntryId id, int depth, List<ContentCompoundRecordView> views)
    {
        CompoundNodeState state = _nodes[id];
        ContentEntryId[] childIds = ApplySiblingOrder(state.ChildIds).ToArray();
        views.Add(new ContentCompoundRecordView(state.Record, state.ParentId, childIds, depth));
        foreach (ContentEntryId childId in childIds)
        {
            AddRecordViews(childId, depth + 1, views);
        }
    }

    private IReadOnlyList<ContentEntryId> CollectSubtree(ContentEntryId id)
    {
        var ids = new List<ContentEntryId>();
        AddSubtreeIds(id, ids);
        return ids;
    }

    private void AddSubtreeIds(ContentEntryId id, List<ContentEntryId> ids)
    {
        ids.Add(id);
        foreach (ContentEntryId childId in ApplySiblingOrder(_nodes[id].ChildIds))
        {
            AddSubtreeIds(childId, ids);
        }
    }

    private void RemoveSubtree(ContentEntryId id)
    {
        CompoundNodeState state = _nodes[id];
        foreach (ContentEntryId childId in state.ChildIds.ToArray())
        {
            RemoveSubtree(childId);
        }

        if (state.ParentId is null)
        {
            _rootIds.Remove(id);
        }
        else if (_nodes.TryGetValue(state.ParentId.Value, out CompoundNodeState? parent))
        {
            parent.ChildIds.Remove(id);
        }

        _nodes.Remove(id);
        _nodeIds.Remove(id);
    }

    private IEnumerable<ContentEntryId> ApplySiblingOrder(IReadOnlyList<ContentEntryId> ids)
    {
        return SiblingReadOrder == ContentCompoundSiblingReadOrder.OldestFirst
            ? ids
            : ids.Reverse();
    }

    private ContentCompoundNode CreateNode(CompoundNodeState state)
    {
        return new ContentCompoundNode(state.Record, state.ParentId, ApplySiblingOrder(state.ChildIds));
    }

    private static void EnsureValidId(ContentEntryId id)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
        {
            throw new ArgumentException("Content entry ID cannot be empty.", nameof(id));
        }
    }

    private void OnChanged(ContentChangedEventArgs args)
    {
        Changed?.Invoke(this, args);
    }

    private void RestoreNodes(
        IEnumerable<ContentEntryRecord> records,
        IReadOnlyDictionary<ContentEntryId, ContentEntryId?> parentIds)
    {
        ContentEntryRecord[] recordArray = records.ToArray();
        foreach (ContentEntryRecord record in recordArray)
        {
            _nodes.Add(record.Id, new CompoundNodeState(record, parentIds[record.Id]));
            _nodeIds.Add(record.Id);
        }

        foreach (ContentEntryRecord record in recordArray)
        {
            ContentEntryId? parentId = parentIds[record.Id];
            if (parentId is null)
            {
                _rootIds.Add(record.Id);
            }
            else
            {
                _nodes[parentId.Value].ChildIds.Add(record.Id);
            }
        }
    }

    private ContentSnapshotEncodedValue CreateRelationshipSnapshot(ContentEntryId id)
    {
        ContentEntryId? parentId = _nodes[id].ParentId;
        return new ContentSnapshotEncodedValue
        {
            Data = ContentSnapshotValue.Object(new[]
            {
                ContentSnapshotProperties.Named("id", ContentSnapshotCodecs.Encode(id.Value)),
                ContentSnapshotProperties.Named("hasParent", ContentSnapshotCodecs.Encode(parentId is not null)),
                ContentSnapshotProperties.Named("parentId", ContentSnapshotCodecs.Encode(parentId?.Value ?? string.Empty))
            })
        };
    }

    private sealed class ContentCompoundStructureSnapshotFactory :
        ContentCompoundStructureSnapshotFactoryBase<TId, ContentCompoundStructure<TId>>
    {
        public ContentCompoundStructureSnapshotFactory(IContentGeneratedIdSourceFactory<TId> idSourceFactory)
            : base(SnapshotKind, SnapshotDataVersion, idSourceFactory)
        {
        }

        protected override bool TryRestoreValidatedCompoundSnapshot(
            ContentStructureSnapshot snapshot,
            ContentEntryRecord[] records,
            IReadOnlyDictionary<ContentEntryId, ContentEntryId?> parentIds,
            IContentGeneratedIdSource<TId> idSource,
            out ContentCompoundStructure<TId>? structure,
            out ContentFailure? failure)
        {
            structure = null;

            if (!TryRestoreData(snapshot.Data, out ContentCompoundChildRemovalPolicy childRemovalPolicy, out ContentCompoundSiblingReadOrder siblingReadOrder, out failure))
            {
                return false;
            }

            structure = new ContentCompoundStructure<TId>(idSource, childRemovalPolicy, siblingReadOrder, records, parentIds);
            failure = null;
            return true;
        }
    }

    internal static bool TryRestoreData(
        ContentSnapshotValue data,
        out ContentCompoundChildRemovalPolicy childRemovalPolicy,
        out ContentCompoundSiblingReadOrder siblingReadOrder,
        out ContentFailure? failure)
    {
        childRemovalPolicy = ContentCompoundChildRemovalPolicy.Reject;
        siblingReadOrder = ContentCompoundSiblingReadOrder.OldestFirst;

        if (data is null || data.Kind != ContentSnapshotValueKind.Object)
        {
            failure = ContentFailures.SnapshotMalformed("Compound structure snapshot data must be an object.");
            return false;
        }

        if (!ContentSnapshotProperties.TryDecodeRequiredString(data, "childRemovalPolicy", out string childRemovalPolicyText, out failure)
            || !ContentSnapshotProperties.TryDecodeRequiredString(data, "siblingReadOrder", out string siblingReadOrderText, out failure))
        {
            return false;
        }

        if (!Enum.TryParse(childRemovalPolicyText, out childRemovalPolicy)
            || !Enum.IsDefined(typeof(ContentCompoundChildRemovalPolicy), childRemovalPolicy))
        {
            failure = ContentFailures.SnapshotMalformed($"Compound structure snapshot child removal policy '{childRemovalPolicyText}' is not supported.");
            return false;
        }

        if (!Enum.TryParse(siblingReadOrderText, out siblingReadOrder)
            || !Enum.IsDefined(typeof(ContentCompoundSiblingReadOrder), siblingReadOrder))
        {
            failure = ContentFailures.SnapshotMalformed($"Compound structure snapshot sibling read order '{siblingReadOrderText}' is not supported.");
            return false;
        }

        failure = null;
        return true;
    }

    private sealed class CompoundNodeState
    {
        public CompoundNodeState(ContentEntryRecord record, ContentEntryId? parentId)
        {
            Record = record;
            ParentId = parentId;
        }

        public ContentEntryRecord Record { get; }

        public ContentEntryId? ParentId { get; }

        public List<ContentEntryId> ChildIds { get; } = new List<ContentEntryId>();
    }
}

/// <summary>
/// Stores content records as a long-ID compound tree.
/// </summary>
public sealed class ContentCompoundStructure : ContentCompoundStructure<long>
{
    /// <summary>
    /// Gets the snapshot factory for the built-in long-ID compound structure.
    /// </summary>
    public static IContentStructureSnapshotFactory Factory { get; } = new LongContentCompoundStructureSnapshotFactory();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCompoundStructure"/> class.
    /// </summary>
    public ContentCompoundStructure(
        ContentCompoundChildRemovalPolicy childRemovalPolicy = ContentCompoundChildRemovalPolicy.Reject,
        ContentCompoundSiblingReadOrder siblingReadOrder = ContentCompoundSiblingReadOrder.OldestFirst)
        : base(new LongContentGeneratedIdSource(), childRemovalPolicy, siblingReadOrder)
    {
    }

    private ContentCompoundStructure(
        IContentGeneratedIdSource<long> idSource,
        ContentCompoundChildRemovalPolicy childRemovalPolicy,
        ContentCompoundSiblingReadOrder siblingReadOrder,
        IEnumerable<ContentEntryRecord> records,
        IReadOnlyDictionary<ContentEntryId, ContentEntryId?> parentIds)
        : base(idSource, childRemovalPolicy, siblingReadOrder, records, parentIds)
    {
    }

    /// <inheritdoc />
    public override ContentManagerBase CreateManager()
    {
        return new ContentCompoundManager(this);
    }

    /// <inheritdoc />
    public override IContentStructureSnapshotFactory SnapshotFactory => Factory;

    private sealed class LongContentCompoundStructureSnapshotFactory :
        ContentCompoundStructureSnapshotFactoryBase<long, ContentCompoundStructure>
    {
        public LongContentCompoundStructureSnapshotFactory()
            : base(SnapshotKind, SnapshotDataVersion, LongContentGeneratedIdSource.Factory)
        {
        }

        protected override bool TryRestoreValidatedCompoundSnapshot(
            ContentStructureSnapshot snapshot,
            ContentEntryRecord[] records,
            IReadOnlyDictionary<ContentEntryId, ContentEntryId?> parentIds,
            IContentGeneratedIdSource<long> idSource,
            out ContentCompoundStructure? structure,
            out ContentFailure? failure)
        {
            structure = null;

            if (!TryRestoreData(snapshot.Data, out ContentCompoundChildRemovalPolicy childRemovalPolicy, out ContentCompoundSiblingReadOrder siblingReadOrder, out failure))
            {
                return false;
            }

            structure = new ContentCompoundStructure(idSource, childRemovalPolicy, siblingReadOrder, records, parentIds);
            failure = null;
            return true;
        }
    }
}
