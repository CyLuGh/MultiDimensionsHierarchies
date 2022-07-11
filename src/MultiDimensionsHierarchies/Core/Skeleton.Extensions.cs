using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public static class SkeletonExtensions
    {
        public static Option<Skeleton<T>> Aggregate<T>( this IEnumerable<Skeleton<T>> skeletons , Func<IEnumerable<T> , T> aggregator )
        {
            var seq = skeletons.ToSeq();
            if ( seq.All( x => x.Key.Equals( seq.First().Key ) ) )
            {
                return seq.First().With( value: aggregator( seq.Select( o => o.Value ).Somes() ) );
            }

            return Option<Skeleton<T>>.None;
        }

        public static Skeleton<T> Aggregate<T>( this IEnumerable<Skeleton<T>> skeletons ,
            Skeleton key , Func<IEnumerable<T> , T> aggregator )
            => new( aggregator( skeletons.Select( s => s.Value ).Somes() ) , key );

        public static Skeleton<T> Aggregate<T>( this IEnumerable<Skeleton<T>> skeletons ,
            Skeleton key , Func<IEnumerable<T> , T> aggregator , Func<T , double , T> weightEffect )
        {
            return new( aggregator( skeletons.Select( o =>
            {
                var w = Skeleton.ComputeResultingWeight( o.Key , key );
                return o.Value.Some( v => Option<(T, double)>.Some( (v, w) ) ).None( () => Option<(T, double)>.None );
            } ).Somes()
            .Select( t => weightEffect( t.Item1 , t.Item2 ) ) ) , key );
        }

        public static Option<Skeleton<T>> Aggregate<T>( this IEnumerable<Skeleton<T>> skeletons , Func<T , T , T> aggregator )
        {
            var seq = skeletons.ToSeq();
            if ( seq.All( x => x.Key.Equals( seq.First().Key ) ) )
            {
                var total = seq.Select( o => o.Value ).Somes()
                    .Aggregate( default , aggregator );
                return seq.First().With( value: total is null ? Option<T>.None : total );
            }

            return Option<Skeleton<T>>.None;
        }

        public static Skeleton<T> Aggregate<T>( this IEnumerable<Skeleton<T>> skeletons , Skeleton key , Func<T , T , T> aggregator )
            => new( skeletons.Select( o => o.Value ).Somes().Aggregate( default , aggregator ) , key );

        public static Option<Skeleton> Find( this IEnumerable<Skeleton> skeletons , params string[] labels )
            => skeletons.Find( s => s.Bones.Length == labels.Length
                && Enumerable.Range( 0 , labels.Length ).All( i => s.Bones[i].Label.Equals( labels[i] ) ) );

        public static Option<Skeleton> Find( this IEnumerable<Skeleton> skeletons , params (string label, string dimensionName)[] bones )
            => skeletons.Find( s => bones.All( b => s.Bones.Find( x => x.DimensionName.Equals( b.dimensionName ) && x.Label.Equals( b.label ) ).IsSome ) );

        public static Option<Skeleton<T>> Find<T>( this IEnumerable<Skeleton<T>> skeletons , params string[] labels )
            => skeletons.Find( s => s.Bones.Length == labels.Length
                && Enumerable.Range( 0 , labels.Length ).All( i => s.Bones[i].Label.Equals( labels[i] ) ) );

        public static Option<Skeleton<T>> Find<T>( this IEnumerable<Skeleton<T>> skeletons , params (string label, string dimensionName)[] bones )
            => skeletons.Find( s => bones.All( b => s.Bones.Find( x => x.DimensionName.Equals( b.dimensionName ) && x.Label.Equals( b.label ) ).IsSome ) );

        public static Seq<Skeleton> FindAll( this IEnumerable<Skeleton> skeletons , params string[] labels )
            => skeletons.Where( s => s.Bones.Length == labels.Length
                 && Enumerable.Range( 0 , labels.Length ).All( i => s.Bones[i].Label.Equals( labels[i] ) ) )
            .ToSeq();

        public static Seq<Skeleton> FindAll( this IEnumerable<Skeleton> skeletons , params (string label, string dimensionName)[] bones )
            => skeletons.Where( s => bones.All( b => s.Bones.Find( x => x.DimensionName.Equals( b.dimensionName ) && x.Label.Equals( b.label ) ).IsSome ) )
            .ToSeq();

        public static Seq<Skeleton<T>> FindAll<T>( this IEnumerable<Skeleton<T>> skeletons , params string[] labels )
            => skeletons.Where( s => s.Bones.Length == labels.Length
                 && Enumerable.Range( 0 , labels.Length ).All( i => s.Bones[i].Label.Equals( labels[i] ) ) )
            .ToSeq();

        public static Seq<Skeleton<T>> FindAll<T>( this IEnumerable<Skeleton<T>> skeletons , params (string label, string dimensionName)[] bones )
            => skeletons.Where( s => bones.All( b => s.Bones.Find( x => x.DimensionName.Equals( b.dimensionName ) && x.Label.Equals( b.label ) ).IsSome ) )
            .ToSeq();

        public static Seq<Skeleton> FindAll( this IEnumerable<Skeleton> skeletons , params string[][] labels )
            => labels.SelectMany( ls => skeletons.ToSeq().FindAll( ls ) )
            .ToSeq();

        public static Seq<Skeleton> FindAll( this IEnumerable<Skeleton> skeletons , params (string label, string dimensionName)[][] bones )
            => bones.SelectMany( bs => skeletons.ToSeq().FindAll( bs ) )
            .ToSeq();

        public static Seq<Skeleton<T>> FindAll<T>( this IEnumerable<Skeleton<T>> skeletons , params string[][] labels )
            => labels.SelectMany( ls => skeletons.ToSeq().FindAll( ls ) )
            .ToSeq();

        public static Seq<Skeleton<T>> FindAll<T>( this IEnumerable<Skeleton<T>> skeletons , params (string label, string dimensionName)[][] bones )
            => bones.SelectMany( bs => skeletons.ToSeq().FindAll( bs ) )
            .ToSeq();

        public static long AncestorsCount( this IEnumerable<Skeleton> skeletons )
            => skeletons.AsParallel().Sum( s => s.Ancestors().LongCount() );

        public static long AncestorsCount<T>( this IEnumerable<Skeleton<T>> skeletons )
            => skeletons.AsParallel().Sum( s => s.Key.Ancestors().LongCount() );

        public static IEnumerable<Skeleton> GetAncestors( this IEnumerable<Skeleton> skeletons )
            => skeletons.AsParallel().SelectMany( s => s.Ancestors() ).Distinct();

        public static IEnumerable<Skeleton> GetAncestors<T>( this IEnumerable<Skeleton<T>> skeletons )
            => skeletons.AsParallel().SelectMany( s => s.Key.Ancestors() ).Distinct();
    }
}