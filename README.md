# Notification Processing API

A small ASP.NET Core Web API coding exercise solution targeting **.NET 10**. It accepts email notifications, validates and persists them in SQLite, retrieves them by ID/customer, and processes due notifications in priority order.

## Requirements

- .NET 10 SDK
- No real email account/provider is required.

## Run the API

From this directory:

```bash
dotnet restore
dotnet run --project src/Notification.Api/Notification.Api.csproj
```

The launch profile serves the API at `http://localhost:5080`. In Development, open `http://localhost:5080/swagger` for interactive API documentation. SQLite creates `notifications.db` in the process working directory.

## Run tests

```bash
dotnet test NotificationProcessingApi.sln
```

## Endpoints

### Create a notification

`POST /api/notifications`

```json
{
  "customerId": "CUST-101",
  "type": "Email",
  "recipient": "customer@example.com",
  "subject": "Order Confirmed",
  "message": "Your order has been confirmed.",
  "priority": "Normal",
  "requestedDeliveryTime": "2099-10-06T12:30:00Z"
}
```

A new notification returns `201 Created`; an identical request received within five minutes returns `200 OK` with `Idempotent-Replay: true`. Invalid input returns a validation response (`400 Bad Request`). Only Email is supported. Priority is Low, Normal, or High. The delivery timestamp must not be earlier than current UTC time; a timestamp equal to current time is accepted.

### Retrieve

- `GET /api/notifications/{id}`
- `GET /api/notifications/customer/{customerId}`

Unknown IDs return `404 Not Found`; a customer with no notifications returns an empty array.

### Process due notifications

`POST /api/notifications/process-pending`

Processes notifications whose status is Pending and requested delivery time is due. High priority is processed first, then Normal, then Low. Ties are ordered by requested delivery time and creation time. Response example:

```json
{ "processed": 3, "sent": 2, "failed": 1 }
```

The simulated provider logs successful sends. To exercise provider failure, use a recipient ending in `@fail.test`, for example `demo@fail.test`. This deterministic failure hook is only for the exercise.

## Design

- **Controller:** HTTP concerns, status codes and request binding.
- **NotificationService:** validation/business rules, idempotency fingerprinting and processing workflow.
- **INotificationRepository / EF repository:** persistence and queries.
- **IEmailNotificationProvider:** isolates the external provider from application logic.
- **SQLite:** lightweight durable persistence. Tests use EF Core InMemory to isolate each test.

## Assumptions and trade-offs

1. The duplicate window is five minutes. A SHA-256 fingerprint is calculated from normalized request values; matching requests in the window return the original notification rather than creating another.
2. Duplicate detection is a read-then-write operation. Concurrent identical requests could race in a multi-instance deployment. Production should enforce idempotency with a database uniqueness strategy or dedicated idempotency key and transactional handling.
3. The processing endpoint is manually triggered for this exercise. A production deployment would normally use a durable queue/background worker, retries with backoff, dead-letter handling, and distributed locking/atomic claiming.
4. Provider exceptions mark an item Failed and processing continues with the next item. Automatic retries are intentionally omitted.
5. `EnsureCreated` is used to keep local setup simple. Production should use EF Core migrations and a managed database.
6. The provider is simulated. Production should use a real email provider, secret management, provider timeouts, observability, and delivery receipts.
7. The API uses UTC `DateTimeOffset` values. The request model requires an explicit delivery timestamp; clients should send ISO-8601 timestamps with a timezone/offset.
8. Subject is optional because the exercise's minimum required fields do not explicitly make it mandatory.

## What I would improve for production

- Durable queue and worker (for example, a message broker), atomic claiming, distributed coordination and recovery for stale Processing records.
- Explicit idempotency keys, transactional outbox, database constraints and race-safe duplicate handling.
- Retry policy, exponential backoff, dead-letter queue, cancellation/timeout policy, and delivery status callbacks.
- Authentication/authorization, rate limiting, request-size limits, PII-safe logs, metrics/tracing, health checks, and structured operational alerts.
- EF Core migrations, integration tests against SQLite, API contract tests, and load/concurrency tests.

## Clarifications

- “Exact same request” is interpreted as the same customer, type, recipient (case-insensitive), subject, message, priority, and requested delivery instant.
- “Short period” is interpreted as five minutes.
- “Process pending” processes only due notifications; future notifications remain Pending.
- The demo provider uses `@fail.test` to deterministically simulate a provider failure.
