# DESIGN REFERENCE

This file contains examples and reusable patterns for C# package design.

Use this as a reference when concrete examples are useful. The shorter `DESIGN_GUIDELINES.md` should remain the main design checklist.

## Public API Shape

A package should usually have one obvious entry point.

```csharp
public sealed class PackageRoot
{
    public PackageRoot()
    {
        Registry = new PackageRegistry();
        History = new PackageHistory();
    }

    public PackageRegistry Registry { get; }
    public PackageHistory History { get; }
}
```

Prefer names that describe the package's domain instead of generic names like `Processor`, `Handler`, or `Service`.

```csharp
// Prefer
public sealed class CommandRegistry
{
}

// Avoid unless the domain truly needs it
public sealed class CommandProcessor
{
}
```

## Ownership And Facades

If one object owns another, expose that relationship directly.

```csharp
public sealed class ConsoleSystem
{
    public ConsoleSystem()
    {
        Commands = new CommandRegistry();
        Output = new ConsoleOutput();
    }

    public CommandRegistry Commands { get; }
    public ConsoleOutput Output { get; }
}
```

If a type is a facade over owned state, make that clear in XML documentation.

```csharp
/// <summary>
/// Facade for writing messages to the console output pipeline.
/// </summary>
/// <remarks>
/// This type does not own separate output storage. Messages are forwarded to the parent output sink.
/// </remarks>
public sealed class ConsoleLog
{
}
```

## Try Method And Throwing Wrapper

Use `Try...` methods for expected failure paths, and provide throwing wrappers when success is normally expected.

```csharp
public bool TryRegister(string id, ICommand command, out string? error)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        error = "Command id cannot be empty.";
        return false;
    }

    if (command == null)
    {
        error = "Command cannot be null.";
        return false;
    }

    if (_commands.ContainsKey(id))
    {
        error = $"A command with id '{id}' is already registered.";
        return false;
    }

    _commands.Add(id, command);
    error = null;
    return true;
}

public void Register(string id, ICommand command)
{
    if (!TryRegister(id, command, out var error))
        throw new InvalidOperationException(error);
}
```

## Validate Before Mutation

Rejected operations should leave state unchanged.

```csharp
public bool TryRename(string oldId, string newId, out string? error)
{
    if (!_items.ContainsKey(oldId))
    {
        error = $"No item with id '{oldId}' exists.";
        return false;
    }

    if (string.IsNullOrWhiteSpace(newId))
    {
        error = "New id cannot be empty.";
        return false;
    }

    if (_items.ContainsKey(newId))
    {
        error = $"An item with id '{newId}' already exists.";
        return false;
    }

    var item = _items[oldId];

    _items.Remove(oldId);
    _items.Add(newId, item);

    error = null;
    return true;
}
```

For compound operations, validate the complete proposed result before mutating anything.

## Read-Only Collection Exposure

Do not expose mutable collections directly.

```csharp
private readonly List<CommandInfo> _commands = new List<CommandInfo>();

public IReadOnlyList<CommandInfo> Commands => _commands;
```

If callers need lookup behavior, expose a read-only dictionary.

```csharp
private readonly Dictionary<string, CommandInfo> _commandsById = new Dictionary<string, CommandInfo>();

public IReadOnlyDictionary<string, CommandInfo> CommandsById => _commandsById;
```

## Immutable Public Data

Prefer immutable public data objects.

```csharp
public sealed class CommandInfo
{
    public CommandInfo(string id, string description)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    public string Id { get; }
    public string Description { get; }
}
```

For larger DTOs, constructor validation should preserve invariants.

## Extension Point Shape

Use small strategy interfaces when behavior genuinely varies.

```csharp
public interface ICommandFormatter
{
    string Format(CommandInfo command);
}
```

Avoid broad interfaces that force users to implement unrelated behavior.

```csharp
// Too broad for an early package
public interface ICommandSystemPlugin
{
    void RegisterCommands(CommandRegistry registry);
    void FormatOutput(ConsoleOutput output);
    void HandleErrors(Exception exception);
    void ConfigureStorage(object storage);
}
```

## Registry Pattern

Use registries when keyed setup-time lookup is part of the package contract.

```csharp
public sealed class CommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();

    public IReadOnlyDictionary<string, ICommand> Commands => _commands;

    public bool TryRegister(string id, ICommand command, out string? error)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "Command id cannot be empty.";
            return false;
        }

        if (command == null)
        {
            error = "Command cannot be null.";
            return false;
        }

        if (_commands.ContainsKey(id))
        {
            error = $"Command id '{id}' is already registered.";
            return false;
        }

        _commands.Add(id, command);
        error = null;
        return true;
    }
}
```

## Builder Pattern

Use builders when multiple changes must be staged before validation or commit.

```csharp
public sealed class SaveSystemBuilder
{
    private readonly List<ISaveSerializer> _serializers = new List<ISaveSerializer>();

    public SaveSystemBuilder AddSerializer(ISaveSerializer serializer)
    {
        if (serializer == null)
            throw new ArgumentNullException(nameof(serializer));

        _serializers.Add(serializer);
        return this;
    }

    public SaveSystem Build()
    {
        if (_serializers.Count == 0)
            throw new InvalidOperationException("At least one serializer must be registered.");

        return new SaveSystem(_serializers);
    }
}
```

The builder stages configuration. The built object owns runtime behavior.

## Event Payloads

Events should describe committed changes.

```csharp
public sealed class CommandRegisteredEventArgs : EventArgs
{
    public CommandRegisteredEventArgs(string commandId)
    {
        CommandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
    }

    public string CommandId { get; }
}
```

```csharp
public event EventHandler<CommandRegisteredEventArgs>? CommandRegistered;

private void OnCommandRegistered(string commandId)
{
    CommandRegistered?.Invoke(this, new CommandRegisteredEventArgs(commandId));
}
```

Do not fire events for rejected operations unless the event explicitly represents rejection.

## Folder Structure Example

Keep the initial structure shallow.

```text
src/
├── Core/
│   ├── PackageRoot.cs
│   └── PackageManager.cs
├── Commands/
│   ├── CommandRegistry.cs
│   └── ICommand.cs
├── Output/
│   └── ConsoleOutput.cs
└── Errors/
    └── PackageError.cs
```

Avoid creating deep layers before there is real complexity.

```text
src/
├── Application/
├── Domain/
├── Infrastructure/
├── Services/
├── Managers/
├── Providers/
└── Utilities/
```

That structure might become useful later, but it should not be the default skeleton.

## XML Documentation Example

Document responsibility and important ownership rules.

```csharp
/// <summary>
/// Registry for commands that can be executed by the console system.
/// </summary>
/// <remarks>
/// Command ids are stable public identifiers. Display names should not be used as command ids.
/// </remarks>
public sealed class CommandRegistry
{
}
```

Avoid documenting implementation trivia.

```csharp
/// <summary>
/// Stores commands in a dictionary.
/// </summary>
public sealed class CommandRegistry
{
}
```

## Skeleton Test Examples

Start with construction and invariant tests.

```csharp
[Test]
public void Root_CanBeConstructed()
{
    var root = new PackageRoot();

    Assert.That(root, Is.Not.Null);
}
```

```csharp
[Test]
public void Registry_StartsEmpty()
{
    var registry = new CommandRegistry();

    Assert.That(registry.Commands, Is.Empty);
}
```

Test rejected operations when behavior exists.

```csharp
[Test]
public void TryRegister_ReturnsFalse_WhenIdIsEmpty()
{
    var registry = new CommandRegistry();

    var result = registry.TryRegister("", new TestCommand(), out var error);

    Assert.That(result, Is.False);
    Assert.That(error, Is.Not.Null);
    Assert.That(registry.Commands, Is.Empty);
}
```

## Minimal Package Skeleton Example

A minimal early package should show intended shape without pretending unfinished systems are implemented.

```csharp
public sealed class PackageRoot
{
    public PackageRoot()
    {
        Registry = new PackageRegistry();
    }

    public PackageRegistry Registry { get; }
}
```

```csharp
public sealed class PackageRegistry
{
    private readonly List<string> _ids = new List<string>();

    public IReadOnlyList<string> Ids => _ids;

    public bool TryAdd(string id, out string? error)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "Id cannot be empty.";
            return false;
        }

        if (_ids.Contains(id))
        {
            error = $"Id '{id}' already exists.";
            return false;
        }

        _ids.Add(id);
        error = null;
        return true;
    }
}
```

This is enough to test ownership, mutation, validation, and public API style.

## Review Questions For Examples

When using an example from this file, ask:

- Does this match the current package's domain?
- Is the public owner of the behavior obvious?
- Is the example too general for the current need?
- Does this introduce an abstraction before the package needs it?
- Does the example preserve validation-before-mutation?
- Are names specific enough for the package being built?
