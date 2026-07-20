# Frontend development

## Active Aspire frontends

The AppHost starts two Vite applications:

- `src/Frontend/eshop-web` — the consumer application on the requested port 5173. It includes authentication, catalog, basket, checkout, order status through SSE, wishlist, seller integration, and notifications.
- `src/Frontend/eshop-sellers` — the seller Module Federation remote on the requested port 5174. Its `dev` script builds and previews the remote; `dev:standalone` starts the normal Vite development server.

Both should normally be started through Aspire so `VITE_GATEWAY_URL` and other resource references come from the AppHost. Standalone commands are useful for isolated UI work, but fixed `.env` URLs may not match an isolated Aspire session.

## Legacy prototype

`src/Sellers/Frontend` is a separate seller portal prototype. It is not referenced by the AppHost or the .NET solution and uses a different package name and dependency versions. Do not treat it as the active seller frontend without an explicit ownership/consolidation decision. Its original note is preserved in the [archive](../plans/archive/sellers-frontend-prototype.md).

## Historical feature notes

The original checkout, saved-address, order-details, and notification implementation summaries are preserved under [`docs/plans/archive`](../plans/archive/README.md). They contain useful implementation history but are not authoritative: several TODOs have since been completed and some concurrency/retry claims were inaccurate.

