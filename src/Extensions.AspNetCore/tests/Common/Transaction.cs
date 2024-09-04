namespace OpaDotNet.Extensions.AspNetCore.Tests.Common;

internal class Transaction
{
    public HttpRequestMessage? Request { get; set; }
    public HttpResponseMessage? Response { get; set; }
    public string? ResponseText { get; set; }
}