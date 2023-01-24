using LanguageExt;
using LanguageExt.Pretty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace MultiDimensionsHierarchies.Core
{
    public class Dimension : IEquatable<Dimension>
    {
        public string Name { get; }
        public Seq<Bone> Frame { get; }

        internal Dimension( string name , IEnumerable<Bone> bones )
        {
            Name = name;
            Frame = new Seq<Bone>( bones );
        }

        public Dimension Rename( string name )
            => new( name , Frame.Select( b => b.With( dimensionName: name ) ) );

        public Seq<Bone> Flatten() => Frame.Flatten().ToSeq();

        public Seq<Bone> Leaves() => Frame.SelectMany( d => d.Leaves() ).ToSeq();

        public int Complexity => Frame.Sum( d => d.Complexity );

        public Either<string , bool> Check()
            => Frame.Check();

        public Option<Bone> Find( string label )
            => Frame.Flatten().Find( b => b.Label.Equals( label ) );

        public Seq<Bone> FindAll( string label )
            => Frame.Flatten().Where( b => b.Label.Equals( label ) );

        private int? _hashCode;

        public override int GetHashCode()
        {
            if ( _hashCode.HasValue )
                return _hashCode.Value;

            unchecked
            {
                var hashCode = Name.GetHashCode()
                               + Enumerable.Sum( Frame.Flatten() , bone => bone.GetHashCode() );

                _hashCode = hashCode;
                return hashCode;
            }
        }

        public override bool Equals( object obj )
        {
            if ( obj is null ) return false;
            if ( ReferenceEquals( this , obj ) ) return true;
            if ( obj.GetType() != this.GetType() ) return false;
            return Equals( (Dimension) obj );
        }

        public bool Equals( Dimension other )
        {
            if ( other is null ) return false;
            if ( ReferenceEquals( this , other ) ) return true;
            return string.Equals( Name , other.Name )
                && Frame.SequenceEqual( other.Frame );
        }
    }
}