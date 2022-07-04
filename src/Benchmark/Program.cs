using Benchmark;
using BenchmarkDotNet.Running;

var summary = BenchmarkRunner.Run<TargetedAggregate>();
//var summary = BenchmarkRunner.Run<TargetedAggregate>();

//var test = new AllMethodsAggregate();
//test.SampleSize = 10000;
//test.TargetsCount = 100;
//test.DimensionsCount = 4;
//test.GlobalSetup();
//test.Targeted();