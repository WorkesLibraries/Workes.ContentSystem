# Failures

Workes.ContentSystem uses the same broad failure philosophy as the other Workes packages.

## Expected Failures

Expected content-system rejection is represented as structured failure data.

Examples:

- an entry cannot be added because a structure is read-only;
- an entry ID cannot be found;
- an export attachment cannot handle the active structure;
- a persistence operation fails in a recoverable way;
- a bridge rejects an entry because it cannot map the entry type.

The shared failure model is:

- `ContentFailure`;
- `ContentFailureKind`;
- `ContentFailureCodes`;
- `ContentSystemException`;
- `ContentOperationException`.

`ContentFailure` carries:

- `Kind`, a broad category such as `Entry`, `Structure`, `Export`, or `Extension`;
- `Code`, a stable machine-readable string prefixed with `workes.content.`;
- `Message`, a human-readable description;
- optional `Component`, `Source`, and nested `Cause` values.

Built-in failure kinds are:

- `Unknown`;
- `Validation`;
- `Configuration`;
- `Entry`;
- `Structure`;
- `Snapshot`;
- `Export`;
- `Attachment`;
- `Persistence`;
- `Bridge`;
- `Extension`.

Callers should branch on `Kind` or `Code`, not on display messages.

## Try And Expected-Success APIs

Try-style APIs should return structured failure data when ordinary operation failure is expected.

Expected-success APIs should throw package-owned exceptions that carry the same structured failure.

`ContentSystemException` is the base exception for package-owned expected-success failures. `ContentOperationException` is the standard operation-level exception for expected content operation rejection.

Programmer misuse, such as null arguments or invalid setup values, should use standard .NET exceptions.

Manager workflow resolution uses structured failures for expected rejection:

- `ManagerMismatch` when typed manager resolution expects a different manager type than the structure creates.

Snapshot capture and restore use `ContentFailureKind.Snapshot` for expected rejection such as unsupported entries or structures, missing or duplicate restore factories, malformed snapshot payloads, unsupported snapshot versions, or codec rejection. Keyed snapshot restore can also surface `EntryIdInvalid` when a restored stored ID is rejected by the configured ID strategy.

## Why This Matters

This keeps error handling consistent across Workes packages.

A caller can choose the workflow that fits the situation without losing information:

- use try APIs when failure is a normal branch;
- use expected-success APIs when failure should interrupt the workflow;
- inspect structured failure kind and code in either case.
