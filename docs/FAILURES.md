# Failure Handling

Workes.ContentSystem represents expected operation rejection as structured `ContentFailure` data. This lets callers handle normal domain outcomes without parsing strings or catching exceptions for every branch.

Extension authors should start with [Extension-Authored Failures](EXTENSION_AUTHORING.md#extension-authored-failures) for a practical pattern and code examples. This guide is the reference for the shared failure model.

## Expected Rejection And Programmer Misuse

Use structured failures for expected domain outcomes such as a missing record, capacity rejection, unsupported capability, snapshot incompatibility, or extension-specific rule rejection. Use standard .NET exceptions for programmer misuse, such as null arguments, invalid constructor configuration, or calling a method with an invalid argument shape.

| Situation | Expected behavior |
|---|---|
| Expected operation rejection | `Try...` returns `false` and supplies a `ContentFailure` |
| Advisory preflight rejection | `Assess...` returns `ContentPreflightResult.Rejected(failure)` without committing |
| Expected-success operation rejection | The method throws `ContentOperationException` containing the structured failure |
| Programmer misuse or invalid setup | A standard .NET exception, commonly `ArgumentNullException`, `ArgumentException`, or `InvalidOperationException` |
| Unplanned exception from extension code | Propagates unless the extension contract explicitly converts that boundary into a structured rejection |

Preflight is advisory and not a reservation. Committing APIs revalidate because state may change after an assessment.

## Failure Model

The public failure model consists of:

- `ContentFailure`, immutable expected-rejection data;
- `ContentFailureKind`, a broad category;
- `ContentFailureCodes`, stable package-owned code constants;
- `ContentSystemException`, the base package exception carrying a failure;
- `ContentOperationException`, the standard exception for expected operation rejection;
- `ContentPreflightResult`, a side-effect-free assessment result that can carry a failure.

`ContentFailure` contains:

- `Kind`: broad category such as `Entry`, `Structure`, `Snapshot`, or `Extension`;
- `Code`: stable machine-readable string;
- `Message`: human-readable description;
- `Component` and `Source`: optional origin information;
- `Cause`: optional nested `ContentFailure` explaining a lower-level rejection.

Callers should branch on `Kind` and `Code`. Messages are for people, may change for clarity, and must not be used as identifiers.

## Built-In Failure Kinds And Codes

`ContentFailureKind` is a package-owned enum with broad categories: `Unknown`, `Validation`, `Configuration`, `Entry`, `Structure`, `Snapshot`, `Export`, `Attachment`, `Persistence`, `Bridge`, and `Extension`. The enum is not an extension registry and extensions should not attempt to add categories to it. Choose the narrowest existing kind that accurately describes the failing subsystem; use `Extension` when no other existing kind fits.

Built-in constants on `ContentFailureCodes` use the reserved `workes.content.` prefix. Examples include:

- `EntryNotFound`, `EntryIdInvalid`, and `EntryIdDuplicate`;
- `StructureReadOnly`, `StructureUnsupportedOperation`, and `StructureCapacityReached`;
- `ManagerMismatch`;
- snapshot unsupported, malformed, factory, version, and codec failures;
- `ExtensionRejected` for a general extension failure.

Use package constants when their meaning matches. Do not copy a built-in code for a different meaning and do not define extension-specific codes under `workes.content.`.

## Failure Creation And Wrapping

Create a failure with a broad kind, a stable code, a useful message, and optional source metadata:

```csharp
public static class QuestLogFailureCodes
{
    public const string Archived = "example.questlog.content.archived";
}

ContentFailure failure = ContentFailure.Create(
    ContentFailureKind.Extension,
    QuestLogFailureCodes.Archived,
    "The quest log is archived and cannot accept new entries.",
    component: nameof(QuestLogStructure));
```

The example code uses an application-owned namespace; replace it with a stable identifier owned by your library or application. Keep codes stable once consumers may depend on them. Include the action and relevant context in `Message`, but do not put volatile values in the code.

When a higher-level operation fails because of a lower-level `ContentFailure`, wrap it and preserve the cause:

```csharp
ContentFailure wrapped = ContentFailure.Wrap(
    ContentFailureKind.Extension,
    "example.questlog.content.append_rejected",
    "The quest update could not be appended.",
    cause: lowerLevelFailure,
    component: nameof(QuestLogStructure));
```

`wrapped.Cause` retains the original failure object. This gives logs and callers a structured chain without forcing them to parse concatenated messages. If there is no meaningful lower-level failure, create a direct failure instead of inventing a cause.

## Extension-Authored Failures

Extensions may add as many stable string codes as their domain needs, but use the existing broad failure categories. Prefer an existing category such as `Entry`, `Structure`, or `Snapshot` when it accurately identifies the failed subsystem; otherwise use `ContentFailureKind.Extension`. Do not add a custom enum, modify `ContentFailureKind`, or reuse package-owned `ContentFailureCodes` names for an extension-specific meaning.

Keep extension codes namespaced and stable, for example `vendor.product.content.quest_log.archived`. Put them in the extension's own `static class` so consumers can compare constants without duplicating string literals. Do not use the reserved `workes.content.` prefix.

Inside an extension's `Try...` method, an expected rejection branch looks like this:

```csharp
if (_isArchived)
{
    record = null;
    failure = ContentFailure.Create(
        ContentFailureKind.Extension,
        QuestLogFailureCodes.Archived,
        "The quest log is archived and cannot accept new entries.",
        component: nameof(QuestLogStructure));
    return false;
}
```

On expected rejection, return `false`, set output values to their documented failure state, and leave retained state and change events untouched. On success, set `failure` to `null`. Do not throw for a normal domain rejection from a `Try...` method.

## Assess, Try, And Throwing APIs

The three forms serve different call sites:

```csharp
ContentPreflightResult assessment = structure.AssessAppend(entry);
if (!assessment.CanCommit)
{
    ShowMessage(assessment.Failure!.Message);
}

if (!structure.TryAppend(entry, out ContentEntryRecord? record, out ContentFailure? failure))
{
    Log(failure!.Code);
}

try
{
    ContentEntryRecord record = structure.Append(entry);
}
catch (ContentOperationException exception)
{
    Log(exception.Failure.Code);
}
```

The assessment predicts current acceptance but does not reserve capacity or ID state. The `Try...` operation is authoritative. An expected-success wrapper should throw `ContentOperationException` carrying the failure that its `Try...` form would return. Do not wrap programmer misuse in `ContentOperationException`.

`ContentSystemException` is the base type for package exceptions that carry a `ContentFailure`. `ContentOperationException` is the normal operation-level exception. Callers can inspect `exception.Failure` for kind, code, component, source, and nested cause.

## Converting Exceptions

`ContentFailure.FromException(exception, kind, code)` converts an exception at an intentional expected-failure boundary:

```csharp
ContentFailure failure = ContentFailure.FromException(
    callbackException,
    ContentFailureKind.Extension,
    "vendor.product.content.callback_failed");
```

If `exception` is a `ContentSystemException`, `FromException` returns its existing `Failure` unchanged. For another exception, it uses the supplied kind and code; defaults are `ContentFailureKind.Extension` and `ContentFailureCodes.ExtensionRejected`.

Only convert exceptions when the extension's contract says that failure is an expected result—for example, an optional callback explicitly allowed to reject an operation. Do not broadly catch every exception. Let programmer errors and unexpected faults propagate, and do not convert `ArgumentNullException` from invalid caller input into a domain failure.

## Failure Testing

Tests should verify behavior, not just that a failure object exists:

- expected rejection returns `false`, the intended `Kind`, and a stable `Code`;
- returned record/output values match the documented failure state;
- retained state, ID-source state, and events are unchanged after rejection;
- assessments report equivalent failures without side effects;
- throwing wrappers expose a `ContentOperationException` whose `Failure` is structurally equal to the `Try...` failure;
- wrappers preserve `Cause`, `Component`, and `Source` where relevant;
- exception conversion preserves an existing `ContentSystemException.Failure` and applies the requested defaults/code to ordinary exceptions;
- programmer misuse continues to throw standard .NET exceptions.

The general extension walkthrough and runnable example are in [Extension Authoring](EXTENSION_AUTHORING.md#extension-authored-failures). Existing package failure tests are in `tests/Core/ContentFailureTests.cs`.

## Built-In Failure Areas

- Typed manager resolution uses `ManagerMismatch` when the resolved manager is not the requested type.
- Missing stored IDs use `EntryNotFound`; invalid and duplicate IDs use the corresponding entry ID codes.
- Built-in sequence/stack capacity and single replacement policies use `StructureCapacityReached`.
- Snapshot capture and restore use the `Snapshot` kind for unsupported entries/structures, missing or duplicate factories, malformed payloads, unsupported versions, or codec rejection.
- Optional structure workflows report `StructureUnsupportedOperation` when the active structure does not implement the required focused capability.

See [Content Managers](CONTENT_MANAGERS.md), [Content Identity](CONTENT_IDENTITY.md), and [Content Snapshots](CONTENT_SNAPSHOTS.md) for behavior-specific failure cases.
