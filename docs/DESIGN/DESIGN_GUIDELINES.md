# C# Package Design Guidelines

Use this document as a reusable design reference for C# package work. It is intentionally package-agnostic: apply the guidelines to the package being built, and use examples from existing packages only as style references, not as domain templates.

These guidelines are defaults, not laws. Project-specific decisions in `docs/PROJECT/DECISIONS.md` override this document when there is a deliberate conflict.

## 1. Public API Design

- Make the normal usage path obvious from the public surface. A new user should be able to identify the main coordinator type, construct it, and perform the first useful operation without understanding internals.
- Prefer a small number of meaningful public entry points over many helper classes.
- Put common operations on the object that naturally owns the state being changed.
- Keep public types small, named by responsibility, and easy to scan.
- Avoid public abstractions that only exist for hypothetical future use.
- Use explicit method names. Prefer `TryAdd`, `Commit`, `Create`, `Resolve`, `Serialize`, or `Register` over vague names such as `Process`, `Handle`, or `Do`.
- Prefer immutable public data. Use constructor-set `get`-only properties where practical.
- Expose read-only views for owned collections: `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>`, `IReadOnlyCollection<T>`.
- Do not expose mutable internal collections directly.
- Avoid global static state unless the package's purpose explicitly requires it.
- Do not enforce singletons in a reusable package. Document intended normal usage instead.

## 2. Namespace And File Structure

- Use one root namespace matching the package name.
- Group files by responsibility, not by arbitrary technical layers.
- Put foundational package objects in a `Core/` folder rather than directly in `src/`.
- Keep the initial folder structure shallow until real complexity justifies more layers.
- Keep public contracts near their primary implementations when that improves discoverability.
- Avoid creating separate infrastructure projects before there is a concrete need.
- Internal helpers should stay internal and should not drive the public organization.

## 3. Construction And Ownership

- Constructors should establish valid objects.
- Required dependencies should be constructor parameters.
- Optional dependencies should have clear defaults, but defaults should not hide important ownership rules.
- If one object owns another, expose that relationship directly through read-only properties.
- Do not make unrelated systems look independent if one is conceptually a facade over the other.
- Clone mutable default state when creating runtime instances that should not share mutations.
- If an object is only a facade, document what state it forwards to or coordinates.

## 4. Mutability And State Changes

- Validate before mutating state.
- Failed operations should leave observable state unchanged.
- Rejected operations should not emit events, write output, or partially update related objects.
- Prefer one clear mutation path over several paths that can bypass validation.
- Keep mutation methods close to the state they mutate.
- Use private/internal helpers for implementation steps, but keep public mutation semantics simple.
- If an operation is compound, validate the proposed final state before committing.
- If an operation has a visible side effect, document when that side effect occurs.

## 5. Error Handling Pattern

- Use `Try...` methods for expected failure paths.
- Return `false` from `Try...` methods and provide a clear `out string? error` or typed error result when useful.
- Provide throwing convenience wrappers when callers normally expect success.
- Throw argument exceptions for invalid caller input:
  - `ArgumentNullException`
  - `ArgumentOutOfRangeException`
  - `ArgumentException`
- Throw `InvalidOperationException` when the object state or domain rules reject an expected-success operation.
- Error messages should be specific enough for tests, logs, and UI display.
- Do not use exceptions for ordinary conditional checks when a `Try...` shape is part of the API style.

## 6. Extension Points

- Add extension points only around behavior that is genuinely expected to vary.
- Prefer small strategy interfaces over large inheritance hierarchies.
- Keep normal application APIs separate from extension contracts.
- Document extension interfaces clearly as extension contracts.
- Avoid requiring users to implement many interfaces to customize one behavior.
- Do not expose low-level hooks as the normal usage path.
- If a strategy has mutable configuration, prefer creating a validated replacement over mutating it in place.
- Keep extension contracts stable and narrow; they become hard to change once public.

Good extension point candidates:

- validation policies,
- formatting strategies,
- storage adapters,
- command handlers,
- sort/comparison behavior,
- output sinks when sinks are already justified.

Poor early extension point candidates:

- speculative middleware,
- abstract factories with one implementation,
- lifecycle hooks without a demonstrated caller,
- broad service containers inside a small package.

## 7. Builder And Registry Patterns

- Use builders when several operations must be staged before validation or commit.
- Builders should build or stage; the owning object should commit.
- Keep builder APIs focused. Do not duplicate every convenience method from the owning object.
- Use registries for setup-time named or keyed objects when lookup is part of the package contract.
- If a registry becomes immutable after setup, expose an explicit freeze/finalize step.
- Validate duplicates and invalid registrations during registration or freeze, not during unrelated runtime work.
- Do not implement dynamic removal unless the package has a clear workflow for it.

## 8. Identity And Keys

- Use explicit stable identifiers for public identity.
- Do not treat list indexes, UI positions, or array offsets as persistent identity.
- If the package is generic over key type, carry that type consistently through related public APIs.
- Use string identifiers for ergonomic authoring APIs when they are meant for humans.
- Use internal key/value objects when they protect invariants, but avoid leaking them into normal public workflows.
- Validate duplicate identifiers early.
- Keep identity separate from display names and descriptions.

## 9. Events And Notifications

- Events should describe committed changes, not attempted changes.
- Prefer typed event payloads over loosely structured dictionaries.
- Group related changes into one event for one public operation when practical.
- Include enough information for consumers to update efficiently without diffing all state.
- Avoid firing events from constructors.
- Avoid firing events for no-op or rejected operations unless the event explicitly represents rejection.
- Document whether events are synchronous and when they fire.

## 10. Data Objects And DTOs

- Use small immutable DTOs for public data snapshots.
- Keep DTOs free of behavior unless the behavior is intrinsic and simple.
- Do not use `object` or dictionaries in public DTOs unless flexible content is an explicit design goal.
- If flexible metadata is needed, isolate it behind a clearly named type.
- Keep persistence DTOs separate from runtime objects when persistence exists.
- Do not add serialization early unless it is part of the current requirement.

## 11. Async And Long-Running Work

- Do not introduce `async` APIs until the package has a concrete asynchronous operation.
- If async is part of the intended core contract, define it deliberately at the command or operation boundary.
- Avoid fake async wrappers around synchronous placeholder code.
- Accept `CancellationToken` on real long-running operations.
- Keep synchronous skeletons synchronous unless the public contract would be wrong without async.

## 12. Documentation Standards

- Generate XML documentation for public APIs.
- Document responsibility, not implementation trivia.
- Mention important ownership and mutation rules in remarks.
- Mark incomplete placeholder APIs clearly, but do not over-document designs that are not settled.
- README examples should show the normal workflow before advanced customization.
- Extension documentation should explain when users should implement the interface and when they should call a higher-level API instead.
- Keep examples short and compilable when possible.

## 13. Testing Standards

- Test public behavior before private helpers.
- Start with construction and invariant tests for skeleton work.
- Do not write behavioral tests for features that do not exist.
- Test both expected-success and expected-failure paths once behavior is implemented.
- Test rejected operations for atomicity.
- Test that public read-only collections cannot mutate internal state.
- Add regression tests for previously broken or risky behavior.
- Include extension-contract tests when public extension interfaces exist.
- Keep example tests aligned with README snippets when the package uses example-driven docs.
- Avoid broad integration fixtures when a focused unit test proves the contract.

## 14. Project Settings

- Keep library projects broadly compatible when possible, such as `netstandard2.1` for reusable .NET packages.
- Keep test projects on a modern runtime when useful, such as `net9.0`.
- Enable nullable reference types.
- Keep warnings clean where reasonably possible.
- Generate XML documentation for packages with public APIs.
- Be explicit about language version and implicit using preferences if the package has an established style.
- Do not add package dependencies until the current feature requires them.

## 15. Minimal Skeleton Rules

When creating a new package skeleton:

- Create only the types needed to show ownership and intended API shape.
- Prefer immutable properties and minimal constructors.
- Use placeholder methods only when they clarify the future public contract.
- Do not implement parsing, execution, persistence, permissions, background work, serialization, or pipelines unless the current task explicitly requires them.
- Do not add speculative abstractions.
- Do not hide unsettled design decisions behind overly general interfaces.
- Add XML docs where they clarify responsibility.
- Add minimal tests for construction, initial state, and simple value preservation.
- Build and run tests before considering the skeleton complete.

## 16. Review Checklist

Before accepting a public API addition, check:

- Is this needed for the current package goal?
- Is the owner of this behavior clear?
- Can users follow the normal workflow without understanding internals?
- Is mutable state protected?
- Are failures handled consistently?
- Does validation happen before mutation?
- Are extension points narrow and justified?
- Are names specific and domain-appropriate without being overdesigned?
- Are docs and tests aligned with the actual behavior?
- Is anything pretending to be implemented when it is only a placeholder?
