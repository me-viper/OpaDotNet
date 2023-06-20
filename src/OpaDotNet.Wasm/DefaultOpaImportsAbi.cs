namespace OpaDotNet.Wasm;

public class DefaultOpaImportsAbi : IOpaImportsAbi
{
    protected ILogger Logger { get; }

    public DefaultOpaImportsAbi(ILogger<DefaultOpaImportsAbi>? logger = null)
    {
        Logger = logger ?? NullLogger<DefaultOpaImportsAbi>.Instance;
    }

    public virtual void Abort(string message)
    {
        throw new OpaEvaluationException("Aborted: " + message);
    }

    public virtual void PrintLn(string message)
    {
    }

    public virtual object Func(BuiltinContext context)
    {
        throw new NotImplementedException(context.FunctionName);
    }

    public virtual object Func(BuiltinContext context, BuiltinArg arg1)
    {
        throw new NotImplementedException(context.FunctionName);
    }

    public virtual object Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2)
    {
        throw new NotImplementedException(context.FunctionName);
    }

    public virtual object Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
    {
        throw new NotImplementedException(context.FunctionName);
    }

    public virtual object Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
    {
        throw new NotImplementedException(context.FunctionName);
    }
}