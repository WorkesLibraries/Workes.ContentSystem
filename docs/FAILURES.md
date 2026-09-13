# Failures

Workes.ContentSystem should use the same broad failure philosophy as the other Workes packages.

## Expected Failures

Expected content-system rejection should be represented as structured failure data.

Examples:

- an entry cannot be added because a structure is read-only;
- an entry ID cannot be found;
- an export attachment cannot handle the active structure;
- a persistence operation fails in a recoverable way;
- a bridge rejects an entry because it cannot map the entry type.

The planned failure model is:

- `ContentFailure`;
- `ContentFailureKind`;
- `ContentFailureCodes`;
- `ContentSystemException`;
- `ContentOperationException`.

## Try And Expected-Success APIs

Try-style APIs should return structured failure data when ordinary operation failure is expected.

Expected-success APIs should throw package-owned exceptions that carry the same structured failure.

Programmer misuse, such as null arguments or invalid setup values, should use standard .NET exceptions.

## Why This Matters

This keeps error handling consistent across Workes packages.

A caller can choose the workflow that fits the situation without losing information:

- use try APIs when failure is a normal branch;
- use expected-success APIs when failure should interrupt the workflow;
- inspect structured failure kind and code in either case.