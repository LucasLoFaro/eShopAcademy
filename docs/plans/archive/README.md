# Archived implementation notes

Status: historical reference only. These files preserve implementation history and should not be used as current setup, deployment, architecture, or test instructions.

- `architecture-vision.png` is an earlier Azure concept. It depicts Blazor, mobile clients, Kubernetes, and data-store choices that do not match the current AppHost; it is not the current architecture diagram.
- `customers-messaging-implementation.md` records the worker split; its old deployment example was removed because Aspire is the supported local workflow and production deployment is not defined.
- `mongodb-guid-serialization-fix.md` records a fix that remains present on `SavedAddress.CustomerId`.
- `multiple-addresses-implementation.md` and `multiple-address-consumer-logic.md` record the saved-address rollout. Several original TODOs are now implemented, while claims about atomic concurrency and automatic retry were not guaranteed.
- `checkout-flow-implementation.md` records the checkout rollout; saved-address integration now exists.
- `order-details-notifications-improvements.md` records UI work; SSE and notification API integration now exist.
- `sellers-frontend-prototype.md` describes `src/Sellers/Frontend`, which is not the seller frontend orchestrated by Aspire.

Current frontend information is in [frontends.md](../../development/frontends.md).

