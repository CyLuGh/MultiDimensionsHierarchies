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
    [SimpleJob( RuntimeMoniker.Net472 , iterationCount: 5 , warmupCount: 3 ),
     SimpleJob( RuntimeMoniker.Net70 , iterationCount: 5 , warmupCount: 3 )]
    [MemoryDiagnoser]
    public class TargetedAggregate : AllMethodsAggregate
    {
        [Params( 1000 )]
        public int TargetsCount { get; set; }

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
            return Aggregator.Aggregate( Method.TopDown , Data , ( a , b ) => a + b , Targets , doubles => doubles.Sum() );
        }

        [Benchmark]
        public AggregationResult<double> TopDownGroup()
        {
            return Aggregator.Aggregate( Method.TopDownGroup , Data , ( a , b ) => a + b , Targets , doubles => doubles.Sum() );
        }

        //[Benchmark]
        //public DetailedAggregationResult<double> DetailedTargeted()
        //{
        //    return Aggregator.DetailedAggregate( Method.TopDown , Data , doubles => doubles.Sum( t => t.value ) , Targets );
        //}

        //[Benchmark]
        //public AggregationResult<double> BottomTop()
        //{
        //    return Aggregator.Aggregate( Method.BottomTopGroup , Data , ( a , b ) => a + b , Targets , doubles => doubles.Sum() );
        //}
    }
}