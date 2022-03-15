﻿using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionsHierarchies.Core
{
    public static class CombineExtensions
    {
        public static IEnumerable<Skeleton> Combine( this IEnumerable<Dimension> dimensions , bool onlyRoots = false )
            => onlyRoots ?
                dimensions.Select( d => d.Frame.Where( b => !b.HasParent() ).ToList() ).Combine()
              : dimensions.Select( d => d.Frame.FlatList().ToArray() ).Combine();

        public static IEnumerable<Skeleton> Combine( this IEnumerable<IEnumerable<Bone>> dimensions )
            => dimensions.Cartesian( x => new Skeleton( x.ToArray() ) );

        public static IEnumerable<Skeleton> Extract( this IEnumerable<Skeleton> skeletons , params string[] concepts )
            => skeletons.Select( s => s.Extract( concepts ) ).Distinct();
    }
}