﻿using BenchmarkDotNet.Attributes;
using Bogus;
using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Linq;

namespace Benchmark
{
    public abstract class AllMethodsAggregate
    {
        [Params( /*10_000 ,*/ 100_000 , 200_000 /*, 1_000_000*/ )]
        public int SampleSize { get; set; }

        [Params( /*2 , 3 ,*/ 4 , 5 , 6 )]
        public int DimensionsCount { get; set; }

        public Skeleton<double>[] Data;

        protected readonly Dimension dimA;
        protected readonly Dimension dimB;
        protected readonly Dimension dimC;
        protected readonly Dimension dimD;
        protected readonly Dimension dimE;
        protected readonly Dimension dimF;

        public Dimension[] Dimensions => new[] { dimA , dimB , dimC , dimD , dimE , dimF };

        protected AllMethodsAggregate()
        {
            dimA = DimensionFactory.BuildWithParentLink( "Dim A" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimB = DimensionFactory.BuildWithParentLink( "Dim B" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimC = DimensionFactory.BuildWithParentLink( "Dim C" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimD = DimensionFactory.BuildWithParentLink( "Dim D" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimE = DimensionFactory.BuildWithParentLink( "Dim E" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
            dimF = DimensionFactory.BuildWithParentLink( "Dim F" , BuildHierarchy( "1" , Option<string>.None , 4 ) , o => o.Id , o => o.ParentId );
        }

        [GlobalSetup]
        public virtual void GlobalSetup()
        {
            //Console.WriteLine( "Building sample" );
            var sample = BuildSample( dimA.Flatten().Select( b => b.Label ).Distinct().ToArray() , SampleSize );

            //Console.WriteLine( "Building skeletons" );
            Data = SkeletonFactory.BuildSkeletons(
                    sample ,
                    DataInput.Parser ,
                    x => x.Value ,
                    Dimensions.Take( DimensionsCount ) ).ToArray();
        }

        internal static IEnumerable<ParentHierarchyInput<string>> BuildHierarchy( string id , Option<string> parentId , int maxDepth , int count = 1 )
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

        internal static DataInput[] BuildSample( string[] labels , int sampleSize )
        {
            Randomizer.Seed = new Random( 0 );
            var faker = new Faker();

            var data = Enumerable.Range( 0 , sampleSize )
              .AsParallel()
              .Select( _ => new DataInput
              {
                  DimA = faker.PickRandom( labels ) ,
                  DimB = faker.PickRandom( labels ) ,
                  DimC = faker.PickRandom( labels ) ,
                  DimD = faker.PickRandom( labels ) ,
                  DimE = faker.PickRandom( labels ) ,
                  DimF = faker.PickRandom( labels ) ,
                  DimG = faker.PickRandom( labels ) ,
                  Value = faker.Random.Double()
              } )

              .ToArray();

            return data;
        }
    }
}