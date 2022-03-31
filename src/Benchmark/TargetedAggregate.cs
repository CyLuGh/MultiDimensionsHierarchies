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
    [MemoryDiagnoser]
    public class TargetedAggregate
    {
        [Params( 1000 , 10000 , 100000 /*, 1000000*/ )]
        public int SampleSize { get; set; }

        [Params( 3 , 4 )]
        public int DimensionsCount { get; set; }

        [Params( 1 , 10 , 50 , 100 )]
        public int TargetsCount { get; set; }

        public Skeleton<double>[] Data;
        public Skeleton[] Targets;

        private readonly Dimension dimA;
        private readonly Dimension dimB;
        private readonly Dimension dimC;
        private readonly Dimension dimD;

        private Dimension[] Dimensions => new[] { dimA , dimB , dimC , dimD };

        public TargetedAggregate()
        {
            dimA = DimensionFactory.BuildWithParentLink( "Dim A" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimB = DimensionFactory.BuildWithParentLink( "Dim B" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimC = DimensionFactory.BuildWithParentLink( "Dim C" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimD = DimensionFactory.BuildWithParentLink( "Dim D" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
        }

        public void GlobalSetup()
        {
            var sample = BuildSample( dimA.Flatten().Select( b => b.Label ).Distinct().ToArray() , SampleSize );
            Data = SkeletonFactory.BuildSkeletons(
                    sample ,
                    DataInput.Parser ,
                    x => x.Value ,
                    Dimensions.Take( DimensionsCount ) ).ToArray();

            Targets = Dimensions.Take( DimensionsCount )
                 .Select( d => d.Flatten().Where( b => b.Depth <= 3 ).ToArray() )
                 .Combine()
                 .Take( TargetsCount )
                 .ToArray();
        }

        [Benchmark]
        public AggregationResult<double> Targeted()
        {
            return Aggregator.Aggregate( Method.Targeted , Data , ( a , b ) => a + b , Targets , doubles => doubles.Sum() );
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
