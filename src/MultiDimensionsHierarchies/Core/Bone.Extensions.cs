﻿using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class BoneExtensions
    {
        public static Seq<Bone> Flatten( this IEnumerable<Bone> bones )
            => bones.SelectMany( b => b.Descendants() ).ToSeq();

        public static Either<string , bool> Check( this IEnumerable<Bone> bones )
        {
            var checks = new Queue<string>();

            var flattened = bones.Flatten();
            if ( flattened.GroupBy( b => b.Label ).Any( g => g.Count() > 1 ) )
                checks.Enqueue( "Same label is defined several times in the dimension, this may lead to false results if hierarchy is parsed from string inputs." );

            if ( flattened.GroupBy( b => b.Label )
                .Any( g => g.Count() > 1 && g.Select( b => b.Ancestors() )
                        .Aggregate( ( prvSeq , nxtSeq ) => prvSeq.Intersect( nxtSeq ).ToSeq() )
                        .Length > 0 ) )
            {
                checks.Enqueue( "Hierarchies may include some diamond shapes!" );
            }

            if ( checks.Count > 0 )
                return string.Join( Environment.NewLine , checks );

            return true;
        }

        public static string ToComposedString( this Seq<Bone> bones )
            => string.Join( ":" , bones.OrderBy( b => b.DimensionName ).Select( b => b.FullPath ) );

        public static Seq<Bone> GetSiblings( this Bone bone , Dimension dimension )
            => bone.Parent.Match( p => p.Children , () => dimension.Frame );

        public static Seq<Bone> GetSiblings( this Bone bone , Option<Dimension> dimension )
            => dimension.Match( d => bone.GetSiblings( d ) , () => Seq<Bone>.Empty );
    }
}