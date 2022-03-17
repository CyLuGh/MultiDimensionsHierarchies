using LanguageExt;
using System;
using System.Collections.Generic;
using System.Text;

namespace MultiDimensionsHierarchies.Core
{
    public class Skeleton<T>
    {
        public Skeleton Key { get; }
        public Option<T> Value { get; set; }
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
            => $"{Key} [{Value}]";
    }
}