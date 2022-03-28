using LanguageExt;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class SkeletonFactory
    {
        public static Seq<Skeleton> BuildSkeletons<T>( IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            return CreateSkeletons( inputs , parser , dimensions , dimensionsOfInterest ).ToSeq();
        }

        public static Seq<Skeleton<T>> BuildSkeletons<T, TI>( IEnumerable<TI> inputs ,
          Func<TI , string , string> parser ,
          Func<TI , T> evaluator ,
          IEnumerable<Dimension> dimensions ,
          string[] dimensionsOfInterest = null )
        {
            return CreateSkeletons( inputs , parser , evaluator , dimensions , dimensionsOfInterest ).ToSeq();
        }

        public static Seq<Skeleton> BuildSkeletons( IEnumerable<string> stringInputs ,
            Func<string , string[]> partitioner ,
            Func<string[] , string , string> selectioner ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
        {
            return BuildSkeletons( stringInputs.Select( stringInput => partitioner( stringInput ) ) ,
                selectioner , dimensions , dimensionsOfInterest );
        }

        internal static IEnumerable<Skeleton> CreateSkeletons<T>( IEnumerable<T> inputs ,
            Func<T , string , string> parser ,
            IEnumerable<Dimension> dimensions ,
            string[] dimensionsOfInterest = null )
            => CreateSkeletons( inputs , parser , _ => Unit.Default , dimensions , dimensionsOfInterest )
                .Select( s => s.Key );

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

            //var dimMap = HashMap.createRange<string , HashMap<string , Bone[]>>(
            //    seqDimensions.Select( d => (d.Name,
            //        HashMap.createRange<string , Bone[]>(
            //            d.Flatten().GroupBy( x => x.Label )
            //                .Select( h => (h.Key, h.ToArray()) ) )) ) );

            //static Either<string , Bone[]> FindBones( T input , Func<T , string , string> parser , string dimensionName , HashMap<string , HashMap<string , Bone[]>> map )
            //{
            //    var boneLabel = parser( input , dimensionName );
            //    var f = Prelude.Try( () => map[dimensionName][boneLabel] );
            //    var bones = f.Match( bones => bones , _ => Array.Empty<Bone>() );

            //    if ( bones.Length == 0 )
            //        return $"Couldn't find {boneLabel} in dimension {dimensionName}";
            //    return bones;
            //}

            return inputs.AsParallel().SelectMany( input =>
            {
                //var bones = dimensionsOfInterest.Select( d => FindBones( input , parser , d , dimMap ) )
                //    .ToArray();

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
