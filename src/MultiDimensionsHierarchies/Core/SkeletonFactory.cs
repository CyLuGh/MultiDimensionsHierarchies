using LanguageExt;
using LanguageExt.Common;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class SkeletonFactory
    {
        public static Seq<Skeleton> BuildSkeletons(
            IEnumerable<string> inputs ,
            Func<string , string[]> parser ,
            Arr<Dimension> dimensions
        )
        {
            return inputs
                .AsParallel()
                .Select( input => BuildSkeleton( input , parser , dimensions ) )
                .ToSeq();
        }

        public static Skeleton BuildSkeleton(
            string input ,
            Func<string , string[]> parser ,
            Arr<Dimension> dimensions
        )
        {
            return TryBuildSkeleton( input , parser , dimensions )
                .Match( s => s , e => throw e );
        }

        public static Seq<Either<Error , Skeleton>> TryBuildSkeletons(
            IEnumerable<string> inputs ,
            Func<string , string[]> parser ,
            Arr<Dimension> dimensions
        )
        {
            return inputs
                .AsParallel()
                .Select( input => TryBuildSkeleton( input , parser , dimensions ) )
                .ToSeq();
        }

        public static Either<Error , Skeleton> TryBuildSkeleton(
            string input ,
            Func<string , string[]> parser ,
            Arr<Dimension> dimensions
        )
        {
            var split = parser( input );
            if ( split.Length != dimensions.Length )
                return Error.New( new ArgumentException( $"Dimensions count doesn't match parsed string {input}" ) );

            var elements = split.Select( ( s , i ) => dimensions[i].Find( s ) ).Somes().ToSeq();
            var missings = dimensions.Select( x => x.Name )
                .Except( elements.Select( o => o.DimensionName ) )
                .ToSeq();

            if ( missings.Any() )
            {
                var missing = string.Join( ", " , missings );
                return Error.New( new ArgumentException( $"Some dimensions couldn't be resolved: {missing}." ) );
            }

            return new Skeleton( elements );
        }

        /// <summary>
        /// Build skeletons from source items.
        /// </summary>
        /// <typeparam name="T">Type of source data</typeparam>
        /// <param name="inputs">Source data</param>
        /// <param name="parser">How to find defined bone in dimension in source item</param>
        /// <param name="dimensions">Dimensions with their hierarchies</param>
        /// <param name="dimensionsOfInterest">(Optional) Subset of dimensions to be used</param>
        /// <returns>Sequence of properly defined Skeletons</returns>
        /// <exception cref="ArgumentException">Throws if a dimension of interest is not included in dimensions</exception>
        /// <exception cref="ApplicationException">Throws if skeleton creation can't find a required value in dimensions</exception>
        public static Seq<Skeleton> BuildSkeletons<T>(
            IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            return CreateSkeletons( inputs , parser , dimensions , dimensionsOfInterest ).ToSeq();
        }

        /// <summary>
        /// Build skeletons with their associated value from source items.
        /// </summary>
        /// <typeparam name="T">Type of source data</typeparam>
        /// <typeparam name="TI">Type of value associated to skeleton</typeparam>
        /// <param name="inputs">Source data</param>
        /// <param name="parser">How to find defined bone in dimension in source item</param>
        /// <param name="evaluator">How to find associated value in source item</param>
        /// <param name="dimensions">Dimensions with their hierarchies</param>
        /// <param name="dimensionsOfInterest">(Optional) Subset of dimensions to be used</param>
        /// <returns>Sequence of properly defined Skeletons, containing data</returns>
        /// <exception cref="ArgumentException">Throws if a dimension of interest is not included in dimensions</exception>
        /// <exception cref="ApplicationException">Throws if skeleton creation can't find a required value in dimensions</exception>
        public static Seq<Skeleton<T>> BuildSkeletons<T, TI>(
            IEnumerable<TI> inputs ,
            Func<TI , string , string> parser ,
            Func<TI , T> evaluator ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            return CreateSkeletons( inputs , parser , evaluator , dimensions , dimensionsOfInterest ).ToSeq();
        }

        /// <summary>
        /// Build skeletons from string sources
        /// </summary>
        /// <param name="stringInputs">Inputs, in string format</param>
        /// <param name="partitioner">How to split the input string</param>
        /// <param name="selectioner">How to find needed dimension in input string parts</param>
        /// <param name="dimensions">Dimensions with their hierarchies</param>
        /// <param name="dimensionsOfInterest">(Optional) Subset of dimensions to be used</param>
        /// <returns>Sequence of properly defined Skeletons</returns>
        /// <exception cref="ArgumentException">Throws if a dimension of interest is not included in dimensions</exception>
        /// <exception cref="ApplicationException">Throws if skeleton creation can't find a required value in dimensions</exception>
        public static Seq<Skeleton> BuildSkeletons(
            IEnumerable<string> stringInputs ,
            Func<string , string[]> partitioner ,
            Func<string[] , string , string> selectioner ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            return BuildSkeletons( stringInputs.Select( stringInput => partitioner( stringInput ) ) ,
                selectioner , dimensions , dimensionsOfInterest );
        }

        /// <summary>
        /// Build skeletons from source items.
        /// </summary>
        /// <typeparam name="T">Type of source data</typeparam>
        /// <param name="inputs">Source data</param>
        /// <param name="parser">How to find defined bone in dimension in source item</param>
        /// <param name="dimensions">Dimensions with their hierarchies</param>
        /// <param name="dimensionsOfInterest">(Optional) Subset of dimensions to be used</param>
        /// <returns>Sequence of properly defined Skeletons</returns>
        /// <exception cref="ArgumentException">Throws if a dimension of interest is not included in dimensions</exception>
        public static Seq<Either<Error , Skeleton>> TryBuildSkeletons<T>(
            IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            return TryCreateSkeletons( inputs , parser , dimensions , dimensionsOfInterest ).ToSeq();
        }

        /// <summary>
        /// Build skeletons with their associated value from source items.
        /// </summary>
        /// <typeparam name="T">Type of source data</typeparam>
        /// <typeparam name="TI">Type of value associated to skeleton</typeparam>
        /// <param name="inputs">Source data</param>
        /// <param name="parser">How to find defined bone in dimension in source item</param>
        /// <param name="evaluator">How to find associated value in source item</param>
        /// <param name="dimensions">Dimensions with their hierarchies</param>
        /// <param name="dimensionsOfInterest">(Optional) Subset of dimensions to be used</param>
        /// <returns>Sequence of properly defined Skeletons, containing data</returns>
        /// <exception cref="ArgumentException">Throws if a dimension of interest is not included in dimensions</exception>
        public static Seq<Either<Error , Skeleton<T>>> TryBuildSkeletons<T, TI>(
            IEnumerable<TI> inputs ,
            Func<TI , string , string> parser ,
            Func<TI , T> evaluator ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            return TryCreateSkeletons( inputs , parser , evaluator , dimensions , dimensionsOfInterest ).ToSeq();
        }

        /// <summary>
        /// Build skeletons from string sources
        /// </summary>
        /// <param name="stringInputs">Inputs, in string format</param>
        /// <param name="partitioner">How to split the input string</param>
        /// <param name="selectioner">How to find needed dimension in input string parts</param>
        /// <param name="dimensions">Dimensions with their hierarchies</param>
        /// <param name="dimensionsOfInterest">(Optional) Subset of dimensions to be used</param>
        /// <returns>Sequence of properly defined Skeletons</returns>
        /// <exception cref="ArgumentException">Throws if a dimension of interest is not included in dimensions</exception>
        public static Seq<Either<Error , Skeleton>> TryBuildSkeletons(
            IEnumerable<string> stringInputs ,
            Func<string , string[]> partitioner ,
            Func<string[] , string , string> selectioner ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            return TryBuildSkeletons( stringInputs.Select( stringInput => partitioner( stringInput ) ) ,
                selectioner , dimensions , dimensionsOfInterest );
        }

        internal static IEnumerable<Skeleton> CreateSkeletons<T>(
            IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
            => CreateSkeletons( inputs , parser , _ => Unit.Default , dimensions , dimensionsOfInterest )
                .Select( s => s.Key );

        internal static IEnumerable<Either<Error , Skeleton>> TryCreateSkeletons<T>(
            IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
            => TryCreateSkeletons( inputs , parser , _ => Unit.Default , dimensions , dimensionsOfInterest )
                .Map( x => x.Map( s => s.Key ) );

        internal static IEnumerable<Skeleton<U>> CreateSkeletons<T, U>(
            IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            Func<T , U> evaluator ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            var seqDimensions = dimensions.ToSeq();

            if ( dimensionsOfInterest != null )
            {
                var missings = dimensionsOfInterest
                .Select( d => seqDimensions.Find( o => o.Name.Equals( d ) )
                    .Some( _ => string.Empty ).None( () => d ) ).Where( s => !string.IsNullOrEmpty( s ) )
                .ToSeq();

                if ( missings.Any() )
                {
                    var missing = string.Join( ", " , missings );
                    throw new ArgumentException( $"Some dimensions of interest aren't available in the dimensions collection: {missing}." );
                }
            }
            else
            {
                dimensionsOfInterest = seqDimensions.Select( d => d.Name ).ToArray();
            }

            var dimLookup = seqDimensions.SelectMany( d => d.Flatten() )
               .ToLookup( b => (b.DimensionName, b.Label) );

            return inputs.AsParallel().SelectMany( input =>
            {
                var bones = dimensionsOfInterest.Select( d => FindBones( input , parser , d , dimLookup ) )
                    .ToArray();

                if ( bones.Lefts().Any() )
                {
                    var missing = string.Join( ", " , bones.Lefts() );
                    throw new ApplicationException( $"Couldn't find some dimensions values: {missing}" );
                }

                return bones.Rights()
                    .Aggregate( new List<Seq<Bone>>() ,
                        ( list , bs ) =>
                        {
                            return list.Count == 0
                                ? bs.Select( b => Seq.create( b ) ).ToList()
                                : list.Cartesian( bs , ( seq , b ) => seq.Add( b ) ).ToList();
                        } )
                    .Select( bs => new Skeleton<U>( evaluator( input ) , bs ) );
            } );
        }

        internal static IEnumerable<Either<Error , Skeleton<U>>> TryCreateSkeletons<T, U>(
            IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            Func<T , U> evaluator ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            var seqDimensions = dimensions.ToSeq();

            if ( dimensionsOfInterest != null )
            {
                var missings = dimensionsOfInterest
                .Select( d => seqDimensions.Find( o => o.Name.Equals( d ) )
                    .Some( _ => string.Empty ).None( () => d ) ).Where( s => !string.IsNullOrEmpty( s ) )
                .ToSeq();

                if ( missings.Any() )
                {
                    var missing = string.Join( ", " , missings );
                    throw new ArgumentException( $"Some dimensions of interest aren't available in the dimensions collection: {missing}." );
                }
            }
            else
            {
                dimensionsOfInterest = seqDimensions.Select( d => d.Name ).ToArray();
            }

            var dimLookup = seqDimensions.SelectMany( d => d.Flatten() )
                .ToLookup( b => (b.DimensionName, b.Label) );

            return inputs.AsParallel().SelectMany( input =>
            {
                var bones = dimensionsOfInterest
                    .Select( d => FindBones( input , parser , d , dimLookup ) )
                    .ToArray();
                return TryCreateSkeletons( input , evaluator , bones );
            } );
        }

        internal static IEnumerable<Either<Error , Skeleton<U>>> TryCreateSkeletons<T, U>(
            T input ,
            Func<T , U> evaluator ,
            Either<string , Bone[]>[] bones )
        {
            if ( bones.Lefts().Any() )
            {
                var missing = string.Join( ", " , bones.Lefts() );
                yield return Error.New( $"Couldn't find some dimensions values for {input}: {missing}" );
            }
            else
            {
                foreach ( var s in bones.Rights()
                             .Aggregate( new List<Seq<Bone>>() ,
                                 ( list , bs ) =>
                                 {
                                     return list.Count == 0
                                         ? bs.Select( b => Seq.create( b ) ).ToList()
                                         : list.Cartesian( bs , ( seq , b ) => seq.Add( b ) ).ToList();
                                 } )
                             .Select( bs => new Skeleton<U>( evaluator( input ) , bs ) ) )
                {
                    yield return s;
                }
            }
        }

        internal static Either<string , Bone[]> FindBones<T>(
            T input ,
            Func<T , string , string> parser ,
            string dimensionName ,
            ILookup<(string, string) , Bone> dimensionsLookup )
        {
            var boneLabel = parser( input , dimensionName );

            /* Because the same label can be found at different places in the hierarchy, we may have several bones for a single label */
            var bones = dimensionsLookup[(dimensionName, boneLabel)].ToArray();

            if ( bones.Length == 0 )
                return $"Couldn't find {boneLabel} in dimension {dimensionName}";
            return bones;
        }

        public static Seq<Skeleton> FastBuild<T>(
            IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions )
            => FastBuild( inputs , parser , _ => Unit.Default , dimensions )
               .Select( s => s.Key );

        public static Seq<Skeleton> FastBuild<T, TK>(
            IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions ,
            Func<T , TK> keySelector ,
            Func<IGrouping<TK , T> , T> inputsAggregator )
            => FastBuild( inputs , parser , _ => Unit.Default , dimensions , keySelector , inputsAggregator )
                .Select( s => s.Key );

        /// <summary>
        /// Build skeletons from raw data input, only returning valid items. No exception will be thrown for invalid inputs. Group raw data to reduce iterations.
        /// </summary>
        /// <param name="inputs">Raw data</param>
        /// <param name="parser">Delegate to extract needed dimensions from input</param>
        /// <param name="evaluator">Delegate to determine value associated to input</param>
        /// <param name="dimensions">Dimensions to be used for hierarchical aggregates</param>
        /// <param name="keySelector">Delegate to build group keys from input</param>
        /// <param name="inputsAggregator">Delegate that will be applied to group of input data</param>
        /// <param name="groupAggregator">Optional delegate that will be applied to group of skeletons; use to limit skeleton output</param>
        /// <param name="checkTargets">Optional collection of targets; use to limit skeletons output</param>
        /// <typeparam name="TI">Raw data type</typeparam>
        /// <typeparam name="TO">Associated value type</typeparam>
        /// <typeparam name="TK">Type of the key that will be used to group raw inputs</typeparam>
        /// <returns>Sequence of skeletons</returns>
        public static Seq<Skeleton<TO>> FastBuild<TI, TO, TK>
        (
            IEnumerable<TI> inputs ,
            Func<TI , string , string> parser ,
            Func<TI , TO> evaluator ,
            IEnumerable<Dimension> dimensions ,
            Func<TI , TK> keySelector ,
            Func<IGrouping<TK , TI> , TI> inputsAggregator ,
            Func<IEnumerable<TO> , TO> groupAggregator = null ,
            IEnumerable<Skeleton> checkTargets = null
        )
        {
            inputs = inputs
                .GroupBy( x => keySelector( x ) )
                .Select( g => inputsAggregator( g ) );

            return FastBuild( inputs , parser , evaluator , dimensions , groupAggregator , checkTargets );
        }

        /// <summary>
        /// Build skeletons from raw data input, only returning valid items. No exception will be thrown for invalid inputs.
        /// </summary>
        /// <param name="inputs">Raw data</param>
        /// <param name="parser">Delegate to extract needed dimensions from input</param>
        /// <param name="evaluator">Delegate to determine value associated to input</param>
        /// <param name="dimensions">Dimensions to be used for hierarchical aggregates</param>
        /// <param name="groupAggregator">Optional delegate that will be applied to group of skeletons; use to limit skeleton output</param>
        /// <param name="checkTargets">Optional collection of targets; use to limit skeletons output</param>
        /// <typeparam name="TI">Raw data type</typeparam>
        /// <typeparam name="TO">Associated value type</typeparam>
        /// <returns>Sequence of skeletons</returns>
        public static Seq<Skeleton<TO>> FastBuild<TI, TO>
        (
            IEnumerable<TI> inputs ,
            Func<TI , string , string> parser ,
            Func<TI , TO> evaluator ,
            IEnumerable<Dimension> dimensions ,
            Func<IEnumerable<TO> , TO> groupAggregator = null ,
            IEnumerable<Skeleton> checkTargets = null
        )
        {
            var skeletons = FastParse( inputs , parser , evaluator , dimensions );

            if ( groupAggregator != null )
            {
                skeletons = skeletons.GroupBy( x => x.Key )
                    .Select( g => g.Aggregate( g.Key , groupAggregator ) );
            }

            if ( checkTargets != null )
            {
                var bonesPerDimension = checkTargets
                                .AsParallel()
                                .SelectMany( s => s.Bones )
                                .GroupBy( b => b.DimensionName )
                                .ToDictionary( g => g.Key , g => HashSet.createRange( g.Flatten().Distinct() ) );

                skeletons = skeletons.Where( s => s.Key.CheckBones( bonesPerDimension ) );
            }

            return skeletons.ToSeq();
        }

        internal static ParallelQuery<Skeleton<TO>> FastParse<TI, TO>(
            IEnumerable<TI> inputs ,
            Func<TI , string , string> parser ,
            Func<TI , TO> evaluator ,
            IEnumerable<Dimension> dimensions
        )
        {
            var dimSeq = dimensions
                .Select( d => (d.Name, Bones: d.Flatten().GroupBy( b => b.Label ).ToDictionary( g => g.Key , g => g.ToSeq().Strict() )) )
                .ToSeq()
                .Strict();

            return FastParse( inputs , parser , evaluator , dimSeq );
        }

        internal static ParallelQuery<Skeleton<TO>> FastParse<TI, TO>(
            IEnumerable<TI> inputs ,
            Func<TI , string , string> parser ,
            Func<TI , TO> evaluator ,
            Seq<(string Name, Dictionary<string , Seq<Bone>> Bones)> dimensions
        )
        {
            return inputs
                .AsParallel()
                .SelectMany( input => FastParse( input , parser , evaluator , dimensions ) );
        }

        internal static ParallelQuery<(TI Input, Skeleton<TO> Result)> FastTagParse<TI, TO>(
            IEnumerable<TI> inputs ,
            Func<TI , string , string> parser ,
            Func<TI , TO> evaluator ,
            Seq<(string Name, Dictionary<string , Seq<Bone>> Bones)> dimensions
        )
        {
            return inputs
                .AsParallel()
                .SelectMany( input => FastParse( input , parser , evaluator , dimensions )
                    .Select( r => (input, r) ) );
        }

        public static Seq<Skeleton<TO>> FastParse<TI, TO>(
            TI input ,
            Func<TI , string , string> parser ,
            Func<TI , TO> evaluator ,
            Seq<(string Name, Dictionary<string , Seq<Bone>> Bones)> dimensions
        )
        {
            var bones = dimensions.Select( d => d.Bones.TryGetValue( parser( input , d.Name ) , out var b ) ? b : Seq<Bone>.Empty );
            if ( bones.Any( b => b.IsEmpty ) )
                return Seq<Skeleton<TO>>.Empty;

            var components = bones.Aggregate( new List<Seq<Bone>>() ,
                ( list , bs ) =>
                {
                    return list.Count == 0
                        ? bs.Select( b => Seq.create( b ) ).ToList()
                        : list.Cartesian( bs , ( seq , b ) => seq.Add( b ) ).ToList();
                } );

            return components
                .Select( bs => new Skeleton<TO>( evaluator( input ) , bs ) )
                .ToSeq();
        }
    }
}