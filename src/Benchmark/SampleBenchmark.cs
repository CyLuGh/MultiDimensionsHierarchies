using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using SampleGenerator;
using System;
using System.Collections.Generic;

namespace Benchmark;

[SimpleJob( RuntimeMoniker.Net70 , iterationCount: 5 , warmupCount: 2 )]
[MemoryDiagnoser( false )]
[Orderer( SummaryOrderPolicy.FastestToSlowest )]
[RankColumn]
public class SampleBenchmark
{
    [Benchmark]
    [ArgumentsSource( nameof( BenchmarkArguments ) )]
    public AggregationResult<int> BenchTopDownGroup( DataArgument argument ) =>
        Aggregator.Aggregate( Method.TopDownGroup , argument.Samples , ( a , b ) => a + b , argument.Targets );

    public static IEnumerable<DataArgument> BenchmarkArguments()
    {
        /* Test dimensions increase */
        yield return new DataArgument( 50_000 , 3 , 10_000 );
        yield return new DataArgument( 50_000 , 4 , 10_000 );
        yield return new DataArgument( 50_000 , 5 , 10_000 );
        yield return new DataArgument( 50_000 , 6 , 10_000 );

        /* Test sample increase */
        yield return new DataArgument( 100_000 , 6 , 10_000 );
        yield return new DataArgument( 200_000 , 6 , 10_000 );
        yield return new DataArgument( 300_000 , 6 , 10_000 );

        /* Test targets increase */
        yield return new DataArgument( 100_000 , 6 , 20_000 );
        yield return new DataArgument( 100_000 , 6 , 50_000 );
    }
}

public class DataArgument : IDisposable
{
    public int SampleSize { get; }
    public int TargetsCount { get; }
    public int DimensionsCount { get; }

    public Seq<Skeleton> Targets { get; }
    public Seq<Skeleton<int>> Samples { get; }

    public DataArgument( int sampleSize , int dimensionsCount , int targetsCount )
    {
        this.DimensionsCount = dimensionsCount;
        this.TargetsCount = targetsCount;
        this.SampleSize = sampleSize;

        var generator = new Generator( sampleSize , dimensionsCount );
        Samples = generator.Skeletons.Strict();
        Targets = generator.GenerateTargets( targetsCount ).Strict();
    }

    public void Dispose()
    {
        GC.SuppressFinalize( this );
    }

    public override string ToString()
        => $"D {DimensionsCount} S {SampleSize} T {TargetsCount}";
}