namespace Opa.WebApp.Infra;

internal class BasicAuthenticationEvents
{
    public Func<BasicAuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = _ => Task.CompletedTask;

    public Func<ValidateCredentialsContext, Task> OnValidateCredentials { get; set; } = _ => Task.CompletedTask;

    public Task AuthenticationFailed(BasicAuthenticationFailedContext context) => OnAuthenticationFailed(context);

    public Task ValidateCredentials(ValidateCredentialsContext context) => OnValidateCredentials(context);
}