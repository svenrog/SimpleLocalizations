using BenchmarkDotNet.Running;
using SimpleLocalizations.Benchmarks;

// Run every benchmark, or one: `dotnet run -c Release -- --filter *Runtime*`.
BenchmarkSwitcher.FromAssembly(typeof(RuntimeBenchmarks).Assembly).Run(args);
