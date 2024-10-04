global using Xunit;
global using Xunit.Abstractions;

global using JetBrains.Annotations;

global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;

#if DISABLEPARALLELIZATION
[assembly: CollectionBehavior(DisableTestParallelization = true)]
#endif