using LanguageExt;
using MoreLinq;
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
        /// Compute every aggregate from base input data. This method is the <b>slowest</b> option but has the less memory growth.
        /// Computing all aggregates is time consuming, prefer using the method with defined targets if possible.
        /// </summary>
        /// <typeparam name="T">Type of data that will be aggregated.</typeparam>
        /// <param name="inputs">Data to be aggregated, with their hierarchical information.</param>
        /// <param name="groupAggregator">How a group of T should be aggregated to create a new T result.</param>
        /// <param name="weightEffect"><i>Optional.</i> How relative weight between hierarchical elements should be applied. No effect by default.</param>
        /// <returns>Aggregation result, with valid aggregations, status and information message.</returns>
        public static AggregationResult<T> Aggregate<T>( IEnumerable<Skeleton<T>> inputs , Func<IEnumerable<T> , T> groupAggregator , Func<T , double , T> weightEffect = null )
        {
            /* Aggregate base data that might have common keys */
            var groupedInputs = inputs.GroupBy( x => x.Key ).Select( g => g.Aggregate( g.Key , groupAggregator ) ).ToSeq();

            weightEffect ??= ( t , _ ) => t;

            return DownTopGroupAggregate( groupedInputs , groupAggregator , weightEffect );
        }

        /// <summary>
        /// Compute every aggregate from base input data. This method is <b>faster</b> option but can consume more memory.
        /// Computing all aggregates is time consuming, prefer using the method with defined targets if possible.
        /// </summary>
        /// <typeparam name="T">Type of data that will be aggregated.</typeparam>
        /// <param name="inputs">Data to be aggregated, with their hierarchical information.</param>
        /// <param name="aggregator">How two instances of T should be aggregated to create a new T result.</param>
        /// <param name="weightEffect"><i>Optional.</i> How relative weight between hierarchical elements should be applied. No effect by default.</param>
        /// <returns>Aggregation result, with valid aggregations, status and information message.</returns>
        public static AggregationResult<T> Aggregate<T>( IEnumerable<Skeleton<T>> inputs , Func<T , T , T> aggregator , Func<T , double , T> weightEffect = null )
        {
            /* Aggregate base data that might have common keys */
            T groupAggregator( IEnumerable<T> items ) => items.AsParallel().Aggregate( aggregator );
            var groupedInputs = inputs.GroupBy( x => x.Key ).Select( g => g.Aggregate( g.Key , groupAggregator ) ).ToSeq();

            weightEffect ??= ( t , _ ) => t;

            return DownTopHashMapAggregate( groupedInputs , aggregator , weightEffect );
        }

        /// <summary>
        /// Compute aggregates defined by a set of targets.
        /// </summary>
        /// <typeparam name="T">Type of data that will be aggregated.</typeparam>
        /// <param name="inputs">Data to be aggregated, with their hierarchical information.</param>
        /// <param name="targets">Defined set of targeted aggregation, with their hierarchical information.</param>
        /// <param name="aggregator">How two instances of T should be aggregated to create a new T result.</param>
        /// <param name="groupAggregator"><i>Optional.</i> How a group of T should be aggregated to create a new T result.</param>
        /// <param name="weightEffect"><i>Optional.</i> How relative weight between hierarchical elements should be applied. No effect by default.</param>
        /// <returns>Aggregation result, with valid aggregations, status and information message.</returns>
        public static AggregationResult<T> Aggregate<T>( IEnumerable<Skeleton<T>> inputs , IEnumerable<Skeleton> targets , Func<T , T , T> aggregator , Func<IEnumerable<T> , T> groupAggregator = null , Func<T , double , T> weightEffect = null )
        {
            var targetsHash = HashSet.createRange( targets );

            if ( targetsHash.IsEmpty ) return new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "No targets have been defined!" );

            /* Aggregate base data that might have common keys */
            groupAggregator ??= ( items ) => items.AsParallel().Aggregate( aggregator );
            var groupedInputs = inputs.GroupBy( x => x.Key ).Select( g => g.Aggregate( g.Key , groupAggregator ) ).ToSeq();

            weightEffect ??= ( t , _ ) => t;

            return TopDownAggregate( groupedInputs , targetsHash , groupAggregator , weightEffect );
        }

        /// <summary>
        /// Compute aggregates defined by a set of targets.
        /// </summary>
        /// <typeparam name="T">Type of data that will be aggregated.</typeparam>
        /// <param name="baseData">Data to be aggregated, with their hierarchical information.</param>
        /// <param name="targets">Defined set of targeted aggregation, with their hierarchical information.</param>
        /// <param name="groupAggregator">How a group of T should be aggregated to create a new T result.</param>
        /// <param name="weightEffect"><i>Optional.</i> How relative weight between hierarchical elements should be applied. No effect by default.</param>
        /// <param name="checkUse">Whether or not the algorithm should filter base data to only include relevant items. False by default.</param>
        /// <returns>Enumerable of aggregation results.</returns>
        public static IEnumerable<Skeleton<T>> StreamAggregateResults<T>( Seq<Skeleton<T>> baseData , LanguageExt.HashSet<Skeleton> targets , Func<IEnumerable<T> , T> groupAggregator , Func<T , double , T> weightEffect = null , bool checkUse = false )
        {
            if ( baseData.Length == 0 || targets.Length == 0 ) return Seq<Skeleton<T>>.Empty;

            baseData = checkUse ? baseData.CheckUse( targets ).Rights() : baseData;

            if ( baseData.Length == 0 ) return Seq<Skeleton<T>>.Empty;

            weightEffect ??= ( t , _ ) => t;

            var uniqueTargetBaseBones = targets.SelectMany( t => t.Bones )
                .GroupBy( b => b.DimensionName )
                .Where( g => g.Distinct().Count() == 1 && !g.Any( b => b.HasWeightElement() ) )
                .Select( g => g.First() )
                .ToSeq();

            var (simplifiedData, simplifiedTargets) = uniqueTargetBaseBones.Any()
                ? SimplifyTargets( baseData , targets , uniqueTargetBaseBones , groupAggregator , weightEffect )
                : (baseData, targets);

            return GroupTargets( simplifiedTargets.ToArray() , simplifiedData , groupAggregator , weightEffect , 0 , simplifiedData[0].Key.Bones.Length ).Select( r => r.Add( uniqueTargetBaseBones ) );
        }

        private static (Seq<Skeleton<T>>, LanguageExt.HashSet<Skeleton>) SimplifyTargets<T>( Seq<Skeleton<T>> baseData , LanguageExt.HashSet<Skeleton> targets , Seq<Bone> uniqueTargetBaseBones , Func<IEnumerable<T> , T> groupAggregator , Func<T , double , T> weightEffect )
        {
            var uniqueDimensions = uniqueTargetBaseBones.Select( u => u.DimensionName ).ToArray();
            var dataFilter = uniqueTargetBaseBones.Select( b => (b.DimensionName, Set: HashSet.createRange( b.Descendants() )) );

            var simplifiedData = baseData
                .Where( d => dataFilter
                    .All( i => i.Set.Contains( d.Bones.Find( b => b.DimensionName == i.DimensionName ).Some( b => b ).None( () => Bone.None ) ) ) )
                .Select( d => d.Except( uniqueDimensions ) )
                .GroupBy( x => x.Key )
                .Select( g => g.Aggregate( g.Key , groupAggregator , weightEffect ) )
                .ToSeq();

            var hash = HashSet.createRange( targets.Select( s => s.Except( uniqueDimensions ) ) );

            return (simplifiedData, hash);
        }

        private static AggregationResult<T> TopDownAggregate<T>( Seq<Skeleton<T>> baseData , LanguageExt.HashSet<Skeleton> targets , Func<IEnumerable<T> , T> groupAggregator , Func<T , double , T> weightEffect , bool checkUse = false )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var results = StreamAggregateResults( baseData , targets , groupAggregator , weightEffect , checkUse ).ToArray();
                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
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

                return new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , sb.ToString() );
            } );
        }

        private static AggregationResult<T> DownTopGroupAggregate<T>( Seq<Skeleton<T>> data , Func<IEnumerable<T> , T> groupAggregator , Func<T , double , T> weightEffect )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                var groups = GroupAccumulate( data , Seq<Seq<Bone>>.Empty , groupAggregator , weightEffect , 0 , data[0].Bones.Length );
                var res = groups.GroupBy( x => x.Key )
                    .Select( group => new Skeleton<T>( groupAggregator( group.Select( x => x.Value ).Somes() ) , group.Key ) )
                    .ToSeq().Strict();
                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
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

                return new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , sb.ToString() );
            } );
        }

        private static AggregationResult<T> DownTopHashMapAggregate<T>( Seq<Skeleton<T>> data , Func<T , T , T> aggregator , Func<T , double , T> weightEffect )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                var hashMap = HashMapAccumulate( data , aggregator , weightEffect );
                var res = hashMap.AsParallel().Select( s => new Skeleton<T>( s.Value , s.Key ) ).ToSeq().Strict();
                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
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

                return new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , sb.ToString() );
            } );
        }

        private static AtomHashMap<Skeleton , Option<T>> HashMapAccumulate<T>( Seq<Skeleton<T>> data , Func<T , T , T> aggregator , Func<T , double , T> weightEffect )
        {
            var hashMap = Prelude.AtomHashMap<Skeleton , Option<T>>();
            data.AsParallel()
                .ForAll( sourceSkeleton =>
                {
                    foreach ( var ancestor in sourceSkeleton.Key.Ancestors() )
                    {
                        var resultingWeight = Skeleton.ComputeResultingWeight( sourceSkeleton.Key , ancestor );
                        var value = from v in sourceSkeleton.Value select weightEffect( v , resultingWeight );

                        hashMap.AddOrUpdate( ancestor , some => from s in some from v in value select aggregator( s , v ) , () => value );
                    }
                } );
            return hashMap;
        }

        private static Seq<Skeleton<T>> GroupAccumulate<T>( Seq<Skeleton<T>> data , Seq<Seq<Bone>> bonesAncestors , Func<IEnumerable<T> , T> groupAggregator , Func<T , double , T> weightEffect , int boneIndex , int dimensionsCount )
        {
            if ( data.Length > 0 && boneIndex == dimensionsCount )
            {
                var sourceSkeleton = data[0];
                var grouped = data.Aggregate( groupAggregator );

                return bonesAncestors.CombineSeq()
                                     .AsParallel()
                                     .WithMergeOptions( ParallelMergeOptions.NotBuffered )
                                     .Select( skeleton =>
                                     {
                                         var resultingWeight = Skeleton.ComputeResultingWeight( sourceSkeleton.Key , skeleton );
                                         var value = from g in grouped from v in g.Value select weightEffect( v , resultingWeight );

                                         return new Skeleton<T>( value , skeleton );
                                     } )
                                     .ToSeq()
                                     .Strict();
            }

            if ( boneIndex >= dimensionsCount ) return Seq<Skeleton<T>>.Empty;

            return data.GroupBy( s => s.Key.GetBone( boneIndex ) )
                       .Select( group =>
                       {
                           var ancestors = bonesAncestors.Add( group.Key.Ancestors() );
                           return GroupAccumulate( group.ToSeq() , ancestors , groupAggregator , weightEffect , boneIndex + 1 , dimensionsCount );
                       } )
                       .AsParallel()
                       .ToSeq()
                       .Flatten()
                       .Strict()
                       /* Code below will make things a bit slower but will greatly preserve memory */
                       .GroupBy( x => x.Key )
                       .Select( group => new Skeleton<T>( groupAggregator( group.Select( x => x.Value ).Somes() ) , group.Key ) )
                       .AsParallel()
                       .ToSeq()
                       .Strict();
        }

        private static IEnumerable<Skeleton<T>> GroupTargets<T>( Skeleton[] targets , Seq<Skeleton<T>> data , Func<IEnumerable<T> , T> groupAggregator , Func<T , double , T> weightEffect , int boneIndex , int dimensionsCount )
        {
            if ( targets.Length == 1 && boneIndex == dimensionsCount )
                return new[] { data.Aggregate( targets[0] , groupAggregator , weightEffect ) };

            if ( boneIndex >= dimensionsCount )
                return Array.Empty<Skeleton<T>>();

            return targets.GroupBy( s => s.GetBone( boneIndex ) )
                .SelectMany( g =>
                    GroupTargets( g.ToArray() ,
                        data.AsParallel().Where( s => s.Key.HasAnyBone( boneIndex , g.Key.DescendantsHashSet() ) ).ToSeq().Strict() ,
                        groupAggregator ,
                        weightEffect ,
                        boneIndex + 1 ,
                        dimensionsCount ) );
        }
    }
}