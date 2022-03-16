using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class SkeletonExtensions
    {
        public static Option<Skeleton<T>> Aggregate<T>( this IEnumerable<Skeleton<T>> skeletons , Func<IEnumerable<T> , T> aggregator )
        {
            var seq = skeletons.ToSeq();
            if ( seq.All( x => x.Key.Equals( seq.First().Key ) ) )
            {
                return seq.First().With( value: aggregator( seq.Select( o => o.Value ) ) );
            }

            return Option<Skeleton<T>>.None;
        }

        public static Skeleton<T> Aggregate<T>( this IEnumerable<Skeleton<T>> skeletons , Skeleton key , Func<IEnumerable<T> , T> aggregator )
            => new( aggregator( skeletons.Select( o => o.Value ) ) , key );
    }
}
