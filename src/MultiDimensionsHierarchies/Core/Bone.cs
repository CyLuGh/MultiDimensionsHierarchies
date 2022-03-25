using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public class Bone : IEquatable<Bone>
    {
        public static Bone None { get; } = new Bone( "None" , "?" );

        public string DimensionName { get; }
        public string Label { get; }
        public double Weight { get; } = 1d;
        public Option<Bone> Parent { get; }
        public Seq<Bone> Children { get; private set; }

        public Bone( string label , string dimensionName )
        {
            DimensionName = dimensionName;
            Label = label;
            Parent = Option<Bone>.None;
            Children = new Seq<Bone>();
        }

        public Bone( string label , string dimensionName , Option<Bone> parent )
            : this( label , dimensionName )
        {
            Parent = parent;
        }

        public Bone( string label , string dimensionName , Option<Bone> parent , Seq<Bone> children )
            : this( label , dimensionName , parent )
        {
            Children = children;
        }

        public Bone( string label , string dimensionName , Option<Bone> parent , Seq<Bone> children , double weight )
            : this( label , dimensionName , parent , children )
        {
            Weight = weight;
        }

        public Bone( string label , string dimensionName , params Bone[] children )
            : this( label , dimensionName , 1d , children )
        { }

        public Bone( string label , string dimensionName , double weight , params Bone[] children )
            : this( label , dimensionName , weight )
        {
            Children = new Seq<Bone>( children.Select( c => c.With( parent: this ) ) );
        }

        public Bone( string label , string dimensionName , double weight )
            : this( label , dimensionName )
        {
            Weight = weight;
        }

        public Bone With( string label = null , string dimensionName = null , Bone parent = null , Seq<Bone>? children = null , double weight = double.NaN )
        {
            var bone = new Bone(
                    label ?? Label ,
                    dimensionName ?? DimensionName ,
                    parent ?? Parent ,
                    children ?? Children ,
                    double.IsNaN( weight ) ? Weight : weight
                );

            bone.Children = bone.Children.Select( c => c.With( parent: bone ) )
                .GroupBy( x => x )
                .Select( g => g.Key.With( children: g.SelectMany( o => o.Children ).ToSeq() ) )
                .ToSeq();

            return bone;
        }

        public bool IsLeaf() => Children.Count == 0;

        public bool HasChild() => Children.Count > 0;

        public bool HasParent() => Parent.IsSome;

        public bool HasWeightElement()
            => Descendants().Any( b => b.Weight != 1d );

        public int Depth =>
            1 + Parent.Some( p => p.Depth ).None( () => 0 );

        public int Complexity =>
            1 + Children.Sum( c => c.Complexity );

        public Bone Root()
            => Parent.Some( p => p.Root() )
                .None( () => this );

        private Seq<Bone> _leaves = Seq.empty<Bone>();
        public Seq<Bone> Leaves()
        {
            if ( _leaves.IsEmpty )
                _leaves = Prelude.Atom( FetchLeaves().ToSeq() );
            return _leaves;
        }

        private Seq<Bone> _descendants = Seq.empty<Bone>();
        public Seq<Bone> Descendants()
        {
            if ( _descendants.IsEmpty )
                _descendants = Prelude.Atom( BuildDescendants().ToSeq() );
            return _descendants;
        }

        private Seq<Bone> _ancestors = Seq.empty<Bone>();
        public Seq<Bone> Ancestors()
        {
            if ( _ancestors.IsEmpty )
                _ancestors = Prelude.Atom( BuildHierarchy().ToSeq() );
            return _ancestors;
        }

        private IEnumerable<Bone> FetchLeaves()
        {
            if ( HasChild() )
                return Children.SelectMany( child => child.Leaves() );

            return new[] { this };
        }

        private IEnumerable<Bone> BuildDescendants()
        {
            yield return this;

            foreach ( var child in Children.SelectMany( c => c.Descendants() ) )
                yield return child;
        }

        private IEnumerable<Bone> BuildHierarchy()
        {
            yield return this;

            foreach ( var b in Parent.Some( p => p.Ancestors() ).None( () => Seq.empty<Bone>() ) )
                yield return b;
        }

        public bool Equals( Bone other )
        {
            if ( other is null ) return false;
            if ( ReferenceEquals( this , other ) ) return true;
            return string.Equals( Label , other.Label )
                && string.Equals( DimensionName , other.DimensionName )
                && Parent.Equals( other.Parent );
        }

        public override bool Equals( object obj )
        {
            if ( obj is null ) return false;
            if ( ReferenceEquals( this , obj ) ) return true;
            if ( obj.GetType() != this.GetType() ) return false;
            return Equals( (Bone) obj );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Parent.Match( p => p.GetHashCode() , () => 1 );
                hashCode += ( hashCode * 397 ) ^ Label.GetHashCode();
                hashCode += ( hashCode * 397 ) ^ DimensionName.GetHashCode();

                return hashCode;
            }
        }

        public override string ToString()
            => $"{Label} in {DimensionName}";

        public string FullPath()
            => Parent
                .Some( parent => string.Join( ">" , parent.FullPath() , Label ) )
                .None( () => Label );

        public static double ComputeResultingWeight( Bone current , Bone ancestor , Func<Bone , Bone , double> f )
            => f( current , ancestor );

        public static double ComputeResultingWeight( Bone current , Bone ancestor )
            => DetermineWeight( current , ancestor );

        internal static Func<Bone , Bone , double> DetermineWeight =
            ( currentBone , ancestorBone ) =>
            {
                if ( currentBone.Equals( ancestorBone ) )
                    return 1d; /* Always weight 1 when compared to itself */

                var ancestors = currentBone.Ancestors();
                if ( !ancestors.Contains( ancestorBone ) )
                    return 0d; /* Shouldn't have any weight to an unrelated element */

                var weight = 1d;
                var processedBone = currentBone;

                do
                {
                    weight *= processedBone.Weight;
                    processedBone = processedBone.Parent.Some( p => p ).None( () => ancestorBone );
                } while ( !processedBone.Equals( ancestorBone ) );

                return weight;
            };
    }
}