using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace MultiDimensionsHierarchies.Core
{
    public static class DimensionExtensions
    {
        public static long Complexity( this IEnumerable<Dimension> dimensions )
            => dimensions.Aggregate( 1L , ( prv , nxt ) => prv * nxt.Complexity );

        public static Seq<Option<Bone>> Find( this IEnumerable<Dimension> dimensions , params string[] labels )
        {
            var dimsSeq = dimensions.OrderBy( d => d.Name ).ToSeq();

            if ( labels.Length != dimsSeq.Length )
                return Seq<Option<Bone>>.Empty;

            return Enumerable.Range( 0 , labels.Length )
                .Select( i => dimsSeq[i].Find( labels[i] ) )
                .ToSeq();
        }

        public static Seq<Option<Bone>> Find( this IEnumerable<Dimension> dimensions , params (string dimName, string label)[] searchItems )
            => searchItems.Select( x =>
            {
                return from dimension in dimensions.Find( d => String.CompareOrdinal( d.Name , x.dimName ) == 0 )
                       from bone in dimension.Find( x.label )
                       select bone;
            }
            ).ToSeq();

        public static Skeleton BuildSkeleton( this IEnumerable<Dimension> dimensions , params string[] labels )
            => new( dimensions.Find( labels ).Somes() );

        public static Skeleton BuildSkeleton( this IEnumerable<Dimension> dimensions , params (string dimName, string label)[] searchItems )
            => new( dimensions.Find( searchItems ).Somes() );
    }
}