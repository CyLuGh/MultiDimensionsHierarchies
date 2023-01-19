using LanguageExt;
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
        Heuristic = 0 ,

        /// <summary>
        /// Computes all aggregates from source data in a bottom-top way.
        /// </summary>
        BottomTop = 1 ,

        BottomTopCached = 2 ,

        /// <summary>
        /// Computes limited aggregates in top-down way.
        /// </summary>
        TopDown = 3 ,

        BottomTopGroup = 11 ,
        BottomTopDictionary = 12 ,
        BottomTopGroupCached = 13 ,
        BottomTopDictionaryCached = 14 ,
        TopDownGroup = 31 ,
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
        public static AggregationResult<T> Aggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator , Func<IEnumerable<T> , T> groupAggregator = null ,
            Func<T , double , T> weightEffect = null , bool useCachedSkeletons = true , bool checkUse = false ) =>
            Aggregate( method , inputs , aggregator , Array.Empty<Skeleton>() , Seq<Bone>.Empty , groupAggregator ,
                weightEffect , useCachedSkeletons , checkUse );

        public static AggregationResult<T> Aggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator , Seq<Bone> ancestorsFilters , Func<IEnumerable<T> , T> groupAggregator = null ,
            Func<T , double , T> weightEffect = null , bool useCachedSkeletons = true , bool checkUse = false ) =>
            Aggregate( method , inputs , aggregator , Array.Empty<Skeleton>() , ancestorsFilters , groupAggregator ,
                weightEffect , useCachedSkeletons , checkUse );

        public static AggregationResult<T> Aggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator , IEnumerable<Skeleton> targets ,
            Func<IEnumerable<T> , T> groupAggregator = null , Func<T , double , T> weightEffect = null ,
            bool useCachedSkeletons = true , bool checkUse = false ) =>
            Aggregate( method , inputs , aggregator , targets , Seq<Bone>.Empty , groupAggregator , weightEffect ,
                useCachedSkeletons , checkUse );

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
            Func<T , T , T> aggregator , IEnumerable<Skeleton> targets , Seq<Bone> ancestorsFilters ,
            Func<IEnumerable<T> , T> groupAggregator = null , Func<T , double , T> weightEffect = null ,
            bool useCachedSkeletons = true , bool checkUse = false )
        {
            var hashTarget = new LanguageExt.HashSet<Skeleton>().TryAddRange( targets );

            if ( ( method == Method.TopDown || method == Method.TopDownGroup ) && hashTarget.Count == 0 )
                return new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero ,
                    "TopDown method requires targets but none have been defined!" );

            /* Aggregate base data that might have common keys */
            groupAggregator ??= ( items ) => items.Aggregate( aggregator );
            var groupedInputs = inputs.GroupBy( x => x.Key )
                .Select( g => g.Aggregate( g.Key , groupAggregator ) )
                .ToSeq();

            weightEffect ??= ( t , _ ) => t;

            // Check for most well suited method if Heuristic
            if ( method == Method.Heuristic )
            {
                method = FindBestMethod( groupedInputs , hashTarget );
                useCachedSkeletons = UseCache( method );
            }

            return method switch
            {
                Method.TopDown => TopDownAggregate( groupedInputs , hashTarget , groupAggregator , weightEffect ,
                    false , checkUse ) ,
                Method.TopDownGroup => TopDownAggregate( groupedInputs , hashTarget , groupAggregator , weightEffect ,
                    true , checkUse ) ,
                Method.BottomTop => BottomTopAggregate( groupedInputs , aggregator , hashTarget , ancestorsFilters ,
                    weightEffect , useCachedSkeletons ) ,
                Method.BottomTopDictionary => BottomTopDictionaryAggregate( groupedInputs , aggregator , hashTarget ,
                    ancestorsFilters , weightEffect , useCachedSkeletons ) ,
                Method.BottomTopGroup => BottomTopGroupAggregate( groupedInputs , aggregator , hashTarget ,
                    ancestorsFilters , weightEffect , useCachedSkeletons ) ,
                Method.BottomTopDictionaryCached => BottomTopDictionaryAggregate( groupedInputs , aggregator ,
                    hashTarget , ancestorsFilters , weightEffect , true ) ,
                Method.BottomTopGroupCached => BottomTopGroupAggregate( groupedInputs , aggregator , hashTarget ,
                    ancestorsFilters , weightEffect , true ) ,
                _ => new AggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero , "No method was defined." )
            };
        }

        internal static bool UseCache( Method method ) =>
            method switch
            {
                Method.BottomTopCached => true ,
                Method.BottomTopGroupCached => true ,
                Method.BottomTopDictionaryCached => true ,
                _ => false
            };

        internal static Method FindBestMethod<T>( Seq<Skeleton<T>> inputs , LanguageExt.HashSet<Skeleton> targets )
        {
            if ( inputs.Length == 0 || inputs[0].Bones.IsEmpty ) return Method.None;

            if ( !targets.IsEmpty )
            {
                if ( Environment.ProcessorCount < 4 ) return Method.BottomTopGroup;

                return Method.TopDown;
            }
            else
            {
                var complexity = inputs.EstimateComplexity();
                return FindBestBottomTopMethod( complexity , inputs.Length );
            }
        }

        private static Method FindBestBottomTopMethod( long complexity , int inputsCount ) =>
            complexity < 2_500_000 ? FindBestCachedMethod( inputsCount ) : FindBestNotCachedMethod( inputsCount );

        private static Method FindBestCachedMethod( int inputsCount ) =>
            inputsCount < 1_000_000 ? Method.BottomTopGroupCached : Method.BottomTopDictionaryCached;

        private static Method FindBestNotCachedMethod( int inputsCount ) =>
            inputsCount < 1_000_000 ? Method.BottomTopGroup : Method.BottomTopDictionary;

        private static AggregationResult<T> BottomTopAggregate<T>( Seq<Skeleton<T>> baseData ,
            Func<T , T , T> aggregator , LanguageExt.HashSet<Skeleton> targets , Seq<Bone> ancestorsFilters ,
            Func<T , double , T> weightEffect , bool useCachedSkeletons = true )
        {
            // Choose most adequate method?
            return BottomTopGroupAggregate( baseData , aggregator , targets , ancestorsFilters , weightEffect ,
                useCachedSkeletons );
        }

        private static HashMap<string , LanguageExt.HashSet<Bone>>
            BuildFiltersFromTargets( LanguageExt.HashSet<Skeleton> targets ) =>
            HashMap.createRange( targets.SelectMany( t => t.Bones.Values )
                .GroupBy( b => b.DimensionName )
                .Select( g => ( g.Key , HashSet.createRange( g ) ) ) );

        private static Func<Skeleton , Seq<Skeleton>> GetAncestorsBuilder( Seq<Bone> ancestorsFilters ,
            AtomHashMap<string , Skeleton> cache , LanguageExt.HashSet<Skeleton> targets )
        {
            if ( targets.Count == 0 ) return s => s.Ancestors( ancestorsFilters , cache );

            var targetFilters = BuildFiltersFromTargets( targets );
            return s => s.BuildFilteredSkeletons( targetFilters ).Where( x => targets.Contains( x ) ).ToSeq().Strict();
        }

        private static AggregationResult<T> BottomTopGroupAggregate<T>( Seq<Skeleton<T>> baseData ,
            Func<T , T , T> aggregator , LanguageExt.HashSet<Skeleton> targets , Seq<Bone> ancestorsFilters ,
            Func<T , double , T> weightEffect , bool useCachedSkeletons )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var cache = useCachedSkeletons ? Prelude.AtomHashMap<string , Skeleton>() : null;

                var res = baseData.AsParallel()
                    .SelectMany( skeleton => skeleton.Value.Some( v =>
                            GetAncestorsBuilder( ancestorsFilters , cache , targets )( skeleton.Key )
                                .Select( ancestor =>
                                    new Skeleton<T>(
                                        weightEffect( v , Skeleton.ComputeResultingWeight( skeleton.Key , ancestor ) ) ,
                                        ancestor ) ) )
                        .None( () => Seq.empty<Skeleton<T>>() ) )
                    .GroupBy( s => s.Key )
                    .Select( g => g.Aggregate( aggregator ) )
                    .Somes()
                    .ToArray();

                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
            } );

            return f.Match( res => res ,
                exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static AggregationResult<T> BottomTopDictionaryAggregate<T>( Seq<Skeleton<T>> baseData ,
            Func<T , T , T> aggregator , LanguageExt.HashSet<Skeleton> targets , Seq<Bone> ancestorsFilters ,
            Func<T , double , T> weightEffect , bool useCachedSkeletons )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                var results = Prelude.AtomHashMap<Skeleton , Option<T>>();
                var cache = useCachedSkeletons ? Prelude.AtomHashMap<string , Skeleton>() : null;

                Parallel.ForEach( baseData , skeleton =>
                {
                    foreach ( var ancestor in
                             GetAncestorsBuilder( ancestorsFilters , cache , targets )( skeleton.Key ) )
                    {
                        var weight = Skeleton.ComputeResultingWeight( skeleton.Key , ancestor );
                        var wVal = from v in skeleton.Value select weightEffect( v , weight );

                        results.AddOrUpdate( ancestor ,
                            data => from d in data from v in wVal select aggregator( d , v ) , wVal );
                    }
                } );

                var res = results.Select( kvp => new Skeleton<T>( kvp.Value , kvp.Key ) ).ToArray();

                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res , "Process OK" );
            } );

            return f.Match( res => res ,
                exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static (Seq<Skeleton<T>> , LanguageExt.HashSet<Skeleton>) SimplifyTargets<T>(
            Seq<Skeleton<T>> baseData , LanguageExt.HashSet<Skeleton> targets , Seq<Bone> uniqueTargetBaseBones ,
            Func<IEnumerable<T> , T> groupAggregator , Func<T , double , T> weightEffect )
        {
            var uniqueDimensions = uniqueTargetBaseBones.Select( u => u.DimensionName ).ToArray();
            var dataFilter = uniqueTargetBaseBones.Select( b => ( b.DimensionName , b.Descendants() ) ).ToHashMap();

            var simplifiedData = baseData
                .Where( d => dataFilter.All( i =>
                    i.Value.Contains( d.Bones.Find( i.Key ).Some( b => b ).None( () => Bone.None ) ) ) )
                .Select( d => d.Except( uniqueDimensions ) )
                .GroupBy( x => x.Key )
                .Select( g => g.Aggregate( g.Key , groupAggregator , weightEffect ) )
                .ToSeq();

            var hash = new LanguageExt.HashSet<Skeleton>().TryAddRange( targets.Select( s =>
                s.Except( uniqueDimensions ) ) );

            return ( simplifiedData , hash );
        }

        private static AggregationResult<T> TopDownAggregate<T>( Seq<Skeleton<T>> baseData ,
            LanguageExt.HashSet<Skeleton> targets , Func<IEnumerable<T> , T> groupAggregator ,
            Func<T , double , T> weightEffect , bool group = false , bool checkUse = false )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var results =
                    StreamAggregateResults( baseData , targets , groupAggregator , weightEffect , group , checkUse )
                        .ToArray();
                stopWatch.Stop();

                return new AggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res ,
                exc => new AggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        /// <summary>
        /// Create an aggregate by keeping references to base data element. If simplifyData is turned on, there are some changes applied to the base data.
        /// </summary>
        /// <param name="method">Algorithm to use to compute the aggregate</param>
        /// <param name="inputs">Base data</param>
        /// <param name="aggregator"></param>
        /// <param name="targets">Keys to be computed</param>
        /// <param name="simplifyData">Whether the data can be simplified (to speed up computation) or if it should be kept intact</param>
        /// <param name="dimensionsToPreserve">Dimensions that shouldn't be simplified if simplifyData is set to true</param>
        /// <param name="groupAggregator">How base data should be aggregated when simplifyData is set to true</param>
        public static DetailedAggregationResult<T> DetailedAggregate<T>( Method method ,
            IEnumerable<Skeleton<T>> inputs , Func<IEnumerable<(T value , double weight)> , T> aggregator ,
            IEnumerable<Skeleton> targets = null , bool simplifyData = false , string[] dimensionsToPreserve = null ,
            Func<IEnumerable<T> , T> groupAggregator = null ) =>
            DetailedAggregate( method , inputs , aggregator , Seq<Bone>.Empty , targets , simplifyData: simplifyData ,
                dimensionsToPreserve: dimensionsToPreserve , groupAggregator: groupAggregator );

        public static DetailedAggregationResult<T> DetailedAggregate<T>( Method method ,
            IEnumerable<Skeleton<T>> inputs , Func<IEnumerable<(T value , double weight)> , T> aggregator ,
            Seq<Bone> ancestorsFilters , IEnumerable<Skeleton> targets = null , bool useCachedSkeletons = true ,
            bool simplifyData = false , string[] dimensionsToPreserve = null ,
            Func<IEnumerable<T> , T> groupAggregator = null )
        {
            var hashTarget = new LanguageExt.HashSet<Skeleton>();
            if ( targets != null ) hashTarget = hashTarget.TryAddRange( targets );

            if ( method == Method.TopDown && hashTarget.Count == 0 )
                return new DetailedAggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero ,
                    "Method is Targeted but no targets have been defined!" );

            var inputsArray = inputs.ToSeq();
            // Check for most well suited method if Heuristic
            if ( method == Method.Heuristic )
            {
                method = FindBestMethod( inputsArray , hashTarget );
                useCachedSkeletons = UseCache( method );
            }

            return method switch
            {
                Method.TopDown => DetailedTargetedAggregate( inputsArray , aggregator , hashTarget , false ,
                    simplifyData , dimensionsToPreserve , groupAggregator ) ,
                Method.TopDownGroup => DetailedTargetedAggregate( inputsArray , aggregator , hashTarget , true ,
                    simplifyData , dimensionsToPreserve , groupAggregator ) ,

                Method.BottomTop => HeuristicDetailedGroupAggregate( inputsArray , aggregator , ancestorsFilters ,
                    hashTarget , useCachedSkeletons ) ,
                Method.BottomTopDictionary => HeuristicDetailedDictionaryAggregate( inputsArray , aggregator ,
                    ancestorsFilters , hashTarget , useCachedSkeletons ) ,
                Method.BottomTopGroup => HeuristicDetailedGroupAggregate( inputsArray , aggregator , ancestorsFilters ,
                    hashTarget , useCachedSkeletons ) ,
                Method.BottomTopDictionaryCached => HeuristicDetailedDictionaryAggregate( inputsArray , aggregator ,
                    ancestorsFilters , hashTarget , true ) ,
                Method.BottomTopGroupCached => HeuristicDetailedGroupAggregate( inputsArray , aggregator ,
                    ancestorsFilters , hashTarget , true ) ,
                _ => new DetailedAggregationResult<T>( AggregationStatus.NO_RUN , TimeSpan.Zero ,
                    "No method was defined." )
            };
        }

        private static DetailedAggregationResult<T> HeuristicDetailedGroupAggregate<T>( Seq<Skeleton<T>> baseData ,
            Func<IEnumerable<(T , double)> , T> aggregator , Seq<Bone> ancestorsFilters ,
            LanguageExt.HashSet<Skeleton> targets , bool useCachedSkeletons )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var cache = useCachedSkeletons ? Prelude.AtomHashMap<string , Skeleton>() : null;

                var res = baseData.AsParallel()
                    .SelectMany<Skeleton<T> , (Skeleton Key , double Weight , Skeleton<T> Input)>( skeleton =>
                        skeleton.Value.Some( _ =>
                                GetAncestorsBuilder( ancestorsFilters , cache , targets )( skeleton.Key )
                                    .Select( ancestor => ( Ancestor: ancestor ,
                                        Weight: Skeleton.ComputeResultingWeight( skeleton.Key , ancestor ) ,
                                        Input: skeleton ) ) )
                            .None( () => Seq.empty<(Skeleton , double , Skeleton<T>)>() ) )
                    .Where( r => targets.Count == 0 || targets.Contains( r.Key ) )
                    .GroupBy( s => s.Key )
                    .Select( g =>
                        new SkeletonsAccumulator<T>( g.Key , g.Select( x => ( x.Weight , x.Input ) ) , aggregator ) )
                    .ToArray();

                stopWatch.Stop();

                return new DetailedAggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res ,
                    "Process OK" );
            } );

            return f.Match( res => res ,
                exc => new DetailedAggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static DetailedAggregationResult<T> HeuristicDetailedDictionaryAggregate<T>( Seq<Skeleton<T>> baseData ,
            Func<IEnumerable<(T , double)> , T> aggregator , Seq<Bone> ancestorsFilters ,
            LanguageExt.HashSet<Skeleton> targets , bool useCachedSkeletons )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();

                var results = Prelude.AtomHashMap<Skeleton , List<(double , Skeleton<T>)>>();
                var cache = useCachedSkeletons ? Prelude.AtomHashMap<string , Skeleton>() : null;

                Parallel.ForEach( baseData , skeleton =>
                {
                    foreach ( var ancestor in
                             GetAncestorsBuilder( ancestorsFilters , cache , targets )( skeleton.Key ) )
                    {
                        var weight = Skeleton.ComputeResultingWeight( skeleton.Key , ancestor );

                        results.AddOrUpdate( ancestor , list =>
                        {
                            list.Add( ( weight , skeleton ) );
                            return list;
                        } , new List<(double , Skeleton<T>)> { ( weight , skeleton ) } );
                    }
                } );

                var res = results.AsParallel()
                    .Select( kvp => new SkeletonsAccumulator<T>( kvp.Key , kvp.Value , aggregator ) )
                    .ToArray();

                stopWatch.Stop();

                return new DetailedAggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , res ,
                    "Process OK" );
            } );

            return f.Match( res => res ,
                exc => new DetailedAggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        private static DetailedAggregationResult<T> DetailedTargetedAggregate<T>( Seq<Skeleton<T>> baseData ,
            Func<IEnumerable<(T , double)> , T> aggregator , LanguageExt.HashSet<Skeleton> targets ,
            bool group = false , bool simplifyData = false , string[] dimensionsToPreserve = null ,
            Func<IEnumerable<T> , T> groupAggregator = null , bool checkUse = false )
        {
            var f = Prelude.Try( () =>
            {
                var stopWatch = Stopwatch.StartNew();
                var results = StreamDetailedAggregateResults( baseData , targets , aggregator , group , simplifyData ,
                        dimensionsToPreserve , groupAggregator , checkUse )
                    .ToArray();
                stopWatch.Stop();

                return new DetailedAggregationResult<T>( AggregationStatus.OK , stopWatch.Elapsed , results );
            } );

            return f.Match( res => res ,
                exc => new DetailedAggregationResult<T>( AggregationStatus.ERROR , TimeSpan.Zero , exc.Message ) );
        }

        public static IEnumerable<Skeleton<T>> StreamAggregateResults<T>( Seq<Skeleton<T>> baseData ,
            LanguageExt.HashSet<Skeleton> targets , Func<IEnumerable<T> , T> groupAggregator ,
            Func<T , double , T> weightEffect = null , bool group = false , bool checkUse = false )
        {
            if ( baseData.Length == 0 || targets.Length == 0 ) return Seq<Skeleton<T>>.Empty;

            baseData = checkUse ? baseData.CheckUse( targets ).Rights() : baseData;

            if ( baseData.Length == 0 ) return Seq<Skeleton<T>>.Empty;

            weightEffect ??= ( t , _ ) => t;

            var uniqueTargetBaseBones = targets.SelectMany( t => t.Bones.Values )
                .GroupBy( b => b.DimensionName )
                .Where( g => g.Distinct().Count() == 1 && !g.Any( b => b.HasWeightElement() ) )
                .Select( g => g.First() )
                .ToSeq();

            var (simplifiedData , simplifiedTargets) = uniqueTargetBaseBones.Any()
                ? SimplifyTargets( baseData , targets , uniqueTargetBaseBones , groupAggregator , weightEffect )
                : ( baseData , targets );

            if ( group )
            {
                return GroupTargets( simplifiedTargets.ToArray() , simplifiedData , groupAggregator , weightEffect , 0 ,
                        simplifiedData[0].Key.Bones.Keys.ToArray() )
                    .Select( r => r.Add( uniqueTargetBaseBones ) );
            }

            return simplifiedTargets.AsParallel()
                .Select( t =>
                    t.GetComposingSkeletons( simplifiedData ).Aggregate( t , groupAggregator , weightEffect ) )
                .Select( r => r.Add( uniqueTargetBaseBones ) );
        }

        public static IEnumerable<SkeletonsAccumulator<T>> StreamDetailedAggregateResults<T>(
            Seq<Skeleton<T>> baseData , LanguageExt.HashSet<Skeleton> targets ,
            Func<IEnumerable<(T , double)> , T> aggregator , bool group = false , bool simplifyData = false ,
            string[] dimensionsToPreserve = null , Func<IEnumerable<T> , T> groupAggregator = null ,
            bool checkUse = false )
        {
            if ( baseData.Length == 0 || targets.Length == 0 ) return Seq<SkeletonsAccumulator<T>>.Empty;

            baseData = checkUse ? baseData.CheckUse( targets ).Rights() : baseData;

            if ( baseData.Length == 0 ) return Seq<SkeletonsAccumulator<T>>.Empty;

            return simplifyData
                ? StreamSimplifiedDetailedAggregateResults( baseData , targets , aggregator , dimensionsToPreserve ,
                    groupAggregator , group )
                : StreamSourceDetailedAggregateResults( baseData , targets , aggregator , group );
        }

        private static IEnumerable<SkeletonsAccumulator<T>> StreamSimplifiedDetailedAggregateResults<T>(
            Seq<Skeleton<T>> baseData , LanguageExt.HashSet<Skeleton> targets ,
            Func<IEnumerable<(T , double)> , T> aggregator , string[] dimensionsToPreserve ,
            Func<IEnumerable<T> , T> groupAggregator , bool group = false )
        {
            if ( groupAggregator == null )
                throw new ArgumentException( "Argument can't be null" , nameof(groupAggregator) );

            if ( dimensionsToPreserve == null )
                throw new ArgumentException( "Argument can't be null" , nameof(dimensionsToPreserve) );

            baseData = baseData.GroupBy( x => x.Key ).Select( g => g.Aggregate( g.Key , groupAggregator ) ).ToSeq();

            var uniqueTargetBaseBones = targets.SelectMany( t => t.Bones.Values )
                .GroupBy( b => b.DimensionName )
                .Where( g =>
                    !dimensionsToPreserve.Contains( g.Key ) && g.Distinct().Count() == 1 &&
                    !g.Any( b => b.HasWeightElement() ) )
                .Select( g => g.First() )
                .ToSeq();

            //TODO: check if weight effect is needed
            var (simplifiedData , simplifiedTargets) = uniqueTargetBaseBones.Any()
                ? SimplifyTargets( baseData , targets , uniqueTargetBaseBones , groupAggregator , ( t , _ ) => t )
                : ( baseData , targets );

            if ( group )
            {
                return GroupTargets( simplifiedTargets.ToArray() , simplifiedData , aggregator , 0 ,
                        simplifiedData[0].Key.Bones.Keys.ToArray() )
                    .Select( s =>
                        new SkeletonsAccumulator<T>( s.Key.Add( uniqueTargetBaseBones ) , s.Components ,
                            s.Aggregator ) );
            }

            return simplifiedTargets.AsParallel()
                .Select( skeleton =>
                {
                    var components = skeleton.GetComposingSkeletons( simplifiedData )
                        .Select( cmp => ( Skeleton.ComputeResultingWeight( cmp.Key , skeleton ) , cmp ) );
                    return new SkeletonsAccumulator<T>( skeleton.Add( uniqueTargetBaseBones ) , components ,
                        aggregator );
                } );
        }

        private static IEnumerable<SkeletonsAccumulator<T>> StreamSourceDetailedAggregateResults<T>(
            Seq<Skeleton<T>> baseData , LanguageExt.HashSet<Skeleton> targets ,
            Func<IEnumerable<(T , double)> , T> aggregator , bool group = false )
        {
            if ( group )
                return GroupTargets( targets.ToArray() , baseData , aggregator , 0 ,
                    baseData[0].Key.Bones.Keys.ToArray() );

            return targets.AsParallel()
                .Select( skeleton =>
                {
                    var components = skeleton.GetComposingSkeletons( baseData )
                        .Select( cmp => ( Skeleton.ComputeResultingWeight( cmp.Key , skeleton ) , cmp ) );
                    return new SkeletonsAccumulator<T>( skeleton , components , aggregator );
                } );
        }

        private static IEnumerable<Skeleton<T>> GroupTargets<T>( Skeleton[] targets , Seq<Skeleton<T>> data ,
            Func<IEnumerable<T> , T> groupAggregator , Func<T , double , T> weightEffect , int boneIndex ,
            string[] dimensions )
        {
            if ( targets.Length == 1 && boneIndex == dimensions.Length )
                return new[] { data.Aggregate( targets[0] , groupAggregator , weightEffect ) };

            if ( boneIndex >= dimensions.Length ) return Array.Empty<Skeleton<T>>();

            return targets.AsParallel()
                .WithDegreeOfParallelism( boneIndex == 0 ? Environment.ProcessorCount : 1 )
                .GroupBy( s => s.GetBone( dimensions[boneIndex] ) )
                .SelectMany( g => GroupTargets( g.ToArray() ,
                    data.Where( s => s.Key.HasAnyBone( g.Key.Descendants() ) ).ToSeq() , groupAggregator ,
                    weightEffect , boneIndex + 1 , dimensions ) );
        }

        private static IEnumerable<SkeletonsAccumulator<T>> GroupTargets<T>( Skeleton[] targets ,
            Seq<Skeleton<T>> data , Func<IEnumerable<(T , double)> , T> aggregator , int boneIndex ,
            string[] dimensions )
        {
            if ( targets.Length == 1 && boneIndex == dimensions.Length )
                return new[]
                {
                    new SkeletonsAccumulator<T>( targets[0] ,
                        data.Select( s => ( Skeleton.ComputeResultingWeight( s.Key , targets[0] ) , s ) ) ,
                        aggregator )
                };

            if ( boneIndex >= dimensions.Length ) return Array.Empty<SkeletonsAccumulator<T>>();

            return targets.AsParallel()
                .WithDegreeOfParallelism( boneIndex == 0 ? Environment.ProcessorCount : 1 )
                .GroupBy( s => s.GetBone( dimensions[boneIndex] ) )
                .SelectMany( g => GroupTargets( g.ToArray() ,
                    data.Where( s => s.Key.HasAnyBone( g.Key.Descendants() ) ).ToSeq() , aggregator , boneIndex + 1 ,
                    dimensions ) );
        }
    }
}