using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MultiDimensionsHierarchies
{
    public enum Method { Heuristic, Targeted }
    //public enum CollectionMode { Skeleton, FullId, ShortId }

    public static class Aggregator
    {
        public static AggregationResult<T> Aggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator , Func<IEnumerable<T> , T> groupAggregator = null )
            => Aggregate( method , inputs , aggregator , Array.Empty<Skeleton>() , groupAggregator );

        public static AggregationResult<T> Aggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator , IEnumerable<Skeleton> targets , Func<IEnumerable<T> , T> groupAggregator = null )
        {
            var seqTargets = targets.ToSeq();

            if ( method == Method.Targeted && seqTargets.Length == 0 )
                return new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "Method is Targeted but no targets have been defined!" );

            /* Aggregate base data that might have common keys */
            groupAggregator ??= ( items ) => items.Aggregate( aggregator );
            var seqInputs = inputs.GroupBy( x => x.Key )
                .Select( g => g.Aggregate( g.Key , groupAggregator ) )
                .ToSeq();

            return method switch
            {
                Method.Targeted => TargetedAggregate( seqInputs , seqTargets , groupAggregator ),
                Method.Heuristic => HeuristicAggregate( seqInputs , aggregator ),
                _ => new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "No method was defined." )
            };
        }

        private static AggregationResult<T> HeuristicAggregate<T>( Seq<Skeleton<T>> baseData , Func<T , T , T> aggregator )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var results = new ConcurrentDictionary<Skeleton , Option<T>>();

                baseData
                    .AsParallel()
                    .ForAll( skeleton =>
                    {
                        foreach ( var ancestor in skeleton.Key.GetAncestors() )
                        {
                            results.AddOrUpdate( ancestor , skeleton.Value ,
                                ( _ , data ) =>
                                    data.Some( d => skeleton.Value.Some( s => aggregator( d , s ) ).None( () => d ) )
                                        .None( () => skeleton.Value.Some( s => s ).None( () => default ) ) );
                        }
                    } );

                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results.Select( kvp => new Skeleton<T>( kvp.Value , kvp.Key ) ) , "Process OK" );
            } );

            return f.Match( res => res , exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static AggregationResult<T> TargetedAggregate<T>( Seq<Skeleton<T>> baseData , Seq<Skeleton> targets , Func<IEnumerable<T> , T> groupAggregator )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var uniqueTargetBaseBones = targets.SelectMany( t => t.Bones )
                    .GroupBy( b => b.DimensionName )
                    .Where( g => g.Distinct().Count() == 1 )
                    .Select( g => g.First() )
                    .ToSeq();

                var simplifiedTargets = targets;
                var simplifiedData = baseData;
                if ( uniqueTargetBaseBones.Any() )
                {
                    var uniqueDimensions = uniqueTargetBaseBones.Select( u => u.DimensionName ).ToArray();
                    var dataFilter = uniqueTargetBaseBones.Select( b => (b.DimensionName, b.GetDescendants()) ).ToHashMap();

                    simplifiedData = baseData
                       .Where( d => dataFilter.All( i => i.Value.Contains( d.Bones.Find( b => b.DimensionName.Equals( i.Key ) ).Some( b => b ).None( () => Bone.None ) ) ) )
                       .Select( d => d.Except( uniqueTargetBaseBones.Select( u => u.DimensionName ).ToArray() ) )
                       .GroupBy( x => x.Key )
                       .Select( g => g.Aggregate( g.Key , groupAggregator ) )
                       .ToSeq();

                    simplifiedTargets = targets.Select( s => s.Except( uniqueDimensions ) );
                }

                var results = simplifiedTargets
                    .AsParallel()
                    .Select( t =>
                    {
                        var composingData = simplifiedData;

                        foreach ( var bone in t.Bones )
                        {
                            var expectedBones = bone.GetDescendants();
                            var unneededKeys = simplifiedData.Where( s => s.Bones.Find( x => x.DimensionName.Equals( bone.DimensionName ) )
                                                                                                        .Some( b => !expectedBones.Contains( b ) )
                                                                                                        .None( () => false )
                                                                        )
                                .Select( s => s.Key );
                            composingData = composingData.Except( composingData.Where( o => unneededKeys.Contains( o.Key ) ) ).ToSeq();
                        }

                        return composingData.Aggregate( t , groupAggregator );
                    } );

                results = results.Select( r => r.Add( uniqueTargetBaseBones ) );
                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res , exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }
    }
}
