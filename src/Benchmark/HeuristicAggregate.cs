using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks;
using LanguageExt;
using MultiDimensionsHierarchies;
using System.Linq;

namespace Benchmark
{
    [/*SimpleJob( RuntimeMoniker.Net472 , warmupCount: 3 , targetCount: 7 ),*/
     SimpleJob( RuntimeMoniker.Net60 , warmupCount: 3 , targetCount: 7 )]
    [MemoryDiagnoser( false )]
    public class HeuristicAggregate : AllMethodsAggregate
    {
        public HeuristicAggregate() : base()
        {
        }

        [Benchmark]
        public AggregationResult<double> Group()
        {
            return Aggregator.Aggregate( Method.BottomTopGroup , Data , ( a , b ) => a + b , doubles => doubles.Sum() , useCachedSkeletons: false );
        }

        //[Benchmark]
        //public DetailedAggregationResult<double> Detailed()
        //{
        //    return Aggregator.DetailedAggregate( Method.Heuristic , Data , doubles => doubles.Sum( t => t.value ) );
        //}

        [Benchmark]
        public AggregationResult<double> Dictionary()
        {
            return Aggregator.Aggregate( Method.BottomTopDictionary , Data , ( a , b ) => a + b , doubles => doubles.Sum() , useCachedSkeletons: false );
        }

        [Benchmark]
        public AggregationResult<double> GroupCache()
        {
            return Aggregator.Aggregate( Method.BottomTopGroup , Data , ( a , b ) => a + b , doubles => doubles.Sum() , useCachedSkeletons: true );
        }

        [Benchmark]
        public AggregationResult<double> DictionaryCache()
        {
            return Aggregator.Aggregate( Method.BottomTopDictionary , Data , ( a , b ) => a + b , doubles => doubles.Sum() , useCachedSkeletons: true );
        }
    }
}