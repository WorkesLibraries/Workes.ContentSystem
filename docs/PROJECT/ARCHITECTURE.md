# ARCHITECTURE

## Purpose

Describe how the project is intended to be structured internally and how the main systems relate to each other.

This is a project-control document for maintaining architectural consistency. It is not intended to replace focused user documentation under `docs/`.

## Current Architecture

Workes.ContentSystem is a new package foundation. The public implementation is not designed in code yet; this document records the intended architecture to guide the first implementation stages.

The package should be engine-neutral and centered on a root object, likely `ContentManager`, that owns one active content structure and exposes a simple workflow for normal users.

## Main Concepts

- `IContentEntry` is the core content payload abstraction.
- `ContentEntryRecord` pairs a stored entry with the active structure's ID.
- `IContentStructure` is the storage and organization abstraction.
- The first structure should be a bounded chronological FIFO structure.
- A simple keyed structure should exercise configurable ID strategy after the FIFO foundation.
- A shared failure model should represent expected content-system rejection.
- Optional attachments should support export, persistence, bridges, and platform adapters without making those features mandatory.

## Intended Data Flow

The normal in-memory flow should be:

```text
host application
-> ContentManager
-> IContentStructure
-> retained ContentEntryRecord values
-> host UI, exporter, bridge, or adapter
```

The manager should be the convenient root. The structure should own ordering, retention, lookup, ID assignment or validation, mutability rules, and supported capabilities.

## Structures

The structure abstraction should be close in spirit to the InventorySystem structure model: core behavior belongs behind an abstraction so new storage models can be introduced without changing the manager into a one-purpose container.

The first implementation should stay small and useful:

- bounded capacity;
- append entries;
- drop oldest on overflow;
- read retained entries in chronological order;
- assign structure-owned IDs.

Future structures may be grouped, threaded, indexed, persistent, channel-based, or grid-like.

## Entries

Entries should be extensible content payloads. Core may provide simple entry types, but host applications should be able to define entries for their own domains.

ContentSystem should not include a built-in user/role model. If a host needs users, authors, permissions, channels, moderation data, or ownership, it can represent those through custom entries, custom structures, or higher-level packages.

## Failure Model

The package should mirror the error style used in Workes.InventorySystem and Workes.ConsoleSystem:

- expected operation rejection is structured failure data;
- try APIs return failure values;
- expected-success APIs throw package-owned exceptions carrying the same failure;
- programmer misuse uses standard .NET exceptions.

## Attachments

Attachments are planned optional capabilities around the core model.

Examples include file export, append-only logging, host logging bridges, persistence, Unity adapters, Godot adapters, and .NET logging adapters.

Core should make these possible without requiring every user or every structure to configure them.

## Relationship To ConsoleSystem

Workes.ConsoleSystem is expected to be replaced or rebuilt later on top of Workes.ContentSystem.

Console history, logs, command input, command output, command failures, semantic text, and custom console entries map naturally to the content-entry model. ContentSystem should therefore focus on the reusable content foundation and leave console-specific commands, parsing, permissions, autocomplete, and help generation to ConsoleSystem.
