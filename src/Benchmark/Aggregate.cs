using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Bogus;
using LanguageExt;
using MultiDimensionsHierarchies;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmark
{
    [SimpleJob( RuntimeMoniker.Net472 , warmupCount: 3 , targetCount: 10 ),
        SimpleJob( RuntimeMoniker.NetCoreApp31 , warmupCount: 3 , targetCount: 10 ),
        SimpleJob( RuntimeMoniker.Net60 , warmupCount: 3 , targetCount: 10 )]
    //[NativeMemoryProfiler]
    //[MemoryDiagnoser]
    public class Aggregate
    {
        private static readonly Dimension dimA;
        private static readonly Dimension dimB;
        private static readonly Dimension dimC;
        private static readonly Dimension dimD;

        private static Dimension[] Dimensions => new[] { dimA , dimB , dimC , dimD };

        private static readonly DataInput[] sample;
        private static readonly IDictionary<(int, int) , Skeleton<double>[]> datas;
        private static readonly IDictionary<int , Skeleton[]> targets;

        static Aggregate()
        {
            dimA = DimensionFactory.BuildWithParentLink( "Dim A" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimB = DimensionFactory.BuildWithParentLink( "Dim B" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimC = DimensionFactory.BuildWithParentLink( "Dim C" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimD = DimensionFactory.BuildWithParentLink( "Dim D" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );

            Console.WriteLine( "Build samples" );
            sample = BuildSample( dimA.Flatten().Select( b => b.Label ).Distinct().ToArray() , 1_000_000 );

            var dimCounts = new[] { 3 , 4 };
            var sampleSizes = new[] { 10_000 , 100_000 , 1_000_000 };

            Console.WriteLine( "Build skeletons" );
            datas = dimCounts.SelectMany( dimCount =>
                {
                    var data = SkeletonFactory.BuildSkeletons(
                    sample ,
                    DataInput.Parser ,
                    x => x.Value ,
                    Dimensions.Take( dimCount ) ).ToArray();

                    return sampleSizes.Select( sampleSize => (dimCount, sampleSize, data.Take( sampleSize ).ToArray()) );
                } ).ToDictionary( x => (x.dimCount, x.sampleSize) , x => x.Item3 );

            foreach ( var t in datas )
                Console.WriteLine( "{0} dimensions & {1} samples: {2} skeletons to compute" , t.Key.Item1 , t.Key.Item2 , t.Value.GetAncestors().LongCount() );

            Console.WriteLine( "Build targets" );
            targets = dimCounts.Select( dimCount => (dimCount,
                    Dimensions.Take( dimCount )
                        .Select( d => d.Flatten().Where( b => b.Depth <= 3 ).ToArray() )
                        .Combine().OrderBy( s => s.Depth ).ToArray()) )
                .ToDictionary( x => x.dimCount , x => x.Item2 );

            foreach ( var t in targets )
                Console.WriteLine( "{0} dimensions: {1} targets" , t.Key , t.Value.LongLength );

            Console.WriteLine( "Cache ready" );
        }

        [Benchmark]
        [Arguments( 3 , 100_000 )]
        //[Arguments( 3 , 500_000 )]
        [Arguments( 3 , 1_000_000 )]
        [Arguments( 4 , 100_000 )]
        //[Arguments( 4 , 500_000 )]
        //[Arguments( 4 , 1_000_000 )]
        public AggregationResult<double> Group( int dimensionsCount , int sampleSize )
        {
            var data = datas[(dimensionsCount, sampleSize)];
            return Aggregator.Aggregate( Method.HeuristicGroup , data , ( a , b ) => a + b , doubles => doubles.Sum() );
        }

        [Benchmark]
        [Arguments( 3 , 100_000 )]
        //[Arguments( 3 , 500_000 )]
        [Arguments( 3 , 1_000_000 )]
        [Arguments( 4 , 100_000 )]
        //[Arguments( 4 , 500_000 )]
        //[Arguments( 4 , 1_000_000 )]
        public AggregationResult<double> Dictionary( int dimensionsCount , int sampleSize )
        {
            var data = datas[(dimensionsCount, sampleSize)];
            return Aggregator.Aggregate( Method.HeuristicDictionary , data , ( a , b ) => a + b , doubles => doubles.Sum() );
        }

        [Benchmark]
        [Arguments( 3 , 10_000 , 1 )]
        [Arguments( 3 , 10_000 , 10 )]
        [Arguments( 3 , 100_000 , 1 )]
        //[Arguments( 3 , 1_000_000 )]
        [Arguments( 4 , 10_000 , 1 )]
        //[Arguments( 4 , 100_000 )]
        //[Arguments( 4 , 500_000 )]
        //[Arguments( 4 , 1_000_000 )]
        public AggregationResult<double> Targeted( int dimensionsCount , int sampleSize , int targetSize )
        {
            var data = datas[(dimensionsCount, sampleSize)];
            var target = targets[dimensionsCount].Take( targetSize );
            return Aggregator.Aggregate( Method.Targeted , data , ( a , b ) => a + b , target , doubles => doubles.Sum() );
        }

        static IEnumerable<ParentHierarchyInput<string>> BuildHierarchy( string id , Option<string> parentId , int maxDepth , int count = 1 )
        {
            var item = new ParentHierarchyInput<string> { Id = id };

            parentId.IfSome( p => item.ParentId = p );

            yield return item;

            if ( count++ < maxDepth )
            {
                var limit = Math.Pow( count , 1 );
                for ( int i = 1 ; i <= limit ; i++ )
                {
                    foreach ( var child in BuildHierarchy( $"{id}.{i}" , Option<string>.Some( id ) , maxDepth , count ) )
                        yield return child;
                }
            }
        }

        static DataInput[] BuildSample( string[] labels , int sampleSize )
        {
            Randomizer.Seed = new Random( 0 );
            var faker = new Faker();

            var data = Enumerable.Range( 0 , sampleSize )
              .AsParallel()
              .Select( _ => new DataInput { DimA = faker.PickRandom( labels ) , DimB = faker.PickRandom( labels ) , DimC = faker.PickRandom( labels ) , DimD = faker.PickRandom( labels ) , Value = faker.Random.Double() } )
              .ToArray();

            return data;
        }
    }
}