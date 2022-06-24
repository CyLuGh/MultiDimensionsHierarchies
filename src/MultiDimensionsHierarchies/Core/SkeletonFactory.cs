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
        public static Seq<Skeleton> BuildSkeletons<T>( IEnumerable<T> inputs ,
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
        public static Seq<Skeleton<T>> BuildSkeletons<T, TI>( IEnumerable<TI> inputs ,
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
        public static Seq<Skeleton> BuildSkeletons( IEnumerable<string> stringInputs ,
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
        public static Seq<Either<Error , Skeleton>> TryBuildSkeletons<T>( IEnumerable<T> inputs ,
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
        public static Seq<Either<Error , Skeleton<T>>> TryBuildSkeletons<T, TI>( IEnumerable<TI> inputs ,
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
        public static Seq<Either<Error , Skeleton>> TryBuildSkeletons( IEnumerable<string> stringInputs ,
            Func<string , string[]> partitioner ,
            Func<string[] , string , string> selectioner ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            return TryBuildSkeletons( stringInputs.Select( stringInput => partitioner( stringInput ) ) ,
                selectioner , dimensions , dimensionsOfInterest );
        }

        internal static IEnumerable<Skeleton> CreateSkeletons<T>( IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
            => CreateSkeletons( inputs , parser , _ => Unit.Default , dimensions , dimensionsOfInterest )
                .Select( s => s.Key );

        internal static IEnumerable<Either<Error , Skeleton>> TryCreateSkeletons<T>( IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
            => TryCreateSkeletons( inputs , parser , _ => Unit.Default , dimensions , dimensionsOfInterest )
                .Map( x => x.Map( s => s.Key ) );

        internal static IEnumerable<Skeleton<U>> CreateSkeletons<T, U>( IEnumerable<T> inputs ,
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

            return inputs.AsParallel().SelectMany( input =>
            {
                var bones = dimensionsOfInterest.Select( d => FindBones( input , parser , d , seqDimensions ) )
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

        internal static IEnumerable<Either<Error , Skeleton<U>>> TryCreateSkeletons<T, U>( IEnumerable<T> inputs ,
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

            return inputs.AsParallel().SelectMany( input =>
            {
                var bones = dimensionsOfInterest.Select( d => FindBones( input , parser , d , seqDimensions ) )
                    .ToArray();
                return TryCreateSkeletons( input , evaluator , bones );
            } );
        }

        internal static IEnumerable<Either<Error , Skeleton<U>>> TryCreateSkeletons<T, U>( T input , Func<T , U> evaluator , Either<string , Bone[]>[] bones )
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

        internal static Either<string , Bone[]> FindBones<T>( T input , Func<T , string , string> parser , string dimensionName , Seq<Dimension> dimensions )
        {
            var boneLabel = parser( input , dimensionName );
            var bones = dimensions.Find( d => d.Name.Equals( dimensionName ) )
                .Some( d => d.Flatten().Where( b => b.Label.Equals( boneLabel ) ).ToArray() )
                .None( () => Array.Empty<Bone>() );

            if ( bones.Length == 0 )
                return $"Couldn't find {boneLabel} in dimension {dimensionName}";
            return bones;
        }
    }
}