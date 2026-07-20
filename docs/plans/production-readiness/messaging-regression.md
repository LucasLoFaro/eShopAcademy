# Messaging regression plan

Status: executable checklist for the messaging-foundation branch.

## Purpose

This plan protects behavior while RabbitMQ and Azure Service Bus remain interchangeable MassTransit transports. Broker configuration tests prove composition; saga and consumer tests prove message semantics; API and frontend checks prove that user-visible workflows still initiate and observe the same messages.

## Automated checks

| Area | Required assertion |
| --- | --- |
| Transport registration | RabbitMQ and Azure Service Bus registrations build without connecting to either broker. |
| Topology contract | Durable endpoint names are unique; every routed command has exactly one destination; discovered consumers are represented in the manifest. |
| Package consistency | Every MassTransit package uses the centralized `MassTransitVersion`. |
| Broker isolation | Application assemblies do not reference RabbitMQ or Azure Service Bus APIs. |
| Command semantics | Saga commands are sent to the endpoint declared in `messaging-topology.json`; inline published commands are documented exceptions. |
| Scheduler | Submitting an order creates a scheduled `OrderExpiredEvent`; RabbitMQ registers Quartz and Azure Service Bus uses its native scheduler. |
| Happy path | Payment completion sends stock commit, basket empty, shipping schedule, and order-status commands; stock commit sends package preparation; pickup sends shipping confirmation; delivery publishes the documented completion exception and sends the final status update. |
| Compensation | Payment failure/timeout, stock failure, package issue, and shipping failure send the correct refund, stock release, shipping cancellation, order cancellation, and status commands. |
| Consumer behavior | Order cancellation is idempotent for missing/already-cancelled orders and sends compensation only when applicable. |

## Direct API regression

Run against the endpoints reported by Aspire rather than fixed ports.

| Scenario | Actions | Expected observable result | Messaging evidence |
| --- | --- | --- | --- |
| Catalog browse | List products, retrieve one product, retrieve its stock. | Successful responses contain seeded products and stock. | Product and stock hosts remain healthy; no fault queues or transport errors. |
| Basket mutation | Add an in-stock product, read the basket, remove it. | Quantity and total change and return to their original values. | Basket API bus remains connected. |
| Order submission | Create a basket and customer, then place an order. | API returns `202 Accepted`; order is queryable with `Created` status and a payment URL. | `OrderSubmittedEvent` reaches orchestration, notifications, and seller subscribers; saga schedules payment timeout. |
| Payment accepted | Confirm the payment in the PSP simulator. | Order progresses through paid/stock/shipping states; basket empties. | Commit-stock, empty-basket, schedule-shipping, and status commands reach their deterministic queues. |
| Payment rejected | Reject a new order payment. | Order becomes cancelled and stock is released. | Payment-failed event triggers release-stock and cancel-order commands. |
| Stock failure | Submit an order whose reservation cannot be committed. | Order is cancelled and payment is compensated. | Refund, cancel-shipping, and cancel-order commands are sent once. |
| Shipping lifecycle | Mark an order ready, confirm pickup, transition shipment to delivered. | Order progresses to shipped then delivered. | Confirm-shipping and status commands are sent; delivery event completes the saga. |
| Cancellation | Delete/cancel an eligible order and query it again. | Order is cancelled without duplicate compensation on retry. | Cancel-order consumer publishes facts and sends compensation only when required. |
| Customer address | Create/update a saved address. | Updated address is returned by the customer API. | `CustomerAddressUpdatedEvent` reaches its durable subscriber. |
| Product update | Update a product or publish a seller product. | Catalog and stock reflect the change. | Product facts reach basket and stock subscribers. |

Tests that mutate data must use freshly generated identifiers and record created entity IDs. Cleanup is best-effort; no scenario may target production resources.

## Frontend smoke regression

The frontend pass is intentionally smaller than the API pass. It validates browser authentication, gateway routing, request/response models, and real-time status updates.

1. Open the consumer frontend endpoint reported by Aspire and confirm the seeded catalog renders without console or failed-network errors.
2. Sign in with the configured development Entra account.
3. Add an in-stock item, change quantity, and verify the basket total.
4. Complete address and payment-method steps, submit an order, and verify the order detail/payment link.
5. Confirm one payment and verify the order page receives a status update through SSE and the basket becomes empty.
6. Create a second order, reject payment, and verify cancellation is visible.
7. Review browser console, failed network requests, MassTransit faults, and the involved service traces.

If interactive Entra authentication is unavailable, run the catalog portion through the frontend and execute authenticated scenarios directly against service APIs. Do not mock the broker or gateway and claim that as an end-to-end pass.

## Known limitations requiring separate work

- Duplicate delivery/idempotency is not comprehensively enforced for every saga event. This deserves dedicated inbox/outbox or state-machine idempotency tests before production traffic.
- The orchestration tests currently report an unrelated EF Core 10.0.4/10.0.9 assembly-version warning.
- External SendGrid delivery is not a safe local assertion; notification persistence and consumer completion should be asserted instead.
