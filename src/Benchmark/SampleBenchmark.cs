using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using SampleGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmark;

[SimpleJob( RuntimeMoniker.Net70 , iterationCount: 5 , warmupCount: 2 )]
[MemoryDiagnoser( false )]
public class SampleBenchmark
{
    [Benchmark]
    [ArgumentsSource( nameof( BenchmarkDownTop ) )]
    public AggregationResult<int> BenchDownTopGroup( DataArgument argument ) =>
        Aggregator.Aggregate( argument.Samples , ds => ds.Sum() );

    [Benchmark]
    [ArgumentsSource( nameof( BenchmarkDownTop ) )]
    public AggregationResult<int> BenchDownTopMap( DataArgument argument ) =>
        Aggregator.Aggregate( argument.Samples , ( a , b ) => a + b );

    [Benchmark]
    [ArgumentsSource( nameof( BenchmarkScaleArguments ) )]
    public AggregationResult<int> BenchTopDown( DataArgument argument ) =>
        Aggregator.Aggregate( argument.Samples , argument.Targets , ( a , b ) => a + b , ds => ds.Sum() );

    public static IEnumerable<DataArgument> BenchmarkScaleArguments()
    {
        yield return new DataArgument( 10_000 , 4 , 10_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 20_000 , 4 , 10_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 10_000 , 4 , 20_000 , DimensionIdentifier.Cooking );

        yield return new DataArgument( 10_000 , 5 , 10_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 20_000 , 5 , 10_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 10_000 , 5 , 20_000 , DimensionIdentifier.Cooking );

        yield return new DataArgument( 10_000 , 6 , 10_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 20_000 , 6 , 10_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 10_000 , 6 , 20_000 , DimensionIdentifier.Cooking );

        yield return new DataArgument( 10_000 , 4 , 10_000 , DimensionIdentifier.Countries );
        yield return new DataArgument( 20_000 , 4 , 10_000 , DimensionIdentifier.Countries );
        yield return new DataArgument( 10_000 , 4 , 20_000 , DimensionIdentifier.Countries );

        yield return new DataArgument( 10_000 , 5 , 10_000 , DimensionIdentifier.Countries );
        yield return new DataArgument( 20_000 , 5 , 10_000 , DimensionIdentifier.Countries );
        yield return new DataArgument( 10_000 , 5 , 20_000 , DimensionIdentifier.Countries );

        yield return new DataArgument( 10_000 , 7 , 10_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 10_000 , 8 , 10_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 10_000 , 9 , 10_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 10_000 , 10 , 10_000 , DimensionIdentifier.Cooking );

        yield return new DataArgument( 100_000 , 6 , 100_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 200_000 , 6 , 100_000 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 100_000 , 6 , 200_000 , DimensionIdentifier.Cooking );
    }

    public static IEnumerable<DataArgument> BenchmarkDownTop()
    {
        yield return new DataArgument( 10_000 , 4 , 1 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 20_000 , 4 , 1 , DimensionIdentifier.Cooking );

        yield return new DataArgument( 10_000 , 5 , 1 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 20_000 , 5 , 1 , DimensionIdentifier.Cooking );

        yield return new DataArgument( 10_000 , 4 , 1 , DimensionIdentifier.Countries );
        yield return new DataArgument( 20_000 , 4 , 1 , DimensionIdentifier.Countries );

        yield return new DataArgument( 10_000 , 5 , 1 , DimensionIdentifier.Countries );
        yield return new DataArgument( 20_000 , 5 , 1 , DimensionIdentifier.Countries );

        yield return new DataArgument( 1_000 , 5 , 1 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 1_000 , 6 , 1 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 1_000 , 7 , 1 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 1_000 , 8 , 1 , DimensionIdentifier.Cooking );
        yield return new DataArgument( 1_000 , 9 , 1 , DimensionIdentifier.Cooking );
    }
}

public record DataArgument( int SampleSize , int TargetsCount , int DimensionsCount , Option<DimensionIdentifier> DimensionIdentifier , Seq<Skeleton> Targets , Seq<Skeleton<int>> Samples ) : IDisposable
{
    public DataArgument( int sampleSize , int dimensionsCount , int targetsCount , Option<DimensionIdentifier> dimensionIdentifier )
        : this( sampleSize , targetsCount , dimensionsCount , dimensionIdentifier , default , default )
    {
        var generator = dimensionIdentifier.Match( o => new Generator( sampleSize , o , dimensionsCount ) , () => new Generator( sampleSize , dimensionsCount ) );
        Samples = generator.Skeletons.Strict();
        Targets = generator.GenerateTargets( targetsCount ).Strict();
    }

    public void Dispose()
    {
        GC.SuppressFinalize( this );
    }

    public override string ToString()
        => $"D{DimensionsCount} S{SampleSize} T{TargetsCount} {DimensionIdentifier.Match( o => (int) o , () => -1 )}";
}