# Sellers Frontend (Planned)

This folder is reserved for the dedicated Sellers frontend application.

## Architecture direction
- It should be implemented as a **microfrontend**.
- It will be exposed under the same platform surface using a dedicated host such as `sellers.eshopacademy.com`.

## Planned initial capabilities
- Seller registration flow.
- Seller management dashboard for:
  - published products;
  - creating new products;
  - managing stock levels.
- Sales insights:
  - current sales summary;
  - transaction history based on seller ledger entries.

## Operations integration
- Once a seller is registered, they will manage operational order actions in the Operations frontend (pack, ship, cancel, etc.).
- Access control will require ABAC to limit scope to the seller's own products/orders.
