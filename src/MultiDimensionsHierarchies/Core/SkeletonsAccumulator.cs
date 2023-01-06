using LanguageExt;
using System;
using System.Collections.Generic;

namespace MultiDimensionsHierarchies.Core
{
    public class SkeletonsAccumulator<T>
    {
        public Skeleton Key { get; }
        private Option<T> _value;

        public Option<T> Value
        {
            get
            {
                if ( _value.IsNone )
                {
                    _value = Aggregator( Components.Select( s => s.data.Value.Match( v => (v, s.weight) , () => Option<(T, double)>.None ) ).Somes() );
                }
                return _value;
            }
        }

        public Arr<(double weight, Skeleton<T> data)> Components { get; }
        public Func<IEnumerable<(T value, double weight)> , T> Aggregator { get; }

        public SkeletonsAccumulator( Skeleton key , IEnumerable<(double, Skeleton<T>)> data , Func<IEnumerable<(T value, double weight)> , T> aggregator )
        {
            Key = key;
            Components = Arr.createRange( data );
            Aggregator = aggregator;
        }

        public int Count => Components.Count;
        public Arr<Bone> Bones => Key.Bones;
    }
}