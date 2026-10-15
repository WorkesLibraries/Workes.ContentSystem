# Compatibility And Upgrades

## 1.0 Compatibility Line

Workes.ContentSystem 1.0.0 establishes the supported public API. Compatible bug fixes belong in patch releases. New compatible capabilities belong in minor releases. A future breaking change requires a new major version.

The 1.1.0 release adds read-only manager filtering and query composition over materialized record snapshots. It preserves the 1.0.0 API and structure contracts.

The 1.2.0 release candidate adds optional atomic batch and predicate-removal structure contracts. Existing custom structures and generated-ID sources remain compatible unless they choose to implement the new opt-in contracts.

The 0.x releases were prereleases and do not carry the compatibility guarantees of 1.0.0. Review the changelog and verify prerelease integrations against the 1.0 guides when upgrading. The 1.0.0 release adds sorting capabilities and documentation; it does not intentionally remove or rename the 0.8.0 public API.

## Snapshot Compatibility

Structure snapshot kinds and data versions are part of the persisted contract. Applications should keep snapshots with their application data and test restore when upgrading package versions. A structure extension should preserve the meaning of an existing snapshot version or introduce an explicit migration/new version when its schema changes incompatibly.

Custom entry and structure snapshot kinds are application-owned. Keep those identifiers stable, register custom entry factories before restore, and provide migration logic when an application changes its own snapshot schema. See [Content Snapshots](CONTENT_SNAPSHOTS.md) and [Extension Authoring](EXTENSION_AUTHORING.md).

## Extension Compatibility

Custom structures should implement only the focused contracts they can honor. A package release can add optional contracts without requiring existing custom structures to implement them. If a custom structure opts into a contract, it must satisfy that contract's documented atomicity, failure, ordering, event, and snapshot behavior.

Before updating, check the [Changelog](../CHANGELOG.md) for public API changes and the focused guide for each workflow your application uses.
