using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Aeroverra.PayPalSharp;
using Aeroverra.PayPalSharp.OrdersV2;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Cat = Aeroverra.PayPalSharp.CatalogProductsV1;
using Subs = Aeroverra.PayPalSharp.SubscriptionsV1;
using Refs = Aeroverra.PayPalSharp.PartnerReferralsV2;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// OPTIONAL, human-in-the-loop tests. They open your browser so you can approve/pay/onboard with a
/// sandbox account, then drive the endpoints that need a completed interactive flow (capture, refund,
/// subscription activation, seller onboarding) which the automated suite cannot. Every request and
/// response body is logged so you can eyeball the real payloads and tighten models. These never run in
/// a normal `dotnet test` / CI run: they skip unless user-secret PayPal:RunInteractive is "true".
///
/// Enable and run just these:
///   dotnet user-secrets set "PayPal:RunInteractive" "true"
///   dotnet test --filter "InteractiveFlowTests" --logger "console;verbosity=detailed"
/// </summary>
[Collection(PayPalCollection.Name)]
[TestCaseOrderer("Aeroverra.PayPalSharp.IntegrationTests.TestPriorityOrderer", "Aeroverra.PayPalSharp.IntegrationTests")]
public class InteractiveFlowTests
{
    private readonly PayPalTestFixture _fx;
    private readonly ITestOutputHelper _output;

    public InteractiveFlowTests(PayPalTestFixture fx, ITestOutputHelper output)
    {
        _fx = fx;
        _output = output;
    }

    private bool InteractiveEnabled =>
        _fx.IsConfigured && string.Equals(_fx.Configuration["PayPal:RunInteractive"], "true", StringComparison.OrdinalIgnoreCase);

    [SkippableFact]
    [TestPriority(1)]
    public async Task Pay_for_an_order_then_capture_and_refund()
    {
        Skip.IfNot(InteractiveEnabled,
            "Interactive test. Set user-secret PayPal:RunInteractive=true (and PayPal creds) to run.");

        var log = new Recorder();
        var paypal = BuildRecordingClient(log);
        using var callback = new LocalCallbackServer();

        var created = await paypal.Orders.CreateAsync(
            EnrichedOrder(callback), payPal_Request_Id: Guid.NewGuid().ToString("N"));
        _output.WriteLine($"Created order {created.Id} ({created.Status}).");
        Assert.False(string.IsNullOrEmpty(created.Id));

        var approveUrl = log.FindLink("approve", "payer-action");
        Assert.False(string.IsNullOrEmpty(approveUrl), "No approve/payer-action link on the created order.");

        _output.WriteLine($"Opening browser to approve payment:\n{approveUrl}\nLog in with your SANDBOX BUYER account and pay...");
        OpenBrowser(approveUrl!);

        var callbackUri = await callback.WaitForCallbackAsync(TimeSpan.FromMinutes(5));
        Skip.If(callbackUri is null, "Timed out waiting for approval in the browser.");
        Skip.If(callbackUri!.AbsolutePath.Contains("cancel", StringComparison.OrdinalIgnoreCase), "Payment was cancelled in the browser.");
        _output.WriteLine($"Buyer returned: {callbackUri}");

        var captured = await paypal.Orders.CaptureAsync(created.Id, payPal_Request_Id: Guid.NewGuid().ToString("N"));
        _output.WriteLine($"Captured order {captured.Id} ({captured.Status}).");
        Assert.Equal("COMPLETED", captured.Status, ignoreCase: true);

        var captureId = (string?)JObject.FromObject(captured).SelectToken("purchase_units[0].payments.captures[0].id");
        Assert.False(string.IsNullOrEmpty(captureId), "Could not find a capture id in the capture response.");
        _output.WriteLine($"Capture id: {captureId}");

        var refund = await paypal.Payments.CapturesRefundAsync(captureId!, payPal_Request_Id: Guid.NewGuid().ToString("N"));
        _output.WriteLine($"Refund {refund.Id} ({refund.Status}).");
        Assert.False(string.IsNullOrEmpty(refund.Id));

        log.Dump(_output);
    }

    [SkippableFact]
    [TestPriority(2)]
    public async Task Create_a_subscription_and_activate_it()
    {
        Skip.IfNot(InteractiveEnabled,
            "Interactive test. Set user-secret PayPal:RunInteractive=true (and PayPal creds) to run.");

        var log = new Recorder();
        var paypal = BuildRecordingClient(log);
        using var callback = new LocalCallbackServer();

        // 1. A catalog product.
        var product = await paypal.CatalogProducts.CreateAsync(
            body: new Cat.Product_request_POST
            {
                Name = "Aero Test Product",
                Type = PayPalProductType.Digital,
                Category = "SOFTWARE",
                Description = "AeroPayPalSharp interactive subscription product",
            },
            payPal_Request_Id: Guid.NewGuid().ToString("N"));
        _output.WriteLine($"Product {product.Id}");
        Assert.False(string.IsNullOrEmpty(product.Id));

        // 2. A monthly plan priced at 9.99.
        var plan = await paypal.Subscriptions.PlansCreateAsync(
            body: new Subs.Plan_request_POST
            {
                Product_id = product.Id,
                Name = "Aero Test Plan",
                Description = "Monthly test plan",
                Billing_cycles = new Subs.Billing_cycle_list
                {
                    new Subs.Billing_cycle
                    {
                        Tenure_type = "REGULAR",
                        Sequence = 1,
                        Total_cycles = 0,
                        Frequency = new Subs.Frequency { Interval_unit = "MONTH", Interval_count = 1 },
                        Pricing_scheme = new Subs.Pricing_scheme
                        {
                            Fixed_price = new Subs.Money { Currency_code = PayPalCurrency.Usd, Value = "9.99" },
                        },
                    },
                },
                Payment_preferences = new Subs.Payment_preferences
                {
                    Auto_bill_outstanding = true,
                    Setup_fee_failure_action = "CONTINUE",
                    Payment_failure_threshold = 3,
                },
            },
            payPal_Request_Id: Guid.NewGuid().ToString("N"));
        _output.WriteLine($"Plan {plan.Id}");
        Assert.False(string.IsNullOrEmpty(plan.Id));

        // 3. A subscription against the plan.
        var subscription = await paypal.Subscriptions.CreateAsync(
            body: new Subs.Subscription_request_post
            {
                Plan_id = plan.Id,
                Application_context = new Subs.Application_context
                {
                    Brand_name = "AeroPayPalSharp",
                    User_action = "SUBSCRIBE_NOW",
                    Return_url = new Uri(callback.ReturnUrl),
                    Cancel_url = new Uri(callback.CancelUrl),
                },
            },
            payPal_Request_Id: Guid.NewGuid().ToString("N"));
        _output.WriteLine($"Subscription {subscription.Id} ({subscription.Status})");
        Assert.False(string.IsNullOrEmpty(subscription.Id));

        var approveUrl = log.FindLink("approve", "subscriber-action");
        Assert.False(string.IsNullOrEmpty(approveUrl), "No approve link on the subscription.");
        _output.WriteLine($"Opening browser to approve subscription:\n{approveUrl}\nLog in with your SANDBOX BUYER and confirm...");
        OpenBrowser(approveUrl!);

        var callbackUri = await callback.WaitForCallbackAsync(TimeSpan.FromMinutes(5));
        Skip.If(callbackUri is null, "Timed out waiting for approval in the browser.");
        Skip.If(callbackUri!.AbsolutePath.Contains("cancel", StringComparison.OrdinalIgnoreCase), "Subscription approval was cancelled.");
        _output.WriteLine($"Buyer returned: {callbackUri}");

        // 4. It should activate shortly after approval; poll briefly.
        Subs.Subscription active = subscription;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            active = await paypal.Subscriptions.GetAsync(subscription.Id);
            _output.WriteLine($"Subscription status: {active.Status}");
            if (string.Equals(active.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            await Task.Delay(2000);
        }

        Assert.Equal("ACTIVE", active.Status, ignoreCase: true);
        log.Dump(_output);
    }

    [SkippableFact]
    [TestPriority(3)]
    public async Task Onboard_a_seller_via_partner_referral()
    {
        Skip.IfNot(InteractiveEnabled,
            "Interactive test. Set user-secret PayPal:RunInteractive=true (and PayPal creds) to run.");

        var log = new Recorder();
        var paypal = BuildRecordingClient(log);
        using var callback = new LocalCallbackServer();

        var trackingId = "aero-" + Guid.NewGuid().ToString("N")[..12];
        try
        {
            await paypal.PartnerReferralsV2.CreateAsync(new Refs.Referral_data
            {
                Tracking_id = trackingId,
                Partner_config_override = new Refs.Partner_config_override
                {
                    Return_url = new Uri(callback.ReturnUrl),
                    Return_url_description = "Return to the AeroPayPalSharp onboarding test",
                },
                Operations = new Refs.Operation_list
                {
                    new Refs.Operation
                    {
                        Operation1 = "API_INTEGRATION",
                        Api_integration_preference = new Refs.Integration_details
                        {
                            Rest_api_integration = new Refs.Rest_api_integration
                            {
                                Integration_method = "PAYPAL",
                                Integration_type = "THIRD_PARTY",
                                Third_party_details = new Refs.Third_party_details
                                {
                                    Features = new Refs.Rest_api_integration_rest_endpoint_features_enum_list { "PAYMENT", "REFUND" },
                                },
                            },
                        },
                    },
                },
                Products = new Refs.Product_list { "EXPRESS_CHECKOUT" },
                Legal_consents = new Refs.Legal_consent_list
                {
                    new Refs.Legal_consent { Type = "SHARE_DATA_CONSENT", Granted = true },
                },
            });
        }
        catch (PayPalApiException ex)
        {
            Skip.If(true, $"Partner referral could not be created (PayPal {ex.StatusCode}). This account may not be a platform/partner, or the request needs adjusting. Detail: {ex.Message}");
            return;
        }
        _output.WriteLine($"Partner referral created (tracking_id {trackingId}).");

        var actionUrl = log.FindLink("action_url");
        Assert.False(string.IsNullOrEmpty(actionUrl), "No action_url link on the referral response.");
        var partnerId = _fx.Data("MerchantId");
        Skip.If(string.IsNullOrWhiteSpace(partnerId),
            "Set user-secret PayPal:MerchantId (your platform/partner account merchant id) so onboarding completion can be polled.");

        callback.BeginCapture(); // in case PayPal honors partner_config_override.return_url and redirects back
        _output.WriteLine($"Opening browser to onboard a seller:\n{actionUrl}\nSign up / log in as a SANDBOX SELLER to complete onboarding...");
        OpenBrowser(actionUrl!);

        // Try to detect completion via BOTH the return redirect (if PayPal honors the override) and the
        // merchant-integration status poll, for a SHORT window so this never blocks the suite. Note: for
        // many partner accounts neither fires (the redirect is unreliable and the status endpoint 401s),
        // in which case use the MERCHANT.ONBOARDING.COMPLETED webhook - this test then skips.
        _output.WriteLine($"Briefly watching for the return redirect AND polling merchant-integration status (tracking_id {trackingId})...");
        Uri? redirect = null;
        var polled = false;
        for (var attempt = 0; attempt < 12 && redirect is null && !polled; attempt++) // up to ~60 seconds
        {
            if (callback.Received is not null)
            {
                redirect = callback.Received;
                break;
            }
            try
            {
                await paypal.PartnerReferralsV1.MerchantIntegrationFindAsync(partnerId!, trackingId);
                polled = true;
                break;
            }
            catch (PayPalApiException ex)
            {
                // Best-effort: 404 = not onboarded yet; 401/403 = this partner_id likely is not the right
                // one for the status endpoint. Either way, keep watching for the redirect. Log once.
                if (attempt == 0)
                {
                    _output.WriteLine($"(status poll unavailable: PayPal {ex.StatusCode}; relying on the return redirect. In production use the MERCHANT.ONBOARDING.COMPLETED webhook.)");
                }
            }
            await Task.Delay(5000);
        }
        redirect ??= callback.Received; // catch a redirect that landed during the final poll

        Skip.If(redirect is null && !polled,
            "Referral created and browser opened, but completion was not detected by redirect or status poll. " +
            "For this account use the MERCHANT.ONBOARDING.COMPLETED webhook to confirm onboarding (the redirect is unreliable and the status endpoint returned 401).");
        if (redirect is not null)
        {
            _output.WriteLine($"Detected via RETURN REDIRECT (the override worked): {redirect}");
        }
        if (polled)
        {
            _output.WriteLine("Detected via merchant-integration STATUS POLL.");
        }
        log.Dump(_output);
    }

    // A fully-loaded order request: line items, an amount breakdown that sums, shipping, and buyer-facing
    // fields, so the create exercises as much of the model as possible (not just amount + description).
    private static Order_request EnrichedOrder(LocalCallbackServer callback)
    {
        // 2x 5.00 + 1x 3.00 = 13.00 items; tax 1.30; shipping 2.00; handling 0.50; discount 1.00 => 15.80
        return new Order_request
        {
            Intent = PayPalIntent.Capture,
            Purchase_units = new List<Purchase_units>
            {
                new Purchase_units
                {
                    Reference_id = "unit-1",
                    Description = "AeroPayPalSharp interactive test order",
                    Custom_id = "aeropaypalsharp-custom-1",
                    Invoice_id = "aeropaypalsharp-" + Guid.NewGuid().ToString("N")[..12],
                    Soft_descriptor = "AEROTEST",
                    Items = new List<Items>
                    {
                        new Items
                        {
                            Name = "Aero Widget",
                            Description = "A well-tested widget",
                            Sku = "AERO-WIDGET",
                            Quantity = "2",
                            Category = "DIGITAL_GOODS",
                            Unit_amount = new Unit_amount { Currency_code = PayPalCurrency.Usd, Value = "5.00" },
                            Tax = new Tax { Currency_code = PayPalCurrency.Usd, Value = "0.50" },
                        },
                        new Items
                        {
                            Name = "Aero Gadget",
                            Description = "A single gadget",
                            Sku = "AERO-GADGET",
                            Quantity = "1",
                            Category = "DIGITAL_GOODS",
                            Unit_amount = new Unit_amount { Currency_code = PayPalCurrency.Usd, Value = "3.00" },
                            Tax = new Tax { Currency_code = PayPalCurrency.Usd, Value = "0.30" },
                        },
                    },
                    Amount = new Amount3
                    {
                        Currency_code = PayPalCurrency.Usd,
                        Value = "15.80",
                        Breakdown = new Amount_breakdown
                        {
                            Item_total = new Item_total { Currency_code = PayPalCurrency.Usd, Value = "13.00" },
                            Tax_total = new Tax_total { Currency_code = PayPalCurrency.Usd, Value = "1.30" },
                            Shipping = new Shipping { Currency_code = PayPalCurrency.Usd, Value = "2.00" },
                            Handling = new Handling { Currency_code = PayPalCurrency.Usd, Value = "0.50" },
                            Discount = new Discount { Currency_code = PayPalCurrency.Usd, Value = "1.00" },
                        },
                    },
                    Shipping = new Shipping2
                    {
                        Name = new Name3 { Full_name = "John Doe" },
                        Address = new Address2
                        {
                            Address_line_1 = "1 Main St",
                            Admin_area_2 = "San Jose",
                            Admin_area_1 = "CA",
                            Postal_code = "95131",
                            Country_code = "US",
                        },
                    },
                },
            },
            Application_context = new Application_context
            {
                Brand_name = "AeroPayPalSharp",
                Locale = "en-US",
                Return_url = new Uri(callback.ReturnUrl),
                Cancel_url = new Uri(callback.CancelUrl),
                User_action = PayPalUserAction.PayNow,
            },
        };
    }

    // ---- shared infrastructure ----

    private IPayPalApiClient BuildRecordingClient(Recorder recorder)
    {
        var services = new ServiceCollection();
        services.AddPayPalSharp(_fx.Configuration);
        services.ConfigureHttpClientDefaults(b => b.AddHttpMessageHandler(() => new RecordingHandler(recorder)));
        return services.BuildServiceProvider().GetRequiredService<IPayPalApiClient>();
    }

    private static void OpenBrowser(string url)
    {
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch { /* URL is already printed for the user to open manually */ }
    }

    private sealed record Recorded(string Method, string Url, int Status, string RequestBody, string ResponseBody);

    /// <summary>Thread-safe store of every request/response, with helpers to find links and dump the log.</summary>
    private sealed class Recorder
    {
        private readonly List<Recorded> _records = new();
        private readonly object _gate = new();

        public void Add(Recorded record)
        {
            lock (_gate) { _records.Add(record); }
        }

        /// <summary>Most recent response link whose rel matches any of <paramref name="rels"/>.</summary>
        public string? FindLink(params string[] rels)
        {
            lock (_gate)
            {
                for (var i = _records.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrWhiteSpace(_records[i].ResponseBody))
                    {
                        continue;
                    }
                    JToken root;
                    try { root = JToken.Parse(_records[i].ResponseBody); } catch { continue; }
                    if (root["links"] is not JArray links)
                    {
                        continue;
                    }
                    foreach (var link in links)
                    {
                        var rel = (string?)link["rel"];
                        if (rel is not null && rels.Contains(rel, StringComparer.OrdinalIgnoreCase))
                        {
                            return (string?)link["href"];
                        }
                    }
                }
            }
            return null;
        }

        public void Dump(ITestOutputHelper output)
        {
            lock (_gate)
            {
                output.WriteLine("\n================ FULL REQUEST/RESPONSE LOG ================");
                foreach (var r in _records)
                {
                    output.WriteLine($"\n>>> {r.Method} {r.Url}\n{Pretty(r.RequestBody)}\n<<< {r.Status}\n{Pretty(r.ResponseBody)}");
                }
            }
        }

        private static string Pretty(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return "(empty)";
            }
            try { return JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.Indented); } catch { return json; }
        }
    }

    /// <summary>Logs (and makes re-readable) every request/response body flowing through the client.</summary>
    private sealed class RecordingHandler : DelegatingHandler
    {
        private readonly Recorder _recorder;

        public RecordingHandler(Recorder recorder) => _recorder = recorder;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            var response = await base.SendAsync(request, cancellationToken);

            var bytes = response.Content is null ? Array.Empty<byte>() : await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (response.Content is not null)
            {
                var replacement = new ByteArrayContent(bytes);
                foreach (var header in response.Content.Headers)
                {
                    replacement.Headers.TryAddWithoutValidation(header.Key, string.Join(",", header.Value));
                }
                response.Content = replacement;
            }

            _recorder.Add(new Recorded(
                request.Method.ToString(), request.RequestUri?.ToString() ?? string.Empty,
                (int)response.StatusCode, requestBody, Encoding.UTF8.GetString(bytes)));
            return response;
        }
    }

    /// <summary>A throwaway localhost HTTP listener that catches PayPal's post-approval redirect.</summary>
    private sealed class LocalCallbackServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly int _port;

        public LocalCallbackServer()
        {
            _port = FreePort();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
        }

        public string ReturnUrl => $"http://localhost:{_port}/return";
        public string CancelUrl => $"http://localhost:{_port}/cancel";

        /// <summary>Set once a redirect is received (used by flows that poll for completion in parallel).</summary>
        public Uri? Received { get; private set; }

        /// <summary>Starts listening in the background; stashes the first redirect in <see cref="Received"/>.</summary>
        public void BeginCapture()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    Received = context.Request.Url;
                    Respond(context);
                }
                catch
                {
                    // listener stopped / disposed
                }
            });
        }

        public async Task<Uri?> WaitForCallbackAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(cts.Token);
                Received = context.Request.Url;
                Respond(context);
                return context.Request.Url;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private static void Respond(HttpListenerContext context)
        {
            var body = Encoding.UTF8.GetBytes(
                "<html><body style='font-family:sans-serif'><h2>Done. You can close this tab and return to the test.</h2></body></html>");
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(body, 0, body.Length);
            context.Response.Close();
        }

        private static int FreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        public void Dispose()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
            ((IDisposable)_listener).Dispose();
        }
    }
}
