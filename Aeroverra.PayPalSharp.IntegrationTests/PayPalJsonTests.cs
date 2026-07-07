using System.Text.Json.Nodes;
using Aeroverra.PayPalSharp;
using Aeroverra.PayPalSharp.OrdersV2;
using Xunit;

namespace Aeroverra.PayPalSharp.IntegrationTests;

/// <summary>
/// PayPalJson serializes SDK models to PayPal's wire format (snake_case, money-as-string) and round-trips
/// faithfully, including fields the model does not name (via extension data). No network.
/// </summary>
public class PayPalJsonTests
{
    [Fact]
    public void Serialize_uses_wire_field_names_not_csharp_names()
    {
        var order = new Order { Id = "5O190127TN364715T", Status = "CREATED" };

        var json = JsonNode.Parse(PayPalJson.Serialize(order))!;

        Assert.Equal("5O190127TN364715T", (string)json["id"]!); // "id", not "Id"
        Assert.Equal("CREATED", (string)json["status"]!);
        Assert.Null(json["Id"]); // the C# name must not leak
    }

    [Fact]
    public void Round_trips_a_paypal_response_and_keeps_unmapped_fields()
    {
        // A response with a field the model does not explicitly declare; it must survive the round-trip.
        var raw = "{\"id\":\"ABC123\",\"status\":\"COMPLETED\",\"some_future_field\":\"keep-me\"}";

        var order = PayPalJson.Deserialize<Order>(raw)!;
        Assert.Equal("ABC123", order.Id);

        var json = JsonNode.Parse(PayPalJson.Serialize(order))!;
        Assert.Equal("ABC123", (string)json["id"]!);
        Assert.Equal("keep-me", (string)json["some_future_field"]!); // preserved via extension data
    }
}
