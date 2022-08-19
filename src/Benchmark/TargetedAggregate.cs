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
    [SimpleJob( RuntimeMoniker.Net472 , warmupCount: 3 , targetCount: 7 )/*,
     SimpleJob( RuntimeMoniker.Net60 , warmupCount: 3 , targetCount: 7 )*/]
    [MemoryDiagnoser( false )]
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
        public AggregationResult<double> Targeted()
        {
            return Aggregator.Aggregate( Method.Targeted , Data , ( a , b ) => a + b , Targets , doubles => doubles.Sum() );
        }

        [Benchmark]
        public DetailedAggregationResult<double> DetailedTargeted()
        {
            return Aggregator.DetailedAggregate( Method.Targeted , Data , doubles => doubles.Sum( t => t.value ) , Targets );
        }

        [Benchmark]
        public AggregationResult<double> Heuristic()
        {
            return Aggregator.Aggregate( Method.HeuristicGroup , Data , ( a , b ) => a + b , Targets , doubles => doubles.Sum() );
        }
    }
}