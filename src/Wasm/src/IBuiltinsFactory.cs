namespace OpaDotNet.Wasm;

public interface IBuiltinsFactory
{
    IOpaImportsAbi Create();
}