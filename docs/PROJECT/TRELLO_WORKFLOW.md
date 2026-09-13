# TRELLO WORKFLOW

## Purpose

Describe how this project uses Trello for task state and how AI assistants should interact with Trello through the `trello` CLI.

Trello is the source of truth for task state once this file says the project board is ready.

## Usage

Read this file before inspecting or changing task state.

Task state includes:

- current work
- next work
- backlog items
- bugs
- blocked work
- completed work
- roadmap/planning cards, if used

Do not use markdown files as a parallel task tracker.

## Maintenance

Update this file when the Trello board, list names, list IDs, authentication assumptions, or CLI usage rules change.

This file should contain enough concrete command usage that an AI assistant does not need to guess how the Trello workflow works.

## Setup Status

Project board ready.

The project board, exact standard workflow lists, and Blocked label are mapped below.

Run the standalone `setup-trello-board.ps1` script from the Project Formula folder to create or discover the standard lists and label, normalize list order, and populate this file.

## CLI Availability

The Trello CLI installation directory must be available on the current user's `PATH`.

Before Trello-dependent work, check that the command can be resolved:

```powershell
Get-Command trello
trello version
```

If `trello` was recently added to the system or user `PATH`, restart Codex before trying again so the desktop process receives the updated environment.

Do not hard-code a machine-specific path to the executable in project documentation.

## Authentication

Authentication is handled manually by the developer.

AI assistants must not request, generate, store, print, or edit Trello API keys or tokens.

Before Trello-dependent work, the AI may check authentication with:

```powershell
trello auth status --pretty
```

If authentication fails, stop using Trello commands and ask the developer to re-authenticate manually.

## Codex Sandbox Note

When running from Codex, Trello CLI authentication may only be visible outside the sandboxed command environment.

If `trello auth status --pretty` reports authenticated in a normal terminal but not inside Codex, run Trello CLI checks and setup commands with sandbox escalation instead of re-authenticating or changing project files.

Use the elevated environment for Trello commands that need the authenticated local CLI configuration, including board discovery, list/card/label updates, and `setup-trello-board.ps1`.

Do not paste Trello API keys or tokens into chat, source files, README.md, CHANGELOG.md, or docs. Authentication remains local machine state. The workaround is environment selection, not credential storage.

Useful authentication commands for the developer:

```powershell
trello auth login
trello auth set --api-key <key> --token <token>
trello auth status --pretty
trello auth clear
```

## CLI Output Contract

The Trello CLI returns JSON.

Success shape:

```json
{"ok":true,"data":{}}
```

Error shape:

```json
{"ok":false,"error":{"code":"...","message":"..."}}
```

Use `--pretty` when humans need to read command output.

Prefer compact output without `--pretty` in scripts or automation.

## Project Board

Board name: Content System

Board ID: 6aa71fa3995ee6da8718eb38

Board URL: https://trello.com/b/2j0Afk1n/content-system

## Expected Lists

Use this exact active list shape unless the project has a documented reason to differ.

The active workflow lists must be, in this order:

1. Current
2. Next
3. Backlog
4. Bugs
5. Done

Do not keep Trello starter lists such as `To Do` or `Doing` on package project boards. If Trello creates starter lists automatically, the setup script should archive empty `To Do` and `Doing` lists during board setup. If those starter lists contain cards, move or migrate those cards before archiving the lists.

| List | Purpose | List ID |
|---|---|---|
| Current | Work currently in progress | 6aa71fb6d051fc317d9719b9 |
| Next | Work intended soon, but not currently active | 6aa71fb782004d434728e876 |
| Backlog | Possible future work | 6aa71fb8a36ee93b5b2ab160 |
| Bugs | Known defects | 6aa71fb9a36ee93b5b2ab255 |
| Done | Completed work | 6aa71fba566d4767484b1242 |

## Expected Labels

| Label | Purpose | Color | Label ID |
|---|---|---|---|
| Blocked | Work that cannot progress because it is waiting on information, a decision, or an external dependency | Red | 6aa72043d8e3624b3b7e4264 |

## Board Discovery Commands

List boards:

```powershell
trello boards list --pretty
```

Get one board:

```powershell
trello boards get --board <board-id> --pretty
```

Note: Under normal circumstances, the board should be set in this file, and board discovery should not be needed.

List lists on a board:

```powershell
trello lists list --board <board-id> --pretty
```

List labels on a board:

```powershell
trello labels list --board <board-id> --pretty
```

List cards on a board:

```powershell
trello cards list --board <board-id> --pretty
```

List cards in a specific list:

```powershell
trello cards list --list <list-id> --pretty
```

Search cards:

```powershell
trello search cards --query "<search text>" --pretty
```

## Board Setup Commands

Board creation should usually be done by the developer.

After creating the board, run the standalone setup script from the Project Formula folder:

```powershell
& "<Project Formula>\setup-trello-board.ps1" -BoardId "<board-id>" -TargetPath "<project-path>"
```

The script:

- checks that `trello` is available and authenticated;
- reuses uniquely named standard lists and the `Blocked` label when they already exist;
- creates missing standard lists and the label;
- orders the standard lists as Current, Next, Backlog, Bugs, Done;
- archives empty Trello starter lists named `To Do` and `Doing`;
- fills in the board, list, and label IDs in this file;
- changes Setup Status to `Project board ready.`;
- leaves unrelated non-starter lists and labels unchanged.

The script is intentionally not part of the package-creation orchestrator because the developer creates and selects the Trello board separately.

## Card Commands

Create a card:

```powershell
trello cards create --list <list-id> --name "<card title>" --desc "<card description>"
```

Get a card:

```powershell
trello cards get --card <card-id> --pretty
```

Update a card:

```powershell
trello cards update --card <card-id> --name "<new title>"
trello cards update --card <card-id> --desc "<new description>"
trello cards update --card <card-id> --due <iso-8601>
```

Move a card:

```powershell
trello cards move --card <card-id> --list <target-list-id>
```

Add the Blocked label:

```powershell
trello labels add --card <card-id> --label <blocked-label-id>
```

Remove the Blocked label:

```powershell
trello labels remove --card <card-id> --label <blocked-label-id>
```

Archive a card:

```powershell
trello cards archive --card <card-id>
```

Do not delete cards unless the developer explicitly asks for deletion.

## Task State Rules

- Trello owns task state.
- docs/PROJECT/AI_CONTEXT.md may summarize current direction, but must not become a duplicate task board.
- Prefer one Trello card per coherent unit of work.
- Keep card titles short and action-oriented.
- Put details, constraints, and links in card descriptions.
- Move cards rather than rewriting their status in markdown.
- Use Bugs for confirmed or suspected defects.
- Apply the Blocked label only when progress requires outside information, user input, a decision, or an external dependency.
- Keep a blocked card in its normal workflow list so its intended priority and state remain visible.
- Record the blocking reason and the condition for becoming unblocked in the card description or a comment.
- Remove the Blocked label as soon as progress can resume.
- Use Done for completed work that should remain visible.
- Archive only when cards are no longer useful to inspect.

## AI Workflow

When task state matters:

1. Read docs/PROJECT/PROJECT_CONTEXT.md.
2. Read this file.
3. Check Setup Status.
4. If Setup Status is not `Project board ready.`, do not mutate Trello state.
5. If Setup Status is `Project board ready.`, run `trello auth status --pretty`.
6. Discover relevant cards and labels using list or search commands.
7. Perform only the smallest necessary task-board update.
8. Summarize any Trello changes in the response.

## Safety Rules

- Do not print API keys or tokens.
- Do not ask the developer to paste secrets into project files.
- Do not store credentials in documentation.
- Do not use `trello auth clear` unless the developer explicitly asks.
- Do not delete cards unless explicitly asked.
- Prefer `archive` over `delete` for cleanup.
- If a Trello command returns an error JSON object, stop and explain the failure.




