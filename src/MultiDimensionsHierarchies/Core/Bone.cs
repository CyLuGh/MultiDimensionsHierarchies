using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionsHierarchies.Core
{
    public class Bone : IEquatable<Bone>
    {
        public static Bone None { get; } = new Bone( "None" , "?" );

        public string DimensionName { get; }
        public string Label { get; }
        public Option<Bone> Parent { get; }
        public Seq<Bone> Children { get; }

        public List<Bone> Leaves
        {
            get
            {
                var leaves = FetchLeaves().Memo();
                return leaves();
            }
        }

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

        public Bone( string label , string dimensionName , params Bone[] children )
            : this( label , dimensionName )
        {
            Children = new Seq<Bone>( children.Select( c => c.With( parent: this ) ) );
        }

        public Bone With( string label = null , string dimensionName = null , Bone parent = null )
            => new Bone(
                    label ?? this.Label ,
                    dimensionName ?? this.DimensionName ,
                    parent ?? this.Parent
                );

        public bool IsLeaf() => Children.Count == 0;

        public bool HasChild() => Children.Count > 0;

        public bool HasParent() => Parent.IsSome;

        public Bone GetRoot()
            => Parent.Some( p => p )
                .None( () => this );

        private Func<List<Bone>> FetchLeaves()
            => () =>
            {
                if ( HasChild() )
                    return Children.SelectMany( child => child.Leaves ).ToList();

                return new List<Bone> { this };
            };

        public bool Equals( Bone other )
        {
            if ( ReferenceEquals( null , other ) ) return false;
            if ( ReferenceEquals( this , other ) ) return true;
            return string.Equals( Label , other.Label )
                && string.Equals( DimensionName , other.DimensionName )
                && Parent.Equals( other.Parent );
        }

        public override bool Equals( object obj )
        {
            if ( ReferenceEquals( null , obj ) ) return false;
            if ( ReferenceEquals( this , obj ) ) return true;
            if ( obj.GetType() != this.GetType() ) return false;
            return Equals( (Bone) obj );
        }
    }
}