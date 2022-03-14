using LanguageExt;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDimensionsHierarchies.Core
{
    public class Skeleton : IEquatable<Skeleton>
    {
        public Arr<Bone> Bones { get; }

        public Skeleton( IEnumerable<Bone> bones )
            : this( bones.ToArray() )
        { }

        public Skeleton( params Bone[] bones )
        {
            if ( bones.Any( x => string.IsNullOrWhiteSpace( x.DimensionName ) ) )
                throw new ArgumentException( "A bone should always define its dimension name!" );

            if ( bones.GroupBy( x => x.DimensionName ).Any( g => g.Count() > 1 ) )
                throw new ArgumentException( "A bone with the same dimension name has been defined more than once!" );

            Bones = Arr.create( bones.OrderBy( x => x.DimensionName ).ToArray() );
        }

        public Skeleton Add( Bone addedBone )
            => Add( new[] { addedBone } );

        public Skeleton Add( IEnumerable<Bone> addedBones )
            => new( Bones.Concat( addedBones ) );

        public Skeleton Concat( params Bone[] bones )
            => new( Bones.Concat( bones ) );

        public Skeleton Extract( params string[] dimensions )
        {
            var searched = new System.Collections.Generic.HashSet<string>( dimensions );
            return new Skeleton( Bones.Where( x => searched.Contains( x.DimensionName ) ).ToArray() );
        }

        public Skeleton Except( params string[] dimensions )
        {
            var searched = new System.Collections.Generic.HashSet<string>( dimensions );
            return new Skeleton( Bones.Where( x => !searched.Contains( x.DimensionName ) ).ToArray() );
        }

        public bool IsLeaf()
            => Bones.All( b => b.IsLeaf() );

        public bool IsRoot()
            => Bones.All( b => !b.HasParent() );

        public Skeleton GetRoot()
            => new( Bones.Select( b => b.GetRoot() ) );

        public Seq<Skeleton> GetLeaves()
            => Bones.AsParallel().Select( b => b.GetLeaves().ToArray() ).Combine().ToSeq();

        public Seq<Skeleton> GetAncestors()
           => Bones.AsParallel().Select( x => x.GetAncestors().ToArray() )
                .Cartesian( x => new Skeleton( x.ToArray() ) )
                .ToSeq();

        public bool HasDimension( string dimension , params string[] values )
            => Bones.Find( b => b.DimensionName.Equals( dimension ) )
                .Some( b => values.Contains( b.Label ) )
                .None( () => false );

        public bool HasDimensions( Dictionary<string , string[]> dimensions )
            => dimensions.All( kvp => HasDimension( kvp.Key , kvp.Value ) );

        public double GetResultingWeight( Skeleton ancestor )
        {
            // TODO: add dimensions check
            var sBones = ancestor.Bones.ToDictionary( x => x.DimensionName );
            return Bones
                .Select( b => b.GetResultingWeight( sBones[b.DimensionName] ) )
                .Aggregate( 1d , ( s , w ) => s * w );
        }

        public Skeleton Update( Dimension dim )
        {
            var skel = Bones.Find( b => b.DimensionName.Equals( dim.Name ) )
                .Some( old =>
                {
                    var replace = dim.Find( old.Label );
                    return replace.Some( r =>
                    {
                        return Option<Skeleton>.Some( new Skeleton( Bones.Except( new[] { old } )
                            .Concat( new[] { r } ) ) );
                    } )
                    .None( () => Option<Skeleton>.None );
                } )
                .None( () => Option<Skeleton>.None );

            return skel.Some( s => s )
                .None( () => this );
        }

        public Skeleton Update( params Dimension[] dimensions )
        {
            if ( dimensions.Length != Bones.Count
                || !dimensions.All( d => Bones.Any( b => b.DimensionName.Equals( d.Name ) ) ) )
                throw new ArgumentException( "Dimensions count doesn't match bones!" );

            var skel = this;

            foreach ( var dim in dimensions )
                skel = skel.Update( dim );

            return skel;
        }

        public Skeleton Replace( Bone newBone )
        {
            var skel = Bones.Find( b => b.DimensionName.Equals( newBone.DimensionName ) )
                .Some( old => Option<Skeleton>.Some( new Skeleton( Bones.Except( new[] { old } )
                            .Concat( new[] { newBone } ) ) ) )
                .None( () => Option<Skeleton>.None );

            return skel.Some( s => s )
                .None( () => this );
        }

        /// <summary>
        /// Create local dimensional hierarchy.
        /// </summary>
        /// <returns></returns>
        public Dimension[] GetDimensionsSubset()
            => Bones.Select( b => new Dimension( b.DimensionName , new[] { b } ) ).ToArray();

        public Skeleton StripHierarchies()
            => new( Bones.Select( b => new Bone( b.Label , b.DimensionName ) ) );

        public string ToCompleteString()
            => string.Join( ":" , Bones.Select( b => $"{b.DimensionName}|{b.Label}" ) );

        public string GenerateKey( IEnumerable<string> codes )
        {
            var bones = Bones.ToDictionary( x => x.DimensionName , x => x.Label );
            return string.Join( ":" , codes.Select( x => bones.TryGetValue( x , out var b ) ? b : "?" ) );
        }

        public override string ToString() => string.Join( ":" , Bones );

        public string GetFullPath()
            => string.Join( ":" , Bones.Select( b => b.GetFullPath() ) );

        public Arr<Skeleton> GetComposingSkeletons( Dimension[] dimensions , IEnumerable<Skeleton> sourceSkeletons )
        {
            var targetSkeleton = Update( dimensions );

            var bones = targetSkeleton.Bones.ToDictionary( b => b.DimensionName ,
                b => new System.Collections.Generic.HashSet<string>( b.GetDescendants().Select( x => x.Label ) ) );

            var composingElements = new System.Collections.Generic.HashSet<Skeleton>( sourceSkeletons );

            foreach ( var expectedBones in bones )
            {
                var rejectedElements = new ConcurrentBag<Skeleton>();

                composingElements
                    .GroupBy( x => x.Bones.First( b => b.DimensionName.Equals( expectedBones.Key ) ) )
                    .AsParallel()
                    .ForEach( group =>
                    {
                        if ( !expectedBones.Value.Contains( group.Key.Label ) )
                            group.ForEach( s => rejectedElements.Add( s ) );
                    } );

                foreach ( var r in rejectedElements )
                    composingElements.Remove( r );
            }

            return composingElements.ToArr();
        }

        public Arr<Skeleton> GetComposingSkeletons( Dimension[] dimensions , IEnumerable<string> completeKeys )
            => GetComposingSkeletons( dimensions ,
                completeKeys.Distinct().Select( ParseCompleteString ) );

        public bool Equals( Skeleton other )
        {
            if ( other is null ) return false;
            if ( ReferenceEquals( this , other ) ) return true;
            return Bones.SequenceEqual( other.Bones );
        }

        public override bool Equals( object obj )
        {
            if ( obj is null ) return false;
            if ( ReferenceEquals( this , obj ) ) return true;
            return obj.GetType() == this.GetType() && Equals( (Skeleton) obj );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Bones.Count;
                foreach ( var bone in Bones )
                    hashCode += bone.GetHashCode();
                return hashCode;
            }
        }

        public static Skeleton ParseCompleteString( string s )
            => new( s.Split( ':' )
                .Select( x =>
                 {
                     var parts = x.Split( '|' );
                     return new Bone( parts[1] , parts[0] );
                 } ) );
    }
}