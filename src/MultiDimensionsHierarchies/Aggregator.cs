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
        Heuristic = 0,

        /// <summary>
        /// Computes all aggregates from source data in a bottom-top way.
        /// </summary>
        BottomTop = 1,

        BottomTopCached = 2,

        /// <summary>
        /// Computes limited aggregates in top-down way.
        /// </summary>
        TopDown = 3,

        TopDownGroup = 31,

        BottomTopGroup = 11,
        BottomTopDictionary = 12,
        BottomTopGroupCached = 13,
        BottomTopDictionaryCached = 14,

        None = 255
    }

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
        public static AggregationResult<T> Aggregate<T>(
            Method method ,
            IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator ,
            Func<IEnumerable<T> , T> groupAggregator = null ,
            Func<T , double , T> weightEffect = null ,
            bool useCachedSkeletons = true )
            => Aggregate( method , inputs , aggregator , Array.Empty<Skeleton>() , Seq<Bone>.Empty , groupAggregator , weightEffect , useCachedSkeletons );

        public static AggregationResult<T> Aggregate<T>(
            Method method ,
            IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator ,
            Seq<Bone> ancestorsFilters ,
            Func<IEnumerable<T> , T> groupAggregator = null ,
            Func<T , double , T> weightEffect = null ,
            bool useCachedSkeletons = true )
            => Aggregate( method , inputs , aggregator , Array.Empty<Skeleton>() , ancestorsFilters , groupAggregator , weightEffect , useCachedSkeletons );

        public static AggregationResult<T> Aggregate<T>(
            Method method ,
            IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator ,
            IEnumerable<Skeleton> targets ,
            Func<IEnumerable<T> , T> groupAggregator = null ,
            Func<T , double , T> weightEffect = null ,
            bool useCachedSkeletons = true )
            => Aggregate( method , inputs , aggregator , targets , Seq<Bone>.Empty , groupAggregator , weightEffect , useCachedSkeletons );

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
        public static AggregationResult<T> Aggregate<T>(
            Method method ,
            IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator ,
            IEnumerable<Skeleton> targets ,
            Seq<Bone> ancestorsFilters ,
            Func<IEnumerable<T> , T> groupAggregator = null ,
            Func<T , double , T> weightEffect = null ,
            bool useCachedSkeletons = true )
        {
            var hashTarget = new LanguageExt.HashSet<Skeleton>().TryAddRange( targets );

            if ( method == Method.TopDown && hashTarget.Count == 0 )
                return new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "TopDown method requires targets but none have been defined!" );

            /* Aggregate base data that might have common keys */
            groupAggregator ??= ( items ) => items.Aggregate( aggregator );
            var groupedInputs = inputs.GroupBy( x => x.Key )
                .Select( g => g.Aggregate( g.Key , groupAggregator ) )
                .ToArray();

            weightEffect ??= ( t , _ ) => t;

            // Check for most well suited method if Heuristic
            if ( method == Method.Heuristic )
            {
                method = FindBestMethod( groupedInputs , hashTarget );
                useCachedSkeletons = UseCache( method );
            }

            return method switch
            {
                Method.TopDown => TopDownAggregate( groupedInputs , hashTarget , groupAggregator , weightEffect , false ),
                Method.TopDownGroup => TopDownAggregate( groupedInputs , hashTarget , groupAggregator , weightEffect , true ),
                Method.BottomTop => BottomTopAggregate( groupedInputs , aggregator , hashTarget , ancestorsFilters , weightEffect , useCachedSkeletons ),
                Method.BottomTopDictionary => BottomTopDictionaryAggregate( groupedInputs , aggregator , hashTarget , ancestorsFilters , weightEffect , useCachedSkeletons ),
                Method.BottomTopGroup => BottomTopGroupAggregate( groupedInputs , aggregator , hashTarget , ancestorsFilters , weightEffect , useCachedSkeletons ),
                Method.BottomTopDictionaryCached => BottomTopDictionaryAggregate( groupedInputs , aggregator , hashTarget , ancestorsFilters , weightEffect , true ),
                Method.BottomTopGroupCached => BottomTopGroupAggregate( groupedInputs , aggregator , hashTarget , ancestorsFilters , weightEffect , true ),
                _ => new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "No method was defined." )
            };
        }

        internal static bool UseCache( Method method )
            => method switch
            {
                Method.BottomTopCached => true,
                Method.BottomTopGroupCached => true,
                Method.BottomTopDictionaryCached => true,
                _ => false
            };

        internal static Method FindBestMethod<T>( Skeleton<T>[] inputs , LanguageExt.HashSet<Skeleton> targets )
        {
            if ( inputs.Length == 0 || inputs[0].Bones.IsEmpty )
                return Method.None;

            if ( !targets.IsEmpty )
            {
                if ( Environment.ProcessorCount < 4 )
                    return Method.BottomTopGroup;

                return Method.TopDown;
            }
            else
            {
                var complexity = inputs.EstimateComplexity();
                return FindBestBottomTopMethod( complexity , inputs.Length );
            }
        }

        private static Method FindBestBottomTopMethod( long complexity , int inputsCount )
            => complexity < 2_500_000 ? FindBestCachedMethod( inputsCount ) : FindBestNotCachedMethod( inputsCount );

        private static Method FindBestCachedMethod( int inputsCount )
            => inputsCount < 1_000_000 ? Method.BottomTopGroupCached : Method.BottomTopDictionaryCached;

        private static Method FindBestNotCachedMethod( int inputsCount )
            => inputsCount < 1_000_000 ? Method.BottomTopGroup : Method.BottomTopDictionary;

        private static AggregationResult<T> BottomTopAggregate<T>( Skeleton<T>[] baseData ,
                                                                  Func<T , T , T> aggregator ,
                                                                  LanguageExt.HashSet<Skeleton> targets ,
                                                                  Seq<Bone> ancestorsFilters ,
                                                                  Func<T , double , T> weightEffect ,
                                                                  bool useCachedSkeletons = true )
        {
            // Choose most adequate method?
            return BottomTopGroupAggregate( baseData , aggregator , targets , ancestorsFilters , weightEffect , useCachedSkeletons );
        }

        private static HashMap<string , LanguageExt.HashSet<Bone>> BuildFiltersFromTargets( LanguageExt.HashSet<Skeleton> targets )
           => HashMap.createRange( targets.SelectMany( t => t.Bones )
               .GroupBy( b => b.DimensionName )
               .Select( g => (g.Key, HashSet.createRange( g )) ) );

        private static Func<Skeleton , Seq<Skeleton>> GetAncestorsBuilder( Seq<Bone> ancestorsFilters , NonBlocking.ConcurrentDictionary<string , Skeleton> cache , LanguageExt.HashSet<Skeleton> targets )
        {
            if ( targets.Count == 0 )
                return s => s.Ancestors( ancestorsFilters , cache );

            var targetFilters = BuildFiltersFromTargets( targets );
            return s => s.BuildFilteredSkeletons( targetFilters )
                .Where( x => targets.Contains( x ) )
                .ToSeq().Strict();
        }

        private static AggregationResult<T> BottomTopGroupAggregate<T>( Skeleton<T>[] baseData ,
                                                                  Func<T , T , T> aggregator ,
                                                                  LanguageExt.HashSet<Skeleton> targets ,
                                                                  Seq<Bone> ancestorsFilters ,
                                                                  Func<T , double , T> weightEffect ,
                                                                  bool useCachedSkeletons )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var cache = useCachedSkeletons ?
                    new NonBlocking.ConcurrentDictionary<string , Skeleton>( Environment.ProcessorCount , 1_000_000 ) : null;

                var res = baseData
                    .AsParallel()
                    .SelectMany( skeleton =>
                        skeleton.Value.Some( v =>
                            GetAncestorsBuilder( ancestorsFilters , cache , targets )( skeleton.Key )
                                    .Select( ancestor =>
                                        new Skeleton<T>( weightEffect( v , Skeleton.ComputeResultingWeight( skeleton.Key , ancestor ) ) , ancestor ) ) )
                            .None( () => Seq.empty<Skeleton<T>>() ) )
                    .GroupBy( s => s.Key )
                    .Select( g => g.Aggregate( aggregator ) )
                    .Somes()
                    .ToArray();

                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
            } );

            return f.Match( res => res , exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static AggregationResult<T> BottomTopDictionaryAggregate<T>( Skeleton<T>[] baseData ,
                                                                  Func<T , T , T> aggregator ,
                                                                  LanguageExt.HashSet<Skeleton> targets ,
                                                                  Seq<Bone> ancestorsFilters ,
                                                                  Func<T , double , T> weightEffect ,
                                                                  bool useCachedSkeletons )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                var results = new NonBlocking.ConcurrentDictionary<Skeleton , Option<T>>( Environment.ProcessorCount , 500_000 );
                var cache = useCachedSkeletons ?
                    new NonBlocking.ConcurrentDictionary<string , Skeleton>( Environment.ProcessorCount , 1_000_000 ) : null;

                Parallel.ForEach( baseData , skeleton =>
                {
                    foreach ( var ancestor in GetAncestorsBuilder( ancestorsFilters , cache , targets )( skeleton.Key ) )
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
               .Select( d => d.Except( uniqueDimensions ) )
               .GroupBy( x => x.Key )
               .Select( g => g.Aggregate( g.Key , groupAggregator , weightEffect ) )
               .ToArray();

            var hash = new LanguageExt.HashSet<Skeleton>().TryAddRange( targets.Select( s => s.Except( uniqueDimensions ) ) );

            return (simplifiedData, hash);
        }

        private static AggregationResult<T> TopDownAggregate<T>( Skeleton<T>[] baseData ,
                                                                 LanguageExt.HashSet<Skeleton> targets ,
                                                                 Func<IEnumerable<T> , T> groupAggregator ,
                                                                 Func<T , double , T> weightEffect ,
                                                                 bool group = false )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var results = StreamAggregateResults( baseData , targets , groupAggregator , weightEffect , group )
                    .ToArray();
                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res , exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        public static DetailedAggregationResult<T> DetailedAggregate<T>(
            Method method ,
            IEnumerable<Skeleton<T>> inputs ,
            Func<IEnumerable<(T value, double weight)> , T> aggregator ,
            IEnumerable<Skeleton> targets = null ,
            bool simplifyData = false ,
            string[] dimensionsToPreserve = null ,
            Func<IEnumerable<T> , T> groupAggregator = null )
            => DetailedAggregate( method ,
                inputs ,
                aggregator ,
                Seq<Bone>.Empty ,
                targets ,
                simplifyData: simplifyData ,
                dimensionsToPreserve: dimensionsToPreserve ,
                groupAggregator: groupAggregator );

        public static DetailedAggregationResult<T> DetailedAggregate<T>(
            Method method ,
            IEnumerable<Skeleton<T>> inputs ,
            Func<IEnumerable<(T value, double weight)> , T> aggregator ,
            Seq<Bone> ancestorsFilters ,
            IEnumerable<Skeleton> targets = null ,
            bool useCachedSkeletons = true ,
            bool simplifyData = false ,
            string[] dimensionsToPreserve = null ,
            Func<IEnumerable<T> , T> groupAggregator = null )
        {
            var hashTarget = new LanguageExt.HashSet<Skeleton>();
            if ( targets != null )
                hashTarget = hashTarget.TryAddRange( targets );

            if ( method == Method.TopDown && hashTarget.Count == 0 )
                return new DetailedAggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "Method is Targeted but no targets have been defined!" );

            var inputsArray = inputs.ToArray();
            // Check for most well suited method if Heuristic
            if ( method == Method.Heuristic )
            {
                method = FindBestMethod( inputsArray , hashTarget );
                useCachedSkeletons = UseCache( method );
            }

            return method switch
            {
                Method.TopDown => DetailedTargetedAggregate( inputsArray , aggregator , hashTarget , simplifyData , dimensionsToPreserve , groupAggregator ),
                Method.TopDownGroup => DetailedTargetedAggregate( inputsArray , aggregator , hashTarget , simplifyData , dimensionsToPreserve , groupAggregator ),

                Method.BottomTop => HeuristicDetailedGroupAggregate( inputsArray , aggregator , ancestorsFilters , hashTarget , useCachedSkeletons ),
                Method.BottomTopDictionary => HeuristicDetailedDictionaryAggregate( inputsArray , aggregator , ancestorsFilters , hashTarget , useCachedSkeletons ),
                Method.BottomTopGroup => HeuristicDetailedGroupAggregate( inputsArray , aggregator , ancestorsFilters , hashTarget , useCachedSkeletons ),
                Method.BottomTopDictionaryCached => HeuristicDetailedDictionaryAggregate( inputsArray , aggregator , ancestorsFilters , hashTarget , true ),
                Method.BottomTopGroupCached => HeuristicDetailedGroupAggregate( inputsArray , aggregator , ancestorsFilters , hashTarget , true ),
                _ => new DetailedAggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "No method was defined." )
            };
        }

        private static DetailedAggregationResult<T> HeuristicDetailedGroupAggregate<T>( Skeleton<T>[] baseData ,
                                                                  Func<IEnumerable<(T, double)> , T> aggregator ,
                                                                  Seq<Bone> ancestorsFilters ,
                                                                  LanguageExt.HashSet<Skeleton> targets ,
                                                                  bool useCachedSkeletons )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var cache = useCachedSkeletons ?
                    new NonBlocking.ConcurrentDictionary<string , Skeleton>( Environment.ProcessorCount , 1_000_000 ) : null;

                var res = baseData
                    .AsParallel()
                    .SelectMany<Skeleton<T> , (Skeleton Key, double Weight, Skeleton<T> Input)>( skeleton =>
                        skeleton.Value.Some( v =>
                                GetAncestorsBuilder( ancestorsFilters , cache , targets )( skeleton.Key )
                                    .Select( ancestor =>
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

        private static DetailedAggregationResult<T> HeuristicDetailedDictionaryAggregate<T>( Skeleton<T>[] baseData ,
                                                                  Func<IEnumerable<(T, double)> , T> aggregator ,
                                                                  Seq<Bone> ancestorsFilters ,
                                                                  LanguageExt.HashSet<Skeleton> targets ,
                                                                  bool useCachedSkeletons )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                var results =
                    new NonBlocking.ConcurrentDictionary<Skeleton , List<(double, Skeleton<T>)>>( Environment.ProcessorCount , 500_000 );
                var cache = useCachedSkeletons ?
                    new NonBlocking.ConcurrentDictionary<string , Skeleton>( Environment.ProcessorCount , 1_000_000 ) : null;

                Parallel.ForEach( baseData , skeleton =>
                {
                    foreach ( var ancestor in GetAncestorsBuilder( ancestorsFilters , cache , targets )( skeleton.Key ) )
                    {
                        var weight = Skeleton.ComputeResultingWeight( skeleton.Key , ancestor );

                        results.AddOrUpdate( ancestor , new List<(double, Skeleton<T>)> { (weight, skeleton) } ,
                            ( _ , list ) => { list.Add( (weight, skeleton) ); return list; } );
                    }
                } );

                var res = results
                    .AsParallel()
                    .Select( kvp => new SkeletonsAccumulator<T>( kvp.Key , kvp.Value , aggregator ) )
                    .ToArray();

                stopWatch.Stop();

                return new DetailedAggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
            } );

            return f.Match( res => res , exc => new DetailedAggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static DetailedAggregationResult<T> DetailedTargetedAggregate<T>( Skeleton<T>[] baseData ,
                                                                    Func<IEnumerable<(T, double)> , T> aggregator ,
                                                                    LanguageExt.HashSet<Skeleton> targets ,
                                                                    bool simplifyData = false ,
                                                                    string[] dimensionsToPreserve = null ,
                                                                    Func<IEnumerable<T> , T> groupAggregator = null )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var results = StreamDetailedAggregateResults( baseData , targets , aggregator , simplifyData , dimensionsToPreserve , groupAggregator ).ToArray();
                stopWatch.Stop();

                return new DetailedAggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res , exc => new DetailedAggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        public static IEnumerable<Skeleton<T>> StreamAggregateResults<T>( Skeleton<T>[] baseData ,
                                                                 LanguageExt.HashSet<Skeleton> targets ,
                                                                 Func<IEnumerable<T> , T> groupAggregator ,
                                                                 Func<T , double , T> weightEffect = null ,
                                                                 bool group = false )
        {
            weightEffect ??= ( t , _ ) => t;

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

            var simplifiedMap = simplifiedData.ToDictionary( s => s.Key );

            var results = !group
                ? simplifiedTargets
                    .AsParallel()
                    .Select( t => t.GetComposingSkeletons( simplifiedMap ).Aggregate( t , groupAggregator , weightEffect ) )
                    .Select( r => r.Add( uniqueTargetBaseBones ) )
                : GroupTargets( simplifiedTargets.ToArray() , simplifiedData , groupAggregator , weightEffect , 0 , simplifiedData[0].Key.Bones.Length )
                    .Select( r => r.Add( uniqueTargetBaseBones ) );

            return results;
        }

        private static Skeleton<T>[] GroupTargets<T>( Skeleton[] targets ,
            Skeleton<T>[] data ,
            Func<IEnumerable<T> , T> groupAggregator ,
            Func<T , double , T> weightEffect ,
            int boneIndex ,
            int dimensionsCount )
        {
            if ( targets.Length == 1 )
                return new[] { data.Aggregate( targets[0] , groupAggregator , weightEffect ) };

            if ( boneIndex >= dimensionsCount )
                return Array.Empty<Skeleton<T>>();

            return targets
                .AsParallel()
                .WithDegreeOfParallelism( boneIndex == 0 ? Environment.ProcessorCount : 1 )
                .GroupBy( s => s.GetBone( boneIndex ) )
                .SelectMany( g =>
                    GroupTargets( g.ToArray() , data.Where( s => s.Key.HasAnyBone( g.Key.Descendants() ) ).ToArray() , groupAggregator , weightEffect , boneIndex + 1 , dimensionsCount ) )
                .ToArray();
        }

        public static IEnumerable<SkeletonsAccumulator<T>> StreamDetailedAggregateResults<T>( Skeleton<T>[] baseData ,
                                                                 LanguageExt.HashSet<Skeleton> targets ,
                                                                 Func<IEnumerable<(T, double)> , T> aggregator ,
                                                                 bool simplifyData = false ,
                                                                 string[] dimensionsToPreserve = null ,
                                                                 Func<IEnumerable<T> , T> groupAggregator = null )
        {
            return simplifyData
                ? StreamSimplifiedDetailedAggregateResults( baseData , targets , aggregator , dimensionsToPreserve , groupAggregator )
                : StreamSourceDetailedAggregateResults( baseData , targets , aggregator );
        }

        private static IEnumerable<SkeletonsAccumulator<T>> StreamSimplifiedDetailedAggregateResults<T>( Skeleton<T>[] baseData ,
                                                                 LanguageExt.HashSet<Skeleton> targets ,
                                                                 Func<IEnumerable<(T, double)> , T> aggregator ,
                                                                 string[] dimensionsToPreserve ,
                                                                 Func<IEnumerable<T> , T> groupAggregator )
        {
            if ( groupAggregator == null )
                throw new ArgumentException( "Argument can't be null" , nameof( groupAggregator ) );

            if ( dimensionsToPreserve == null )
                throw new ArgumentException( "Argument can't be null" , nameof( dimensionsToPreserve ) );

            baseData = baseData.GroupBy( x => x.Key )
                    .Select( g => g.Aggregate( g.Key , groupAggregator ) )
                    .ToArray();

            var uniqueTargetBaseBones = targets
                   .SelectMany( t => t.Bones )
                   .GroupBy( b => b.DimensionName )
                   .Where( g => !dimensionsToPreserve.Contains( g.Key ) && g.Distinct().Count() == 1 && !g.Any( b => b.HasWeightElement() ) )
                   .Select( g => g.First() )
                   .ToSeq();

            //TODO: check if weight effect is needed
            var (simplifiedData, simplifiedTargets) =
            uniqueTargetBaseBones.Any()
                ? SimplifyTargets( baseData , targets , uniqueTargetBaseBones , groupAggregator , ( t , _ ) => t )
                : (baseData, targets);

            var dictionary = simplifiedData.AsParallel().GroupBy( s => s.Key )
                    .ToDictionary( g => g.Key , g => g.ToSeq().Strict() );

            var results = simplifiedTargets
                .AsParallel()
                .Select( skeleton =>
                {
                    var components = skeleton
                        .GetComposingSkeletons( dictionary )
                        .Select( cmp => (Skeleton.ComputeResultingWeight( cmp.Key , skeleton ), cmp) );
                    return new SkeletonsAccumulator<T>( skeleton.Add( uniqueTargetBaseBones ) , components , aggregator );
                } );

            return results;
        }

        private static IEnumerable<SkeletonsAccumulator<T>> StreamSourceDetailedAggregateResults<T>( Skeleton<T>[] baseData ,
                                                                 LanguageExt.HashSet<Skeleton> targets ,
                                                                 Func<IEnumerable<(T, double)> , T> aggregator )
        {
            var dictionary = baseData.AsParallel().GroupBy( s => s.Key )
                    .ToDictionary( g => g.Key , g => g.ToSeq().Strict() );

            return targets
                .AsParallel()
                .Select( skeleton =>
                {
                    var components = skeleton
                        .GetComposingSkeletons( dictionary )
                        .Select( cmp => (Skeleton.ComputeResultingWeight( cmp.Key , skeleton ), cmp) );
                    return new SkeletonsAccumulator<T>( skeleton , components , aggregator );
                } );
        }
    }
}