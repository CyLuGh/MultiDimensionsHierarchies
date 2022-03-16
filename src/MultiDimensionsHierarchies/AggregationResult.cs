using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System.Collections.Generic;

namespace MultiDimensionsHierarchies
{
    public class AggregationResult<T>
    {
        public Arr<Skeleton<T>> Results { get; }
        public string Information { get; }

        public AggregationResult( IEnumerable<Skeleton<T>> results , string information )
            : this( results )
        {
            Information = information;
        }

        public AggregationResult( string information )
        {
            Information = information;
            Results = Arr.empty<Skeleton<T>>();
        }

        public AggregationResult( IEnumerable<Skeleton<T>> results )
        {
            Results = results.ToArr();
        }
    }
}
