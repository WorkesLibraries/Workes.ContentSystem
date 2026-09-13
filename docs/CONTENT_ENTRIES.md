# Content Entries

Content entries are the primary extension point of Workes.ContentSystem.

## Purpose

An entry represents one piece of content stored in a content structure.

The core package should support a small set of ordinary entries, but it should not try to predict every game, tool, editor, or server use case. Custom entries are expected and should feel first-class.

## Entry Identity

Every entry has an ID.

The structure decides what IDs mean and how they are assigned. This keeps the model flexible:

- a bounded FIFO structure can use an increasing numeric ID;
- a persistent structure can use a durable string ID;
- a distributed structure can use UUID-like IDs;
- a threaded structure can use IDs that help relate replies and parents.

The public API should avoid pretending there is only one correct ID shape.

## Entry Content

Entries may expose simple plain text for easy rendering, searching, export, or debugging.

Entries may also expose richer data. For example:

- a chat entry can contain a channel, sender display name, and message text;
- a log entry can contain severity and source;
- a stack trace entry can contain collapsed frames;
- an item-link entry can contain an item ID and display text;
- a forum entry can contain parent/thread information.

The core should provide enough common shape to be useful without making every custom entry inherit unnecessary fields.

## Custom Entries

Custom entries should be the normal way to support domain-specific content.

If a host wants clickable player names, expandable exceptions, embedded buttons, item tooltips, or rich moderation events, it should define an entry type that carries those semantics directly. The UI can then render that entry according to the host application's own theme and layout system.

This keeps ContentSystem focused on storing meaning rather than owning visual rendering.