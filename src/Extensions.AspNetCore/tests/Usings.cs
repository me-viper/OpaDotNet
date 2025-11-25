global using Xunit;

global using JetBrains.Annotations;

#if DISABLEPARALLELIZATION
[assembly: CollectionBehavior(DisableTestParallelization = true)]
#endif