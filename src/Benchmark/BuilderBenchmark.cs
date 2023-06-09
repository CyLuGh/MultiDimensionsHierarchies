using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using LanguageExt;
using MultiDimensionsHierarchies.Core;
using SampleGenerator;
using System.Collections.Generic;
using System.Linq;

namespace Benchmark;

[SimpleJob( RuntimeMoniker.Net70 , iterationCount: 5 , warmupCount: 2 )]
[MemoryDiagnoser( false )]
public class BuilderBenchmark
{
    [Benchmark]
    [ArgumentsSource( nameof( BenchmarkScaleArguments ) )]
    public Seq<Skeleton<int>> FastBuild( GeneratorConfig config )
    {
        return SkeletonFactory.FastBuild( config.Samples , ( o , s ) => o.Get( s ) , o => o.Value , config.Dimensions );
    }

    [Benchmark]
    [ArgumentsSource( nameof( BenchmarkScaleArguments ) )]
    public Seq<Skeleton<int>> FastBuildAggreg( GeneratorConfig config )
    {
        return SkeletonFactory.FastBuild( config.Samples , ( o , s ) => o.Get( s ) , o => o.Value , config.Dimensions , g => g.Sum() );
    }

    [Benchmark]
    [ArgumentsSource( nameof( BenchmarkScaleArguments ) )]
    public Seq<Skeleton<int>> FastBuildCheck( GeneratorConfig config )
    {
        return SkeletonFactory.FastBuild( config.Samples , ( o , s ) => o.Get( s ) , o => o.Value , config.Dimensions , checkTargets: config.Targets );
    }

    [Benchmark]
    [ArgumentsSource( nameof( BenchmarkScaleArguments ) )]
    public Seq<Skeleton<int>> FastBuildBoth( GeneratorConfig config )
    {
        return SkeletonFactory.FastBuild( config.Samples , ( o , s ) => o.Get( s ) , o => o.Value , config.Dimensions , g => g.Sum() , config.Targets );
    }

    public static IEnumerable<GeneratorConfig> BenchmarkScaleArguments()
    {
        //yield return new GeneratorConfig( 10_000 , 2000 , 4 , DimensionIdentifier.Cooking );
        //yield return new GeneratorConfig( 10_000 , 5 , DimensionIdentifier.Cooking );
        //yield return new GeneratorConfig( 10_000 , 9 , DimensionIdentifier.Cooking );

        //yield return new GeneratorConfig( 10_000 , 2000 , 4 , DimensionIdentifier.Countries );
        //yield return new GeneratorConfig( 10_000 , 5 , DimensionIdentifier.Countries );
        //yield return new GeneratorConfig( 10_000 , 9 , DimensionIdentifier.Countries );

        //yield return new GeneratorConfig( 100_000 , 2000 , 5 , DimensionIdentifier.Countries );
        //yield return new GeneratorConfig( 1_000_000 , 2000 , 5 , DimensionIdentifier.Countries );
        yield return new GeneratorConfig( 5_000_000 , 2000 , 4 , DimensionIdentifier.Countries );
        yield return new GeneratorConfig( 5_000_000 , 2000 , 5 , DimensionIdentifier.Countries );
        yield return new GeneratorConfig( 5_000_000 , 2000 , 6 , DimensionIdentifier.Countries );
    }
}

public record GeneratorConfig( int SampleSize , int TargetsCount , int DimensionsCount , Option<DimensionIdentifier> DimensionIdentifier , Seq<Dimension> Dimensions , Seq<ISample> Samples , Seq<Skeleton> Targets )
{
    public GeneratorConfig( int sampleSize , int dimensionsCount , Option<DimensionIdentifier> dimensionIdentifier )
        : this( sampleSize , 0 , dimensionsCount , dimensionIdentifier ) { }

    public GeneratorConfig( int sampleSize , int targetsCount , int dimensionsCount , Option<DimensionIdentifier> dimensionIdentifier )
        : this( sampleSize , targetsCount , dimensionsCount , dimensionIdentifier , default , default , default )
    {
        var generator = dimensionIdentifier.Match( o => new Generator( sampleSize , o , dimensionsCount ) , () => new Generator( sampleSize , dimensionsCount ) );
        Dimensions = generator.Dimensions;
        Samples = generator.Samples;
        Targets = generator.GenerateTargets( targetsCount ).Strict();
    }

    public override string ToString()
       => $"D{DimensionsCount} S{SampleSize} X{DimensionIdentifier.Match( o => (int) o , () => -1 )}";
}