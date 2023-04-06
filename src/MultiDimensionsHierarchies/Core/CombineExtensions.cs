using LanguageExt;
using MoreLinq;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class CombineExtensions
    {
        public static IEnumerable<Skeleton> Combine( this IEnumerable<Dimension> dimensions , bool onlyRoots = false )
            => onlyRoots ?
                dimensions.Select( d => d.Frame.Where( b => !b.HasParent() ).Strict() ).Combine()
              : dimensions.Select( d => d.Frame.Flatten().Strict() ).Combine();

        public static IEnumerable<Skeleton> Combine( this IEnumerable<Seq<Bone>> dimensions )
            => dimensions
                .Aggregate<Seq<Bone> , IEnumerable<Seq<Bone>>>( new[] { new Seq<Bone>() } ,
                    ( lists , bones ) => lists.Cartesian( bones , ( l , b ) => l.Add( b ) ) )
                .AsParallel()
                .Select( l => new Skeleton( l ) );

        public static Seq<Skeleton> CombineSeq( this Seq<Seq<Bone>> dimensions )
            => dimensions.Combine().ToSeq().Strict();

        public static IEnumerable<Skeleton> Extract( this IEnumerable<Skeleton> skeletons , params string[] concepts )
            => skeletons.Select( s => s.Extract( concepts ) ).Distinct();
    }
}