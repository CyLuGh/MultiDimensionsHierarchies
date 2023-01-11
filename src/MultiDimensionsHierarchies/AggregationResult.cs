using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;

namespace MultiDimensionsHierarchies
{
    public enum AggregationStatus
    {
        NO_RUN, OK, ERROR, WARN
    }

    public class AggregationResult<T>
    {
        public Arr<Skeleton<T>> Results { get; }
        public string Information { get; }
        public AggregationStatus Status { get; }
        public TimeSpan Duration { get; }

        public AggregationResult( AggregationStatus status , TimeSpan duration , IEnumerable<Skeleton<T>> results , string information )
            : this( status , duration , results )
        {
            Information = information;
        }

        public AggregationResult( AggregationStatus status , TimeSpan duration , string information )
        {
            Status = status;
            Information = information;
            Duration = duration;
            Results = Arr.empty<Skeleton<T>>();
        }

        public AggregationResult( AggregationStatus status , TimeSpan duration , IEnumerable<Skeleton<T>> results )
        {
            Status = status;
            Results = results.ToArr();
            Duration = duration;
        }
    }
}