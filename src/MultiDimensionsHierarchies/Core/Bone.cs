using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public class Bone : IEquatable<Bone>, IComparable<Bone>
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

            bone.Children = bone.Children.Select( c => c.With( dimensionName: dimensionName , parent: bone ) )
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

        private int? _depth;

        public int Depth
        {
            get
            {
                if ( _depth.HasValue )
                    return _depth.Value;

                _depth = 1 + Parent.Some( p => p.Depth ).None( () => 0 );
                return _depth.Value;
            }
        }

        private int? _complexity;

        public int Complexity
        {
            get
            {
                if ( _complexity.HasValue )
                    return _complexity.Value;

                _complexity = 1 + Children.Sum( c => c.Complexity );
                return _complexity.Value;
            }
        }

        public Bone Root()
            => Parent.Some( p => p.Root() )
                .None( () => this );

        private Seq<Bone> _leaves = Seq.empty<Bone>();

        public Seq<Bone> Leaves()
        {
            if ( _leaves.IsEmpty )
                _leaves = Prelude.Atom( FetchLeaves().ToSeq().Strict() );
            return _leaves;
        }

        private Seq<Bone> _descendants = Seq.empty<Bone>();

        public Seq<Bone> Descendants()
        {
            if ( _descendants.IsEmpty )
                _descendants = Prelude.Atom( BuildDescendants().ToSeq().Strict() );
            return _descendants;
        }

        private Seq<Bone> _ancestors = Seq.empty<Bone>();

        public Seq<Bone> Ancestors()
        {
            if ( _ancestors.IsEmpty )
                _ancestors = Prelude.Atom( BuildHierarchy().ToSeq().Strict() );
            return _ancestors;
        }

        public Seq<Bone> Ancestors( Seq<Bone> filters )
            => filters.IsEmpty ? Ancestors() : Ancestors().Intersect( filters ).ToSeq();

        private IEnumerable<Bone> FetchLeaves()
        {
            //System.Diagnostics.Debug.WriteLine( "Fetch leaves for {0} in {1}" , Label , DimensionName );

            if ( HasChild() )
                return Children.SelectMany( child => child.Leaves() );

            return new[] { this };
        }

        private IEnumerable<Bone> BuildDescendants()
        {
            //System.Diagnostics.Debug.WriteLine( "Build descendants for {0} in {1}" , Label , DimensionName );

            yield return this;

            foreach ( var child in Children.SelectMany( c => c.Descendants() ) )
                yield return child;
        }

        private IEnumerable<Bone> BuildHierarchy()
        {
            //System.Diagnostics.Debug.WriteLine( "Build hierarchy for {0} in {1}" , Label , DimensionName );

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
                && double.Equals( Weight , other.Weight )
                && Parent.Equals( other.Parent );
        }

        public override bool Equals( object obj )
        {
            if ( obj is null ) return false;
            if ( ReferenceEquals( this , obj ) ) return true;
            if ( obj.GetType() != this.GetType() ) return false;
            return Equals( (Bone) obj );
        }

        private int? _hashCode;

        public override int GetHashCode()
        {
            if ( _hashCode.HasValue )
                return _hashCode.Value;

            unchecked
            {
                var hashCode = Parent.Match( p => p.GetHashCode() , () => 1 );
                hashCode += ( hashCode * 397 ) ^ Label.GetHashCode();
                hashCode += ( hashCode * 397 ) ^ DimensionName.GetHashCode();

                _hashCode = hashCode;
                return hashCode;
            }
        }

        public override string ToString()
            => $"{Label} in {DimensionName}";

        private string _fullPath = string.Empty;

        public string FullPath
        {
            get
            {
                if ( string.IsNullOrEmpty( _fullPath ) )
                {
                    _fullPath = Parent
                        .Some( parent => string.Join( ">" , parent.FullPath , Label ) )
                        .None( () => Label );
                }

                return _fullPath;
            }
        }

        public static double ComputeResultingWeight( Bone current , Bone ancestor , Func<Bone , Bone , double> f )
            => f( current , ancestor );

        public static double ComputeResultingWeight( Bone current , Bone ancestor )
            => DetermineWeight( current , ancestor );

        public int CompareTo( Bone other )
            => string.Compare( FullPath , other.FullPath )
                & Weight.CompareTo( other.Weight );

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