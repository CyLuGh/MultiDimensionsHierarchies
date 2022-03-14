﻿using LanguageExt;
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

        public Bone GetRoot()
            => Parent.Some( p => p.GetRoot() )
                .None( () => this );

        public double GetResultingWeight( Bone source )
        {
            if ( source.Equals( this ) )
                return 1d;

            return Weight * Parent.Some( p => p.GetResultingWeight( source ) ).None( () => 1d );
        }

        public Seq<Bone> GetLeaves()
        {
            var leaves = BuildLeaves().Memo();
            return leaves();
        }

        public Seq<Bone> GetDescendants()
        {
            var elements = FetchDescendants().Memo();
            return elements();
        }

        public Seq<Bone> GetAncestors()
        {
            var elements = FetchHierarchy().Memo();
            return elements();
        }

        private Func<Seq<Bone>> BuildLeaves()
            => () => FetchLeaves().ToSeq();

        private IEnumerable<Bone> FetchLeaves()
        {
            if ( HasChild() )
                return Children.SelectMany( child => child.GetLeaves() );

            return new[] { this };
        }

        private Func<Seq<Bone>> FetchDescendants()
            => () => BuildDescendants().ToSeq();

        private IEnumerable<Bone> BuildDescendants()
        {
            yield return this;

            foreach ( var child in Children.SelectMany( c => c.GetDescendants() ) )
                yield return child;
        }

        private Func<Seq<Bone>> FetchHierarchy()
            => () => BuildHierarchy().ToSeq();

        private IEnumerable<Bone> BuildHierarchy()
        {
            yield return this;

            foreach ( var b in Parent.Some( p => p.GetAncestors() )
                .None( () => new Seq<Bone>() ) )
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

        public string GetFullPath()
            => Parent
                .Some( parent => string.Join( ">" , parent.GetFullPath() , Label ) )
                .None( () => Label );
    }

    public static class BoneExtensions
    {
        public static Seq<Bone> FlatList( this IEnumerable<Bone> bones )
            => bones.SelectMany( b => b.GetDescendants() ).ToSeq();
    }
}