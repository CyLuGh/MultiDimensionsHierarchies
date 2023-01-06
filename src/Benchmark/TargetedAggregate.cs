using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using System;

using System.Linq;

namespace Benchmark
{
    [/*SimpleJob( RuntimeMoniker.Net472 , iterationCount: 3 , warmupCount: 1 ),*/
     SimpleJob( RuntimeMoniker.Net70 , iterationCount: 3 , warmupCount: 1 )]
    [MemoryDiagnoser]
    public class TargetedAggregate : AllMethodsAggregate
    {
        [Params( 1000 )]
        public int TargetsCount { get; set; }

        [Params( Method.TopDown , Method.TopDownGroup )]
        public Method Method { get; set; }

        public Skeleton[] Targets;

        public TargetedAggregate() : base()
        {
        }

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            //Console.WriteLine( "Building targets" );
            Targets = Dimensions.Take( DimensionsCount )
                .Select( d => d.Flatten().Where( b => b.Depth <= 3 ).ToArray() )
                .Combine()
                .AsParallel()
                .Take( TargetsCount )
                .ToArray();
        }

        [Benchmark]
        public AggregationResult<double> TopDown()
        {
            return Aggregator.Aggregate( Method , Data , ( a , b ) => a + b , Targets , doubles => doubles.Sum() );
        }

        [Benchmark]
        public DetailedAggregationResult<double> DetailedTargeted()
        {
            return Aggregator.DetailedAggregate( Method , Data , doubles => doubles.Sum( t => t.value ) , Targets );
        }

        [Benchmark]
        public DetailedAggregationResult<double> DetailedSimplifiedTargeted()
        {
            return Aggregator.DetailedAggregate( Method , Data , doubles => doubles.Sum( t => t.value ) , Targets , true , Array.Empty<string>() , items => items.Sum() );
        }

        //[Benchmark]
        //public AggregationResult<double> BottomTop()
        //{
        //    return Aggregator.Aggregate( Method.BottomTopGroup , Data , ( a , b ) => a + b , Targets , doubles => doubles.Sum() );
        //}
    }
}