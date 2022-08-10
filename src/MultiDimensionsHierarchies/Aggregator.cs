using LanguageExt;
using MoreLinq;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

        HeuristicGroup,
        HeuristicDictionary,

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
        /// <param name="weightEffect">(Optional) How weight should be applied to T</param>
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
        /// <param name="weightEffect">(Optional) How weight should be applied to T</param>
        /// <returns>AggregationResult which contains execution status and results if process OK.</returns>
        public static AggregationResult<T> Aggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator , IEnumerable<Skeleton> targets , Func<IEnumerable<T> , T> groupAggregator = null ,
            Func<T , double , T> weightEffect = null )
        {
            var hashTarget = new LanguageExt.HashSet<Skeleton>().TryAddRange( targets );

            if ( method == Method.Targeted && hashTarget.Count == 0 )
                return new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "Method is Targeted but no targets have been defined!" );

            /* Aggregate base data that might have common keys */
            groupAggregator ??= ( items ) => items.Aggregate( aggregator );
            var groupedInputs = inputs.GroupBy( x => x.Key )
                .Select( g => g.Aggregate( g.Key , groupAggregator ) )
                .ToArray();

            weightEffect ??= ( t , _ ) => t;

            return method switch
            {
                Method.Targeted => TargetedAggregate( groupedInputs , hashTarget , groupAggregator , weightEffect ),
                Method.Heuristic => HeuristicAggregate( groupedInputs , aggregator , hashTarget , weightEffect ),
                Method.HeuristicDictionary => HeuristicDictionaryAggregate( groupedInputs , aggregator , hashTarget , weightEffect ),
                Method.HeuristicGroup => HeuristicGroupAggregate( groupedInputs , aggregator , hashTarget , weightEffect ),
                _ => new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "No method was defined." )
            };
        }

        private static AggregationResult<T> HeuristicAggregate<T>( Skeleton<T>[] baseData ,
                                                                  Func<T , T , T> aggregator ,
                                                                  LanguageExt.HashSet<Skeleton> targets ,
                                                                  Func<T , double , T> weightEffect )
        {
            // Choose most adequate method?
            return HeuristicGroupAggregate( baseData , aggregator , targets , weightEffect );
        }

        private static AggregationResult<T> HeuristicGroupAggregate<T>( Skeleton<T>[] baseData ,
                                                                  Func<T , T , T> aggregator ,
                                                                  LanguageExt.HashSet<Skeleton> targets ,
                                                                  Func<T , double , T> weightEffect )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                var res = baseData
                    .AsParallel()
                    .SelectMany( skeleton =>
                        skeleton.Value.Some( v =>
                                skeleton.Key.Ancestors().Select( ancestor =>
                                     new Skeleton<T>( weightEffect( v , Skeleton.ComputeResultingWeight( skeleton.Key , ancestor ) ) , ancestor ) ) )
                            .None( () => Seq.empty<Skeleton<T>>() ) )
                    .Where( r => targets.Count == 0 || targets.Contains( r.Key ) )
                    .GroupBy( s => s.Key )
                    .Select( g => g.Aggregate( aggregator ) )
                    .Somes()
                    .ToArray();

                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
            } );

            return f.Match( res => res , exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static AggregationResult<T> HeuristicDictionaryAggregate<T>( Skeleton<T>[] baseData ,
                                                                  Func<T , T , T> aggregator ,
                                                                  LanguageExt.HashSet<Skeleton> targets ,
                                                                  Func<T , double , T> weightEffect )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                var results = new NonBlocking.ConcurrentDictionary<Skeleton , Option<T>>();

                Parallel.ForEach( baseData , skeleton =>
                {
                    foreach ( var ancestor in skeleton.Key.Ancestors().Where( s => targets.IsEmpty || targets.Contains( s ) ) )
                    {
                        var weight = Skeleton.ComputeResultingWeight( skeleton.Key , ancestor );
                        var wVal = from v in skeleton.Value
                                   select weightEffect( v , weight );

                        results.AddOrUpdate( ancestor , wVal , ( _ , data ) => from d in data
                                                                               from v in wVal
                                                                               select aggregator( d , v ) );
                    }
                } );

                var res = results.Select( kvp => new Skeleton<T>( kvp.Value , kvp.Key ) )
                    .ToArray();

                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
            } );

            return f.Match( res => res , exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static (Skeleton<T>[], LanguageExt.HashSet<Skeleton>) SimplifyTargets<T>(
            Skeleton<T>[] baseData ,
            LanguageExt.HashSet<Skeleton> targets ,
            Seq<Bone> uniqueTargetBaseBones ,
            Func<IEnumerable<T> , T> groupAggregator ,
            Func<T , double , T> weightEffect )
        {
            var uniqueDimensions = uniqueTargetBaseBones.Select( u => u.DimensionName ).ToArray();
            var dataFilter = uniqueTargetBaseBones.Select( b => (b.DimensionName, b.Descendants()) ).ToHashMap();

            var simplifiedData = baseData
               .Where( d => dataFilter.All( i => i.Value.Contains( d.Bones.Find( b => b.DimensionName.Equals( i.Key ) ).Some( b => b ).None( () => Bone.None ) ) ) )
               .Select( d => d.Except( uniqueTargetBaseBones.Select( u => u.DimensionName ).ToArray() ) )
               .GroupBy( x => x.Key )
               .Select( g => g.Aggregate( g.Key , groupAggregator , weightEffect ) )
               .ToArray();

            var hash = new LanguageExt.HashSet<Skeleton>().TryAddRange( targets.Select( s => s.Except( uniqueDimensions ) ) );

            return (simplifiedData, hash);
        }

        private static AggregationResult<T> TargetedAggregate<T>( Skeleton<T>[] baseData ,
                                                                 LanguageExt.HashSet<Skeleton> targets ,
                                                                 Func<IEnumerable<T> , T> groupAggregator ,
                                                                 Func<T , double , T> weightEffect )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var uniqueTargetBaseBones = targets
                    .SelectMany( t => t.Bones )
                    .GroupBy( b => b.DimensionName )
                    .Where( g => g.Distinct().Count() == 1 && !g.Any( b => b.HasWeightElement() ) )
                    .Select( g => g.First() )
                    .ToSeq();

                var (simplifiedData, simplifiedTargets) =
                    uniqueTargetBaseBones.Any() ?
                        SimplifyTargets( baseData , targets , uniqueTargetBaseBones , groupAggregator , weightEffect )
                        : (baseData, targets);

                var simplifiedMap = Map.create<Skeleton , Skeleton<T>>()
                    .TryAddRange( simplifiedData.Select( s => (s.Key, s) ) );

                var results = simplifiedTargets
                    .AsParallel()
                    .Select( t => t.GetComposingSkeletons( simplifiedMap ).Aggregate( t , groupAggregator , weightEffect ) )
                    .Select( r => r.Add( uniqueTargetBaseBones ) )
                    .ToArr();

                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res , exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        public static DetailedAggregationResult<T> DetailedAggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs , Func<IEnumerable<(T value, double weight)> , T> aggregator , IEnumerable<Skeleton> targets = null )
        {
            var hashTarget = new LanguageExt.HashSet<Skeleton>();
            if ( targets != null )
                hashTarget = hashTarget.TryAddRange( targets );

            if ( method == Method.Targeted && hashTarget.Count == 0 )
                return new DetailedAggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "Method is Targeted but no targets have been defined!" );

            return method switch
            {
                Method.Targeted => DetailedTargetedAggregate( inputs.ToArray() , aggregator , hashTarget ),
                Method.Heuristic => HeuristicDetailedGroupAggregate( inputs.ToArray() , aggregator , hashTarget ),
                Method.HeuristicDictionary => HeuristicDetailedGroupAggregate( inputs.ToArray() , aggregator , hashTarget ),
                Method.HeuristicGroup => HeuristicDetailedGroupAggregate( inputs.ToArray() , aggregator , hashTarget ),
                _ => new DetailedAggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "No method was defined." )
            };
        }

        private static DetailedAggregationResult<T> HeuristicDetailedGroupAggregate<T>( Skeleton<T>[] baseData ,
                                                                  Func<IEnumerable<(T, double)> , T> aggregator ,
                                                                  LanguageExt.HashSet<Skeleton> targets )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                var res = baseData
                    .AsParallel()
                    .SelectMany<Skeleton<T> , (Skeleton Key, double Weight, Skeleton<T> Input)>( skeleton =>
                        skeleton.Value.Some( v =>
                                skeleton.Key.Ancestors().Select( ancestor =>
                                    (Ancestor: ancestor, Weight: Skeleton.ComputeResultingWeight( skeleton.Key , ancestor ), Input: skeleton) ) )
                            .None( () => Seq.empty<(Skeleton, double, Skeleton<T>)>() ) )
                    .Where( r => targets.Count == 0 || targets.Contains( r.Key ) )
                    .GroupBy( s => s.Key )
                    .Select( g => new SkeletonsAccumulator<T>( g.Key , g.Select( x => (x.Weight, x.Input) ) , aggregator ) )
                    .ToArray();

                stopWatch.Stop();

                return new DetailedAggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
            } );

            return f.Match( res => res , exc => new DetailedAggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static DetailedAggregationResult<T> DetailedTargetedAggregate<T>( Skeleton<T>[] baseData ,
                                                                    Func<IEnumerable<(T, double)> , T> aggregator ,
                                                                    LanguageExt.HashSet<Skeleton> targets )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                //var map = Map.createRange( baseData.AsParallel().GroupBy( s => s.Key )
                //    .Select( g => (g.Key, g.ToSeq()) ) );

                var dictionary = baseData.AsParallel().GroupBy( s => s.Key )
                    .ToDictionary( g => g.Key , g => g.ToSeq() );

                var results = targets
                    .AsParallel()
                    .Select( skeleton =>
                    {
                        var components = skeleton.GetComposingSkeletons( dictionary )
                            .Select( cmp => (Skeleton.ComputeResultingWeight( cmp.Key , skeleton ), cmp) );
                        return new SkeletonsAccumulator<T>( skeleton , components , aggregator );
                    } )
                    .ToArray();

                stopWatch.Stop();

                return new DetailedAggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res , exc => new DetailedAggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }
    }
}