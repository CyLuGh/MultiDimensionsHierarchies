using LanguageExt;
using System;
using System.Collections.Generic;

namespace MultiDimensionsHierarchies.Core
{
    public class SkeletonsAccumulator<T>
    {
        public Skeleton Key { get; private init; }
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

        public Arr<(double weight, Skeleton<T> data)> Components { get; private init; }
        public Func<IEnumerable<(T value, double weight)> , T> Aggregator { get; private init; }

        private SkeletonsAccumulator()
        { }

        public SkeletonsAccumulator( Skeleton key , IEnumerable<(double, Skeleton<T>)> data , Func<IEnumerable<(T value, double weight)> , T> aggregator )
        {
            Key = key;
            Components = Arr.createRange( data );
            Aggregator = aggregator;
        }

        public SkeletonsAccumulator<T> With( Skeleton key )
            => new() { Key = key , Components = this.Components , Aggregator = this.Aggregator };

        public int Count => Components.Count;
        public Arr<Bone> Bones => Key.Bones;

        public SkeletonsAccumulator<T> Add( Bone addedBone )
            => Add( new[] { addedBone } );

        public SkeletonsAccumulator<T> Add( IEnumerable<Bone> addedBones )
            => With( Key.Add( addedBones ) );

        public SkeletonsAccumulator<T> Concat( params Bone[] bones )
            => With( Key.Concat( bones ) );

        public SkeletonsAccumulator<T> Extract( params string[] dimensions )
            => With( Key.Extract( dimensions ) );

        public SkeletonsAccumulator<T> Except( params string[] dimensions )
            => With( Key.Except( dimensions ) );
    }
}