using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class BoneExtensions
    {
        public static Seq<Bone> Flatten( this IEnumerable<Bone> bones )
            => bones.SelectMany( b => b.GetDescendants() ).ToSeq();

        public static Either<string , bool> Check( this IEnumerable<Bone> bones )
        {
            var checks = new Queue<string>();

            var flattened = bones.Flatten();
            if ( flattened.GroupBy( b => b.Label ).Any( g => g.Count() > 1 ) )
                checks.Enqueue( "Same label is defined several times in the dimension, this may lead to false results if hierarchy is parsed from string inputs." );

            if ( flattened.GroupBy( b => b.Label )
                .Where( g => g.Count() > 1 )
                .Any( g =>
                    g.Select( b => b.GetAncestors() )
                        .Aggregate( ( prvSeq , nxtSeq ) => prvSeq.Intersect( nxtSeq ).ToSeq() )
                        .Any() ) )
            {
                checks.Enqueue( "Hierarchies may include some diamond shapes!" );
            }

            if ( checks.Count > 0 )
                return string.Join( Environment.NewLine , checks );

            return true;
        }
    }
}
