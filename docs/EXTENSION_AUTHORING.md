# Extension Authoring

This guide documents the extension contracts that are stable enough to use today. It focuses on custom structures; later stages will expand it for sorting, batch helpers, export, and other extension systems as they are implemented.

## Custom Structure Responsibilities

A custom structure starts with `IContentStructure`.

It must expose retained `ContentEntryRecord` values in its chosen read order and implement lookup by stored `ContentEntryId`. The structure owns what IDs mean: generated numeric IDs, caller-provided string IDs, normalized custom IDs, or another stable format.

Roadmap note: Pre-17.2 will make manager workflow declaration part of `IContentStructure`. Custom structures will declare a stable `ContentStructureWorkflow`, and custom manager workflows will register a factory so `ContentManagers.ForStructure(...)` can create the right manager. This is planned behavior, not implemented in the current package.

Add only the focused contracts that the structure truly supports:

- `IStructureAssignedIdContentStructure` or `IStructureAssignedIdContentStructure<TId>` for structure-assigned add workflows;
- `IKeyedContentStructure<TId>` for caller-provided typed IDs;
- `IContentChangeSource` for synchronous committed-change events;
- `IContentClearableStructure`, `IContentRecordRemovalStructure`, and keyed/natural removal contracts for mutation support;
- `IParameterizedContentStructure` for manager-owned runtime configuration;
- `IContentRetentionPolicyStructure` and `IContentReadOrderStructure` for readable configuration;
- `IContentStructureSnapshotRoundTrippable` for whole-structure snapshot round trips.

Do not use a broad capability flag object. In ContentSystem, implementing the focused interface is the capability.

Workflow descriptors are the planned exception because they identify manager resolution, not supported operations. A custom stack-like structure, for example, may eventually declare a stack workflow so the resolver can create a stack manager, while still using focused contracts for snapshots, mutation, events, and other behavior.

## Snapshot Support

Custom structures opt into snapshot round trips with `IContentStructureSnapshotRoundTrippable`. The structure captures itself and exposes a `SnapshotFactory` so normal manager restore can use `RestoreSnapshot(snapshot)` without a factory argument.

Use stable, namespaced snapshot kinds. Package kinds use `workes.content.structure.*`; application kinds should use an application-owned prefix such as `my.game.content.structure.quest_log`.

Use `DataVersion` to version the structure-owned snapshot schema. Increment it when the meaning of `Data` changes incompatibly.

The helper APIs reduce boilerplate:

- `ContentStructureSnapshotFactoryBase<TStructure>` validates kind/version/data, wraps unexpected exceptions into snapshot failures, and implements throwing restore.
- `ContentSnapshotRecords` captures and restores retained records through registered entry factories, preserves order, rejects duplicate IDs, and can validate positive numeric generated IDs.
- `ContentSnapshotProperties` builds and decodes object-shaped structure data.

Example shape:

```csharp
public sealed class QuestLogSnapshotFactory
    : ContentStructureSnapshotFactoryBase<QuestLogStructure>
{
    public QuestLogSnapshotFactory()
        : base(QuestLogStructure.SnapshotKind, QuestLogStructure.SnapshotDataVersion)
    {
    }

    protected override bool TryRestoreValidatedSnapshot(
        ContentStructureSnapshot snapshot,
        out QuestLogStructure? structure,
        out ContentFailure? failure)
    {
        structure = null;

        if (!ContentSnapshotRecords.TryRestore(snapshot.Records, out ContentEntryRecord[] records, out failure) ||
            !ContentSnapshotProperties.TryDecodeRequiredString(snapshot.Data, "questGroup", out string questGroup, out failure))
        {
            return false;
        }

        structure = new QuestLogStructure(questGroup, records);
        return true;
    }
}
```

The structure should expose the same factory from `SnapshotFactory`:

```csharp
public IContentStructureSnapshotFactory SnapshotFactory => Factory;
```

Keep explicit factory restore available for migrations where the active structure's factory is not the intended target.

## Entry Factories

Structure snapshots contain entry snapshots. Restore cannot recreate custom entries unless their factory is registered.

Register custom entry factories once during application setup:

```csharp
ContentEntrySnapshotFactories.Register(QuestEntry.Factory);
```

Capture does not require registration because the entry instance captures itself. Restore does require registration because the snapshot only contains the entry kind.

## ID Invariants

Stored record IDs must remain reachable through the structure's normal lookup workflow.

For keyed structures, `IContentEntryIdStrategy<TId>` has two responsibilities:

- `TryNormalize` validates caller-facing IDs and converts them to `ContentEntryId`;
- `TryValidateNormalized` validates IDs loaded from snapshots.

Both methods must describe the same stored ID language. A keyed structure should reject a snapshot ID that the typed API can never address.

For generated-ID structures, restore should validate retained IDs and generated counters before constructing the restored structure. `ContentSnapshotRecords.TryGetMaximumPositiveNumericId(...)` is useful for sequence-like structures.

## Events And Atomicity

Snapshot restore should create and validate a replacement structure before changing manager state. Failed restore must leave the existing manager state unchanged and emit no event.

Successful manager-owned restore emits one full-refresh event with `ContentChangeKind.SnapshotRestored`. Custom structures that implement `IContentChangeSource` should raise events only after mutations commit. Handler exceptions are synchronous and are not swallowed.

## Pitfalls

- Do not reuse snapshot kinds for incompatible schemas.
- Do not reset generated IDs during clear, remove, or restore unless that is explicitly part of the structure's documented model.
- Do not silently skip unsupported custom entries; let snapshot capture/restore return structured failures.
- Do not expose mutation APIs on a manager unless the underlying structure contract supports them.
- Do not emit events for rejected or no-op operations.
- Keep snapshot DTOs serializer-friendly and avoid storing live service objects, delegates, or host-specific resources in `ContentSnapshotValue`.
