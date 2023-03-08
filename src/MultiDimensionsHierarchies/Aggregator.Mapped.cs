using LanguageExt;
using MultiDimensionsHierarchies.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies
{
    public static partial class Aggregator
    {
        /// <summary>
        /// Simplified aggregation method without constructing skeletons as base data. This does not handle weights!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseData"></param>
        /// <param name="targets"></param>
        /// <param name="aggregator"></param>
        /// <returns></returns>
        public static IEnumerable<Skeleton<T>> StreamAggregateResults<T>( Seq<IMappedComponents<T>> baseData ,
                                                                 LanguageExt.HashSet<Skeleton> targets ,
                                                                 Func<IEnumerable<T> , T> aggregator )
        {
            if ( baseData.Length == 0 || targets.Length == 0 )
                return Seq<Skeleton<T>>.Empty;

            var uniqueTargetBaseBones = targets
                    .SelectMany( t => t.Bones )
                    .GroupBy( b => b.DimensionName )
                    .Where( g => g.Distinct().Count() == 1 && !g.Any( b => b.HasWeightElement() ) )
                    .Select( g => g.First() )
                    .ToSeq();

            var (simplifiedData, simplifiedTargets) =
            uniqueTargetBaseBones.Any()
                ? SimplifyTargets( baseData , targets , uniqueTargetBaseBones , aggregator )
                : (baseData, targets);

            return simplifiedTargets
                    .AsParallel()
                    .Select( skeleton => new Skeleton<T>( aggregator( skeleton.GetComposingItems( simplifiedData ).Select( t => t.Value ).Somes() ) , skeleton ) )
                    .Select( s => s.Add( uniqueTargetBaseBones ) );
        }

        private static (Seq<IMappedComponents<T>>, LanguageExt.HashSet<Skeleton>) SimplifyTargets<T>(
            Seq<IMappedComponents<T>> baseData ,
            LanguageExt.HashSet<Skeleton> targets ,
            Seq<Bone> uniqueTargetBaseBones ,
            Func<IEnumerable<T> , T> groupAggregator )
        {
            var uniqueDimensions = uniqueTargetBaseBones.Select( u => u.DimensionName ).ToArray();
            var dataFilter = uniqueTargetBaseBones.Select( b => (b.DimensionName, Set: HashSet.createRange( b.Descendants().Select( x => x.Label ) )) );

            var simplifiedData = baseData
                .AsParallel()
                .Where( d => dataFilter.All( i => i.Set.Contains( d.Components.Find( i.DimensionName ).Match( s => s , () => string.Empty ) ) ) )
                .Select( d => new MappedComponentsItem<T>( d , uniqueDimensions ) )
                .GroupAggregate( groupAggregator )
                .ToSeq()
                .Cast<IMappedComponents<T>>()
                .Strict();

            var hash = HashSet.createRange( targets.Select( s => s.Except( uniqueDimensions ) ) );

            return (simplifiedData, hash);
        }
    }
}