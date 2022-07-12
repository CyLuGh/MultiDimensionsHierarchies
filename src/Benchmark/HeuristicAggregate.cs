using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks;
using LanguageExt;
using MultiDimensionsHierarchies;
using System.Linq;

namespace Benchmark
{
    [SimpleJob( RuntimeMoniker.Net472 , warmupCount: 3 , targetCount: 7 ),
     SimpleJob( RuntimeMoniker.Net60 , warmupCount: 3 , targetCount: 7 )]
    [MemoryDiagnoser( false )]
    [CpuDiagnoser]
    public class HeuristicAggregate : AllMethodsAggregate
    {
        public HeuristicAggregate() : base()
        {
        }

        [Benchmark]
        public AggregationResult<double> Group()
        {
            return Aggregator.Aggregate( Method.HeuristicGroup , Data , ( a , b ) => a + b , doubles => doubles.Sum() );
        }

        [Benchmark]
        public DetailedAggregationResult<double> Detailed()
        {
            return Aggregator.DetailedAggregate( Method.Heuristic , Data , doubles => doubles.Sum( t => t.value ) );
        }

        //[Benchmark]
        //public AggregationResult<double> Dictionary()
        //{
        //    return Aggregator.Aggregate( Method.HeuristicDictionary , Data , ( a , b ) => a + b , doubles => doubles.Sum() );
        //}
    }
}