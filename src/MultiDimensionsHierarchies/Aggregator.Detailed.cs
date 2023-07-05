using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MultiDimensionsHierarchies
{
    public static partial class Aggregator
    {
        /// <summary>
        /// Gather from base input data every elements needed to compute aggregates.
        /// </summary>
        /// <typeparam name="T">Type of data that will be aggregated.</typeparam>
        /// <param name="inputs">Data to be aggregated, with their hierarchical information.</param>
        /// <param name="aggregator">How T elements should have their relative weights applied and be aggregated together to create a new T.</param>
        /// <returns>Detailed aggregation result, with valid aggregations, status and information message.</returns>
        public static DetailedAggregationResult<T> DetailedAggregate<T>(
            IEnumerable<Skeleton<T>> inputs ,
            Func<IEnumerable<(T value, double weight)> , T> aggregator )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var hashMap = HashmapDetailedAccumulate( inputs );
                var results = hashMap.AsParallel()
                    .Select( t => new SkeletonsAccumulator<T>( t.Key , t.Value , aggregator ) )
                    .ToArray();

                stopWatch.Stop();

                return new DetailedAggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res , exc =>
            {
                var ex = exc;
                var sb = new StringBuilder();
                sb.AppendLine( ex.Message );

                while ( ex.InnerException != null )
                {
                    ex = ex.InnerException;
                    sb.AppendLine( ex.Message );
                }

                return new DetailedAggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , sb.ToString() );
            } );
        }

        /// <summary>
        /// Gather from base input data every elements needed to compute a defined set of aggregates.
        /// </summary>
        /// <typeparam name="T">Type of data that will be aggregated.</typeparam>
        /// <param name="inputs">Data to be aggregated, with their hierarchical information.</param>
        /// <param name="targets">Defined set of targeted aggregation, with their hierarchical information.</param>
        /// <param name="aggregator">How T elements should have their relative weights applied and be aggregated together to create a new T.</param>
        /// <param name="groupAggregator">How a group of T should be aggregated to create a new T result.</param>
        /// <param name="simplifyData"><i>Optional.</i> Whether or not the simplification part of the algorithm should be applied or not. If set to true, the components in the results may not reflect
        /// the content from the input, but an aggregation of them. True by default.</param>
        /// <param name="dimensionsToPreserve"><i>Optional.</i> Dimensions that should be excluded from the simplification.</param>
        /// <param name="checkUse"><i>Optional.</i> Whether or not the algorithm should filter base data to only include relevant items. False by default.</param>
        /// <returns>Detailed aggregation result, with valid aggregations, status and information message. False by default.</returns>
        public static DetailedAggregationResult<T> DetailedAggregate<T>(
            IEnumerable<Skeleton<T>> inputs ,
            IEnumerable<Skeleton> targets ,
            Func<IEnumerable<(T value, double weight)> , T> aggregator ,
            Func<IEnumerable<T> , T> groupAggregator ,
            bool simplifyData = true ,
            string[] dimensionsToPreserve = null ,
            bool checkUse = false )
        {
            var targetsHash = HashSet.createRange( targets );

            if ( targetsHash.IsEmpty ) return new DetailedAggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "No targets have been defined!" );

            /* Aggregate base data that might have common keys */
            var groupedInputs = inputs.GroupBy( x => x.Key ).Select( g => g.Aggregate( g.Key , groupAggregator ) ).ToSeq();
            dimensionsToPreserve ??= Array.Empty<string>();

            return DetailedTopDownAggregate( groupedInputs , targetsHash , aggregator , groupAggregator , simplifyData , dimensionsToPreserve , checkUse );
        }

        /// <summary>
        /// Gather from base input data every elements needed to compute a defined set of aggregates.
        /// </summary>
        /// <typeparam name="T">Type of data that will be aggregated.</typeparam>
        /// <param name="baseData">Data to be aggregated, with their hierarchical information.</param>
        /// <param name="targets">Defined set of targeted aggregation, with their hierarchical information.</param>
        /// <param name="aggregator">How T elements should have their relative weights applied and be aggregated together to create a new T.</param>
        /// <param name="simplifyData"><i>Optional.</i> Whether or not the simplification part of the algorithm should be applied or not. If set to true, the components in the results may not reflect
        /// the content from the input, but an aggregation of them. True by default.</param>
        /// <param name="dimensionsToPreserve"><i>Optional.</i> Dimensions that should be excluded from the simplification.</param>
        /// <param name="groupAggregator">How a group of T should be aggregated to create a new T result.</param>
        /// <param name="checkUse"><i>Optional.</i> Whether or not the algorithm should filter base data to only include relevant items. False by default.</param>
        /// <returns>Enumerable of aggregation results.</returns>
        public static IEnumerable<SkeletonsAccumulator<T>> StreamDetailedAggregateResults<T>( Seq<Skeleton<T>> baseData , LanguageExt.HashSet<Skeleton> targets , Func<IEnumerable<(T, double)> , T> aggregator , bool simplifyData = false , string[] dimensionsToPreserve = null , Func<IEnumerable<T> , T> groupAggregator = null , bool checkUse = false )
        {
            if ( baseData.Length == 0 || targets.Length == 0 ) return Seq<SkeletonsAccumulator<T>>.Empty;

            baseData = checkUse ? baseData.CheckUse( targets ).Rights() : baseData;

            if ( baseData.Length == 0 ) return Seq<SkeletonsAccumulator<T>>.Empty;

            dimensionsToPreserve ??= Array.Empty<string>();

            return simplifyData
                ? StreamSimplifiedDetailedAggregateResults( baseData , targets , aggregator , dimensionsToPreserve , groupAggregator )
                : StreamSourceDetailedAggregateResults( baseData , targets , aggregator );
        }

        private static AtomHashMap<Skeleton , Seq<(double, Skeleton<T>)>> HashmapDetailedAccumulate<T>( IEnumerable<Skeleton<T>> inputs )
        {
            var hashMap = Prelude.AtomHashMap<Skeleton , Seq<(double, Skeleton<T>)>>();
            inputs.AsParallel()
                .ForAll( sourceSkeleton =>
                {
                    foreach ( var ancestor in sourceSkeleton.Key.Ancestors() )
                    {
                        var resultingWeight = Skeleton.ComputeResultingWeight( sourceSkeleton.Key , ancestor );
                        hashMap.AddOrUpdate( ancestor , some => some.Add( (resultingWeight, sourceSkeleton) ) ,
                            () => Seq.create( (resultingWeight, sourceSkeleton) ) );
                    }
                } );
            return hashMap;
        }

        private static DetailedAggregationResult<T> DetailedTopDownAggregate<T>(
            Seq<Skeleton<T>> baseData ,
            LanguageExt.HashSet<Skeleton> targets ,
            Func<IEnumerable<(T, double)> , T> aggregator ,
            Func<IEnumerable<T> , T> groupAggregator ,
            bool simplifyData = true ,
            string[] dimensionsToPreserve = null ,
            bool checkUse = false )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var results = StreamDetailedAggregateResults( baseData , targets , aggregator , simplifyData , dimensionsToPreserve , groupAggregator , checkUse ).ToArray();
                stopWatch.Stop();

                return new DetailedAggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res , exc =>
            {
                var ex = exc;
                var sb = new StringBuilder();
                sb.AppendLine( ex.Message );

                while ( ex.InnerException != null )
                {
                    ex = ex.InnerException;
                    sb.AppendLine( ex.Message );
                }

                return new DetailedAggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , sb.ToString() );
            } );
        }

        private static IEnumerable<SkeletonsAccumulator<T>> StreamSimplifiedDetailedAggregateResults<T>( Seq<Skeleton<T>> baseData ,
            LanguageExt.HashSet<Skeleton> targets ,
            Func<IEnumerable<(T, double)> , T> aggregator ,
            string[] dimensionsToPreserve ,
            Func<IEnumerable<T> , T> groupAggregator )
        {
            if ( groupAggregator == null ) throw new ArgumentException( "Argument can't be null" , nameof( groupAggregator ) );

            if ( dimensionsToPreserve == null ) throw new ArgumentException( "Argument can't be null" , nameof( dimensionsToPreserve ) );

            baseData = baseData.GroupBy( x => x.Key ).Select( g => g.Aggregate( g.Key , groupAggregator ) ).ToSeq();

            var uniqueTargetBaseBones = targets.SelectMany( t => t.Bones ).GroupBy( b => b.DimensionName ).Where( g => !dimensionsToPreserve.Contains( g.Key ) && g.Distinct().Count() == 1 && !g.Any( b => b.HasWeightElement() ) ).Select( g => g.First() ).ToSeq();

            var (simplifiedData, simplifiedTargets) = uniqueTargetBaseBones.Any()
                ? SimplifyTargets( baseData , targets , uniqueTargetBaseBones , groupAggregator , ( t , _ ) => t )
                : (baseData, targets);

            return GroupTargets( simplifiedTargets.ToArray() , simplifiedData , aggregator , 0 , simplifiedData[0].Key.Bones.Length )
                .Select( s => new SkeletonsAccumulator<T>( s.Key.Add( uniqueTargetBaseBones ) , s.Components , s.Aggregator ) );
        }

        private static IEnumerable<SkeletonsAccumulator<T>> StreamSourceDetailedAggregateResults<T>( Seq<Skeleton<T>> baseData , LanguageExt.HashSet<Skeleton> targets , Func<IEnumerable<(T, double)> , T> aggregator )
            => GroupTargets( targets.ToArray() , baseData , aggregator , 0 , baseData[0].Key.Bones.Length );

        private static IEnumerable<SkeletonsAccumulator<T>> GroupTargets<T>( Skeleton[] targets , Seq<Skeleton<T>> data , Func<IEnumerable<(T, double)> , T> aggregator , int boneIndex , int dimensionsCount )
        {
            if ( targets.Length == 1 && boneIndex == dimensionsCount ) return new[] { new SkeletonsAccumulator<T>( targets[0] , data.Select( s => (Skeleton.ComputeResultingWeight( s.Key , targets[0] ), s) ) , aggregator ) };

            if ( boneIndex >= dimensionsCount ) return Array.Empty<SkeletonsAccumulator<T>>();

            return targets.GroupBy( s => s.GetBone( boneIndex ) ).SelectMany( g => GroupTargets( g.ToArray() , data.AsParallel().Where( s => s.Key.HasAnyBone( boneIndex , g.Key.DescendantsHashSet() ) ).ToSeq().Strict() , aggregator , boneIndex + 1 , dimensionsCount ) );
        }
    }
}