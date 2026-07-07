# Resilience and observability

## Retries (on by default, safe for money)

Transient failures retry automatically. Rules that make it safe:

- Retries only 408/429/500/502/503/504, network errors, and timeouts. Never a 4xx (except 429).
- Retries idempotent methods always; retries **POST/PATCH only when the call has a `PayPal-Request-Id`**
  (PayPal dedups by it, so a retried capture/refund can't charge twice). Without a key, a POST is sent once.
- Bounded: capped attempts, exponential backoff with jitter capped by `MaxDelay`, `Retry-After` honored but
  capped, caller cancellation stops it. `TimeoutSeconds` is the total budget per call.

So pass an idempotency key on mutating calls:

```csharp
await paypal.Orders.CaptureAsync(orderId, payPal_Request_Id: $"{orderId}-capture");
```

Configure or disable:

```csharp
o.Retry.MaxRetries = 3;   // 0 = off
o.Retry.BaseDelay  = TimeSpan.FromMilliseconds(500);
o.Retry.MaxDelay   = TimeSpan.FromSeconds(5);
```

## PayPal-Debug-Id (and other response headers)

Calls return the model directly (no `ApiResponse<T>` wrapper). Read headers when you need them:

```csharp
// on failure
catch (PayPalApiException ex) { var id = PayPalHeaders.GetDebugId(ex.Headers); }

// on success
o.OnResponse = res => log.Info(PayPalHeaders.GetDebugId(res));
```

## Callbacks

`OnRequest` / `OnResponse` / `OnException` fire per attempt (fast, non-throwing; a throwing callback is
swallowed).

- `OnRequest` - before each attempt.
- `OnResponse` - every response, **including 4xx/5xx** (they arrive as responses; the exception is made after).
- `OnException` - transport failure with no response (network/timeout/cancel).

```csharp
o.OnResponse  = res       => Metrics.Record((int)res.StatusCode, PayPalHeaders.GetDebugId(res));
o.OnException = (req, ex) => Metrics.RecordFailure(req.RequestUri, ex);
```

## Logging

Off by default. When on, logs one line per attempt (method, path, status, elapsed, debug id) via `ILogger`;
no bodies (PII/card data).

```csharp
o.Logging.Enabled = true;
o.Logging.Level   = LogLevelOption.Information;
```
