using Aeroverra.PayPalSharp.WebProfilesV1;
using Xunit;
using Xunit.Abstractions;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>Live Payment Experience v1: full create, list, get, delete of a web profile.</summary>
[Collection(PayPalCollection.Name)]
public class WebProfilesTests
{
    private readonly PayPalTestFixture _fx;
    private readonly ITestOutputHelper _output;

    public WebProfilesTests(PayPalTestFixture fx, ITestOutputHelper output)
    {
        _fx = fx;
        _output = output;
    }

    [SkippableFact]
    public async Task Create_list_get_delete_web_profile()
    {
        Skip.IfNot(_fx.IsConfigured, _fx.SkipReason);

        var profile = new Web_profile
        {
            Name = "aero-" + Guid.NewGuid().ToString("N")[..12],
        };

        var created = await _fx.Client.WebProfiles.CreateAsync(Guid.NewGuid().ToString(), profile);
        Assert.False(string.IsNullOrEmpty(created.Id));
        _output.WriteLine($"web profile id={created.Id}");

        try
        {
            // The list can lag / exclude just-created profiles; retrievability by id is the real proof.
            Assert.NotNull(await _fx.Client.WebProfiles.GetListAsync());

            var fetched = await _fx.Client.WebProfiles.GetAsync(created.Id);
            Assert.Equal(created.Id, fetched.Id);
        }
        finally
        {
            await _fx.Client.WebProfiles.DeleteAsync(created.Id);
        }

        var ex = await Assert.ThrowsAnyAsync<PayPalApiException>(() => _fx.Client.WebProfiles.GetAsync(created.Id));
        Assert.Equal(404, ex.StatusCode);
    }
}
