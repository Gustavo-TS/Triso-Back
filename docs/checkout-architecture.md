# Checkout architecture

The checkout flow uses application ports. `Triso.Application` depends only on interfaces in `Ports`; EF Core, PostgreSQL, HTTP and InfinitePay remain in `Triso.Infrastructure`.

## Required environment variables

```env
DATABASE_URL=postgresql://USER:PASSWORD@HOST/DATABASE?sslmode=require
INFINITEPAY_HANDLE=your_infinite_tag_without_the_dollar_sign
INFINITEPAY_BASE_URL=https://api.checkout.infinitepay.io
FRONTEND_URL=https://your-frontend.example
API_PUBLIC_URL=https://your-api.example
FRONTEND_ORIGINS=https://your-frontend.example
```

`database/20260929_add_checkout.sql` is an incremental PostgreSQL script. It has not been executed by this change. Review it and apply it through the normal Neon deployment procedure.

## Development mock

Without all InfinitePay variables, Development uses `MockPaymentGateway`; production never does. Checkout returns `FRONTEND_URL/payment/success?...&mock=true`. To mark a pending mock payment as paid, call `POST /api/v1/mock-payments/{orderNsu}/approve`.

This endpoint exists only in Development and exercises the same payment-webhook use case, including the amount comparison, status history, and outbox transaction.

## Replacing InfinitePay

Implement `IPaymentGateway` in a new Infrastructure adapter, register that adapter in DI instead of `InfinitePayGateway`, and provide its provider-specific configuration. Application use cases and controllers do not change.

## Replacing PostgreSQL

Keep the ports and use cases unchanged. Replace `AddPersistence`'s EF provider/options, provide equivalent EF repository and unit-of-work adapters if needed, and use the target provider's incremental schema migration. No controller or application use case references `TrisoDbContext` for checkout behavior.

## Delivery behavior

The webhook only marks a payment paid after `payment_check` confirms both payment and amount. It stores a unique `transaction_nsu`, updates order/payment/history, and appends an outbox record in one transaction. A log notification adapter is registered for development; an outbox dispatcher can later call an e-mail, WhatsApp, in-app, or SignalR adapter after commit.
