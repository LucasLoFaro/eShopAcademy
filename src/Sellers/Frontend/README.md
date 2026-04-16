# Sellers Frontend (Microfrontend Starter)

This folder now contains the initial Sellers microfrontend shell built with React + Vite.

## Run locally
```bash
cd src/Sellers/Frontend
npm install
npm run dev
```

The portal runs on `http://localhost:5174`.

## Environment variables
- `VITE_SELLERS_API_BASE_URL` (default: `http://localhost:8010`)
- `VITE_DEFAULT_SELLER_ID` (default: empty GUID placeholder)

## Current capabilities (Phase 5 bootstrap)
- Seller dashboard shell.
- Financial summary cards loaded from Sellers API.
- Recent ledger transaction list loaded from Sellers API.

## Planned next capabilities
- Seller registration and onboarding UX.
- Product catalog management for seller-owned products.
- Stock updates for seller-owned SKUs.
- Deep links and scoped workflow handoff to Operations frontend.
