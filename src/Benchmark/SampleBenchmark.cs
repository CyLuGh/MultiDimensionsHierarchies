using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using SampleGenerator;
using System;

namespace Benchmark;

[SimpleJob( RuntimeMoniker.Net70 , iterationCount: 10 )]
[MemoryDiagnoser]
public class SampleBenchmark
{
    private Seq<Skeleton> _targets;
    private Seq<Skeleton<int>> _samples;

    [Params( 5_000 , 25_000 , 50_000 , 100_000 )] public int SampleSize { get; set; }

    [Params( 3 , 4 , 5 , 6 )] public int DimensionsCount { get; set; }

    [Params( 1000 , 10_000 , 50_000 )] public int TargetsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var generator = new Generator( SampleSize , DimensionsCount );

        _samples = generator.Skeletons.Strict();
        _targets = generator.GenerateTargets( TargetsCount ).Strict();
    }

    [Benchmark]
    public AggregationResult<int> BenchTopDownGroup() =>
        Aggregator.Aggregate( Method.TopDownGroup , _samples , ( a , b ) => a + b , _targets );
}