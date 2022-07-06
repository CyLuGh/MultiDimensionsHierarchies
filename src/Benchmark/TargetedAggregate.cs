using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using System.Diagnostics;

using System.Linq;

namespace Benchmark
{
    [SimpleJob( RuntimeMoniker.Net472 , warmupCount: 3 , targetCount: 7 )]
    [MemoryDiagnoser( false )]
    [CpuDiagnoser]
    public class TargetedAggregate : AllMethodsAggregate
    {
        [Params( 1000 , 5000 , 10_000 , 50_000 )]
        public int TargetsCount { get; set; }

        public Skeleton[] Targets;

        public TargetedAggregate() : base()
        {
        }

        public override void GlobalSetup()
        {
            base.GlobalSetup();

            Trace.WriteLine( "Building targets" );
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

        //[Benchmark]
        //public AggregationResult<double> Heuristic()
        //{
        //    return Aggregator.Aggregate( Method.HeuristicGroup , Data , ( a , b ) => a + b , Targets , doubles => doubles.Sum() );
        //}
    }
}