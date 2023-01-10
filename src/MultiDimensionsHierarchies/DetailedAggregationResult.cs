using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;

namespace MultiDimensionsHierarchies
{
    /// <summary>
    /// Result from the detailed aggregation process.
    /// </summary>
    public class DetailedAggregationResult<T>
    {
        public Arr<SkeletonsAccumulator<T>> Results { get; }
        public string Information { get; }
        public AggregationStatus Status { get; }
        public TimeSpan Duration { get; }

        public DetailedAggregationResult( AggregationStatus status , TimeSpan duration , IEnumerable<SkeletonsAccumulator<T>> results , string information )
            : this( status , duration , results )
        {
            Information = information;
        }

        public DetailedAggregationResult( AggregationStatus status , TimeSpan duration , string information )
        {
            Status = status;
            Information = information;
            Duration = duration;
            Results = Arr.empty<SkeletonsAccumulator<T>>();
        }

        public DetailedAggregationResult( AggregationStatus status , TimeSpan duration , IEnumerable<SkeletonsAccumulator<T>> results )
        {
            Status = status;
            Results = results.ToArr();
            Duration = duration;
        }
    }
}