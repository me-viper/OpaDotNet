global using Xunit;
global using Xunit.Abstractions;

global using JetBrains.Annotations;

#if DISABLEPARALLELIZATION
[assembly: CollectionBehavior(DisableTestParallelization = true)]
#endif