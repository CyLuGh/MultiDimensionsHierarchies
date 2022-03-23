using LanguageExt;
using MoreLinq;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MultiDimensionsHierarchies
{
    /// <summary>
    /// Defines aggregation algorithm.
    /// </summary>
    public enum Method
    {
        /// <summary>
        /// Computes all aggregates from source data in a bottom-top way.
        /// </summary>
        Heuristic,
        /// <summary>
        /// Computes limited aggregates in top-down way.
        /// </summary>
        Targeted
    }
    //public enum CollectionMode { Skeleton, FullId, ShortId }

    public static class Aggregator
    {
        /// <summary>
        /// Apply aggregator to inputs according to included hierarchies.
        /// </summary>
        /// <typeparam name="T">Data kind</typeparam>
        /// <param name="method">Aggregation algorithm</param>
        /// <param name="inputs">Source data, including their hierarchy</param>
        /// <param name="aggregator">How to aggregate <typeparamref name="T"/> and <typeparamref name="T"/></param>
        /// <param name="groupAggregator">(Optional) How to aggregate a collection of T <typeparamref name="T"/></param>
        /// <returns>AggregationResult which contains execution status and results if process OK.</returns>
        public static AggregationResult<T> Aggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator , Func<IEnumerable<T> , T> groupAggregator = null , Func<T , double , T> weightEffect = null )
            => Aggregate( method , inputs , aggregator , Array.Empty<Skeleton>() , groupAggregator , weightEffect );

        /// <summary>
        /// Apply aggregator to inputs according to included hierarchies.
        /// </summary>
        /// <typeparam name="T">Data kind</typeparam>
        /// <param name="method">Aggregation algorithm</param>
        /// <param name="inputs">Source data, including their hierarchy</param>
        /// <param name="aggregator">How to aggregate <typeparamref name="T"/> and <typeparamref name="T"/></param>
        /// <param name="targets">Defined keys to compute, needed for <paramref name="Targeted"/> method</param>
        /// <param name="groupAggregator">(Optional) How to aggregate a collection of T <typeparamref name="T"/></param>
        /// <returns>AggregationResult which contains execution status and results if process OK.</returns>
        public static AggregationResult<T> Aggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator , IEnumerable<Skeleton> targets , Func<IEnumerable<T> , T> groupAggregator = null ,
            Func<T , double , T> weightEffect = null )
        {
            var seqTargets = targets.ToSeq();

            if ( method == Method.Targeted && seqTargets.Length == 0 )
                return new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "Method is Targeted but no targets have been defined!" );

            /* Aggregate base data that might have common keys */
            groupAggregator ??= ( items ) => items.Aggregate( aggregator );
            var seqInputs = inputs.GroupBy( x => x.Key )
                .Select( g => g.Aggregate( g.Key , groupAggregator , weightEffect ) )
                .ToSeq();

            weightEffect ??= ( t , _ ) => t;

            return method switch
            {
                Method.Targeted => TargetedAggregate( seqInputs , seqTargets , groupAggregator , weightEffect ),
                Method.Heuristic => HeuristicAggregate( seqInputs , aggregator , seqTargets , weightEffect ),
                _ => new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "No method was defined." )
            };
        }

        private static AggregationResult<T> HeuristicAggregate<T>( Seq<Skeleton<T>> baseData ,
                                                                  Func<T , T , T> aggregator ,
                                                                  Seq<Skeleton> targets ,
                                                                  Func<T , double , T> weightEffect )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var results = new ConcurrentDictionary<Skeleton , Option<T>>();

                baseData
                    .AsParallel()
                    .ForAll( skeleton =>
                    {
                        foreach ( var ancestor in skeleton.Key.Ancestors() )
                        {
                            var weight = skeleton.Key.ResultingWeight( ancestor );
                            results.AddOrUpdate( ancestor , skeleton.Value ,
                                ( _ , data ) =>
                                    data.Some( d => skeleton.Value
                                            .Some( s => aggregator( d , weightEffect( s , weight ) ) )
                                            .None( () => d ) )
                                        .None( () => skeleton.Value.Some( s => s )
                                                                   .None( () => default ) ) );
                        }
                    } );

                var res = results.Select( kvp => new Skeleton<T>( kvp.Value , kvp.Key ) ).ToSeq();
                if ( targets.Count > 0 )
                {
                    res = res.Where( s => targets.Contains( s.Key ) );
                }

                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
            } );

            return f.Match( res => res , exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static AggregationResult<T> TargetedAggregate<T>( Seq<Skeleton<T>> baseData ,
                                                                 Seq<Skeleton> targets ,
                                                                 Func<IEnumerable<T> , T> groupAggregator ,
                                                                 Func<T , double , T> weightEffect )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var uniqueTargetBaseBones = targets.SelectMany( t => t.Bones )
                    .GroupBy( b => b.DimensionName )
                    .Where( g => g.Distinct().Count() == 1 && !g.Any( b => b.HasWeightElement() ) )
                    .Select( g => g.First() )
                    .ToSeq();

                var simplifiedTargets = targets;
                var simplifiedData = baseData;
                if ( uniqueTargetBaseBones.Any() )
                {
                    var uniqueDimensions = uniqueTargetBaseBones.Select( u => u.DimensionName ).ToArray();
                    var dataFilter = uniqueTargetBaseBones.Select( b => (b.DimensionName, b.Descendants()) ).ToHashMap();

                    simplifiedData = baseData
                       .Where( d => dataFilter.All( i => i.Value.Contains( d.Bones.Find( b => b.DimensionName.Equals( i.Key ) ).Some( b => b ).None( () => Bone.None ) ) ) )
                       .Select( d => d.Except( uniqueTargetBaseBones.Select( u => u.DimensionName ).ToArray() ) )
                       .GroupBy( x => x.Key )
                       .Select( g => g.Aggregate( g.Key , groupAggregator , weightEffect ) )
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
                            var expectedBones = bone.Descendants();
                            var unneededKeys = simplifiedData.Where( s => s.Bones.Find( x => x.DimensionName.Equals( bone.DimensionName ) )
                                                                                                        .Some( b => !expectedBones.Contains( b ) )
                                                                                                        .None( () => false )
                                                                        )
                                .Select( s => s.Key );
                            composingData = composingData.Except( composingData.Where( o => unneededKeys.Contains( o.Key ) ) ).ToSeq();
                        }

                        return composingData.Aggregate( t , groupAggregator , weightEffect );
                    } );

                results = results.Select( r => r.Add( uniqueTargetBaseBones ) );
                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res , exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }
    }
}
