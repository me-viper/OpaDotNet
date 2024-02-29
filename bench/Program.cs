using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

// var p = new CallPerf();
//
// for (var i = 0; i < 100; i++)
// {
//     Console.WriteLine(p.DirectCall());
//     Console.WriteLine(p.DelegateCall());
//     Console.WriteLine(p.ReflectionCall());
//     Console.WriteLine(p.ExpressionTreeCall());
// }