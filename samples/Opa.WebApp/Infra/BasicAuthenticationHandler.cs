using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Opa.WebApp.Infra;

internal class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string BasicAuth = "Basic";
    
    private BasicAuthenticationEvents AuthEvents
    {
        get => (BasicAuthenticationEvents)base.Events!;
        set => base.Events = value;
    }
    
    protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new BasicAuthenticationEvents());
    
    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, 
        ILoggerFactory logger, 
        UrlEncoder encoder, 
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return AuthenticateResult.Fail("No credential");
        
        if (string.Equals(BasicAuth, authorizationHeader, StringComparison.Ordinal))
            return AuthenticateResult.Fail("No credentials");
        
        if (!authorizationHeader.StartsWith(BasicAuth + ' ', StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Invalid authentication scheme");
        
        var encodedCredentials = authorizationHeader[BasicAuth.Length..].Trim();

        try
        {
            string decodedCredentials;
            byte[] base64DecodedCredentials;
            
            try
            {
                base64DecodedCredentials = Convert.FromBase64String(encodedCredentials);
            }
            catch (FormatException)
            {
                const string failedToDecodeCredentials = "Cannot convert credentials from Base64.";
                Logger.LogInformation(failedToDecodeCredentials);
                return AuthenticateResult.Fail(failedToDecodeCredentials);
            }

            try
            {
                decodedCredentials = Encoding.UTF8.GetString(base64DecodedCredentials);
            }
            catch (Exception ex)
            {
                const string failedToDecodeCredentials = "Cannot build credentials from decoded base64 value, exception {ex.Message} encountered.";
                Logger.LogInformation(failedToDecodeCredentials, ex.Message);
                return AuthenticateResult.Fail(ex.Message);
            }

            var delimiterIndex = decodedCredentials.IndexOf(":", StringComparison.OrdinalIgnoreCase);
            
            if (delimiterIndex == -1)
            {
                const string missingDelimiterMessage = "Invalid credentials";
                Logger.LogInformation(missingDelimiterMessage);
                return AuthenticateResult.Fail(missingDelimiterMessage);
            }

            var username = decodedCredentials[..delimiterIndex];
            var password = decodedCredentials[(delimiterIndex + 1)..];

            var validateCredentialsContext = new ValidateCredentialsContext(Context, Scheme, Options)
            {
                Username = username,
                Password = password
            };
            
            await AuthEvents.ValidateCredentials(validateCredentialsContext);
            
            if (validateCredentialsContext.Result.Succeeded)
            {
                if (validateCredentialsContext.Principal == null)
                    return AuthenticateResult.Fail("Invalid principal");
                
                var ticket = new AuthenticationTicket(validateCredentialsContext.Principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
            
            if (validateCredentialsContext.Result.Failure != null)
                return AuthenticateResult.Fail(validateCredentialsContext.Result.Failure);

            return AuthenticateResult.NoResult();
        }
        catch (Exception ex)
        {
            var authenticationFailedContext = new BasicAuthenticationFailedContext(Context, Scheme, Options)
            {
                Exception = ex
            };

            await AuthEvents.AuthenticationFailed(authenticationFailedContext).ConfigureAwait(true);

            return authenticationFailedContext.Result;
        }
    }
    
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}