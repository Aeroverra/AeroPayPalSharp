using Xunit;
using Xunit.Abstractions;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// Live read-only smoke tests across the reporting-style clients: they should respond
/// with a well-formed (possibly empty) payload. Transaction Search needs the feature
/// enabled on the app, so those skip on the transport error rather than fail.
/// </summary>
[Collection(PayPalCollection.Name)]
public class ReadTests
{
    private readonly PayPalTestFixture _fx;
    private readonly ITestOutputHelper _output;

    public ReadTests(PayPalTestFixture fx, ITestOutputHelper output)
    {
        _fx = fx;
        _output = output;
    }

    [SkippableFact]
    public async Task Invoices_generate_next_number()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);
        var next = await _fx.Client.Invoices.InvoicingGenerateNextInvoiceNumberAsync();
        Assert.False(string.IsNullOrWhiteSpace(next.Invoice_number1));
        _output.WriteLine($"next invoice number: {next.Invoice_number1}");
    }

    [SkippableFact]
    public async Task Invoices_list_responds()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);
        Assert.NotNull(await _fx.Client.Invoices.ListAsync());
    }

    [SkippableFact]
    public async Task Disputes_list_responds()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);
        Assert.NotNull(await _fx.Client.Disputes.ListAsync());
    }

    [SkippableFact]
    public async Task Subscription_plans_list_responds()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);
        Assert.NotNull(await _fx.Client.Subscriptions.PlansListAsync());
    }

    [SkippableFact]
    public async Task Transaction_search_balances_responds()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);
        try
        {
            var balances = await _fx.Client.TransactionSearch.BalancesGetAsync();
            Assert.NotNull(balances);
        }
        catch (Aeroverra.PayPalSharp.TransactionSearchV1.PayPalApiException ex)
        {
            // Transaction Search must be enabled on the REST app; otherwise PayPal 403s.
            Skip.If(true, $"Transaction Search not enabled for this app (PayPal {ex.StatusCode})");
        }
    }
}
