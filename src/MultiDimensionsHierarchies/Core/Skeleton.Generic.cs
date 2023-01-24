using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public class Skeleton<T> : IEquatable<Skeleton<T>>
    {
        public Skeleton Key { get; }
        public Option<T> Value { get; }
        public T ValueUnsafe => Value.MatchUnsafe( v => v , () => default );

        public Arr<Bone> Bones => Key.Bones;

        public Skeleton( Skeleton key )
        {
            Key = key;
            Value = Option<T>.None;
        }

        public Skeleton( T value , Skeleton key ) : this( Option<T>.Some( value ) , key )
        {
        }

        public Skeleton( Option<T> value , Skeleton key ) : this( key )
        {
            Value = value;
        }

        public Skeleton( IEnumerable<Bone> bones )
            : this( new Skeleton( bones ) ) { }

        public Skeleton( params Bone[] bones )
            : this( new Skeleton( bones ) ) { }

        public Skeleton( T value , IEnumerable<Bone> bones )
           : this( value , new Skeleton( bones ) ) { }

        public Skeleton( T value , params Bone[] bones )
            : this( value , new Skeleton( bones ) ) { }

        public Skeleton<T> Add( Bone addedBone )
            => Add( new[] { addedBone } );

        public Skeleton<T> Add( IEnumerable<Bone> addedBones )
            => new( Value , Key.Add( addedBones ) );

        public Skeleton<T> Concat( params Bone[] bones )
            => new( Value , Key.Concat( bones ) );

        public Skeleton<T> Extract( params string[] dimensions )
            => new( Value , Key.Extract( dimensions ) );

        public Skeleton<T> Except( params string[] dimensions )
            => new( Value , Key.Except( dimensions ) );

        public Skeleton<T> With( Option<T> value = default , Skeleton key = null )
            => new( value.Some( _ => value ).None( () => Value ) , key ?? Key );

        public bool IsLeaf()
            => Bones.All( b => b.IsLeaf() );

        public bool IsRoot()
            => Bones.All( b => !b.HasParent() );

        public override string ToString()
            => $"{Key} => {Value.Some( v => v.ToString() ).None( () => "/" )}";

        public Skeleton<T> Aggregate( Skeleton<T> other , Func<T , T , T> aggregator )
        {
            var t = from v in Value
                    from ov in other.Value
                    select aggregator( v , ov );

            return With( value: t );
        }

        public bool Equals( Skeleton<T> other )
        {
            if ( other is null ) return false;
            if ( ReferenceEquals( this , other ) ) return true;
            return Key.Equals( other.Key ) && Value.Equals( other.Value );
        }

        public override bool Equals( object obj )
        {
            if ( obj is null ) return false;
            if ( ReferenceEquals( this , obj ) ) return true;
            return obj.GetType() == this.GetType() && Equals( (Skeleton<T>) obj );
        }

        public override int GetHashCode() => Key.GetHashCode() ^ Value.GetHashCode();
    }
}