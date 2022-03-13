using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDimensionsHierarchies.Core
{
    public class DimensionFactory
    {
        public static Dimension BuildWithParentLink<TA, TB>(
            string dimensionName ,
            IEnumerable<TA> items ,
            Func<TA , TB> keySelector ,
            Func<TA , Option<TB>> parentKeySelector ,
            Func<TA , string> labeller ,
            Func<TA , double> weighter = null
            )
        {
            weighter ??= _ => 1d;
            var results = HashMap.create<TB , Bone>();

            /* Put all items in a map */
            var hashMap = HashMap.createRange( items.Select( i => (keySelector( i ), i) ) );

            var parentKeys = new LanguageExt.HashSet<Option<TB>>( hashMap.Values.Select( x => parentKeySelector( x ) )
                .Where( o => o.IsSome ) );

            if ( parentKeys.Length == 0 ) /* This is a flat list */
            {
                foreach ( var (Key, Value) in hashMap
                    .Select( x => (x.Key, Value: new Bone( labeller( x.Value ) , dimensionName )) ) )
                {
                    results = results.Add( Key , Value );
                }
            }
            else
            {
                /* For leaves */
                var elements = hashMap.Where( x => !parentKeys.Contains( x.Key ) )
                    .Select( x => x.Value )
                    .ToArray();

                var createdIds = new Seq<TB>();

                foreach ( var group in elements.GroupBy( x => parentKeySelector( x ) ) )
                {
                    var res = BuildBone( dimensionName , hashMap , group.ToSeq() , group.Key , labeller , weighter );
                    results = res
                        .Some( r => results.AddOrUpdate( r.Key , r.Value ) )
                        .None( () => results );

                    createdIds = res.Some( r => createdIds.Add( r.Key ) )
                        .None( () => createdIds );
                }

                while ( createdIds.Length > 0 )
                {
                    elements = hashMap.Where( x => createdIds.Contains( x.Key ) )
                        .Select( x => x.Value )
                        .ToArray();

                    createdIds = new Seq<TB>();

                    foreach ( var group in elements.GroupBy( x => parentKeySelector( x ) )
                        .Where( g => g.Key.IsSome ) )
                    {
                        var keys = group.Select( x => keySelector( x ) ).ToSeq();
                        var seq = results.Where( x => keys.Contains( x.Key ) )
                            .Select( x => x.Value ).ToSeq();
                        var res = BuildBone( dimensionName , hashMap , seq , group.Key , labeller , weighter );
                        results = res.Some( r => results
                                .AddOrUpdate( r.Key ,
                                    existing => existing.With( children: existing.Children.Concat( r.Value.Children ) ) ,
                                    r.Value )
                                .RemoveRange( keys ) )
                            .None( () => results );

                        createdIds = res.Some( r => createdIds.Add( r.Key ) )
                            .None( () => createdIds );
                    }
                }
            }

            return new Dimension( dimensionName , results.Values.Where( x => x.Parent.IsNone ) );
        }

        public static Dimension BuildWithChildLink<TA, TB>(
            string dimensionName ,
            IEnumerable<TA> items ,
            Func<TA , TB> keySelector ,
            Func<TA , Option<TB>> childKeySelector ,
            Func<TA , string> labeller )
        {
            var multiChildItems = items.GroupBy( item => (Key: keySelector( item ), Label: labeller( item )) )
                .Select( g => (
                    g.Key.Key,
                    g.Key.Label,
                    ChildrenIds: g.Select( i => childKeySelector( i )
                            .Some( o => o )
                            .None( () => default( TB ) ) )
                        .Where( o => !o.Equals( default( TB ) ) ).ToArray()) );

            return BuildWithMultipleChildrenLink(
                dimensionName ,
                multiChildItems ,
                o => o.Key ,
                o => o.ChildrenIds ,
                o => o.Label
           );
        }

        public static Dimension BuildWithMultipleChildrenLink<TA, TB>(
            string dimensionName ,
            IEnumerable<TA> items ,
            Func<TA , TB> keySelector ,
            Func<TA , IEnumerable<TB>> childrenKeysSelector ,
            Func<TA , string> labeller )
        {
            var seq = items.ToSeq();
            var parentLinks = seq.Select( o => (Key: keySelector( o ),
                Label: labeller( o ),
                ParentId: seq.Find( x => childrenKeysSelector( x ).Contains( keySelector( o ) ) )
                    .Some( s => Option<TB>.Some( keySelector( s ) ) )
                    .None( () => Option<TB>.None )
                ) );

            return BuildWithParentLink( dimensionName ,
                parentLinks ,
                o => o.Key ,
                o => o.ParentId ,
                o => o.Label ,
                o => 1d );
        }

        private static Option<(TB Key, Bone Value)> BuildBone<TA, TB>(
            string dimensionName ,
            HashMap<TB , TA> hashMap ,
            Seq<TA> items ,
            Option<TB> parentKey ,
            Func<TA , string> labeller ,
            Func<TA , double> weighter )
                => parentKey.Some( pk =>
                {
                    var childrenBones = items.Select( l => new Bone( labeller( l ) , dimensionName , weighter( l ) ) )
                        .ToArray();
                    var parent = hashMap[pk];
                    return Option<(TB, Bone)>.Some( (pk, new Bone( labeller( parent ) , dimensionName , weighter( parent ) , childrenBones )) );
                } ).None( () => Option<(TB, Bone)>.None );

        private static Option<(TB Key, Bone Value)> BuildBone<TA, TB>(
            string dimensionName ,
            HashMap<TB , TA> hashMap ,
            Seq<Bone> childrenBones ,
            Option<TB> parentKey ,
            Func<TA , string> labeller ,
            Func<TA , double> weighter )
                => parentKey.Some( pk =>
                {
                    var parent = hashMap[pk];
                    return Option<(TB, Bone)>.Some( (pk, new Bone( labeller( parent ) , dimensionName , weighter( parent ) , childrenBones.ToArray() )) );
                } ).None( () => Option<(TB, Bone)>.None );
    }
}