﻿namespace OpaDotNet.Extensions.AspNetCore.Tests.Common;

// internal class TestBuiltinsFactory(ILoggerFactory? loggerFactory = null, TimeProvider? timeProvider = null) : IBuiltinsFactory
// {
//     private ILogger<CoreImportsAbi> Logger { get; } = loggerFactory == null
//         ? NullLogger<CoreImportsAbi>.Instance
//         : loggerFactory.CreateLogger<CoreImportsAbi>();
//
//     public IReadOnlyList<Func<IOpaCustomBuiltins>> CustomBuiltins { get; init; } = [];
//
//     public IOpaImportsAbi Create()
//     {
//         return new CompositeImportsHandler(
//             new CoreImportsAbi(Logger, timeProvider ?? TimeProvider.System),
//             CustomBuiltins.Select(p => p()).ToList(),
//             new ImportsCache()
//             );
//     }
// }