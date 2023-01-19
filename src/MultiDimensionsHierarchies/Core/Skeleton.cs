using LanguageExt;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public sealed class Skeleton : IEquatable<Skeleton> , IComparable<Skeleton>
    {
        public Arr<Bone> Bones { get; }
        public Arr<string> Dimensions => Bones.Select( b => b.DimensionName );
        public int Depth => Bones.Max( b => b.Depth );

        public Skeleton( IEnumerable<Bone> bones ) : this( bones.ToArray() )
        {
        }

        public Skeleton( params Bone[] bones )
        {
            if ( bones.Any( x => string.IsNullOrWhiteSpace( x.DimensionName ) ) )
                throw new ArgumentException( "A bone should always define its dimension name!" );
            if ( bones.GroupBy( x => x.DimensionName ).Any( g => g.Count() > 1 ) )
                throw new ArgumentException( "A bone with the same dimension name has been defined more than once!" );
            Bones = Arr.create( bones.OrderBy( x => x.DimensionName ).ToArray() );
        }

        public Skeleton Add( Skeleton other ) => Add( other.Bones );

        public Skeleton Add( Bone addedBone ) => Add( new[] { addedBone } );

        public Skeleton Add( IEnumerable<Bone> addedBones ) => new(Bones.Concat( addedBones ));

        public Skeleton Concat( params Bone[] bones ) => new(Bones.Concat( bones ));

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

        public bool HasBone( Bone b ) => Bones.Contains( b );

        public bool HasAnyBone( Seq<Bone> bones ) => bones.Any( b => Bones.Contains( b ) );

        public bool CheckBones( Dictionary<string , LanguageExt.HashSet<Bone>> bonesPerDimension )
        {
            for ( int i = 0 ; i < Bones.Count ; i++ )
            {
                if ( !bonesPerDimension.TryGetValue( Bones[i].DimensionName , out var hash ) ) return false;
                if ( !hash.Contains( Bones[i] ) ) return false;
            }

            return true;
        }

        public bool HasUnknown() => Bones.Any( x => x.Equals( Bone.None ) );

        public bool IsLeaf() => Bones.All( b => b.IsLeaf() );

        public bool IsRoot() => Bones.All( b => !b.HasParent() );

        public Skeleton Root() => new(Bones.Select( b => b.Root() ));

        private Seq<Skeleton> _leaves = Seq.empty<Skeleton>();

        public Seq<Skeleton> Leaves()
        {
            if ( _leaves.IsEmpty )
                _leaves = Prelude.Atom( Bones.AsParallel().Select( b => b.Leaves().ToArray() ).Combine().ToSeq() );
            return _leaves;
        }

        private Seq<Skeleton> _ancestors = Seq.empty<Skeleton>();

        public Seq<Skeleton> Ancestors() => Ancestors( Option<AtomHashMap<string , Skeleton>>.None );

        public Seq<Skeleton> Ancestors( Option<AtomHashMap<string , Skeleton>> cache ) =>
            Ancestors( Seq<Bone>.Empty , cache );

        public Seq<Skeleton> Ancestors( Seq<Bone> filters ) =>
            Ancestors( filters , Option<AtomHashMap<string , Skeleton>>.None );

        public Seq<Skeleton> Ancestors( Seq<Bone> filters , Option<AtomHashMap<string , Skeleton>> cache )
        {
            if ( _ancestors.IsEmpty )
            {
                _ancestors = cache.Some( atomCache => Prelude.Atom( BuildAncestors( atomCache ).ToSeq().Strict() ) )
                    .None( () => Prelude.Atom( BuildAncestors().ToSeq().Strict() ) );
            }

            if ( filters.IsEmpty ) return _ancestors;

            // Apply filters
            var checks = filters.GroupBy( x => x.DimensionName )
                .ToDictionary( g => g.Key , g => g.Select( x => x.Label ).Distinct().ToArray() );
            return _ancestors.Where( s => s.HasDimensions( checks ) );
        }

        internal IEnumerable<Skeleton> BuildFilteredSkeletons( HashMap<string , LanguageExt.HashSet<Bone>> filters ) =>
            Bones.Select( x =>
                {
                    var ancestors = from set in filters.Find( x.DimensionName )
                        select x.Ancestors().Where( o => set.Contains( o ) );
                    return ancestors.Match( a => a , () => Seq<Bone>.Empty );
                } )
                .Aggregate<Seq<Bone> , IEnumerable<Seq<Bone>>>( new[] { new Seq<Bone>() } ,
                    ( lists , bones ) => lists.Cartesian( bones , ( l , b ) => l.Add( b ) ) )
                .Select( l => new Skeleton( l ) );

        private IEnumerable<Skeleton> BuildAncestors( AtomHashMap<string , Skeleton> cache ) =>
            Bones.Select( x => x.Ancestors().ToArray() )
                .Aggregate<IEnumerable<Bone> , IEnumerable<Seq<Bone>>>( new[] { new Seq<Bone>() } ,
                    ( lists , bones ) => lists.Cartesian( bones , ( l , b ) => l.Add( b ) ) )
                .Select( l =>
                {
                    var key = l.ToComposedString();
                    return cache.FindOrAdd( key , new Skeleton( l ) );
                } );

        private IEnumerable<Skeleton> BuildAncestors() =>
            Bones.Select( x => x.Ancestors().ToArray() )
                .Aggregate<IEnumerable<Bone> , IEnumerable<Seq<Bone>>>( new[] { new Seq<Bone>() } ,
                    ( lists , bones ) => lists.Cartesian( bones , ( l , b ) => l.Add( b ) ) )
                .Select( l => new Skeleton( l ) );

        private Seq<Skeleton> _descendants = Seq.empty<Skeleton>();

        private IEnumerable<Skeleton> BuildDescendants() =>
            Bones.Select( x => x.Descendants().ToArray() )
                .Aggregate<IEnumerable<Bone> , IEnumerable<Seq<Bone>>>( new[] { new Seq<Bone>() } ,
                    ( lists , bones ) => lists.Cartesian( bones , ( l , b ) => l.Add( b ) ) )
                .Select( l => new Skeleton( l ) );

        private IEnumerable<Skeleton> BuildDescendants( AtomHashMap<string , Skeleton> cache ) =>
            Bones.Select( x => x.Descendants().ToArray() )
                .Aggregate<IEnumerable<Bone> , IEnumerable<Seq<Bone>>>( new[] { new Seq<Bone>() } ,
                    ( lists , bones ) => lists.Cartesian( bones , ( l , b ) => l.Add( b ) ) )
                .Select( l =>
                {
                    var key = l.ToComposedString();
                    return cache.FindOrAdd( key , new Skeleton( l ) );
                } );

        public Seq<Skeleton> Descendants() => Descendants( Option<AtomHashMap<string , Skeleton>>.None );

        public Seq<Skeleton> Descendants( Option<AtomHashMap<string , Skeleton>> cache )
        {
            if ( _descendants.IsEmpty )
            {
                _descendants = cache.Some( atomCache => Prelude.Atom( BuildDescendants( atomCache ).ToSeq().Strict() ) )
                    .None( () => Prelude.Atom( BuildDescendants().ToSeq().Strict() ) );
            }

            return _descendants;
        }

        public long Complexity => Bones.Select( b => b.Complexity ).Aggregate( 1 , ( prv , nxt ) => prv * nxt );

        public bool HasDimension( string dimension , params string[] values ) =>
            Bones.Find( b => b.DimensionName.Equals( dimension ) )
                .Some( b => values.Contains( b.Label ) )
                .None( () => false );

        public bool HasDimensions( Dictionary<string , string[]> dimensions ) =>
            dimensions.All( kvp => HasDimension( kvp.Key , kvp.Value ) );

        public Skeleton Update( Dimension dim )
        {
            var skel = Bones.Find( b => b.DimensionName.Equals( dim.Name ) )
                .Some( old =>
                {
                    var replace = dim.Find( old.Label );
                    return replace.Some( r =>
                        {
                            return Option<Skeleton>.Some(
                                new Skeleton( Bones.Except( new[] { old } ).Concat( new[] { r } ) ) );
                        } )
                        .None( () => Option<Skeleton>.None );
                } )
                .None( () => Option<Skeleton>.None );
            return skel.Some( s => s ).None( () => this );
        }

        public Skeleton Update( params Dimension[] dimensions )
        {
            if ( dimensions.Length != Bones.Count ||
                 !dimensions.All( d => Bones.Any( b => b.DimensionName.Equals( d.Name ) ) ) )
            {
                throw new ArgumentException( "Dimensions count doesn't match bones!" );
            }

            var skel = this;
            foreach ( var dim in dimensions ) skel = skel.Update( dim );
            return skel;
        }

        public Skeleton Replace( Bone newBone )
        {
            var skel = Bones.Find( b => b.DimensionName.Equals( newBone.DimensionName ) )
                .Some( old =>
                    Option<Skeleton>.Some( new Skeleton( Bones.Except( new[] { old } ).Concat( new[] { newBone } ) ) ) )
                .None( () => Option<Skeleton>.None );
            return skel.Some( s => s ).None( () => this );
        }

        /// <summary>
        /// Create local dimensional hierarchy.
        /// </summary>
        /// <returns></returns>
        public Dimension[] DimensionsSubset() =>
            Bones.Select( b => new Dimension( b.DimensionName , new[] { b } ) ).ToArray();

        public Skeleton StripHierarchies() => new(Bones.Select( b => new Bone( b.Label , b.DimensionName ) ));

        public string ToCompleteString() => string.Join( ":" , Bones.Select( b => $"{b.DimensionName}|{b.Label}" ) );

        public string GenerateKey( IEnumerable<string> codes )
        {
            var bones = Bones.ToDictionary( x => x.DimensionName , x => x.Label );
            return string.Join( ":" , codes.Select( x => bones.TryGetValue( x , out var b ) ? b : "?" ) );
        }

        public override string ToString() => string.Join( ":" , Bones );

        private string _fullPath = string.Empty;

        public string FullPath
        {
            get
            {
                if ( string.IsNullOrEmpty( _fullPath ) )
                    _fullPath = string.Join( ":" , Bones.Select( b => b.FullPath ) );
                return _fullPath;
            }
        }

        public Seq<Skeleton> GetComposingSkeletons( IEnumerable<Skeleton> sourceSkeletons ) =>
            GetComposingSkeletons( sourceSkeletons.ToSeq() );

        public Seq<Skeleton> GetComposingSkeletons( Seq<Skeleton> sourceSkeletons )
        {
            var components = sourceSkeletons.Strict();

            for ( int i = 0 ; i < Bones.Count ; i++ )
            {
                var expectedBones = HashSet.createRange( Bones[i].Descendants() );
                components = components.AsParallel()
                    .Where( s => expectedBones.Contains( s.GetBone( i ) ) )
                    .ToSeq()
                    .Strict();
            }

            return components;
        }

        public Seq<Skeleton> GetComposingSkeletons( Dimension[] dimensions , IEnumerable<Skeleton> sourceSkeletons )
        {
            var targetSkeleton = Update( dimensions );
            return targetSkeleton.GetComposingSkeletons( sourceSkeletons );
        }

        public Seq<Skeleton> GetComposingSkeletons( Dimension[] dimensions , IEnumerable<string> completeKeys ) =>
            GetComposingSkeletons( dimensions , completeKeys.Distinct().Select( s => ParseCompleteString( s ) ) );

        public Seq<Skeleton<T>> GetComposingSkeletons<T>( IEnumerable<Skeleton<T>> skeletons )
            => GetComposingSkeletons( skeletons.ToSeq() );
        
        public Seq<Skeleton<T>> GetComposingSkeletons<T>( Seq<Skeleton<T>> skeletons )
        {
            var components = skeletons.Strict();

            for ( int i = 0 ; i < Bones.Count ; i++ )
            {
                var expectedBones = HashSet.createRange( Bones[i].Descendants() );
                components = components
                    .AsParallel()
                    .Where( s => expectedBones.Contains( s.Key.GetBone( i ) ) )
                    .ToSeq()
                    .Strict();
            }
            
            return components;
        }

        public Seq<T> GetComposingItems<T>( IEnumerable<T> mappedComponents ) where T : IMappedComponents
        {
            var components = mappedComponents.ToSeq();

            foreach ( var bone in Bones )
            {
                var expectedBones = HashSet.createRange( bone.Descendants().Select( b => b.Label ) );
                components = components.AsParallel()
                    .Where( mc =>
                        mc.Components.Find( bone.DimensionName )
                            .Some( label => expectedBones.Contains( label ) )
                            .None( () => false ) )
                    .ToSeq()
                    .Strict();
            }

            return components;
        }

        public IEnumerable<Skeleton<TO>> BuildComposingSkeletons<TI , TO>( IEnumerable<TI> inputs ,
            Func<TI , TO> evaluator , bool filterComponents = true ) where TI : IMappedComponents
        {
            var dimSeq = Bones.Select( bone => ( bone.DimensionName ,
                    Bones: bone.Descendants()
                        .GroupBy( b => b.Label )
                        .ToDictionary( g => g.Key , g => g.ToSeq().Strict() ) ) )
                .ToSeq()
                .Strict();

            static string Parse( TI input , string dimName ) =>
                input.Components.Find( dimName ).Match( s => s , () => string.Empty );

            return SkeletonFactory.FastParse( filterComponents ? GetComposingItems( inputs ) : inputs , Parse ,
                evaluator , dimSeq );
        }

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

        private int? _hashCode;

        public override int GetHashCode()
        {
            if ( _hashCode.HasValue ) return _hashCode.Value;
            unchecked
            {
                var hashCode = Bones.Count;
                foreach ( var bone in Bones ) hashCode += bone.GetHashCode();
                _hashCode = hashCode;
                return hashCode;
            }
        }

        public static Skeleton ParseCompleteString( string s ) =>
            new(s.Split( ':' )
                .Select( x =>
                {
                    var parts = x.Split( '|' );
                    return new Bone( parts[1] , parts[0] );
                } ));

        public static Skeleton ParseCompleteString( string input , Seq<Dimension> dimensions )
        {
            var skeleton = new Skeleton( input.Split( ':' )
                .Select( x =>
                {
                    var f = Prelude.Try( () =>
                    {
                        var parts = x.Split( '|' );
                        return Arr.create( parts[0] , parts[1] );
                    } );
                    return f.Match(
                        parts => dimensions.Find( o => o.Name.Equals( parts[0] ) )
                            .Some( dim => dim.Find( parts[1] ).Some( b => b ).None( () => Bone.None ) )
                            .None( () => Bone.None ) , _ => Bone.None );
                } ) );
            if ( skeleton.HasUnknown() )
                throw new KeyNotFoundException( "Parsed items don't match dimensions definitions." );
            return skeleton;
        }

        public static double ComputeResultingWeight( Skeleton current , Skeleton ancestor ,
            Func<Skeleton , Skeleton , double> f ) =>
            f( current , ancestor );

        public static double ComputeResultingWeight( Skeleton current , Skeleton ancestor ) =>
            DetermineWeight( current , ancestor );

        public int CompareTo( Skeleton other ) => string.CompareOrdinal( FullPath , other.FullPath );

        private static readonly Func<Skeleton , Skeleton , double> DetermineWeight = ( current , ancestor ) =>
        {
            if ( current.Equals( ancestor ) ) return 1d;
            if ( !current.Dimensions.SequenceEqual( ancestor.Dimensions ) ) return 0d;
            var weight = 1d;
            for ( int i = 0 ; i < current.Bones.Length ; i++ )
                weight *= Bone.ComputeResultingWeight( current.Bones[i] , ancestor.Bones[i] );
            return weight;
        };

        public Bone GetBone( int index ) => index < Bones.Length ? Bones[index] : Bone.None;

        public Bone GetBone( string dimensionName ) =>
            Bones.Find( b => b.DimensionName.Equals( dimensionName ) ).Match( b => b , () => Bone.None );
    }
}