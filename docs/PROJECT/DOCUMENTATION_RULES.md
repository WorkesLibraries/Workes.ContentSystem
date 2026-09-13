# DOCUMENTATION RULES

## Purpose

Define how the user-facing project documentation should be maintained.

This file exists so README.md and CHANGELOG.md can stay clean, user-facing, and first-class while still having clear maintenance rules elsewhere.

## Usage

Read this file before creating or modifying user-facing documentation.

This file applies to:

- README.md
- CHANGELOG.md
- public documentation files under docs/

## Maintenance

Update this file when the expectations for user-facing documentation change.

This file is local/private project guidance and should not be treated as user-facing package documentation.

## Rules

- Keep user-facing documentation clear, accurate, and useful to package consumers.
- Document public features enough that users do not need source-code reading to discover normal use.
- Do not place AI workflow notes in README.md, CHANGELOG.md, or public docs.
- Do not use README.md or CHANGELOG.md as work trackers.
- Do not include private project-planning details in user-facing files.
- Prefer concise examples over long explanations when documenting usage.
- Keep package documentation in focused files under docs/.
- Keep internal project-control documentation under docs/PROJECT/.
- Keep documentation templates in Project Formula templates instead of embedding long documentation bodies in scripts.

## README.md Rules

### Purpose

Act as the package landing page.

It should explain what the project is, who it is for, what users need to know before installing it, and where deeper documentation lives.

### Maintenance

Update README.md when the project purpose, setup instructions, package relationships, prerequisites, installation flow, usage links, or public positioning changes.

### Rules

- Keep it understandable for a new user.
- Keep it user-facing.
- Explain the practical value of the project.
- Include prerequisites and installation information when the project is ready for them.
- Use this installation shape:

    ## Installation

    Install the package from [NuGet](https://www.nuget.org/packages/Workes.PackageName):

    ```bash
    dotnet add package Workes.PackageName --version 0.1.0
    ```

    Or add a package reference:

    ```xml
    <PackageReference Include="Workes.PackageName" Version="0.1.0" />
    ```

    The package targets .NET Standard 2.1.

- Keep the explicit `--version` in the CLI install command.
- Keep the explicit `Version` attribute in the XML package reference.
- Mention connected or companion packages when relevant.
- Link to docs/QUICK_START.md.
- Link to additional focused documentation files when they exist.
- Do not turn the README into the entire manual.
- Do not store AI workflow notes here.
- Do not use it as a task list.

## CHANGELOG.md Rules

### Purpose

Track notable public-facing changes over time. CHANGELOG.md is first-class release documentation alongside README.md.

### Maintenance

Update CHANGELOG.md whenever user-visible behavior changes, during the same work that introduces the change. Release preparation should audit and finalize the changelog, not write it from scratch.

### Rules

- Keep an `Unreleased` section for ongoing work until the release date is known.
- Prefer grouped entries by version or date.
- Focus on final user-visible changes, not the internal sequence of work that produced them.
- Focus on features, fixes, breaking changes, deprecations, and migration notes.
- Make public API changes clear enough that consumers can migrate without reading commit history.
- Do not include tiny internal implementation details.
- Do not use it as a task list.

## Public Docs Rules

- README.md should stay concise.
- QUICK_START.md should be beginner-first.
- Detailed documentation should live in focused docs files.
- If a public feature exists, it should be documented or intentionally called out as advanced/internal.
- Audit public docs against the public API before release.


