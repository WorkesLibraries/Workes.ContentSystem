# Content Identity

Content identity describes how stored entries are referenced after a structure accepts them.

## Stored IDs

Every stored entry has a `ContentEntryId`.

`ContentEntryId` is a small value type around a non-empty string. It gives ContentSystem one structure-agnostic ID representation without forcing every structure to use the same external ID shape.

```csharp
ContentEntryRecord record = content.Add(entry);

ContentEntryId id = record.Id;
```

`IContentEntry` does not expose an ID. Entries describe content. Structures assign or accept IDs when they create `ContentEntryRecord` values.

## ID Ownership

ContentSystem currently supports two ID ownership models, plus generated ID sources for structures that can create IDs automatically.

Structure-assigned IDs are used when the structure decides the stored ID. `ContentSequenceStructure`, `ContentSingleStructure`, `ContentStackStructure`, and `ContentCompoundStructure` are normal long-ID paths:

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

ContentEntryRecord record = content.Add(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
```

Caller-provided IDs are used when the caller decides the ID and the structure validates it. Map structures are the direct ID-addressed built-in workflow:

```csharp
var content = ContentManagers.ForStructure<ContentMapManager<string>>(
    new ContentMapStructure<string>());

ContentEntryRecord record = content.Add(
    "entry-1",
    new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
```

The stored record still exposes a `ContentEntryId` in both cases.

## ID Strategies

`IContentEntryIdStrategy<TId>` validates and normalizes caller-provided IDs. It also validates normalized stored IDs restored from snapshots.

Built-in strategies are:

- `StringContentEntryIdStrategy`, which accepts non-empty strings;
- `IntegerContentEntryIdStrategy`, which accepts positive `long` values and normalizes them to invariant decimal strings;
- `GuidContentEntryIdStrategy`, which accepts non-empty `Guid` values and normalizes them to canonical lowercase D-format strings;
- `ContentEntryIdContentEntryIdStrategy`, which accepts already-normalized `ContentEntryId` values as an identity/fallback path.

Built-in strategies are resolved for supported ID types:

```csharp
var stringMap = ContentManagers.ForStructure<ContentMapManager<string>>(
    new ContentMapStructure<string>());
var numberMap = ContentManagers.ForStructure<ContentMapManager<long>>(
    new ContentMapStructure<long>());
var guidMap = ContentManagers.ForStructure<ContentMapManager<Guid>>(
    new ContentMapStructure<Guid>());
var storedIdMap = ContentManagers.ForStructure<ContentMapManager<ContentEntryId>>(
    new ContentMapStructure<ContentEntryId>());
```

Custom ID types need an explicit strategy or map structure:

```csharp
var structure = new ContentMapStructure<MyEntryId>(new MyEntryIdStrategy());
var content = ContentManagers.ForStructure<ContentMapManager<MyEntryId>>(structure);
```

ID strategies validate caller-provided IDs through `TryNormalize(...)`. They validate restored stored IDs through `TryValidateNormalized(...)`. Both methods must describe the same stored ID language so snapshots cannot restore IDs that the typed map API can never address.

ID strategies do not generate IDs.

## Generated ID Sources

`IContentGeneratedIdSource<TId>` is separate from ID strategies. It owns automatic ID generation and any state needed to keep future IDs coherent.

Generated ID sources:

- expose the validation strategy for their ID type;
- create the next ID for id-less add workflows;
- observe manual or restored IDs so future generated IDs do not collide;
- assess whether generation or observation can succeed without changing source state;
- capture and restore source-owned snapshot state.

Built-in generated ID sources are:

- `LongContentGeneratedIdSource`, which starts at `1`, generates positive `long` IDs, and advances past observed manual or restored IDs;
- `GuidContentGeneratedIdSource`, which generates non-empty `Guid` IDs and has no ordering state to advance.

Normal sequence usage does not require seeing the source:

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));

ContentEntryRecord generated = content.Add(entry);      // ID "1"
ContentEntryRecord manual = content.Add(1000, entry);   // ID "1000"
ContentEntryRecord next = content.Add(entry);           // ID "1001"
```

Custom sequence ID models use generic sequence types and a custom source:

```csharp
var structure = new ContentSequenceStructure<MyEntryId>(
    new MyGeneratedEntryIdSource(),
    ContentOverflowPolicy.None);

var content = ContentManagers.ForStructure<ContentSequenceManager<MyEntryId>>(structure);
```

Single, stack, and compound structures follow the same pattern with `ContentSingleStructure<TId>` / `ContentSingleManager<TId>`, `ContentStackStructure<TId>` / `ContentStackManager<TId>`, and `ContentCompoundStructure<TId>` / `ContentCompoundManager<TId>`.

GUID-generated structures can use the built-in GUID source:

```csharp
var structure = new ContentSequenceStructure<Guid>(
    new GuidContentGeneratedIdSource(),
    ContentOverflowPolicy.None);

var content = ContentManagers.ForStructure<ContentSequenceManager<Guid>>(structure);
```

Mixing manual and generated IDs is allowed, but source state decides how future generated IDs advance. Prefer one approach consistently unless you intentionally want the source to observe manual IDs.

Use `CanCreateNext(...)`, `CanObserve(...)`, and `CanObserveNormalized(...)` for advisory preflight. These methods must not advance the source or record an observation. The corresponding `TryCreateNext(...)`, `TryObserve(...)`, and `TryObserveNormalized(...)` methods commit the source-state change when accepted.

Generated ID sources also participate in sequence snapshots. Sequence restore validates every restored stored ID through the source strategy and observes those IDs before future generated IDs are created. Custom sequence extensions can use `ContentSequenceStructureSnapshotFactoryBase<TId, TStructure>` to get the same restore behavior as built-in generic sequence structures.

## Lookup

Structure-specific and typed-manager code should use natural lookup methods:

```csharp
ContentEntryRecord sequenceRecord = sequence.Get(1);
ContentEntryRecord mapRecord = map.Get("entry-1");
```

For structure-assigned IDs, the structure resolves to a manager with the natural ID type:

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.None));

ContentEntryRecord record = content.Get(1);
```

Structure-agnostic code can use `ContentEntryId`:

```csharp
ContentEntryRecord record = content.Get(storedId);
```

Missing IDs use `ContentFailureCodes.EntryNotFound`. Invalid or duplicate map, sequence, single, stack, or compound IDs use `EntryIdInvalid` or `EntryIdDuplicate` where the operation accepts explicit IDs.
