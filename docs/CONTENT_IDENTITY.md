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

ContentSystem currently supports two ID ownership models.

Structure-assigned IDs are used when the structure decides the stored ID:

```csharp
var content = ContentManagers.ForStructure<ContentSequenceManager>(
    new ContentSequenceStructure(ContentOverflowPolicy.DropOldest(capacity: 200)));

ContentEntryRecord record = content.Add(
    new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
```

Caller-provided IDs are used when the caller decides the ID and the structure validates it:

```csharp
var content = ContentManagers.ForStructure<KeyedContentManager<string>>(
    new KeyedContentStructure<string>());

ContentEntryRecord record = content.Add(
    "entry-1",
    new PlainContentEntry(DateTimeOffset.UtcNow, "Ready."));
```

The stored record still exposes a `ContentEntryId` in both cases.

## ID Strategies

`IContentEntryIdStrategy<TId>` validates and normalizes caller-provided IDs for keyed structures. It also validates normalized stored IDs restored from snapshots.

Built-in strategies are:

- `StringContentEntryIdStrategy`, which accepts non-empty strings;
- `IntegerContentEntryIdStrategy`, which accepts positive `long` values and normalizes them to invariant decimal strings.

The 1.0 direction is to add `Guid` and `ContentEntryId` identity/fallback support while keeping the built-in strategy set narrow.

Built-in strategies are resolved for supported ID types:

```csharp
var stringKeyed = ContentManagers.ForStructure<KeyedContentManager<string>>(
    new KeyedContentStructure<string>());
var numberKeyed = ContentManagers.ForStructure<KeyedContentManager<long>>(
    new KeyedContentStructure<long>());
```

Custom ID types need an explicit strategy or keyed structure:

```csharp
var structure = new KeyedContentStructure<MyEntryId>(new MyEntryIdStrategy());
var content = ContentManagers.ForStructure<KeyedContentManager<MyEntryId>>(structure);
```

ID strategies validate caller-provided IDs through `TryNormalize(...)`. They validate restored stored IDs through `TryValidateNormalized(...)`. Both methods must describe the same stored ID language so snapshots cannot restore IDs that the typed keyed API can never address.

ID strategies do not generate IDs.

## Lookup

Structure-specific and typed-manager code should use natural lookup methods:

```csharp
ContentEntryRecord sequenceRecord = sequence.Get(1);
ContentEntryRecord keyedRecord = keyed.Get("entry-1");
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

Missing IDs use `ContentFailureCodes.EntryNotFound`. Invalid or duplicate keyed IDs use `EntryIdInvalid` or `EntryIdDuplicate`.
