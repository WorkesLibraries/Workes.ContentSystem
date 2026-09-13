# Concepts

Workes.ContentSystem is a planned backend for extensible content streams.

The package should not assume that all content is chat, logs, console output, or forum posts. Those are all possible uses of the same deeper model: an application stores entries in a structure, then a host UI or integration decides how to present or export them.

## Content Entries

A content entry is one item in a content structure.

Entries are intentionally extensible. A simple application might only use plain text entries. A larger game or tool might add entries for chat messages, command output, stack traces, item links, moderation events, audit events, or grouped feed items.

Each entry has an ID. The structure decides what that ID means. A bounded FIFO structure might use an increasing integer or index-like ID. A persistent or distributed structure might use a string or UUID-like ID.

## Content Structures

A content structure owns storage behavior.

The first implementation should be a bounded chronological FIFO structure: new entries are appended, old entries are dropped when capacity is reached, and consumers can read the retained entries in order.

Other structures can behave very differently. A forum-like structure might group entries by thread. A chat structure might group by channel. A searchable structure might maintain indexes. A persistent structure might load and save entries.

## Content Manager

The expected root object is `ContentManager`.

The manager should provide the simple workflow for normal users while allowing advanced users to swap in different structures or optional attachments. Like the other Workes packages, the default path should be easy, and complexity should be opt-in.

## Attachments

Export, persistence, file appenders, log bridges, and platform integrations should be optional. The core package should make those capabilities possible without forcing every structure or every user to support them.

## Relationship To ConsoleSystem

Workes.ConsoleSystem inspired this package. Console history, command output, command failures, and custom console entries are all examples of content entries.

The long-term direction is to build a future ConsoleSystem on top of ContentSystem rather than continue growing ConsoleSystem as a one-off content backend.