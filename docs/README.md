# eShopAcademy documentation

This directory is the authoritative documentation entry point. Implementation is the source of truth when a historical note or future plan differs from current code.

## Current system

- [Architecture overview](architecture/overview.md) — services, data stores, communication, and orchestration.
- [Local development](development/local-development.md) — prerequisites, Aspire startup, resources, and configuration.
- [Build and test](development/testing.md) — solution, test, and frontend validation commands.
- [Frontend development](development/frontends.md) — active Vite applications and the legacy prototype distinction.
- [Production deployment](deployment/production.md) — what exists today and what production still requires.

## Production-readiness plans

- [Roadmap](plans/roadmap.md)
- [Documentation validation gaps](plans/documentation-validation-gaps.md) — actionable gaps and production blockers from the repository audit.
- [Production-readiness specifications](plans/production-readiness/README.md) — detailed target standards and evidence.

## Agent and tool guidance

- [Shared project guidance](agents/project-guidance.md)
- [Tool entry points](agents/tool-entry-points.md)

## Historical material

[Archived implementation notes](plans/archive/README.md) preserve useful context but are not current setup instructions or architecture contracts.

## Other assets

- [`postman/eShopAcademy.postman_collection.json`](postman/eShopAcademy.postman_collection.json) is the existing Postman collection. It has not been proven complete against every current endpoint.
