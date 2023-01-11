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
                    .Aggregate<IEnumerable<Bone> , IEnumerable<Skeleton>>( new[] { new Skeleton() } ,
                        ( skels , bs ) => skels.Cartesian( bs , ( s , b ) => s.Add( b ) ) )
                   .Select( skel => new Skeleton<U>( evaluator( input ) , skel ) );
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
                    .Aggregate<IEnumerable<Bone> , IEnumerable<Skeleton>>( new[] { new Skeleton() } ,
                        ( skels , bs ) => skels.Cartesian( bs , ( s , b ) => s.Add( b ) ) )
                   .Select( skel => new Skeleton<U>( evaluator( input ) , skel ) ) )
                {
                    yield return s;
                }
            }
        }

        internal static Either<string , Bone[]> FindBones<T>(
            T input ,
            Func<T , string , string> parser ,
            string dimensionName ,
            ILookup<(string, string) ,
            Bone> dimensionsLookup )
        {
            var boneLabel = parser( input , dimensionName );

            /* Because the same label can be found at different places in the hierarchy, we may have several bones for a single label */
            var bones = dimensionsLookup[(dimensionName, boneLabel)].ToArray();

            if ( bones.Length == 0 )
                return $"Couldn't find {boneLabel} in dimension {dimensionName}";
            return bones;
        }

        public static Seq<Skeleton<O>> FastBuildAndCheck<I, O>(
            Seq<I> inputs ,
            Func<I , string , string> parser ,
            Func<I , O> evaluator ,
            Seq<Dimension> dimensions ,
            IEnumerable<Skeleton> targets
        )
        {
            var bonesPerDimension = targets
                .AsParallel()
                .SelectMany( s => s.Bones )
                .GroupBy( b => b.DimensionName )
                .ToDictionary( g => g.Key , g => HashSet.createRange( g.Flatten().Distinct() ) );

            return FastBuild( inputs , parser , evaluator , dimensions )
                .Where( s => s.Key.CheckBones( bonesPerDimension ) );
        }

        public static Seq<Skeleton<O>> FastBuild<I, O>(
            Seq<I> inputs ,
            Func<I , string , string> parser ,
            Func<I , O> evaluator ,
            Seq<Dimension> dimensions
        )
        {
            var dimSeq = dimensions
                .Select( d => (d.Name, Bones: d.Flatten().GroupBy( b => b.Label ).ToDictionary( g => g.Key , g => g.ToSeq().Strict() )) )
                .Strict();

            return FastBuild( inputs , parser , evaluator , dimSeq );
        }

        private static Seq<Skeleton<O>> FastBuild<I, O>(
            Seq<I> inputs ,
            Func<I , string , string> parser ,
            Func<I , O> evaluator ,
            Seq<(string Name, Dictionary<string , Seq<Bone>> Bones)> dimensions
        )
        {
            return inputs
                .AsParallel()
                .SelectMany<I , Skeleton<O>>( input =>
                {
                    var bones = dimensions.Select( d => d.Bones.TryGetValue( parser( input , d.Name ) , out var b ) ? b : Seq<Bone>.Empty );

                    if ( bones.Any( b => b.IsEmpty ) )
                        return Seq<Skeleton<O>>.Empty;

                    var components = bones.Aggregate<Seq<Bone> , List<Seq<Bone>>>( new List<Seq<Bone>>() ,
                        ( list , bs ) =>
                        {
                            if ( list.Count == 0 )
                                return bs.Select( b => Seq.create( b ) ).ToList();

                            return list.Cartesian( bs , ( seq , b ) => seq.Add( b ) ).ToList();
                        } );

                    return components.Select( bs => new Skeleton<O>( evaluator( input ) , bs ) )
                        .ToSeq();
                } )
                .ToSeq();
        }
    }
}