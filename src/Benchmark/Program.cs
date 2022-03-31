using Benchmark;
using BenchmarkDotNet.Running;

var summary = BenchmarkRunner.Run<HeuristicAggregate>();
summary = BenchmarkRunner.Run<TargetedAggregate>();