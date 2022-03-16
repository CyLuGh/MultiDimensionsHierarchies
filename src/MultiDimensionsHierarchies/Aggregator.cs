using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionsHierarchies
{
    public enum Method { Heuristic, Targeted }

    public static class Aggregator
    {
        public static AggregationResult<T> Aggregate<T>( Method method , IEnumerable<Skeleton<T>> inputs ,
            Func<T , T , T> aggregator , Func<IEnumerable<T> , T> groupAggregator )
        {
            var seq = inputs.GroupBy( x => x.Key )
                .Select( s => s.Aggregate( groupAggregator ) )
                .Somes()
                .ToSeq();

            return new AggregationResult<T>( "No run" );
        }
    }
}
