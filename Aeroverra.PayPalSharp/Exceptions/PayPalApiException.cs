namespace Aeroverra.PayPalSharp;

/// <summary>
/// Thrown when a PayPal API call returns a non-success status. This is ONE shared type across every
/// sub-client (Orders, Payments, Webhooks, ...), so a single <c>catch (PayPalApiException)</c> handles
/// an error from any of them. <see cref="StatusCode"/> is the HTTP status and <see cref="Response"/> is
/// the raw error body. The generated clients throw the generic <see cref="PayPalApiException{TResult}"/>
/// whose <c>Result</c> holds the deserialized error model; it derives from this type, so catching this
/// base catches both.
/// </summary>
public partial class PayPalApiException : System.Exception
{
    public int StatusCode { get; private set; }

    public string? Response { get; private set; }

    public System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IEnumerable<string>> Headers { get; private set; }

    public PayPalApiException(string message, int statusCode, string? response, System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IEnumerable<string>> headers, System.Exception? innerException)
        : base(message + "\n\nStatus: " + statusCode + "\nResponse: \n" + ((response == null) ? "(null)" : response.Substring(0, response.Length >= 512 ? 512 : response.Length)), innerException)
    {
        StatusCode = statusCode;
        Response = response;
        Headers = headers;
    }

    public override string ToString()
    {
        return string.Format("HTTP Response: \n\n{0}\n\n{1}", Response, base.ToString());
    }
}

/// <summary>A <see cref="PayPalApiException"/> that also carries the deserialized error body as <see cref="Result"/>.</summary>
public partial class PayPalApiException<TResult> : PayPalApiException
{
    public TResult Result { get; private set; }

    public PayPalApiException(string message, int statusCode, string? response, System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IEnumerable<string>> headers, TResult result, System.Exception? innerException)
        : base(message, statusCode, response, headers, innerException)
    {
        Result = result;
    }
}
