using System.Security.Claims;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;

namespace OpaDotNet.Extensions.AspNetCore;

internal class HttpRequestPolicyInput : IHttpRequestPolicyInput
{
    private readonly HttpRequest _request;

    public string Method
    {
        get => _request.Method;
    }

    public string Scheme
    {
        get => _request.Scheme;
    }

    public string Host
    {
        get => _request.Host.Value;
    }

    public string? PathBase
    {
        get => _request.PathBase.Value;
    }

    public string? Path
    {
        get => _request.Path.Value;
    }

    public string? QueryString
    {
        get => _request.QueryString.Value;
    }

    public IQueryCollection Query
    {
        get => _request.Query;
    }

    public string Protocol
    {
        get => _request.Protocol;
    }

    public IConnectionInput Connection
    {
        get => new ConnectionInput(_request.HttpContext.Connection);
    }

    public IDictionary<string, string?> Headers { get; }

    public IEnumerable<ClaimPolicyInput> Claims { get; }

    [UsedImplicitly]
    internal class ConnectionInput : IConnectionInput
    {
        private readonly ConnectionInfo _connection;

        public ConnectionInput(ConnectionInfo connection)
        {
            _connection = connection;
        }

        public string? RemoteIpAddress
        {
            get => _connection.RemoteIpAddress?.ToString();
        }

        public int RemotePort
        {
            get => _connection.RemotePort;
        }

        public string? LocalIpAddress
        {
            get => _connection.LocalIpAddress?.ToString();
        }

        public int LocalPort
        {
            get => _connection.LocalPort;
        }

        // public X509Certificate2? ClientCertificate
        // {
        //     get => _connection.ClientCertificate;
        // }
    }

    public HttpRequestPolicyInput(
        HttpRequest request,
        IReadOnlySet<string> includedHeaders,
        IEnumerable<Claim>? claims = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(includedHeaders);

        _request = request;

        Headers = request.Headers
            .Where(p => includedHeaders.Any(pp => Regex.IsMatch(p.Key, pp)))
            .ToDictionary(p => p.Key, p => (string?)p.Value);

        Claims = claims?.Select(p => new ClaimPolicyInput(p.Type, p.Value)) ?? Array.Empty<ClaimPolicyInput>();
    }
}